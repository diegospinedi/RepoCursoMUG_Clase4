using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Catalogo;
using Optica.Api.Datos;

namespace Optica.Api.Presupuestos;

public sealed record PedidoCliente(string? Apellido, string? Nombre, string? Dni, string? Domicilio, string? Email, string? Telefono);

/// <summary>La pantalla envía solo artículo, cantidad y descuento (AGENTS.md): el precio sale del catálogo.</summary>
public sealed record PedidoLinea(int? Id, int ArticuloCodigo, decimal? Cantidad, decimal? Descuento);

public sealed record PedidoPresupuesto(EstadoPresupuesto? Estado, PedidoCliente? Cliente, List<PedidoLinea>? Lineas);

public abstract record ResultadoPresupuesto
{
    public sealed record Grabado(Presupuesto Presupuesto) : ResultadoPresupuesto;
    public sealed record Invalido(Errores Errores) : ResultadoPresupuesto;
    public sealed record Cerrado : ResultadoPresupuesto;
    public sealed record NoEncontrado : ResultadoPresupuesto;
}

public sealed partial class ServicioPresupuestos(OpticaDbContext db, Numerador numerador, TimeProvider reloj)
{
    public const decimal TotalMaximo = 999_999_999.99m;
    private const string MensajeCantidad = "La cantidad debe ser un número entero mayor a 0";

    /// <summary>Graba un presupuesto nuevo, siempre en Borrador (AC-04).</summary>
    public async Task<ResultadoPresupuesto> CrearAsync(PedidoPresupuesto pedido)
    {
        var errores = new Errores();
        var cliente = ValidarCliente(pedido.Cliente, errores);
        var lineas = await ArmarLineasAsync(pedido.Lineas, [], errores);
        if (errores.Hay) return new ResultadoPresupuesto.Invalido(errores);

        await using var tx = await db.Database.BeginTransactionAsync();
        var p = Presupuesto.Nuevo(await numerador.SiguienteAsync(), reloj.Hoy(),
            cliente.Apellido!, cliente.Nombre!, cliente.Dni!, cliente.Domicilio, cliente.Email, cliente.Telefono);
        p.Lineas = lineas;
        p.Total = Calculadora.Total(lineas.Select(l => l.PrecioFinal));
        db.Add(p);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new ResultadoPresupuesto.Grabado(p);
    }

    /// <summary>Modifica un Borrador y, si se pide, lo pasa a Final. Un Final no se toca (RF-08, RF-67).</summary>
    public async Task<ResultadoPresupuesto> ModificarAsync(int id, PedidoPresupuesto pedido)
    {
        var p = await db.Set<Presupuesto>().Include(x => x.Lineas).SingleOrDefaultAsync(x => x.Id == id);
        if (p is null) return new ResultadoPresupuesto.NoEncontrado();
        if (p.Estado == EstadoPresupuesto.Final) return new ResultadoPresupuesto.Cerrado();

        var errores = new Errores();
        var cliente = ValidarCliente(pedido.Cliente, errores);
        var lineas = await ArmarLineasAsync(pedido.Lineas, p.Lineas, errores);
        if (errores.Hay) return new ResultadoPresupuesto.Invalido(errores);

        await using var tx = await db.Database.BeginTransactionAsync();
        p.AsignarCliente(cliente.Apellido!, cliente.Nombre!, cliente.Dni!, cliente.Domicilio, cliente.Email, cliente.Telefono);
        foreach (var quitada in p.Lineas.Except(lineas).ToList()) db.Remove(quitada);
        p.Lineas = lineas;
        p.Total = Calculadora.Total(lineas.Select(l => l.PrecioFinal));
        await db.SaveChangesAsync();
        // El cierre va en un paso aparte: los triggers rechazan cambios en las líneas de un Final.
        if (pedido.Estado == EstadoPresupuesto.Final)
        {
            p.Cerrar();
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return new ResultadoPresupuesto.Grabado(p);
    }

    private static PedidoCliente ValidarCliente(PedidoCliente? c, Errores e)
    {
        var limpio = new PedidoCliente(
            c?.Apellido?.Trim(), c?.Nombre?.Trim(), c?.Dni?.Trim(),
            Vacio(c?.Domicilio), Vacio(c?.Email), Vacio(c?.Telefono));

        if (string.IsNullOrEmpty(limpio.Apellido)) e.Agregar("cliente.apellido", "Ingresá el apellido del cliente");
        else if (limpio.Apellido.Length > Presupuesto.LargoNombre) e.Agregar("cliente.apellido", "El apellido puede tener como máximo 100 caracteres");
        if (string.IsNullOrEmpty(limpio.Nombre)) e.Agregar("cliente.nombre", "Ingresá el nombre del cliente");
        else if (limpio.Nombre.Length > Presupuesto.LargoNombre) e.Agregar("cliente.nombre", "El nombre puede tener como máximo 100 caracteres");

        if (string.IsNullOrEmpty(limpio.Dni)) e.Agregar("cliente.dni", "Ingresá el DNI del cliente");
        else if (!DniValido().IsMatch(limpio.Dni)) e.Agregar("cliente.dni", "El DNI tiene que tener 7 u 8 dígitos; podés escribirlo con o sin puntos");

        if (limpio.Domicilio?.Length > Presupuesto.LargoDomicilio) e.Agregar("cliente.domicilio", "El domicilio puede tener como máximo 200 caracteres");
        if (limpio.Email is { } email && (email.Length > Presupuesto.LargoEmail || !EmailValido().IsMatch(email)))
            e.Agregar("cliente.email", "Revisá el email: tiene que tener la forma nombre@dominio.com");
        if (limpio.Telefono?.Length > Presupuesto.LargoTelefono) e.Agregar("cliente.telefono", "El teléfono puede tener como máximo 30 caracteres");
        return limpio;
    }

    private async Task<List<LineaPresupuesto>> ArmarLineasAsync(List<PedidoLinea>? pedidas, List<LineaPresupuesto> grabadas, Errores e)
    {
        var resultado = new List<LineaPresupuesto>();
        if (pedidas is not { Count: > 0 })
        {
            e.Agregar("lineas", "Agregá al menos un artículo al presupuesto");
            return resultado;
        }

        var codigos = pedidas.Where(l => l.Id is null).Select(l => l.ArticuloCodigo).Distinct().ToList();
        var articulos = await db.Set<Articulo>().Where(a => codigos.Contains(a.Codigo)).ToDictionaryAsync(a => a.Codigo);

        for (var i = 0; i < pedidas.Count; i++)
        {
            var pedida = pedidas[i];
            var clave = $"lineas[{i}]";
            var valida = true;

            if (pedida.Cantidad is not { } cantidad || cantidad != decimal.Truncate(cantidad) || cantidad < 1)
            {
                e.Agregar($"{clave}.cantidad", MensajeCantidad);
                valida = false;
            }
            else if (cantidad > LineaPresupuesto.CantidadMaxima)
            {
                e.Agregar($"{clave}.cantidad", $"{MensajeCantidad} y como máximo 9.999");
                valida = false;
            }

            var descuento = pedida.Descuento ?? 0m;
            if (!e.DosDecimales($"{clave}.descuento", descuento)) valida = false;
            else if (descuento < 0 || descuento > 100)
            {
                e.Agregar($"{clave}.descuento", "El descuento debe estar entre 0 y 100");
                valida = false;
            }

            LineaPresupuesto? linea;
            if (pedida.Id is { } id)
            {
                // Línea ya grabada: conserva descripción y precio aunque el catálogo haya cambiado (FR-024).
                linea = grabadas.SingleOrDefault(l => l.Id == id);
                if (linea is null)
                {
                    e.Agregar($"{clave}.id", "La línea no pertenece a este presupuesto; volvé a abrirlo");
                    continue;
                }
            }
            else if (articulos.TryGetValue(pedida.ArticuloCodigo, out var articulo))
            {
                if (articulo.PrecioVenta < 0)
                {
                    e.Agregar($"{clave}.precioUnitario", "El precio unitario no puede ser negativo");
                    continue;
                }
                linea = new LineaPresupuesto { ArticuloCodigo = articulo.Codigo, Descripcion = articulo.Descripcion, PrecioUnitario = articulo.PrecioVenta };
            }
            else
            {
                e.Agregar($"{clave}.articuloCodigo", "Elegí un artículo del catálogo");
                continue;
            }

            if (!valida) continue;
            linea.Orden = i + 1;
            linea.Cantidad = (int)pedida.Cantidad!.Value;
            linea.Descuento = descuento;
            (linea.PrecioConDescuento, linea.PrecioFinal) = Calculadora.Linea(linea.PrecioUnitario, descuento, linea.Cantidad);
            resultado.Add(linea);
        }

        if (!e.Hay && Calculadora.Total(resultado.Select(l => l.PrecioFinal)) > TotalMaximo)
            e.Agregar("total", "El total del presupuesto puede ser como máximo $ 999.999.999,99; dividilo en más de un presupuesto");
        return resultado;
    }

    private static string? Vacio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    [GeneratedRegex(@"^(\d{7,8}|\d{1,2}\.\d{3}\.\d{3})$")]
    private static partial Regex DniValido();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}

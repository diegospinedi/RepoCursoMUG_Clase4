import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ErrorApi, ErrorValidacion, pedir } from '../api/cliente'
import type { Articulo, FacturaEmitida, Presupuesto } from '../api/tipos'
import { calcularTotal, formatearImporte } from '../calculos/lineas'
import { CampoConError } from '../comunes/CampoConError'
import { EstadoEmision } from '../facturas/EstadoEmision'
import { formatearFecha } from '../comunes/fechas'
import { aCentavos, aTexto, numeroComprobante } from '../comunes/numeros'
import { BuscadorArticulos } from './BuscadorArticulos'
import { GrillaLineas } from './GrillaLineas'
import { importesLinea, type LineaEditable } from './lineasEditables'

type CamposCliente = 'apellido' | 'nombre' | 'dni' | 'domicilio' | 'email' | 'telefono'

const ETIQUETAS: Record<CamposCliente, string> = {
  apellido: 'Apellido',
  nombre: 'Nombre',
  dni: 'DNI',
  domicilio: 'Domicilio',
  email: 'Email',
  telefono: 'Teléfono',
}

const clienteVacio: Record<CamposCliente, string> = { apellido: '', nombre: '', dni: '', domicilio: '', email: '', telefono: '' }

let siguienteClave = 0

function desdeServidor(p: Presupuesto): LineaEditable[] {
  return p.lineas.map((l) => ({
    clave: `l${l.id}`,
    id: l.id,
    articuloCodigo: l.articuloCodigo,
    descripcion: l.descripcion,
    precioUnitario: aCentavos(l.precioUnitario),
    cantidad: String(l.cantidad),
    descuento: l.descuento ? aTexto(l.descuento) : '',
  }))
}

/** Número entero, decimal con coma o el texto tal cual para que el servidor lo rechace junto al campo. */
function aNumeroParaEnviar(texto: string, siVacio: number | null): number | string | null {
  const limpio = texto.trim().replace(',', '.')
  if (limpio === '') return siVacio
  return /^-?\d+(\.\d+)?$/.test(limpio) ? Number(limpio) : limpio
}

/**
 * Alta y modificación de un presupuesto (US4). Las líneas toman precio y descripción del catálogo en el
 * servidor; la pantalla solo envía artículo, cantidad y descuento. Un Final se muestra sin edición.
 */
export function EditorPresupuesto({ id }: { id?: number }) {
  const [presupuesto, setPresupuesto] = useState<Presupuesto>()
  const [cliente, setCliente] = useState(clienteVacio)
  const [lineas, setLineas] = useState<LineaEditable[]>([])
  const [pasarAFinal, setPasarAFinal] = useState(false)
  const [errores, setErrores] = useState<Record<string, string>>({})
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string }>()
  const [cargado, setCargado] = useState(id === undefined)
  const [facturando, setFacturando] = useState(false)

  function mostrar(p: Presupuesto) {
    setPresupuesto(p)
    setCliente({
      apellido: p.cliente.apellido,
      nombre: p.cliente.nombre,
      dni: p.cliente.dni,
      domicilio: p.cliente.domicilio ?? '',
      email: p.cliente.email ?? '',
      telefono: p.cliente.telefono ?? '',
    })
    setLineas(desdeServidor(p))
    setPasarAFinal(false)
  }

  useEffect(() => {
    if (id === undefined) return
    pedir<Presupuesto>(`/api/presupuestos/${id}`).then((p) => {
      mostrar(p)
      setCargado(true)
    })
  }, [id])

  const editable = presupuesto?.estado !== 'Final'
  const totalCentavos = calcularTotal(lineas.map((l) => importesLinea(l)?.precioFinal ?? 0))

  function agregar(a: Articulo) {
    siguienteClave += 1
    setLineas((ls) => [
      ...ls,
      { clave: `n${siguienteClave}`, id: null, articuloCodigo: a.codigo, descripcion: a.descripcion, precioUnitario: aCentavos(a.precioVenta), cantidad: '1', descuento: '' },
    ])
  }

  async function grabar(e: FormEvent) {
    e.preventDefault()
    setAviso(undefined)
    const cuerpo = {
      estado: pasarAFinal ? 'Final' : 'Borrador',
      cliente,
      lineas: lineas.map((l) => ({
        id: l.id,
        articuloCodigo: l.articuloCodigo,
        cantidad: aNumeroParaEnviar(l.cantidad, null),
        descuento: aNumeroParaEnviar(l.descuento, 0),
      })),
    }
    try {
      const grabado = await pedir<Presupuesto>(presupuesto ? `/api/presupuestos/${presupuesto.id}` : '/api/presupuestos', {
        method: presupuesto ? 'PUT' : 'POST',
        body: cuerpo,
      })
      setErrores({})
      mostrar(grabado)
      setAviso({ tipo: 'exito', texto: `Presupuesto ${grabado.numero} grabado en estado ${grabado.estado}.` })
    } catch (error) {
      if (error instanceof ErrorValidacion) {
        setErrores(error.errores)
        const generales = [error.errores.lineas, error.errores.total].filter(Boolean)
        if (generales.length) setAviso({ tipo: 'error', texto: generales.join(' ') })
      } else throw error
    }
  }

  async function facturar() {
    if (!presupuesto) return
    setAviso(undefined)
    setFacturando(true)
    try {
      const f = await pedir<FacturaEmitida>(`/api/presupuestos/${presupuesto.id}/factura`, { method: 'POST' })
      setPresupuesto({ ...presupuesto, emision: { facturaId: f.facturaId, estado: f.estado } })
      setAviso({ tipo: 'exito', texto: `Factura ${f.tipo} ${numeroComprobante(f.puntoVenta, f.numero)} autorizada. CAE ${f.cae}.` })
    } catch (error) {
      if (!(error instanceof ErrorApi)) throw error
      const facturaId = typeof error.datos.facturaId === 'number' ? error.datos.facturaId : undefined
      if (facturaId) setPresupuesto({ ...presupuesto, emision: { facturaId, estado: 'Pendiente' } })
      setAviso({ tipo: 'error', texto: error.titulo ?? 'No se pudo facturar. Probá de nuevo.' })
    } finally {
      setFacturando(false)
    }
  }

  if (!cargado) return null

  return (
    <form onSubmit={grabar}>
      <div className="tarjeta">
        <h1>
          {presupuesto ? `Presupuesto ${presupuesto.numero}` : 'Presupuesto nuevo'}{' '}
          {presupuesto && (
            <span className={`etiqueta ${presupuesto.estado === 'Final' ? 'etiqueta-final' : 'etiqueta-borrador'}`}>{presupuesto.estado}</span>
          )}
        </h1>
        {presupuesto && <p>Fecha: {formatearFecha(presupuesto.fecha)}</p>}
        <p className="fila">
          {presupuesto?.estado === 'Final' ? (
            <a className="boton" href={`/api/presupuestos/${presupuesto.id}/pdf`} download>
              Descargar PDF
            </a>
          ) : (
            <button className="boton" type="button" disabled title="El PDF se genera cuando el presupuesto está en estado Final.">
              Descargar PDF
            </button>
          )}
          {presupuesto?.emision?.estado === 'Autorizada' ? (
            <Link className="boton" to={`/facturas/${presupuesto.emision.facturaId}`}>
              Ver factura
            </Link>
          ) : presupuesto?.emision ? null : (
            presupuesto && (
              <button
                className="boton boton-primario"
                type="button"
                disabled={presupuesto.estado !== 'Final' || facturando}
                title={presupuesto.estado !== 'Final' ? 'Solo se factura un presupuesto en estado Final.' : undefined}
                onClick={() => void facturar()}
              >
                Facturar
              </button>
            )
          )}
        </p>
        {presupuesto?.emision && presupuesto.emision.estado !== 'Autorizada' && (
          <EstadoEmision
            emision={presupuesto.emision}
            alCambiar={(emision) => setPresupuesto((p) => (p ? { ...p, emision } : p))}
          />
        )}
        {aviso && (
          <p className={`aviso aviso-${aviso.tipo}`} role={aviso.tipo === 'error' ? 'alert' : 'status'}>
            {aviso.texto}
          </p>
        )}
      </div>

      <div className="tarjeta">
        <h2>Cliente</h2>
        {editable ? (
          <div className="fila">
            {(Object.keys(ETIQUETAS) as CamposCliente[]).map((campo) => (
              <CampoConError
                key={campo}
                etiqueta={ETIQUETAS[campo]}
                clave={`cliente.${campo}`}
                errores={errores}
                value={cliente[campo]}
                onChange={(e) => setCliente((c) => ({ ...c, [campo]: e.target.value }))}
              />
            ))}
          </div>
        ) : (
          <dl className="datos">
            {(Object.keys(ETIQUETAS) as CamposCliente[]).map((campo) => (
              <div key={campo}>
                <dt>{ETIQUETAS[campo]}</dt>
                <dd>{cliente[campo] || '—'}</dd>
              </div>
            ))}
          </dl>
        )}
      </div>

      <div className="tarjeta">
        <h2>Líneas</h2>
        {editable && <BuscadorArticulos alElegir={agregar} />}
        <GrillaLineas
          lineas={lineas}
          editable={editable}
          errores={errores}
          alCambiar={(i, campo, valor) => setLineas((ls) => ls.map((l, j) => (j === i ? { ...l, [campo]: valor } : l)))}
          alQuitar={(i) => setLineas((ls) => ls.filter((_, j) => j !== i))}
        />
        <p className="total">
          Total: <strong data-testid="total">{formatearImporte(totalCentavos)}</strong>
        </p>
        <p className="leyenda">Precios finales, IVA incluido</p>
      </div>

      {editable && (
        <div className="tarjeta fila">
          {presupuesto && (
            <label className="casilla">
              <input type="checkbox" checked={pasarAFinal} onChange={(e) => setPasarAFinal(e.target.checked)} /> Pasar a Final (después no se
              puede modificar)
            </label>
          )}
          <button className="boton boton-primario" type="submit">
            Grabar
          </button>
        </div>
      )}
    </form>
  )
}

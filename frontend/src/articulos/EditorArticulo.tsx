import { useState, type FormEvent } from 'react'
import { ErrorValidacion, pedir } from '../api/cliente'
import type { Articulo, Proveedor } from '../api/tipos'
import { CampoConError } from '../comunes/CampoConError'
import { aCentavos, aNumero, aTexto } from '../comunes/numeros'
import { formatearImporte } from '../calculos/lineas'

interface Props {
  articulo?: Articulo
  proveedores: Proveedor[]
  alGrabar: (articulo: Articulo) => void
}

/** Alta y modificación de un artículo. El precio de venta lo calcula el servidor: es de solo lectura (FR-014). */
export function EditorArticulo({ articulo, proveedores, alGrabar }: Props) {
  const [proveedorId, setProveedorId] = useState(String(articulo?.proveedorId ?? proveedores[0]?.id ?? ''))
  const [codigoProveedor, setCodigoProveedor] = useState(articulo?.codigoProveedor ?? '')
  const [descripcion, setDescripcion] = useState(articulo?.descripcion ?? '')
  const [costo, setCosto] = useState(aTexto(articulo?.precioCosto))
  const [margen, setMargen] = useState(aTexto(articulo?.margen))
  const [errores, setErrores] = useState<Record<string, string>>({})

  async function grabar(e: FormEvent) {
    e.preventDefault()
    const numeros = { precioCosto: aNumero(costo), margen: aNumero(margen) }
    const invalidos = Object.entries(numeros).filter(([, v]) => Number.isNaN(v))
    if (invalidos.length) {
      setErrores(Object.fromEntries(invalidos.map(([k]) => [k, 'Ingresá un número, con coma para los decimales.'])))
      return
    }
    try {
      const grabado = await pedir<Articulo>(articulo ? `/api/articulos/${articulo.codigo}` : '/api/articulos', {
        method: articulo ? 'PUT' : 'POST',
        body: { proveedorId: Number(proveedorId), codigoProveedor, descripcion, ...numeros },
      })
      setErrores({})
      alGrabar(grabado)
    } catch (error) {
      if (error instanceof ErrorValidacion) setErrores(error.errores)
      else throw error
    }
  }

  return (
    <form className="tarjeta" onSubmit={grabar}>
      <h2>{articulo ? `Artículo ${articulo.codigo}` : 'Artículo nuevo'}</h2>
      <div className="fila">
        <div className="campo">
          <label htmlFor="proveedor">Proveedor</label>
          <select id="proveedor" value={proveedorId} onChange={(e) => setProveedorId(e.target.value)} aria-invalid={errores.proveedorId ? 'true' : undefined}>
            {proveedores.map((p) => (
              <option key={p.id} value={String(p.id)}>
                {p.nombre}
              </option>
            ))}
          </select>
          {errores.proveedorId && <span className="campo-error">{errores.proveedorId}</span>}
        </div>
        <CampoConError etiqueta="Código en el proveedor" clave="codigoProveedor" errores={errores} value={codigoProveedor} onChange={(e) => setCodigoProveedor(e.target.value)} />
        <CampoConError etiqueta="Descripción" clave="descripcion" errores={errores} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
        <CampoConError etiqueta="Precio de costo" clave="precioCosto" errores={errores} inputMode="decimal" value={costo} onChange={(e) => setCosto(e.target.value)} />
        <CampoConError etiqueta="Margen de utilidad (%)" clave="margen" errores={errores} inputMode="decimal" value={margen} onChange={(e) => setMargen(e.target.value)} />
        <CampoConError
          etiqueta="Precio de venta"
          clave="precioVenta"
          readOnly
          value={articulo ? formatearImporte(aCentavos(articulo.precioVenta)) : 'Se calcula al grabar'}
        />
      </div>
      <p>
        <button className="boton boton-primario" type="submit">
          Grabar artículo
        </button>
      </p>
    </form>
  )
}

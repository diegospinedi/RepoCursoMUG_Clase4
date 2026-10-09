import { formatearImporte } from '../calculos/lineas'
import { importesLinea, type LineaEditable } from './lineasEditables'

export type { LineaEditable } from './lineasEditables'

interface Props {
  lineas: LineaEditable[]
  editable: boolean
  errores: Record<string, string>
  alCambiar: (indice: number, campo: 'cantidad' | 'descuento', valor: string) => void
  alQuitar: (indice: number) => void
}

/** Grilla con los siete campos de la línea (RF-12, AC-25). Precios finales con IVA incluido. */
export function GrillaLineas({ lineas, editable, errores, alCambiar, alQuitar }: Props) {
  return (
    <table className="grilla">
      <thead>
        <tr>
          <th className="numero">Código de artículo</th>
          <th>Descripción</th>
          <th className="numero">Precio unitario</th>
          <th className="numero">Cantidad</th>
          <th className="numero">% Descuento</th>
          <th className="numero">Precio con descuento</th>
          <th className="numero">Precio final</th>
          {editable && <th aria-label="Acciones" />}
        </tr>
      </thead>
      <tbody>
        {lineas.map((l, i) => {
          const importes = importesLinea(l)
          const errorCantidad = errores[`lineas[${i}].cantidad`]
          const errorDescuento = errores[`lineas[${i}].descuento`]
          const errorLinea = errores[`lineas[${i}].precioUnitario`] ?? errores[`lineas[${i}].articuloCodigo`] ?? errores[`lineas[${i}].id`]
          return (
            <tr key={l.clave}>
              <td className="numero">{l.articuloCodigo}</td>
              <td>
                {l.descripcion}
                {errorLinea && <span className="campo-error"> {errorLinea}</span>}
              </td>
              <td className="numero">{formatearImporte(l.precioUnitario)}</td>
              <td className="numero">
                {editable ? (
                  <span className="campo">
                    <input
                      aria-label={`Cantidad de la línea ${i + 1}`}
                      inputMode="numeric"
                      size={5}
                      value={l.cantidad}
                      aria-invalid={errorCantidad ? 'true' : undefined}
                      onChange={(e) => alCambiar(i, 'cantidad', e.target.value)}
                    />
                    {errorCantidad && <span className="campo-error">{errorCantidad}</span>}
                  </span>
                ) : (
                  l.cantidad
                )}
              </td>
              <td className="numero">
                {editable ? (
                  <span className="campo">
                    <input
                      aria-label={`Descuento (%) de la línea ${i + 1}`}
                      inputMode="decimal"
                      size={5}
                      value={l.descuento}
                      aria-invalid={errorDescuento ? 'true' : undefined}
                      onChange={(e) => alCambiar(i, 'descuento', e.target.value)}
                    />
                    {errorDescuento && <span className="campo-error">{errorDescuento}</span>}
                  </span>
                ) : (
                  `${l.descuento || '0'} %`
                )}
              </td>
              <td className="numero">{importes ? formatearImporte(importes.precioConDescuento) : '—'}</td>
              <td className="numero">{importes ? formatearImporte(importes.precioFinal) : '—'}</td>
              {editable && (
                <td>
                  <button className="boton" type="button" aria-label={`Quitar ${l.descripcion}`} onClick={() => alQuitar(i)}>
                    Quitar
                  </button>
                </td>
              )}
            </tr>
          )
        })}
      </tbody>
    </table>
  )
}

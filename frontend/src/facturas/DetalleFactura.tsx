import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { pedir } from '../api/cliente'
import type { FacturaDetalle } from '../api/tipos'
import { formatearImporte } from '../calculos/lineas'
import { formatearFecha } from '../comunes/fechas'
import { aCentavos, formatearDni, numeroComprobante } from '../comunes/numeros'

const CLASE_ESTADO: Record<string, string> = {
  Autorizada: 'etiqueta-final',
  Pendiente: 'etiqueta-borrador',
  Bloqueada: 'etiqueta-error',
  Descartada: 'etiqueta-error',
}

/** Detalle de la factura con el presupuesto de origen (AC-18) y el PDF si tiene CAE (RF-50). */
export function DetalleFactura({ id }: { id: number }) {
  const [f, setF] = useState<FacturaDetalle>()

  useEffect(() => {
    pedir<FacturaDetalle>(`/api/facturas/${id}`).then(setF)
  }, [id])

  if (!f) return null
  return (
    <>
      <div className="tarjeta">
        <h1>
          Factura {f.tipo} {numeroComprobante(f.puntoVenta, f.numero)} <span className={`etiqueta ${CLASE_ESTADO[f.estado]}`}>{f.estado}</span>
        </h1>
        <dl className="datos">
          <div>
            <dt>Fecha</dt>
            <dd>{formatearFecha(f.fecha)}</dd>
          </div>
          <div>
            <dt>Receptor</dt>
            <dd>{f.receptorDocTipo === 96 ? `DNI ${formatearDni(f.receptorDocNro)}` : 'Consumidor Final'}</dd>
          </div>
          <div>
            <dt>Cliente</dt>
            <dd>{`${f.apellido}, ${f.nombre}`}</dd>
          </div>
          <div>
            <dt>Presupuesto de origen</dt>
            <dd>
              <Link to={`/presupuestos/${f.presupuesto.id}`}>Presupuesto {f.presupuesto.numero}</Link>
            </dd>
          </div>
          <div>
            <dt>CAE</dt>
            <dd>{f.cae ?? '—'}</dd>
          </div>
          <div>
            <dt>Vencimiento del CAE</dt>
            <dd>{f.vencimientoCae ? formatearFecha(f.vencimientoCae) : '—'}</dd>
          </div>
        </dl>
        {f.estado === 'Autorizada' && (
          <p>
            <a className="boton" href={`/api/facturas/${f.id}/pdf`} download>
              Descargar PDF
            </a>
          </p>
        )}
      </div>
      <div className="tarjeta">
        <table className="grilla">
          <thead>
            <tr>
              <th>Descripción</th>
              <th className="numero">Cantidad</th>
              <th className="numero">Importe</th>
            </tr>
          </thead>
          <tbody>
            {f.lineas.map((l, i) => (
              <tr key={i}>
                <td>{l.descripcion}</td>
                <td className="numero">{l.cantidad}</td>
                <td className="numero">{formatearImporte(aCentavos(l.precioFinal))}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <p className="total">
          Importe total: <strong>{formatearImporte(aCentavos(f.total))}</strong>
        </p>
      </div>
    </>
  )
}

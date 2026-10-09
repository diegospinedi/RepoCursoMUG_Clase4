import { useState } from 'react'
import { ErrorApi, pedir } from '../api/cliente'
import type { EstadoEmision as Estado, FacturaEmitida } from '../api/tipos'
import { numeroComprobante } from '../comunes/numeros'

export interface Emision {
  facturaId: number
  estado: Estado
}

interface Props {
  emision: Emision
  /** Nuevo estado de la emisión; null si dejó de existir (rechazada o descartada) y se puede volver a facturar. */
  alCambiar: (emision: Emision | null) => void
}

/**
 * Emisión Pendiente con Reintentar, o Bloqueada con el aviso de revisar el punto de venta en ARCA y la
 * confirmación de la revisión (FR-038a, FR-038b). Nunca se reintenta sola.
 */
export function EstadoEmision({ emision, alCambiar }: Props) {
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error' | 'info'; texto: string }>()
  const [ocupado, setOcupado] = useState(false)

  async function reintentar() {
    setOcupado(true)
    setAviso(undefined)
    try {
      const f = await pedir<FacturaEmitida>(`/api/facturas/${emision.facturaId}/reintentar`, { method: 'POST' })
      setAviso({ tipo: 'exito', texto: `Factura ${f.tipo} ${numeroComprobante(f.puntoVenta, f.numero)} autorizada. CAE ${f.cae}.` })
      alCambiar({ facturaId: f.facturaId, estado: 'Autorizada' })
    } catch (error) {
      if (!(error instanceof ErrorApi)) throw error
      setAviso({ tipo: 'error', texto: error.titulo ?? 'No se pudo reintentar. Probá de nuevo más tarde.' })
      if (error.tipo === 'emision-bloqueada') alCambiar({ ...emision, estado: 'Bloqueada' })
      else if (error.tipo === 'arca-rechazo') alCambiar(null)
    } finally {
      setOcupado(false)
    }
  }

  async function confirmarRevision() {
    setOcupado(true)
    try {
      await pedir(`/api/facturas/${emision.facturaId}/confirmar-revision`, { method: 'POST' })
      setAviso({ tipo: 'info', texto: 'Emisión descartada. Ya podés volver a facturar el presupuesto.' })
      alCambiar(null)
    } finally {
      setOcupado(false)
    }
  }

  return (
    <div className="estado-emision">
      {emision.estado === 'Pendiente' && (
        <>
          <p className="aviso aviso-info">
            Emisión pendiente: ARCA no respondió y no se sabe si autorizó el comprobante. Al reintentar, el sistema primero lo
            consulta en ARCA para no duplicarlo.
          </p>
          <button className="boton boton-primario" type="button" disabled={ocupado} onClick={() => void reintentar()}>
            Reintentar
          </button>
        </>
      )}
      {emision.estado === 'Bloqueada' && (
        <>
          <p className="aviso aviso-error">
            El número de comprobante figura autorizado en ARCA con otro importe. Revisá el punto de venta en ARCA antes de
            seguir: no se emitió ni se recuperó ningún comprobante.
          </p>
          <button className="boton" type="button" disabled={ocupado} onClick={() => void confirmarRevision()}>
            Confirmo que revisé el punto de venta
          </button>
        </>
      )}
      {aviso && (
        <p className={`aviso aviso-${aviso.tipo}`} role={aviso.tipo === 'error' ? 'alert' : 'status'}>
          {aviso.texto}
        </p>
      )}
    </div>
  )
}

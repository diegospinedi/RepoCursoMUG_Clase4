import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { EstadoEmision } from './EstadoEmision'

describe('EstadoEmision', () => {
  it('una Pendiente se puede reintentar y al autorizarse lo informa', async () => {
    fetchFalso({
      'POST /api/facturas/5/reintentar': {
        estado: 200,
        cuerpo: { facturaId: 5, estado: 'Autorizada', tipo: 'B', puntoVenta: 3, numero: 1, cae: '76000000000001', vencimientoCae: '2026-10-19' },
      },
    })
    const alCambiar = vi.fn()
    render(<EstadoEmision emision={{ facturaId: 5, estado: 'Pendiente' }} alCambiar={alCambiar} />)
    expect(screen.getByText(/Emisión pendiente/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    expect(await screen.findByText('Factura B 0003-00000001 autorizada. CAE 76000000000001.')).toBeInTheDocument()
    expect(alCambiar).toHaveBeenCalledWith({ facturaId: 5, estado: 'Autorizada' })
  })

  it('si el número figura con otro importe pasa a Bloqueada', async () => {
    fetchFalso({
      'POST /api/facturas/5/reintentar': {
        estado: 409,
        cuerpo: { type: 'emision-bloqueada', title: 'El número 0003-00000001 figura autorizado en ARCA con otro importe. Revisá el punto de venta en ARCA.' },
      },
    })
    const alCambiar = vi.fn()
    render(<EstadoEmision emision={{ facturaId: 5, estado: 'Pendiente' }} alCambiar={alCambiar} />)
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    expect(alCambiar).toHaveBeenCalledWith({ facturaId: 5, estado: 'Bloqueada' })
  })

  it('muestra código y descripción si ARCA rechaza el reintento', async () => {
    fetchFalso({
      'POST /api/facturas/5/reintentar': {
        estado: 502,
        cuerpo: { type: 'arca-rechazo', title: 'ARCA rechazó el comprobante: 10016 — Número inválido.', codigo: '10016', descripcion: 'Número inválido.' },
      },
    })
    const alCambiar = vi.fn()
    render(<EstadoEmision emision={{ facturaId: 5, estado: 'Pendiente' }} alCambiar={alCambiar} />)
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('10016 — Número inválido.')
    expect(alCambiar).toHaveBeenCalledWith(null)
  })

  it('una Bloqueada pide revisar el punto de venta y confirmar la revisión', async () => {
    const pedidos = fetchFalso({ 'POST /api/facturas/5/confirmar-revision': { estado: 200, cuerpo: { estado: 'Descartada' } } })
    const alCambiar = vi.fn()
    render(<EstadoEmision emision={{ facturaId: 5, estado: 'Bloqueada' }} alCambiar={alCambiar} />)
    expect(screen.getByText(/Revisá el punto de venta en ARCA/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reintentar' })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Confirmo que revisé el punto de venta' }))
    expect(pedidos).toHaveLength(1)
    expect(alCambiar).toHaveBeenCalledWith(null)
  })
})

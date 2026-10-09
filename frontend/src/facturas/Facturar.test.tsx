import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { EditorPresupuesto } from '../presupuestos/EditorPresupuesto'
import { fetchFalso } from '../test/fetchFalso'

const presupuesto = (estado: 'Borrador' | 'Final', emision: unknown = null) => ({
  id: 3, numero: 12, fecha: '2026-10-09', estado,
  cliente: { apellido: 'González', nombre: 'Ana', dni: '23456789', domicilio: null, email: null, telefono: null },
  lineas: [{ id: 1, orden: 1, articuloCodigo: 7, descripcion: 'Armazón metal', precioUnitario: 1815, cantidad: 1, descuento: 0, precioConDescuento: 1815, precioFinal: 1815 }],
  total: 1815,
  emision,
})

const editor = () => render(<MemoryRouter><EditorPresupuesto id={3} /></MemoryRouter>)

describe('Facturar un presupuesto', () => {
  it('está deshabilitado en Borrador (AC-17)', async () => {
    fetchFalso({ 'GET /api/presupuestos/3': { estado: 200, cuerpo: presupuesto('Borrador') } })
    editor()
    expect(await screen.findByRole('button', { name: 'Facturar' })).toBeDisabled()
  })

  it('en Final factura y confirma con el número de comprobante (AC-18)', async () => {
    const pedidos = fetchFalso({
      'GET /api/presupuestos/3': { estado: 200, cuerpo: presupuesto('Final') },
      'POST /api/presupuestos/3/factura': {
        estado: 201,
        cuerpo: { facturaId: 5, estado: 'Autorizada', tipo: 'B', puntoVenta: 3, numero: 1, cae: '76000000000001', vencimientoCae: '2026-10-19' },
      },
    })
    editor()
    await userEvent.click(await screen.findByRole('button', { name: 'Facturar' }))
    expect(await screen.findByText('Factura B 0003-00000001 autorizada. CAE 76000000000001.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Ver factura' })).toHaveAttribute('href', '/facturas/5')
    expect(screen.queryByRole('button', { name: 'Facturar' })).not.toBeInTheDocument()
    expect(pedidos.filter((p) => p.metodo === 'POST')).toHaveLength(1)
  })

  it('si ya está facturado muestra el acceso a la factura en lugar de Facturar', async () => {
    fetchFalso({ 'GET /api/presupuestos/3': { estado: 200, cuerpo: presupuesto('Final', { facturaId: 5, estado: 'Autorizada' }) } })
    editor()
    expect(await screen.findByRole('link', { name: 'Ver factura' })).toHaveAttribute('href', '/facturas/5')
    expect(screen.queryByRole('button', { name: 'Facturar' })).not.toBeInTheDocument()
  })
})

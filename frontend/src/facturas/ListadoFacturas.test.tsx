import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { ListadoFacturas } from './ListadoFacturas'

const fila = { id: 5, estado: 'Pendiente', tipo: 'B', puntoVenta: 3, numero: 34561, fecha: '2026-03-20', apellido: 'González', nombre: 'Ana', total: 1815, presupuestoNumero: 12 }

describe('Listado de facturas', () => {
  it('filtra y muestra estado, comprobante, cliente y presupuesto de origen', async () => {
    const pedidos = fetchFalso({ 'GET /api/facturas': { estado: 200, cuerpo: [fila] } })
    render(<MemoryRouter><ListadoFacturas /></MemoryRouter>)
    await userEvent.selectOptions(await screen.findByLabelText('Estado'), 'Pendiente')
    await userEvent.type(screen.getByLabelText('Número'), '34561')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }))

    const tr = (await screen.findByText('B 0003-00034561')).closest('tr')!
    expect(within(tr).getByText('20/03/2026')).toBeInTheDocument()
    expect(within(tr).getByText('González, Ana')).toBeInTheDocument()
    expect(within(tr).getByText('Pendiente')).toBeInTheDocument()
    expect(within(tr).getByText('$ 1.815,00')).toBeInTheDocument()
    expect(within(tr).getByText('12')).toBeInTheDocument()
    expect(within(tr).getByRole('link', { name: 'Ver factura 0003-00034561' })).toHaveAttribute('href', '/facturas/5')
    expect(pedidos.at(-1)?.ruta).toBe('/api/facturas?estado=Pendiente&numero=34561')
  })

  it('avisa cuando no hay facturas', async () => {
    fetchFalso({ 'GET /api/facturas': { estado: 200, cuerpo: [] } })
    render(<MemoryRouter><ListadoFacturas /></MemoryRouter>)
    expect(await screen.findByText('No hay facturas que cumplan todos los filtros.')).toBeInTheDocument()
  })
})

import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { BuscarPresupuestos } from './BuscarPresupuestos'
import { EditorPresupuesto } from './EditorPresupuesto'

const resumen = { id: 3, numero: 12, fecha: '2026-03-20', estado: 'Final', apellido: 'González', nombre: 'Ana', dni: '23456789', total: 1815 }

const presupuesto = (estado: 'Borrador' | 'Final') => ({
  id: 3, numero: 12, fecha: '2026-03-20', estado,
  cliente: { apellido: 'González', nombre: 'Ana', dni: '23456789', domicilio: null, email: null, telefono: null },
  lineas: [{ id: 1, orden: 1, articuloCodigo: 7, descripcion: 'Armazón metal', precioUnitario: 1815, cantidad: 1, descuento: 0, precioConDescuento: 1815, precioFinal: 1815 }],
  total: 1815,
})

describe('Búsqueda de presupuestos', () => {
  it('busca con los filtros cargados y lista los resultados', async () => {
    const pedidos = fetchFalso({ 'GET /api/presupuestos': { estado: 200, cuerpo: [resumen] } })
    render(<MemoryRouter><BuscarPresupuestos /></MemoryRouter>)
    await userEvent.type(screen.getByLabelText('Apellido'), 'ONZALEZ')
    await userEvent.type(screen.getByLabelText('Desde'), '2026-03-15')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }))

    const fila = (await screen.findByText('González, Ana')).closest('tr')!
    expect(within(fila).getByText('12')).toBeInTheDocument()
    expect(within(fila).getByText('20/03/2026')).toBeInTheDocument()
    expect(within(fila).getByText('23.456.789')).toBeInTheDocument()
    expect(within(fila).getByText('$ 1.815,00')).toBeInTheDocument()
    expect(within(fila).getByText('Final')).toBeInTheDocument()
    expect(within(fila).getByRole('link', { name: 'Abrir presupuesto 12' })).toHaveAttribute('href', '/presupuestos/3')
    expect(pedidos.at(-1)?.ruta).toBe('/api/presupuestos?apellido=ONZALEZ&desde=2026-03-15')
  })

  it('avisa cuando no hay resultados', async () => {
    fetchFalso({ 'GET /api/presupuestos': { estado: 200, cuerpo: [] } })
    render(<MemoryRouter><BuscarPresupuestos /></MemoryRouter>)
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }))
    expect(await screen.findByText('No hay presupuestos que cumplan todos los filtros.')).toBeInTheDocument()
  })
})

describe('Descargar PDF del presupuesto', () => {
  it('está deshabilitado en Borrador (AC-08)', async () => {
    fetchFalso({ 'GET /api/presupuestos/3': { estado: 200, cuerpo: presupuesto('Borrador') } })
    render(<MemoryRouter><EditorPresupuesto id={3} /></MemoryRouter>)
    expect(await screen.findByRole('button', { name: 'Descargar PDF' })).toBeDisabled()
  })

  it('en Final descarga el PDF', async () => {
    fetchFalso({ 'GET /api/presupuestos/3': { estado: 200, cuerpo: presupuesto('Final') } })
    render(<MemoryRouter><EditorPresupuesto id={3} /></MemoryRouter>)
    expect(await screen.findByRole('link', { name: 'Descargar PDF' })).toHaveAttribute('href', '/api/presupuestos/3/pdf')
  })
})

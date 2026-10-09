import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { Articulos } from './Articulos'

const lista = [
  { codigo: 7, proveedorId: 1, proveedor: 'Lentes SA', codigoProveedor: 'ABC-1', descripcion: 'Armazón metal', precioCosto: 1210, margen: 50, precioVenta: 1815 },
]

describe('Artículos', () => {
  it('busca por código, código en el proveedor o descripción y lista los resultados', async () => {
    const pedidos = fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: [{ id: 1, nombre: 'Lentes SA' }] },
      'GET /api/articulos': { estado: 200, cuerpo: lista },
    })
    render(<Articulos />)
    await userEvent.type(await screen.findByLabelText('Buscar por código, código en el proveedor o descripción'), 'armazón')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }))
    const fila = (await screen.findByText('ABC-1')).closest('tr')!
    expect(within(fila).getByText('Armazón metal')).toBeInTheDocument()
    expect(within(fila).getByText('$ 1.815,00')).toBeInTheDocument()
    expect(pedidos.some((p) => p.ruta === '/api/articulos?texto=armaz%C3%B3n')).toBe(true)
  })

  it('abre el editor para un artículo nuevo', async () => {
    fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: [{ id: 1, nombre: 'Lentes SA' }] },
      'GET /api/articulos': { estado: 200, cuerpo: [] },
    })
    render(<Articulos />)
    await userEvent.click(await screen.findByRole('button', { name: 'Nuevo artículo' }))
    expect(screen.getByRole('button', { name: 'Grabar artículo' })).toBeInTheDocument()
  })
})

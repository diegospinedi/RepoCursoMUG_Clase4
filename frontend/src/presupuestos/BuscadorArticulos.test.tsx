import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { BuscadorArticulos } from './BuscadorArticulos'

const articulo = {
  codigo: 7, proveedorId: 1, proveedor: 'Lentes SA', codigoProveedor: 'ABC-1', descripcion: 'Armazón metal',
  precioCosto: 1210, margen: 50, precioVenta: 1815,
}

describe('BuscadorArticulos', () => {
  it.each(['7', 'armazón'])('busca por código o descripción ("%s") y devuelve el elegido', async (texto) => {
    const pedidos = fetchFalso({ 'GET /api/articulos': { estado: 200, cuerpo: [articulo] } })
    const alElegir = vi.fn()
    render(<BuscadorArticulos alElegir={alElegir} />)
    await userEvent.type(screen.getByLabelText('Buscar artículo por código o descripción'), texto)
    await userEvent.click(screen.getByRole('button', { name: 'Buscar artículo' }))
    expect(await screen.findByText('$ 1.815,00')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Agregar Armazón metal' }))
    expect(alElegir).toHaveBeenCalledWith(articulo)
    expect(pedidos[0].ruta).toBe(`/api/articulos?texto=${encodeURIComponent(texto)}`)
  })

  it('avisa cuando no encuentra artículos', async () => {
    fetchFalso({ 'GET /api/articulos': { estado: 200, cuerpo: [] } })
    render(<BuscadorArticulos alElegir={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Buscar artículo por código o descripción'), 'xyz')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar artículo' }))
    expect(await screen.findByText('No se encontraron artículos.')).toBeInTheDocument()
  })
})

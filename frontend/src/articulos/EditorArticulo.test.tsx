import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { EditorArticulo } from './EditorArticulo'

const proveedores = [{ id: 1, nombre: 'Lentes SA' }, { id: 2, nombre: 'Ópticos SRL' }]
const articulo = {
  codigo: 7, proveedorId: 1, proveedor: 'Lentes SA', codigoProveedor: 'ABC-1', descripcion: 'Armazón metal',
  precioCosto: 1210, margen: 50, precioVenta: 1815,
}

describe('EditorArticulo', () => {
  it('muestra el precio de venta en solo lectura (AC-81)', () => {
    render(<EditorArticulo articulo={articulo} proveedores={proveedores} alGrabar={vi.fn()} />)
    const venta = screen.getByLabelText('Precio de venta')
    expect(venta).toHaveValue('$ 1.815,00')
    expect(venta).toHaveAttribute('readonly')
  })

  it('al modificar envía costo y margen pero nunca el precio de venta', async () => {
    const pedidos = fetchFalso({ 'PUT /api/articulos/7': { estado: 200, cuerpo: { ...articulo, margen: 60, precioVenta: 1936 } } })
    const alGrabar = vi.fn()
    render(<EditorArticulo articulo={articulo} proveedores={proveedores} alGrabar={alGrabar} />)
    const margen = screen.getByLabelText('Margen de utilidad (%)')
    await userEvent.clear(margen)
    await userEvent.type(margen, '60')
    await userEvent.click(screen.getByRole('button', { name: 'Grabar artículo' }))
    expect(pedidos[0].cuerpo).toEqual({ proveedorId: 1, codigoProveedor: 'ABC-1', descripcion: 'Armazón metal', precioCosto: 1210, margen: 60 })
    expect(alGrabar).toHaveBeenCalledWith(expect.objectContaining({ precioVenta: 1936 }))
  })

  it('da de alta un artículo nuevo y muestra los errores junto a cada campo', async () => {
    fetchFalso({
      'POST /api/articulos': {
        estado: 400,
        cuerpo: { errors: { codigoProveedor: ['Ya existe un artículo con ese código en el proveedor.'], margen: ['El margen debe estar entre 0 y 1000.'] } },
      },
    })
    render(<EditorArticulo proveedores={proveedores} alGrabar={vi.fn()} />)
    await userEvent.selectOptions(screen.getByLabelText('Proveedor'), '2')
    await userEvent.type(screen.getByLabelText('Código en el proveedor'), 'ABC-1')
    await userEvent.click(screen.getByRole('button', { name: 'Grabar artículo' }))
    expect(await screen.findByText('Ya existe un artículo con ese código en el proveedor.')).toBeInTheDocument()
    expect(screen.getByLabelText('Margen de utilidad (%)')).toHaveAttribute('aria-invalid', 'true')
  })
})

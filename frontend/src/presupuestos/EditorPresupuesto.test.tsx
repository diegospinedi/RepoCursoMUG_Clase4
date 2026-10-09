import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { EditorPresupuesto } from './EditorPresupuesto'

const articulo = {
  codigo: 7, proveedorId: 1, proveedor: 'Lentes SA', codigoProveedor: 'ABC-1', descripcion: 'Armazón metal',
  precioCosto: 1210, margen: 50, precioVenta: 1815,
}

const editor = (id?: number) =>
  render(
    <MemoryRouter>
      <EditorPresupuesto id={id} />
    </MemoryRouter>,
  )

async function agregarArticulo() {
  await userEvent.type(screen.getByLabelText('Buscar artículo por código o descripción'), 'ABC')
  await userEvent.click(screen.getByRole('button', { name: 'Buscar artículo' }))
  await userEvent.click(await screen.findByRole('button', { name: 'Agregar Armazón metal' }))
}

describe('EditorPresupuesto', () => {
  it('al elegir un artículo completa código, descripción y precio del catálogo (AC-10)', async () => {
    fetchFalso({ 'GET /api/articulos': { estado: 200, cuerpo: [articulo] } })
    editor()
    await agregarArticulo()
    const fila = screen.getByText('Armazón metal').closest('tr')!
    expect(within(fila).getByText('7')).toBeInTheDocument()
    expect(within(fila).getAllByText('$ 1.815,00').length).toBeGreaterThan(0)
  })

  it('recalcula la línea y el total al cambiar cantidad y descuento (AC-46)', async () => {
    fetchFalso({ 'GET /api/articulos': { estado: 200, cuerpo: [articulo] } })
    editor()
    await agregarArticulo()
    const cantidad = screen.getByLabelText('Cantidad de la línea 1')
    await userEvent.clear(cantidad)
    await userEvent.type(cantidad, '3')
    await userEvent.type(screen.getByLabelText('Descuento (%) de la línea 1'), '10')
    const fila = screen.getByText('Armazón metal').closest('tr')!
    expect(within(fila).getByText('$ 1.633,50')).toBeInTheDocument()
    expect(within(fila).getByText('$ 4.900,50')).toBeInTheDocument()
    expect(screen.getByTestId('total')).toHaveTextContent('$ 4.900,50')
  })

  it('graba y confirma el número y el estado', async () => {
    const pedidos = fetchFalso({
      'GET /api/articulos': { estado: 200, cuerpo: [articulo] },
      'POST /api/presupuestos': {
        estado: 201,
        cuerpo: {
          id: 1, numero: 155, fecha: '2026-10-09', estado: 'Borrador',
          cliente: { apellido: 'González', nombre: 'Ana', dni: '23456789', domicilio: null, email: null, telefono: null },
          lineas: [{ id: 1, orden: 1, articuloCodigo: 7, descripcion: 'Armazón metal', precioUnitario: 1815, cantidad: 1, descuento: 0, precioConDescuento: 1815, precioFinal: 1815 }],
          total: 1815,
        },
      },
    })
    editor()
    await userEvent.type(screen.getByLabelText('Apellido'), 'González')
    await userEvent.type(screen.getByLabelText('Nombre'), 'Ana')
    await userEvent.type(screen.getByLabelText('DNI'), '23.456.789')
    await agregarArticulo()
    await userEvent.click(screen.getByRole('button', { name: 'Grabar' }))
    expect(await screen.findByText('Presupuesto 155 grabado en estado Borrador.')).toBeInTheDocument()
    expect(pedidos.find((p) => p.metodo === 'POST')?.cuerpo).toEqual({
      estado: 'Borrador',
      cliente: { apellido: 'González', nombre: 'Ana', dni: '23.456.789', domicilio: '', email: '', telefono: '' },
      lineas: [{ id: null, articuloCodigo: 7, cantidad: 1, descuento: 0 }],
    })
  })

  it('muestra los errores de validación junto a cada campo (AC-12, AC-34, AC-42, AC-43)', async () => {
    fetchFalso({
      'GET /api/articulos': { estado: 200, cuerpo: [articulo] },
      'POST /api/presupuestos': {
        estado: 400,
        cuerpo: {
          errors: {
            'cliente.dni': ['Ingresá el DNI del cliente'],
            'lineas[0].cantidad': ['La cantidad debe ser un número entero mayor a 0'],
            'lineas[0].descuento': ['El descuento debe estar entre 0 y 100'],
          },
        },
      },
    })
    editor()
    await agregarArticulo()
    await userEvent.click(screen.getByRole('button', { name: 'Grabar' }))
    expect(await screen.findByText('Ingresá el DNI del cliente')).toBeInTheDocument()
    expect(screen.getByLabelText('DNI')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByText('La cantidad debe ser un número entero mayor a 0')).toBeInTheDocument()
    expect(screen.getByLabelText('Cantidad de la línea 1')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Descuento (%) de la línea 1')).toHaveAttribute('aria-invalid', 'true')
  })

  it('no discrimina IVA en ningún importe (AC-28)', async () => {
    fetchFalso({ 'GET /api/articulos': { estado: 200, cuerpo: [articulo] } })
    editor()
    await agregarArticulo()
    expect(screen.queryByText(/neto/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/^IVA\b/)).not.toBeInTheDocument()
    expect(screen.getByText('Precios finales, IVA incluido')).toBeInTheDocument()
  })

  it('un presupuesto Final se muestra sin controles de edición', async () => {
    fetchFalso({
      'GET /api/presupuestos/3': {
        estado: 200,
        cuerpo: {
          id: 3, numero: 12, fecha: '2026-10-09', estado: 'Final',
          cliente: { apellido: 'González', nombre: 'Ana', dni: '23456789', domicilio: null, email: null, telefono: null },
          lineas: [{ id: 1, orden: 1, articuloCodigo: 7, descripcion: 'Armazón metal', precioUnitario: 1815, cantidad: 1, descuento: 0, precioConDescuento: 1815, precioFinal: 1815 }],
          total: 1815,
        },
      },
    })
    editor(3)
    expect(await screen.findByText('Presupuesto 12')).toBeInTheDocument()
    expect(screen.getByText('Final')).toBeInTheDocument()
    expect(screen.queryAllByRole('textbox')).toHaveLength(0)
    expect(screen.queryByRole('button', { name: 'Grabar' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Agregar/ })).not.toBeInTheDocument()
  })
})

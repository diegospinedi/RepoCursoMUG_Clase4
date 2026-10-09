import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { Proveedores } from './Proveedores'

describe('Proveedores', () => {
  it('lista y da de alta proveedores', async () => {
    let lista = [{ id: 1, nombre: 'Lentes SA' }]
    const pedidos = fetchFalso({
      'GET /api/proveedores': () => ({ estado: 200, cuerpo: lista }),
      'POST /api/proveedores': (cuerpo) => {
        lista = [...lista, { id: 2, nombre: (cuerpo as { nombre: string }).nombre }]
        return { estado: 201, cuerpo: lista[1] }
      },
    })
    render(<Proveedores />)
    expect(await screen.findByText('Lentes SA')).toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('Nombre del proveedor nuevo'), 'Ópticos SRL')
    await userEvent.click(screen.getByRole('button', { name: 'Agregar proveedor' }))
    expect(await screen.findByText('Ópticos SRL')).toBeInTheDocument()
    expect(pedidos.find((p) => p.metodo === 'POST')?.cuerpo).toEqual({ nombre: 'Ópticos SRL' })
  })

  it('muestra el error de nombre repetido junto al campo', async () => {
    fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: [] },
      'POST /api/proveedores': { estado: 400, cuerpo: { errors: { nombre: ['Ya existe un proveedor con ese nombre.'] } } },
    })
    render(<Proveedores />)
    await userEvent.type(await screen.findByLabelText('Nombre del proveedor nuevo'), 'lentes sa')
    await userEvent.click(screen.getByRole('button', { name: 'Agregar proveedor' }))
    expect(await screen.findByText('Ya existe un proveedor con ese nombre.')).toBeInTheDocument()
  })

  it('modifica el nombre de un proveedor', async () => {
    let lista = [{ id: 1, nombre: 'Lentes SA' }]
    const pedidos = fetchFalso({
      'GET /api/proveedores': () => ({ estado: 200, cuerpo: lista }),
      'PUT /api/proveedores/1': (cuerpo) => {
        lista = [{ id: 1, nombre: (cuerpo as { nombre: string }).nombre }]
        return { estado: 200, cuerpo: lista[0] }
      },
    })
    render(<Proveedores />)
    const fila = (await screen.findByText('Lentes SA')).closest('tr')!
    await userEvent.click(within(fila).getByRole('button', { name: 'Cambiar nombre' }))
    const campo = screen.getByLabelText('Nombre de Lentes SA')
    await userEvent.clear(campo)
    await userEvent.type(campo, 'Lentes Sur SA')
    await userEvent.click(screen.getByRole('button', { name: 'Guardar nombre' }))
    expect(await screen.findByText('Lentes Sur SA')).toBeInTheDocument()
    expect(pedidos.find((p) => p.metodo === 'PUT')?.cuerpo).toEqual({ nombre: 'Lentes Sur SA' })
  })
})

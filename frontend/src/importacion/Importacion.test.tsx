import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { Importacion } from './Importacion'

const proveedores = [{ id: 1, nombre: 'Lentes SA' }, { id: 2, nombre: 'Ópticos SRL' }]
const archivo = () => new File(['x'], 'lista.xlsx', { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })

describe('Importación de la planilla del proveedor', () => {
  it('importa para el proveedor elegido y muestra el resumen con las filas informadas', async () => {
    const pedidos = fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: proveedores },
      'GET /api/proveedores/2/importaciones': { estado: 200, cuerpo: [] },
      'POST /api/proveedores/2/importaciones': {
        estado: 200,
        cuerpo: {
          id: 5, creados: 1, actualizados: 2,
          filas: [
            { numeroFila: 3, codigoProveedor: 'A-2', resultado: 'NoProcesada', razon: 'precio inválido' },
            { numeroFila: 4, codigoProveedor: 'A-3', resultado: 'ActualizadoPrecioNegativoOCero', razon: 'actualizado con precio negativo o cero' },
          ],
        },
      },
    })
    render(<Importacion />)
    await userEvent.selectOptions(await screen.findByLabelText('Proveedor'), '2')
    await userEvent.upload(screen.getByLabelText('Planilla (.xlsx)'), archivo())
    await userEvent.click(screen.getByRole('button', { name: 'Importar' }))

    expect(await screen.findByText('Se crearon 1 artículos y se actualizaron 2.')).toBeInTheDocument()
    const fila = screen.getByText('precio inválido').closest('tr')!
    expect(within(fila).getByText('3')).toBeInTheDocument()
    expect(within(fila).getByText('A-2')).toBeInTheDocument()
    expect(screen.getByText('actualizado con precio negativo o cero')).toBeInTheDocument()
    const envio = pedidos.find((p) => p.metodo === 'POST')!
    expect((envio.cuerpo as FormData).get('archivo')).toBeInstanceOf(File)
  })

  it('avisa que el formato de la planilla no es el esperado', async () => {
    fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: proveedores },
      'GET /api/proveedores/1/importaciones': { estado: 200, cuerpo: [] },
      'POST /api/proveedores/1/importaciones': {
        estado: 400,
        cuerpo: { type: 'formato-planilla', title: 'La planilla no tiene el formato esperado.', detail: 'Tiene que tener exactamente las columnas Código en el proveedor, Descripción y Precio de Costo.' },
      },
    })
    render(<Importacion />)
    await screen.findByLabelText('Proveedor')
    await userEvent.upload(screen.getByLabelText('Planilla (.xlsx)'), archivo())
    await userEvent.click(screen.getByRole('button', { name: 'Importar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('La planilla no tiene el formato esperado.')
    expect(screen.getByRole('alert')).toHaveTextContent('Código en el proveedor, Descripción y Precio de Costo')
  })

  it('avisa que falta configurar el margen predeterminado', async () => {
    fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: proveedores },
      'GET /api/proveedores/1/importaciones': { estado: 200, cuerpo: [] },
      'POST /api/proveedores/1/importaciones': {
        estado: 409,
        cuerpo: { type: 'margen-sin-configurar', title: 'Antes de importar, configurá el margen predeterminado para artículos nuevos.' },
      },
    })
    render(<Importacion />)
    await screen.findByLabelText('Proveedor')
    await userEvent.upload(screen.getByLabelText('Planilla (.xlsx)'), archivo())
    await userEvent.click(screen.getByRole('button', { name: 'Importar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('configurá el margen predeterminado')
  })

  it('muestra el historial de importaciones del proveedor', async () => {
    fetchFalso({
      'GET /api/proveedores': { estado: 200, cuerpo: proveedores },
      'GET /api/proveedores/1/importaciones': {
        estado: 200,
        cuerpo: [{ id: 5, fecha: '2026-10-09T10:00:00-03:00', nombreArchivo: 'lista-octubre.xlsx', creados: 3, actualizados: 10, noProcesados: 1 }],
      },
    })
    render(<Importacion />)
    const fila = (await screen.findByText('lista-octubre.xlsx')).closest('tr')!
    expect(within(fila).getByText('09/10/2026')).toBeInTheDocument()
    expect(within(fila).getByText('10')).toBeInTheDocument()
  })
})

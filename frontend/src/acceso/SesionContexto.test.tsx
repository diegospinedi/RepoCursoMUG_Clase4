import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { EVENTO_SESION_VENCIDA } from '../api/cliente'
import { fetchFalso } from '../test/fetchFalso'
import { SesionContexto } from './SesionContexto'

const Protegido = () => (
  <label>
    Domicilio
    <input />
  </label>
)

describe('SesionContexto', () => {
  it('pide definir la contraseña la primera vez', async () => {
    fetchFalso({ 'GET /api/acceso/estado': { estado: 200, cuerpo: { definida: false, sesionIniciada: false } } })
    render(<SesionContexto><Protegido /></SesionContexto>)
    expect(await screen.findByRole('heading', { name: 'Definir la contraseña de acceso' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Domicilio')).not.toBeInTheDocument()
  })

  it('sin sesión muestra el ingreso y no las pantallas', async () => {
    fetchFalso({ 'GET /api/acceso/estado': { estado: 200, cuerpo: { definida: true, sesionIniciada: false } } })
    render(<SesionContexto><Protegido /></SesionContexto>)
    expect(await screen.findByRole('button', { name: 'Ingresar' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Domicilio')).not.toBeInTheDocument()
  })

  it('ofrece restablecer la contraseña desde el ingreso', async () => {
    fetchFalso({ 'GET /api/acceso/estado': { estado: 200, cuerpo: { definida: true, sesionIniciada: false } } })
    render(<SesionContexto><Protegido /></SesionContexto>)
    await userEvent.click(await screen.findByRole('button', { name: 'Olvidé la contraseña' }))
    expect(screen.getByRole('heading', { name: 'Restablecer la contraseña' })).toBeInTheDocument()
  })

  it('con sesión muestra las pantallas', async () => {
    fetchFalso({ 'GET /api/acceso/estado': { estado: 200, cuerpo: { definida: true, sesionIniciada: true } } })
    render(<SesionContexto><Protegido /></SesionContexto>)
    expect(await screen.findByLabelText('Domicilio')).toBeInTheDocument()
  })

  it('si la sesión vence pide la contraseña sin perder lo cargado (FR-003a)', async () => {
    fetchFalso({
      'GET /api/acceso/estado': { estado: 200, cuerpo: { definida: true, sesionIniciada: true } },
      'POST /api/acceso/ingresar': { estado: 204 },
    })
    render(<SesionContexto><Protegido /></SesionContexto>)
    await userEvent.type(await screen.findByLabelText('Domicilio'), 'Calle 42 767')

    act(() => window.dispatchEvent(new Event(EVENTO_SESION_VENCIDA)))
    const dialogo = await screen.findByRole('dialog')
    expect(dialogo).toHaveTextContent('La sesión venció')
    expect(screen.getByLabelText('Domicilio')).toHaveValue('Calle 42 767')

    await userEvent.type(screen.getByLabelText('Contraseña'), 'clave-segura')
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Domicilio')).toHaveValue('Calle 42 767')
  })
})

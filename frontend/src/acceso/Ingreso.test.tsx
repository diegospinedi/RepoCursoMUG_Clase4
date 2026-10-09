import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { Ingreso } from './Ingreso'

describe('Ingreso', () => {
  it('ingresa con la contraseña correcta', async () => {
    const pedidos = fetchFalso({ 'POST /api/acceso/ingresar': { estado: 204 } })
    const alIngresar = vi.fn()
    render(<Ingreso alIngresar={alIngresar} />)
    await userEvent.type(screen.getByLabelText('Contraseña'), 'clave-segura')
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }))
    expect(alIngresar).toHaveBeenCalledOnce()
    expect(pedidos[0].cuerpo).toEqual({ contrasena: 'clave-segura' })
  })

  it('avisa que la contraseña es incorrecta', async () => {
    fetchFalso({ 'POST /api/acceso/ingresar': { estado: 401 } })
    render(<Ingreso alIngresar={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Contraseña'), 'mala')
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }))
    expect(await screen.findByText('La contraseña es incorrecta.')).toBeInTheDocument()
  })

  it('avisa el bloqueo con la hora en que se libera', async () => {
    fetchFalso({
      'POST /api/acceso/ingresar': { estado: 423, cuerpo: { type: 'acceso-bloqueado', bloqueadoHasta: '2026-10-09T10:05:00-03:00' } },
    })
    render(<Ingreso alIngresar={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Contraseña'), 'clave-segura')
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }))
    expect(await screen.findByText(/bloqueado.*10:05/i)).toBeInTheDocument()
  })
})

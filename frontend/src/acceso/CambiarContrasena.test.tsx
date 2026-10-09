import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { CambiarContrasena } from './CambiarContrasena'

describe('Cambiar la contraseña', () => {
  it('muestra el error de la contraseña actual junto al campo', async () => {
    fetchFalso({
      'POST /api/acceso/cambiar': { estado: 400, cuerpo: { errors: { actual: ['La contraseña actual no es correcta.'] } } },
    })
    render(<CambiarContrasena />)
    await userEvent.type(screen.getByLabelText('Contraseña actual'), 'otra')
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'nueva-clave-larga')
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }))
    expect(await screen.findByText('La contraseña actual no es correcta.')).toBeInTheDocument()
    expect(screen.getByLabelText('Contraseña actual')).toHaveAttribute('aria-invalid', 'true')
  })

  it('valida el mínimo de 8 caracteres antes de enviar', async () => {
    const pedidos = fetchFalso({})
    render(<CambiarContrasena />)
    await userEvent.type(screen.getByLabelText('Contraseña actual'), 'clave-segura')
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'corta')
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }))
    expect(screen.getByText('La contraseña debe tener al menos 8 caracteres.')).toBeInTheDocument()
    expect(pedidos).toHaveLength(0)
  })

  it('confirma el cambio', async () => {
    fetchFalso({ 'POST /api/acceso/cambiar': { estado: 204 } })
    render(<CambiarContrasena />)
    await userEvent.type(screen.getByLabelText('Contraseña actual'), 'clave-segura')
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'nueva-clave-larga')
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }))
    expect(await screen.findByText(/Contraseña cambiada/)).toBeInTheDocument()
  })
})

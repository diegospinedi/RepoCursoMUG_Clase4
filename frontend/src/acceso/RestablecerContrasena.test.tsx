import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { RestablecerContrasena } from './RestablecerContrasena'

describe('Restablecer la contraseña olvidada', () => {
  it('restablece sin pedir la anterior', async () => {
    const pedidos = fetchFalso({ 'POST /api/acceso/restablecer': { estado: 204 } })
    const alRestablecer = vi.fn()
    render(<RestablecerContrasena alRestablecer={alRestablecer} />)
    expect(screen.queryByLabelText('Contraseña actual')).not.toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'restablecida-123')
    await userEvent.click(screen.getByRole('button', { name: 'Restablecer contraseña' }))
    expect(alRestablecer).toHaveBeenCalledOnce()
    expect(pedidos[0].cuerpo).toEqual({ nueva: 'restablecida-123' })
  })

  it('avisa cuando el pedido no viene de la PC del sistema', async () => {
    fetchFalso({ 'POST /api/acceso/restablecer': { estado: 403, cuerpo: { type: 'solo-pc-local' } } })
    render(<RestablecerContrasena alRestablecer={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'restablecida-123')
    await userEvent.click(screen.getByRole('button', { name: 'Restablecer contraseña' }))
    expect(await screen.findByText(/desde la PC donde corre el sistema/)).toBeInTheDocument()
  })

  it('valida el mínimo de 8 caracteres', async () => {
    const pedidos = fetchFalso({})
    render(<RestablecerContrasena alRestablecer={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'corta')
    await userEvent.click(screen.getByRole('button', { name: 'Restablecer contraseña' }))
    expect(screen.getByText('La contraseña debe tener al menos 8 caracteres.')).toBeInTheDocument()
    expect(pedidos).toHaveLength(0)
  })
})

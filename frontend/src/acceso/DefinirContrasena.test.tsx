import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { DefinirContrasena } from './DefinirContrasena'

describe('Definir la contraseña la primera vez', () => {
  it('no acepta menos de 8 caracteres e indica el mínimo (AC-57)', async () => {
    const pedidos = fetchFalso({})
    render(<DefinirContrasena alDefinir={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), '1234567')
    await userEvent.click(screen.getByRole('button', { name: 'Definir contraseña' }))
    expect(screen.getByText('La contraseña debe tener al menos 8 caracteres.')).toBeInTheDocument()
    expect(pedidos).toHaveLength(0)
  })

  it('define la contraseña con 8 o más caracteres', async () => {
    fetchFalso({ 'POST /api/acceso/definir': { estado: 204 } })
    const alDefinir = vi.fn()
    render(<DefinirContrasena alDefinir={alDefinir} />)
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), '12345678')
    await userEvent.click(screen.getByRole('button', { name: 'Definir contraseña' }))
    expect(alDefinir).toHaveBeenCalledOnce()
  })

  it('avisa si se intenta desde otro equipo', async () => {
    fetchFalso({ 'POST /api/acceso/definir': { estado: 403, cuerpo: { type: 'solo-pc-local' } } })
    render(<DefinirContrasena alDefinir={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), '12345678')
    await userEvent.click(screen.getByRole('button', { name: 'Definir contraseña' }))
    expect(await screen.findByText(/desde la PC donde corre el sistema/)).toBeInTheDocument()
  })
})

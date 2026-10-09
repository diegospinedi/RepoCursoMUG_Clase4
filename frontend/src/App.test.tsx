import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import branding from '../../Marca/branding.json'
import App from './App'
import { fetchFalso } from './test/fetchFalso'

describe('shell de la aplicación (AC-33)', () => {
  it('muestra el logo de la óptica y aplica el color primario de la marca', async () => {
    fetchFalso({ 'GET /api/acceso/estado': { estado: 200, cuerpo: { definida: true, sesionIniciada: false } } })
    render(<App />)
    await screen.findByRole('button', { name: 'Ingresar' })
    const logo = screen.getByRole('img', { name: 'Óptica Sistema' })
    expect(logo.getAttribute('src')).toMatch(/logo.*\.png/)
    expect(document.documentElement.style.getPropertyValue('--marca-primario')).toBe(branding.colores.primario)
  })
})

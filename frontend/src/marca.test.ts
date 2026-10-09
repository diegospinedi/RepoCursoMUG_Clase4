import { describe, expect, it } from 'vitest'
import branding from '../../Marca/branding.json'
import { aplicarMarca, marca } from './marca'

describe('marca', () => {
  it('toma logo y colores de Marca/branding.json', () => {
    expect(marca.colorPrimario).toBe(branding.colores.primario)
    expect(marca.colorFondo).toBe(branding.colores.fondo)
    expect(marca.logo).toMatch(/logo.*\.png/)
  })

  it('publica los colores como variables CSS para tokens.css', () => {
    aplicarMarca(document.documentElement)
    expect(document.documentElement.style.getPropertyValue('--marca-primario')).toBe(branding.colores.primario)
    expect(document.documentElement.style.getPropertyValue('--marca-fondo')).toBe(branding.colores.fondo)
  })
})

import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { CampoConError } from './CampoConError'

describe('CampoConError (FR-041)', () => {
  it('asocia la etiqueta al campo y no marca error si no lo hay', () => {
    render(<CampoConError etiqueta="DNI" clave="cliente.dni" errores={{}} />)
    const campo = screen.getByLabelText('DNI')
    expect(campo).not.toHaveAttribute('aria-invalid', 'true')
  })

  it('muestra el error de su clave junto al campo', () => {
    render(
      <CampoConError
        etiqueta="DNI"
        clave="cliente.dni"
        errores={{ 'cliente.dni': 'Ingresá el DNI del cliente', 'cliente.apellido': 'otro' }}
      />,
    )
    const campo = screen.getByLabelText('DNI')
    expect(campo).toHaveAttribute('aria-invalid', 'true')
    expect(campo).toHaveAccessibleDescription('Ingresá el DNI del cliente')
    expect(screen.queryByText('otro')).not.toBeInTheDocument()
  })

  it('pasa las props al input', async () => {
    const alCambiar = vi.fn()
    render(<CampoConError etiqueta="Apellido" clave="cliente.apellido" errores={{}} value="" onChange={alCambiar} />)
    await userEvent.type(screen.getByLabelText('Apellido'), 'G')
    expect(alCambiar).toHaveBeenCalled()
  })
})

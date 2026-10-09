import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { Configuracion } from './Configuracion'

const actual = {
  alicuotaIva: 21,
  condicionFiscal: 'ResponsableInscripto',
  topeIdentificacion: 10000000,
  multiploRedondeo: 0.01,
  margenPredeterminado: null,
}

describe('Configuración', () => {
  it('muestra los cinco parámetros y nada del certificado ni del punto de venta (AC-50)', async () => {
    fetchFalso({ 'GET /api/configuracion': { estado: 200, cuerpo: actual } })
    const { container } = render(<Configuracion />)
    expect(await screen.findByLabelText('Alícuota de IVA')).toHaveValue('21')
    expect(screen.getByLabelText('Condición fiscal')).toHaveValue('ResponsableInscripto')
    expect(screen.getByLabelText('Tope de identificación')).toHaveValue('10000000')
    expect(screen.getByLabelText('Múltiplo de redondeo')).toHaveValue('0,01')
    expect(screen.getByLabelText('Margen predeterminado para artículos nuevos (%)')).toHaveValue('')
    expect(container.textContent).not.toMatch(/certificado|punto de venta/i)
  })

  it('solo ofrece las alícuotas que acepta ARCA', async () => {
    fetchFalso({ 'GET /api/configuracion': { estado: 200, cuerpo: actual } })
    render(<Configuracion />)
    const opciones = Array.from((await screen.findByLabelText<HTMLSelectElement>('Alícuota de IVA')).options).map((o) => o.value)
    expect(opciones).toEqual(['0', '2.5', '5', '10.5', '21', '27'])
  })

  it('graba y avisa cuántos artículos se recalcularon', async () => {
    const pedidos = fetchFalso({
      'GET /api/configuracion': { estado: 200, cuerpo: actual },
      'PUT /api/configuracion': { estado: 200, cuerpo: { ...actual, multiploRedondeo: 50, articulosRecalculados: 3 } },
    })
    render(<Configuracion />)
    const multiplo = await screen.findByLabelText('Múltiplo de redondeo')
    await userEvent.clear(multiplo)
    await userEvent.type(multiplo, '50')
    await userEvent.type(screen.getByLabelText('Margen predeterminado para artículos nuevos (%)'), '62,5')
    await userEvent.click(screen.getByRole('button', { name: 'Grabar configuración' }))
    expect(await screen.findByText(/Configuración grabada\. Se recalcularon 3 artículos\./)).toBeInTheDocument()
    expect(pedidos.at(-1)?.cuerpo).toEqual({ ...actual, multiploRedondeo: 50, margenPredeterminado: 62.5 })
  })

  it('muestra el error junto al campo', async () => {
    fetchFalso({
      'GET /api/configuracion': { estado: 200, cuerpo: actual },
      'PUT /api/configuracion': { estado: 400, cuerpo: { errors: { multiploRedondeo: ['El múltiplo de redondeo debe ser como mínimo 0,01.'] } } },
    })
    render(<Configuracion />)
    await userEvent.click(await screen.findByRole('button', { name: 'Grabar configuración' }))
    expect(await screen.findByText('El múltiplo de redondeo debe ser como mínimo 0,01.')).toBeInTheDocument()
    expect(screen.getByLabelText('Múltiplo de redondeo')).toHaveAttribute('aria-invalid', 'true')
  })
})

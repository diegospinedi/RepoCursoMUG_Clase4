import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { fetchFalso } from '../test/fetchFalso'
import { DetalleFactura } from './DetalleFactura'

const factura = (estado: string, extra: object = {}) => ({
  id: 5, estado, tipo: 'B', puntoVenta: 3, numero: 1, fecha: '2026-10-09', receptorDocTipo: 99, receptorDocNro: '0',
  total: 1815, neto: 1500, iva: 315, alicuotaIva: 21, cae: '76000000000001', vencimientoCae: '2026-10-19',
  presupuesto: { id: 3, numero: 12 }, apellido: 'González', nombre: 'Ana', dni: '23456789',
  lineas: [{ descripcion: 'Armazón metal', cantidad: 1, precioFinal: 1815 }],
  ...extra,
})

const detalle = () => render(<MemoryRouter><DetalleFactura id={5} /></MemoryRouter>)

describe('Detalle de factura', () => {
  it('muestra los datos del comprobante y el presupuesto de origen', async () => {
    fetchFalso({ 'GET /api/facturas/5': { estado: 200, cuerpo: factura('Autorizada') } })
    detalle()
    expect(await screen.findByRole('heading', { name: /Factura B 0003-00000001/ })).toBeInTheDocument()
    expect(screen.getByText('09/10/2026')).toBeInTheDocument()
    expect(screen.getByText('Consumidor Final')).toBeInTheDocument()
    expect(screen.getByText('76000000000001')).toBeInTheDocument()
    expect(screen.getByText('19/10/2026')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Presupuesto 12' })).toHaveAttribute('href', '/presupuestos/3')
    const fila = screen.getByText('Armazón metal').closest('tr')!
    expect(within(fila).getByText('$ 1.815,00')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Descargar PDF' })).toHaveAttribute('href', '/api/facturas/5/pdf')
  })

  it('con DNI identifica al receptor con el documento', async () => {
    fetchFalso({ 'GET /api/facturas/5': { estado: 200, cuerpo: factura('Autorizada', { receptorDocTipo: 96, receptorDocNro: '23456789' }) } })
    detalle()
    expect(await screen.findByText('DNI 23.456.789')).toBeInTheDocument()
  })

  it('sin autorizar no ofrece el PDF', async () => {
    fetchFalso({ 'GET /api/facturas/5': { estado: 200, cuerpo: factura('Pendiente', { cae: null, vencimientoCae: null }) } })
    detalle()
    expect(await screen.findByText('Pendiente')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Descargar PDF' })).not.toBeInTheDocument()
  })
})

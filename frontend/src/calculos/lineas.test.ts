import { describe, expect, it } from 'vitest'
import vectores from '../../../tests/vectores-calculo.json'
import {
  calcularLinea,
  calcularTotal,
  formatearImporte,
  parsearImporte,
  parsearPorcentaje,
} from './lineas'

describe('cálculo de líneas en centavos (mismos vectores que el backend)', () => {
  it.each(vectores.lineas)('$caso', (v) => {
    const r = calcularLinea({
      precioUnitario: parsearImporte(v.precioUnitario),
      descuento: parsearPorcentaje(v.descuento),
      cantidad: v.cantidad,
    })
    expect(r.precioConDescuento).toBe(parsearImporte(v.precioConDescuento))
    expect(r.precioFinal).toBe(parsearImporte(v.precioFinal))
  })

  it.each(vectores.totales)('total $caso', (v) => {
    expect(calcularTotal(v.preciosFinales.map(parsearImporte))).toBe(parsearImporte(v.esperado))
  })
})

describe('conversión y formato', () => {
  it('parsea importes y porcentajes a enteros sin usar flotantes', () => {
    expect(parsearImporte('1815.00')).toBe(181500)
    expect(parsearImporte('0.1')).toBe(10)
    expect(parsearImporte('2.01')).toBe(201)
    expect(parsearPorcentaje('10.5')).toBe(1050)
    expect(parsearPorcentaje('0.45')).toBe(45)
  })

  it('formatea importes en pesos argentinos', () => {
    expect(formatearImporte(181500)).toBe('$ 1.815,00')
    expect(formatearImporte(1)).toBe('$ 0,01')
    expect(formatearImporte(-180000)).toBe('-$ 1.800,00')
  })
})

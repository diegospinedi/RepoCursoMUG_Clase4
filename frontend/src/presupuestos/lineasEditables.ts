import { calcularLinea, parsearPorcentaje } from '../calculos/lineas'

export interface LineaEditable {
  /** Clave estable para React (id grabado o una generada). */
  clave: string
  id: number | null
  articuloCodigo: number
  descripcion: string
  /** Centavos, del catálogo; no editable (FR-019). */
  precioUnitario: number
  cantidad: string
  descuento: string
}

/** Cantidad entera positiva o null si el texto no lo es. */
function cantidadValida(texto: string): number | null {
  return /^\d+$/.test(texto.trim()) && Number(texto) > 0 ? Number(texto) : null
}

/** Descuento en centésimos (10,5 % = 1050) o null si no es un número de 0 a 100 con hasta 2 decimales. */
function descuentoValido(texto: string): number | null {
  const limpio = texto.trim().replace(',', '.')
  if (limpio === '') return 0
  if (!/^\d+(\.\d{1,2})?$/.test(limpio)) return null
  const valor = parsearPorcentaje(limpio)
  return valor <= 10000 ? valor : null
}

/** Importes de la línea calculados igual que el servidor, en centavos (RF-13, RF-14, RF-44). */
export function importesLinea(l: LineaEditable) {
  const cantidad = cantidadValida(l.cantidad)
  const descuento = descuentoValido(l.descuento)
  if (cantidad === null || descuento === null) return null
  return calcularLinea({ precioUnitario: l.precioUnitario, descuento, cantidad })
}

// Cálculo de líneas en enteros (centavos y centésimos), nunca con Math.round sobre importes en number
// (AGENTS.md): 2,01 × 50 % tiene que dar 1,01 igual que el servidor. Vectores: tests/vectores-calculo.json.

/** Importe en centavos o porcentaje en centésimos a partir de un decimal en texto o número JSON. */
function aCentesimos(valor: string | number): number {
  const texto = String(valor).trim()
  const m = /^(-?)(\d*)(?:\.(\d{0,2}))?$/.exec(texto)
  if (!m) throw new Error(`Valor decimal inválido: ${texto}`)
  const entero = Number(m[2] || '0')
  const decimales = Number((m[3] ?? '').padEnd(2, '0'))
  const resultado = entero * 100 + decimales
  return m[1] === '-' ? -resultado : resultado
}

export const parsearImporte = aCentesimos
export const parsearPorcentaje = aCentesimos

/** División entera con redondeo mitad hacia arriba (lejos de cero), como MidpointRounding.AwayFromZero. */
function dividirRedondeando(dividendo: number, divisor: number): number {
  const signo = Math.sign(dividendo)
  return signo * Math.floor((Math.abs(dividendo) + divisor / 2) / divisor)
}

export interface LineaEntrada {
  /** Centavos. */
  precioUnitario: number
  /** Centésimos de punto porcentual (10 % = 1000). */
  descuento: number
  cantidad: number
}

export interface LineaCalculada {
  precioConDescuento: number
  precioFinal: number
}

/** RF-13, RF-14 y RF-44: primero el precio con descuento redondeado, después × cantidad. */
export function calcularLinea({ precioUnitario, descuento, cantidad }: LineaEntrada): LineaCalculada {
  const precioConDescuento = dividirRedondeando(precioUnitario * (10000 - descuento), 10000)
  return { precioConDescuento, precioFinal: precioConDescuento * cantidad }
}

/** RF-15: suma de los precios finales ya redondeados. */
export function calcularTotal(preciosFinales: number[]): number {
  return preciosFinales.reduce((suma, p) => suma + p, 0)
}

const formatoPesos = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })

/** "$ 1.815,00" (skill frontend-design). Intl usa espacio duro; se normaliza a espacio común. */
export function formatearImporte(centavos: number): string {
  return formatoPesos.format(centavos / 100).replace(/ /g, ' ')
}

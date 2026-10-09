// Números en los campos de texto con coma decimal (formato argentino).

/** 0.01 → "0,01"; null → "". */
export function aTexto(valor: number | null | undefined): string {
  return valor === null || valor === undefined ? '' : String(valor).replace('.', ',')
}

/** "62,5" → 62.5; "" → null; texto que no es número → NaN. */
export function aNumero(texto: string): number | null {
  const limpio = texto.trim().replace(',', '.')
  if (limpio === '') return null
  return /^-?\d+(\.\d+)?$/.test(limpio) ? Number(limpio) : Number.NaN
}

/** Importe JSON (pesos) a centavos sin errores de punto flotante. */
export function aCentavos(pesos: number): number {
  return Math.round(pesos * 100)
}

/** "23456789" → "23.456.789". */
export function formatearDni(digitos: string): string {
  return digitos.replace(/\B(?=(\d{3})+(?!\d))/g, '.')
}

import { vi } from 'vitest'

type Respuesta = { estado: number; cuerpo?: unknown }

/** Reemplaza fetch con respuestas por "MÉTODO ruta"; registra los pedidos. */
export function fetchFalso(rutas: Record<string, Respuesta | ((cuerpo: unknown) => Respuesta)>) {
  const pedidos: { metodo: string; ruta: string; cuerpo: unknown }[] = []
  globalThis.fetch = vi.fn(async (ruta: RequestInfo | URL, opciones?: RequestInit) => {
    const metodo = opciones?.method ?? 'GET'
    const cuerpo = typeof opciones?.body === 'string' ? JSON.parse(opciones.body) : opciones?.body
    pedidos.push({ metodo, ruta: String(ruta), cuerpo })
    const clave = `${metodo} ${String(ruta)}`
    const definida = rutas[clave] ?? rutas[`${metodo} ${String(ruta).split('?')[0]}`]
    if (!definida) throw new Error(`Pedido no esperado: ${clave}`)
    const r = typeof definida === 'function' ? definida(cuerpo) : definida
    return new Response(r.cuerpo === undefined ? null : JSON.stringify(r.cuerpo), {
      status: r.estado,
      headers: { 'Content-Type': 'application/json' },
    })
  }) as typeof fetch
  return pedidos
}

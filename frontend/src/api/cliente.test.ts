import { afterEach, describe, expect, it, vi } from 'vitest'
import { ErrorApi, ErrorValidacion, EVENTO_SESION_VENCIDA, pedir } from './cliente'

function responder(estado: number, cuerpo?: unknown) {
  globalThis.fetch = vi.fn().mockResolvedValue(
    new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), {
      status: estado,
      headers: { 'Content-Type': 'application/problem+json' },
    }),
  )
}

afterEach(() => vi.restoreAllMocks())

describe('cliente de la API', () => {
  it('devuelve el JSON de una respuesta correcta y envía JSON con la cookie', async () => {
    responder(200, { numero: 1 })
    await expect(pedir('/api/presupuestos', { method: 'POST', body: { a: 1 } })).resolves.toEqual({ numero: 1 })
    const [ruta, opciones] = vi.mocked(fetch).mock.calls[0]
    expect(ruta).toBe('/api/presupuestos')
    expect(opciones?.credentials).toBe('same-origin')
    expect(opciones?.body).toBe('{"a":1}')
  })

  it('devuelve undefined en un 204', async () => {
    responder(204)
    await expect(pedir('/api/acceso/salir', { method: 'POST' })).resolves.toBeUndefined()
  })

  it('mapea los errores de validación por clave de campo', async () => {
    responder(400, {
      title: 'One or more validation errors occurred.',
      errors: { 'cliente.dni': ['Ingresá el DNI del cliente'], 'lineas[0].cantidad': ['La cantidad debe ser un número entero mayor a 0'] },
    })
    const error = await pedir('/api/presupuestos').catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ErrorValidacion)
    expect((error as ErrorValidacion).errores['cliente.dni']).toBe('Ingresá el DNI del cliente')
    expect((error as ErrorValidacion).errores['lineas[0].cantidad']).toBe('La cantidad debe ser un número entero mayor a 0')
  })

  it('avisa que la sesión venció ante un 401', async () => {
    responder(401)
    const escucha = vi.fn()
    window.addEventListener(EVENTO_SESION_VENCIDA, escucha)
    const error = await pedir('/api/articulos').catch((e: unknown) => e)
    window.removeEventListener(EVENTO_SESION_VENCIDA, escucha)
    expect(escucha).toHaveBeenCalledOnce()
    expect((error as ErrorApi).estado).toBe(401)
  })

  it.each([409, 502, 504])('expone el type del ProblemDetails en un %i', async (estado) => {
    responder(estado, { type: 'arca-rechazo', title: 'ARCA rechazó el comprobante', codigo: '10016' })
    const error = await pedir('/api/presupuestos/1/factura', { method: 'POST' }).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ErrorApi)
    expect((error as ErrorApi).estado).toBe(estado)
    expect((error as ErrorApi).tipo).toBe('arca-rechazo')
    expect((error as ErrorApi).titulo).toBe('ARCA rechazó el comprobante')
    expect((error as ErrorApi).datos.codigo).toBe('10016')
  })
})

// Cliente HTTP según specs/001-presupuestos-facturacion-arca/contracts/api-http.md.

export const EVENTO_SESION_VENCIDA = 'optica:sesion-vencida'

/** Error de la API con el ProblemDetails recibido. */
export class ErrorApi extends Error {
  readonly estado: number
  readonly tipo?: string
  readonly titulo?: string
  readonly datos: Record<string, unknown>

  constructor(estado: number, datos: Record<string, unknown> = {}) {
    const titulo = typeof datos.title === 'string' ? datos.title : undefined
    super(titulo ?? `Error ${estado}`)
    this.estado = estado
    this.tipo = typeof datos.type === 'string' ? datos.type : undefined
    this.titulo = titulo
    this.datos = datos
  }
}

/** 400 con errores por clave de campo (`cliente.dni`, `lineas[0].cantidad`). */
export class ErrorValidacion extends ErrorApi {
  readonly errores: Record<string, string>

  constructor(datos: Record<string, unknown>) {
    super(400, datos)
    const crudos = (datos.errors ?? {}) as Record<string, string[]>
    this.errores = Object.fromEntries(Object.entries(crudos).map(([clave, mensajes]) => [clave, mensajes[0] ?? '']))
  }
}

export interface OpcionesPedido {
  method?: 'GET' | 'POST' | 'PUT'
  body?: unknown
  /** En el ingreso, un 401 es "contraseña incorrecta", no una sesión vencida. */
  sinAvisoDeSesion?: boolean
}

export async function pedir<T = unknown>(ruta: string, opciones: OpcionesPedido = {}): Promise<T> {
  const esArchivo = opciones.body instanceof FormData
  const respuesta = await fetch(ruta, {
    method: opciones.method ?? 'GET',
    credentials: 'same-origin',
    headers: opciones.body === undefined || esArchivo ? undefined : { 'Content-Type': 'application/json' },
    body: opciones.body === undefined ? undefined : esArchivo ? (opciones.body as FormData) : JSON.stringify(opciones.body),
  })

  if (respuesta.status === 401) {
    if (!opciones.sinAvisoDeSesion) window.dispatchEvent(new Event(EVENTO_SESION_VENCIDA))
    throw new ErrorApi(401)
  }

  const texto = await respuesta.text()
  const datos = texto ? (JSON.parse(texto) as unknown) : undefined

  if (!respuesta.ok) {
    const problema = (datos ?? {}) as Record<string, unknown>
    if (respuesta.status === 400 && problema.errors) throw new ErrorValidacion(problema)
    throw new ErrorApi(respuesta.status, problema)
  }
  return datos as T
}

/** URL de descarga de un PDF (se abre en el navegador con la cookie de sesión). */
export function urlPdf(ruta: string): string {
  return ruta
}

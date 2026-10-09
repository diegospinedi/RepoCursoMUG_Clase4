export const LONGITUD_MINIMA = 8
export const MENSAJE_LONGITUD = 'La contraseña debe tener al menos 8 caracteres.'

export function soloPcLocal(accion: string): string {
  return `La contraseña solo se puede ${accion} desde la PC donde corre el sistema.`
}

const formatoHora = new Intl.DateTimeFormat('es-AR', {
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  timeZone: 'America/Argentina/Buenos_Aires',
})

export function horaArgentina(iso: string): string {
  return formatoHora.format(new Date(iso))
}

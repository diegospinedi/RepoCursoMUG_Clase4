import { useState, type FormEvent, type ReactNode } from 'react'
import { ErrorApi, ErrorValidacion, pedir } from '../api/cliente'
import { CampoConError } from '../comunes/CampoConError'
import { LONGITUD_MINIMA, MENSAJE_LONGITUD } from './contrasena'

interface Props {
  titulo: string
  ruta: string
  boton: string
  pedirActual?: boolean
  avisoSoloLocal?: string
  avisoExito?: string
  ayuda?: ReactNode
  alTerminar?: () => void
}

/** Base de definir, cambiar y restablecer: valida el mínimo de 8 antes de enviar (RNF-04). */
export function FormularioContrasenaNueva({ titulo, ruta, boton, pedirActual, avisoSoloLocal, avisoExito, ayuda, alTerminar }: Props) {
  const [actual, setActual] = useState('')
  const [nueva, setNueva] = useState('')
  const [errores, setErrores] = useState<Record<string, string>>({})
  const [aviso, setAviso] = useState<{ tipo: 'error' | 'exito'; texto: string }>()
  const claveNueva = pedirActual || ruta.endsWith('restablecer') ? 'nueva' : 'contrasena'

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setAviso(undefined)
    if (nueva.length < LONGITUD_MINIMA) {
      setErrores({ [claveNueva]: MENSAJE_LONGITUD })
      return
    }
    setErrores({})
    try {
      await pedir(ruta, { method: 'POST', body: pedirActual ? { actual, nueva } : { [claveNueva]: nueva } })
      setActual('')
      setNueva('')
      if (avisoExito) setAviso({ tipo: 'exito', texto: avisoExito })
      alTerminar?.()
    } catch (error) {
      if (error instanceof ErrorValidacion) setErrores(error.errores)
      else if (error instanceof ErrorApi && error.estado === 403 && avisoSoloLocal) setAviso({ tipo: 'error', texto: avisoSoloLocal })
      else setAviso({ tipo: 'error', texto: 'No se pudo guardar la contraseña. Probá de nuevo.' })
    }
  }

  return (
    <form className="tarjeta" onSubmit={enviar}>
      <h1>{titulo}</h1>
      {ayuda}
      {aviso && <p className={`aviso aviso-${aviso.tipo}`} role={aviso.tipo === 'error' ? 'alert' : 'status'}>{aviso.texto}</p>}
      <div className="fila">
        {pedirActual && (
          <CampoConError
            etiqueta="Contraseña actual"
            clave="actual"
            type="password"
            autoComplete="current-password"
            errores={errores}
            value={actual}
            onChange={(e) => setActual(e.target.value)}
          />
        )}
        <CampoConError
          etiqueta="Contraseña nueva"
          clave={claveNueva}
          type="password"
          autoComplete="new-password"
          errores={errores}
          value={nueva}
          onChange={(e) => setNueva(e.target.value)}
        />
        <button className="boton boton-primario" type="submit">
          {boton}
        </button>
      </div>
    </form>
  )
}

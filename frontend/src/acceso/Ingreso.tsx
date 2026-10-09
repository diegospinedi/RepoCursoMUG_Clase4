import { useState, type FormEvent } from 'react'
import { ErrorApi, pedir } from '../api/cliente'
import { CampoConError } from '../comunes/CampoConError'
import { horaArgentina } from './contrasena'

export function Ingreso({ alIngresar }: { alIngresar: () => void }) {
  const [contrasena, setContrasena] = useState('')
  const [aviso, setAviso] = useState<string>()
  const [enviando, setEnviando] = useState(false)

  async function ingresar(e: FormEvent) {
    e.preventDefault()
    setEnviando(true)
    setAviso(undefined)
    try {
      await pedir('/api/acceso/ingresar', { method: 'POST', body: { contrasena }, sinAvisoDeSesion: true })
      setContrasena('')
      alIngresar()
    } catch (error) {
      if (error instanceof ErrorApi && error.estado === 423)
        setAviso(`El acceso está bloqueado por 5 intentos fallidos. Se libera a las ${horaArgentina(String(error.datos.bloqueadoHasta))}.`)
      else if (error instanceof ErrorApi && error.estado === 401) setAviso('La contraseña es incorrecta.')
      else setAviso('No se pudo ingresar. Probá de nuevo.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <form className="tarjeta" onSubmit={ingresar}>
      <h1>Ingresar</h1>
      {aviso && <p className="aviso aviso-error" role="alert">{aviso}</p>}
      <div className="fila">
        <CampoConError
          etiqueta="Contraseña"
          clave="contrasena"
          type="password"
          autoComplete="current-password"
          value={contrasena}
          onChange={(e) => setContrasena(e.target.value)}
          autoFocus
        />
        <button className="boton boton-primario" type="submit" disabled={enviando}>
          Ingresar
        </button>
      </div>
    </form>
  )
}

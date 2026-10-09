import { useEffect, useState, type ReactNode } from 'react'
import { EVENTO_SESION_VENCIDA, pedir } from '../api/cliente'
import { DefinirContrasena } from './DefinirContrasena'
import { Ingreso } from './Ingreso'
import { RestablecerContrasena } from './RestablecerContrasena'

interface EstadoAcceso {
  definida: boolean
  sesionIniciada: boolean
}

type Vista = 'cargando' | 'definir' | 'ingresar' | 'restablecer' | 'adentro'

/**
 * Protege las pantallas con la sesión (RNF-04). Si la sesión vence con un formulario abierto, muestra el
 * ingreso en un diálogo sin desmontar las pantallas, para no perder lo cargado (FR-003a, research R5).
 */
export function SesionContexto({ children }: { children: ReactNode }) {
  const [vista, setVista] = useState<Vista>('cargando')
  const [vencida, setVencida] = useState(false)

  useEffect(() => {
    pedir<EstadoAcceso>('/api/acceso/estado').then((estado) =>
      setVista(!estado.definida ? 'definir' : estado.sesionIniciada ? 'adentro' : 'ingresar'),
    )
    const alVencer = () => setVencida(true)
    window.addEventListener(EVENTO_SESION_VENCIDA, alVencer)
    return () => window.removeEventListener(EVENTO_SESION_VENCIDA, alVencer)
  }, [])

  if (vista === 'cargando') return null
  if (vista === 'definir') return <DefinirContrasena alDefinir={() => setVista('ingresar')} />
  if (vista === 'restablecer')
    return (
      <>
        <RestablecerContrasena alRestablecer={() => setVista('ingresar')} />
        <button className="boton" type="button" onClick={() => setVista('ingresar')}>
          Volver al ingreso
        </button>
      </>
    )
  if (vista === 'ingresar')
    return (
      <>
        <Ingreso alIngresar={() => setVista('adentro')} />
        <button className="boton" type="button" onClick={() => setVista('restablecer')}>
          Olvidé la contraseña
        </button>
      </>
    )

  return (
    <>
      {children}
      {vencida && (
        <div className="dialogo-fondo">
          <div className="dialogo" role="dialog" aria-modal="true" aria-labelledby="titulo-sesion-vencida">
            <p id="titulo-sesion-vencida" className="aviso aviso-info">
              La sesión venció por inactividad. Ingresá la contraseña para seguir; lo que estabas cargando no se perdió.
            </p>
            <Ingreso alIngresar={() => setVencida(false)} />
          </div>
        </div>
      )}
    </>
  )
}

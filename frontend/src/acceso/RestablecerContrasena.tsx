import { soloPcLocal } from './contrasena'
import { FormularioContrasenaNueva } from './FormularioContrasenaNueva'

export function RestablecerContrasena({ alRestablecer }: { alRestablecer: () => void }) {
  return (
    <FormularioContrasenaNueva
      titulo="Restablecer la contraseña"
      ruta="/api/acceso/restablecer"
      boton="Restablecer contraseña"
      avisoSoloLocal={soloPcLocal('restablecer')}
      ayuda={<p>Si olvidaste la contraseña, definí una nueva sin conocer la anterior. Solo funciona en la computadora donde está instalado el sistema.</p>}
      alTerminar={alRestablecer}
    />
  )
}

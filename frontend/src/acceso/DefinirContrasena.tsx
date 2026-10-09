import { soloPcLocal } from './contrasena'
import { FormularioContrasenaNueva } from './FormularioContrasenaNueva'

export function DefinirContrasena({ alDefinir }: { alDefinir: () => void }) {
  return (
    <FormularioContrasenaNueva
      titulo="Definir la contraseña de acceso"
      ruta="/api/acceso/definir"
      boton="Definir contraseña"
      avisoSoloLocal={soloPcLocal('definir')}
      ayuda={<p>Es la primera vez que se usa el sistema. La contraseña tiene que tener al menos 8 caracteres.</p>}
      alTerminar={alDefinir}
    />
  )
}

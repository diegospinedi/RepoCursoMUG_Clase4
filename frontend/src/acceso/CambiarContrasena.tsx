import { FormularioContrasenaNueva } from './FormularioContrasenaNueva'

export function CambiarContrasena() {
  return (
    <FormularioContrasenaNueva
      titulo="Cambiar la contraseña"
      ruta="/api/acceso/cambiar"
      boton="Cambiar contraseña"
      pedirActual
      avisoExito="Contraseña cambiada. Las demás sesiones abiertas se cerraron."
    />
  )
}

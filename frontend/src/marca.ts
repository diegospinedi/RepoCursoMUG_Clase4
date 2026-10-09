// Única fuente de la marca: Marca/branding.json (skill frontend-design, regla 1).
import branding from '../../Marca/branding.json'
import logo from '../../Marca/logo.png'

export const marca = {
  logo,
  colorPrimario: branding.colores.primario,
  colorFondo: branding.colores.fondo,
}

/** Publica los colores de la marca como variables CSS; tokens.css deriva el resto de ellas. */
export function aplicarMarca(raiz: HTMLElement): void {
  raiz.style.setProperty('--marca-primario', marca.colorPrimario)
  raiz.style.setProperty('--marca-fondo', marca.colorFondo)
}

import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'
import { describe, expect, it } from 'vitest'

// Regla de la skill frontend-design: los colores salen de Marca/branding.json y de tokens.css.
const src = join(process.cwd(), 'src') // vitest corre desde frontend/
const permitido = join('estilos', 'tokens.css')

function archivos(dir: string): string[] {
  return readdirSync(dir).flatMap((nombre) => {
    const ruta = join(dir, nombre)
    if (statSync(ruta).isDirectory()) return archivos(ruta)
    return /\.(tsx?|css)$/.test(nombre) && !/\.test\.tsx?$/.test(nombre) ? [ruta] : []
  })
}

describe('colores', () => {
  it('ningún archivo fuera de tokens.css tiene colores hexadecimales escritos a mano', () => {
    const conColores = archivos(src)
      .filter((ruta) => relative(src, ruta) !== permitido)
      .filter((ruta) => /#[0-9a-fA-F]{3,8}\b/.test(readFileSync(ruta, 'utf8')))
      .map((ruta) => relative(src, ruta))
    expect(conColores).toEqual([])
  })
})

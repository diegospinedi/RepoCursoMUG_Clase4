import { useId, type InputHTMLAttributes } from 'react'

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  etiqueta: string
  /** Clave del campo en los errores de validación del servidor (`cliente.dni`). */
  clave: string
  errores?: Record<string, string>
}

/** Campo con el error junto a él y la acción correctiva (FR-041, RF-35). */
export function CampoConError({ etiqueta, clave, errores = {}, ...input }: Props) {
  const id = useId()
  const error = errores[clave]
  return (
    <div className="campo">
      <label htmlFor={id}>{etiqueta}</label>
      <input
        id={id}
        name={clave}
        aria-invalid={error ? 'true' : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        {...input}
      />
      {error && (
        <span id={`${id}-error`} className="campo-error">
          {error}
        </span>
      )}
    </div>
  )
}

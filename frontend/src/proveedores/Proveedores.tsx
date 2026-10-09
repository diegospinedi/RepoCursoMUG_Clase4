import { useEffect, useState, type FormEvent } from 'react'
import { ErrorValidacion, pedir } from '../api/cliente'
import type { Proveedor } from '../api/tipos'
import { CampoConError } from '../comunes/CampoConError'

export function Proveedores() {
  const [lista, setLista] = useState<Proveedor[]>([])
  const [nombre, setNombre] = useState('')
  const [errores, setErrores] = useState<Record<string, string>>({})
  const [editando, setEditando] = useState<Proveedor>()
  const [nombreEditado, setNombreEditado] = useState('')
  const [erroresEdicion, setErroresEdicion] = useState<Record<string, string>>({})

  const cargar = () => pedir<Proveedor[]>('/api/proveedores').then(setLista)
  useEffect(() => {
    void cargar()
  }, [])

  async function agregar(e: FormEvent) {
    e.preventDefault()
    try {
      await pedir('/api/proveedores', { method: 'POST', body: { nombre } })
      setNombre('')
      setErrores({})
      await cargar()
    } catch (error) {
      if (error instanceof ErrorValidacion) setErrores(error.errores)
      else throw error
    }
  }

  async function guardarNombre(e: FormEvent) {
    e.preventDefault()
    if (!editando) return
    try {
      await pedir(`/api/proveedores/${editando.id}`, { method: 'PUT', body: { nombre: nombreEditado } })
      setEditando(undefined)
      setErroresEdicion({})
      await cargar()
    } catch (error) {
      if (error instanceof ErrorValidacion) setErroresEdicion(error.errores)
      else throw error
    }
  }

  return (
    <>
      <form className="tarjeta" onSubmit={agregar}>
        <h1>Proveedores</h1>
        <div className="fila">
          <CampoConError etiqueta="Nombre del proveedor nuevo" clave="nombre" errores={errores} value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <button className="boton boton-primario" type="submit">
            Agregar proveedor
          </button>
        </div>
      </form>
      <div className="tarjeta">
        <table className="grilla">
          <thead>
            <tr>
              <th>Nombre</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {lista.map((p) => (
              <tr key={p.id}>
                <td>
                  {editando?.id === p.id ? (
                    <form className="fila" onSubmit={guardarNombre}>
                      <CampoConError etiqueta={`Nombre de ${p.nombre}`} clave="nombre" errores={erroresEdicion} value={nombreEditado} onChange={(e) => setNombreEditado(e.target.value)} />
                      <button className="boton boton-primario" type="submit">
                        Guardar nombre
                      </button>
                    </form>
                  ) : (
                    p.nombre
                  )}
                </td>
                <td>
                  {editando?.id !== p.id && (
                    <button
                      className="boton"
                      type="button"
                      onClick={() => {
                        setEditando(p)
                        setNombreEditado(p.nombre)
                        setErroresEdicion({})
                      }}
                    >
                      Cambiar nombre
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

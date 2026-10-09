import { useEffect, useState, type FormEvent } from 'react'
import { pedir } from '../api/cliente'
import type { Articulo, Proveedor } from '../api/tipos'
import { aCentavos, aTexto } from '../comunes/numeros'
import { formatearImporte } from '../calculos/lineas'
import { EditorArticulo } from './EditorArticulo'

export function Articulos() {
  const [proveedores, setProveedores] = useState<Proveedor[]>([])
  const [lista, setLista] = useState<Articulo[]>([])
  const [texto, setTexto] = useState('')
  const [editando, setEditando] = useState<Articulo | 'nuevo'>()

  const buscar = (t: string) => pedir<Articulo[]>(t ? `/api/articulos?texto=${encodeURIComponent(t)}` : '/api/articulos').then(setLista)

  useEffect(() => {
    void pedir<Proveedor[]>('/api/proveedores').then(setProveedores)
    void pedir<Articulo[]>('/api/articulos').then(setLista)
  }, [])

  function alBuscar(e: FormEvent) {
    e.preventDefault()
    void buscar(texto)
  }

  return (
    <>
      <form className="tarjeta" onSubmit={alBuscar}>
        <h1>Artículos</h1>
        <div className="fila">
          <div className="campo">
            <label htmlFor="buscar-articulo">Buscar por código, código en el proveedor o descripción</label>
            <input id="buscar-articulo" value={texto} onChange={(e) => setTexto(e.target.value)} />
          </div>
          <button className="boton boton-primario" type="submit">
            Buscar
          </button>
          <button className="boton" type="button" onClick={() => setEditando('nuevo')}>
            Nuevo artículo
          </button>
        </div>
      </form>
      {editando && (
        <EditorArticulo
          key={editando === 'nuevo' ? 'nuevo' : editando.codigo}
          articulo={editando === 'nuevo' ? undefined : editando}
          proveedores={proveedores}
          alGrabar={() => {
            setEditando(undefined)
            void buscar(texto)
          }}
        />
      )}
      <div className="tarjeta">
        <table className="grilla">
          <thead>
            <tr>
              <th className="numero">Código</th>
              <th>Proveedor</th>
              <th>Código en el proveedor</th>
              <th>Descripción</th>
              <th className="numero">Costo</th>
              <th className="numero">Margen</th>
              <th className="numero">Precio de venta</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {lista.map((a) => (
              <tr key={a.codigo}>
                <td className="numero">{a.codigo}</td>
                <td>{a.proveedor}</td>
                <td>{a.codigoProveedor}</td>
                <td>{a.descripcion}</td>
                <td className="numero">{formatearImporte(aCentavos(a.precioCosto))}</td>
                <td className="numero">{aTexto(a.margen)} %</td>
                <td className="numero">{formatearImporte(aCentavos(a.precioVenta))}</td>
                <td>
                  <button className="boton" type="button" onClick={() => setEditando(a)}>
                    Editar
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

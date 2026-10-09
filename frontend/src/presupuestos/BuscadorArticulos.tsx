import { useState, type FormEvent } from 'react'
import { pedir } from '../api/cliente'
import type { Articulo } from '../api/tipos'
import { formatearImporte } from '../calculos/lineas'
import { aCentavos } from '../comunes/numeros'

/** Busca en el catálogo por código o descripción (máximo 50) y agrega el elegido como línea (RF-11). */
export function BuscadorArticulos({ alElegir }: { alElegir: (articulo: Articulo) => void }) {
  const [texto, setTexto] = useState('')
  const [resultados, setResultados] = useState<Articulo[]>()

  async function buscar(e: FormEvent) {
    e.preventDefault()
    setResultados(await pedir<Articulo[]>(`/api/articulos?texto=${encodeURIComponent(texto)}`))
  }

  return (
    <div className="buscador">
      <form className="fila" onSubmit={buscar}>
        <div className="campo">
          <label htmlFor="buscar-linea">Buscar artículo por código o descripción</label>
          <input id="buscar-linea" value={texto} onChange={(e) => setTexto(e.target.value)} />
        </div>
        <button className="boton" type="submit">
          Buscar artículo
        </button>
      </form>
      {resultados?.length === 0 && <p className="aviso aviso-info">No se encontraron artículos.</p>}
      {resultados && resultados.length > 0 && (
        <table className="grilla">
          <thead>
            <tr>
              <th className="numero">Código</th>
              <th>Código en el proveedor</th>
              <th>Descripción</th>
              <th className="numero">Precio de venta</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {resultados.map((a) => (
              <tr key={a.codigo}>
                <td className="numero">{a.codigo}</td>
                <td>{a.codigoProveedor}</td>
                <td>{a.descripcion}</td>
                <td className="numero">{formatearImporte(aCentavos(a.precioVenta))}</td>
                <td>
                  <button className="boton" type="button" aria-label={`Agregar ${a.descripcion}`} onClick={() => alElegir(a)}>
                    Agregar
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

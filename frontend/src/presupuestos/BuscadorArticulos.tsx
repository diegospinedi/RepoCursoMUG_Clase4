import { useState } from 'react'
import { pedir } from '../api/cliente'
import type { Articulo } from '../api/tipos'
import { formatearImporte } from '../calculos/lineas'
import { aCentavos } from '../comunes/numeros'

/** Busca en el catálogo por código o descripción (máximo 50) y agrega el elegido como línea (RF-11). */
export function BuscadorArticulos({ alElegir }: { alElegir: (articulo: Articulo) => void }) {
  const [texto, setTexto] = useState('')
  const [resultados, setResultados] = useState<Articulo[]>()

  function elegir(a: Articulo) {
    alElegir(a)
    setResultados(undefined)
    setTexto('')
  }

  // Sin <form>: el buscador vive dentro del formulario del presupuesto y no tiene que enviarlo.
  async function buscar() {
    setResultados(await pedir<Articulo[]>(`/api/articulos?texto=${encodeURIComponent(texto)}`))
  }

  return (
    <div className="buscador">
      <div className="fila">
        <div className="campo">
          <label htmlFor="buscar-linea">Buscar artículo por código o descripción</label>
          <input
            id="buscar-linea"
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault()
                void buscar()
              }
            }}
          />
        </div>
        <button className="boton" type="button" onClick={() => void buscar()}>
          Buscar artículo
        </button>
      </div>
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
                  <button className="boton" type="button" aria-label={`Agregar ${a.descripcion}`} onClick={() => elegir(a)}>
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

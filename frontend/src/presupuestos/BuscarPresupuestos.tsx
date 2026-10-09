import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { pedir } from '../api/cliente'
import type { PresupuestoResumen } from '../api/tipos'
import { formatearImporte } from '../calculos/lineas'
import { formatearFecha } from '../comunes/fechas'
import { aCentavos, formatearDni } from '../comunes/numeros'

const FILTROS = [
  { clave: 'apellido', etiqueta: 'Apellido', tipo: 'text' },
  { clave: 'nombre', etiqueta: 'Nombre', tipo: 'text' },
  { clave: 'dni', etiqueta: 'DNI', tipo: 'text' },
  { clave: 'desde', etiqueta: 'Desde', tipo: 'date' },
  { clave: 'hasta', etiqueta: 'Hasta', tipo: 'date' },
] as const

type Filtros = Record<(typeof FILTROS)[number]['clave'], string>

/** Búsqueda por fecha y datos del cliente, con los filtros combinados (RF-03, RF-81 a RF-83, RF-88). */
export function BuscarPresupuestos() {
  const [filtros, setFiltros] = useState<Filtros>({ apellido: '', nombre: '', dni: '', desde: '', hasta: '' })
  const [resultados, setResultados] = useState<PresupuestoResumen[]>()

  async function buscar(f: Filtros) {
    const params = new URLSearchParams(Object.entries(f).filter(([, v]) => v.trim() !== ''))
    const consulta = params.toString()
    setResultados(await pedir<PresupuestoResumen[]>(`/api/presupuestos${consulta ? `?${consulta}` : ''}`))
  }

  useEffect(() => {
    pedir<PresupuestoResumen[]>('/api/presupuestos').then(setResultados)
  }, [])

  function alBuscar(e: FormEvent) {
    e.preventDefault()
    void buscar(filtros)
  }

  return (
    <>
      <form className="tarjeta" onSubmit={alBuscar}>
        <div className="fila encabezado-seccion">
          <h1>Presupuestos</h1>
          <Link className="boton boton-primario" to="/presupuestos/nuevo">
            Nuevo presupuesto
          </Link>
        </div>
        <div className="fila">
          {FILTROS.map((f) => (
            <div className="campo" key={f.clave}>
              <label htmlFor={`filtro-${f.clave}`}>{f.etiqueta}</label>
              <input
                id={`filtro-${f.clave}`}
                type={f.tipo}
                value={filtros[f.clave]}
                onChange={(e) => setFiltros((x) => ({ ...x, [f.clave]: e.target.value }))}
              />
            </div>
          ))}
          <button className="boton boton-primario" type="submit">
            Buscar
          </button>
        </div>
      </form>

      {resultados && (
        <div className="tarjeta">
          {resultados.length === 0 ? (
            <p className="aviso aviso-info">No hay presupuestos que cumplan todos los filtros.</p>
          ) : (
            <table className="grilla">
              <thead>
                <tr>
                  <th className="numero">Número</th>
                  <th>Fecha</th>
                  <th>Cliente</th>
                  <th>DNI</th>
                  <th>Estado</th>
                  <th className="numero">Total</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {resultados.map((p) => (
                  <tr key={p.id}>
                    <td className="numero">{p.numero}</td>
                    <td>{formatearFecha(p.fecha)}</td>
                    <td>{`${p.apellido}, ${p.nombre}`}</td>
                    <td>{formatearDni(p.dni)}</td>
                    <td>
                      <span className={`etiqueta ${p.estado === 'Final' ? 'etiqueta-final' : 'etiqueta-borrador'}`}>{p.estado}</span>
                    </td>
                    <td className="numero">{formatearImporte(aCentavos(p.total))}</td>
                    <td>
                      <Link className="boton" to={`/presupuestos/${p.id}`} aria-label={`Abrir presupuesto ${p.numero}`}>
                        Abrir
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          {resultados.length >= 200 && <p className="aviso aviso-info">Se muestran los 200 más recientes. Agregá filtros para encontrar otros.</p>}
        </div>
      )}
    </>
  )
}

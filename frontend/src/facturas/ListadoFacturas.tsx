import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { pedir } from '../api/cliente'
import type { FacturaResumen } from '../api/tipos'
import { formatearImporte } from '../calculos/lineas'
import { formatearFecha } from '../comunes/fechas'
import { aCentavos, numeroComprobante } from '../comunes/numeros'

const TEXTOS = [
  { clave: 'apellido', etiqueta: 'Apellido', tipo: 'text' },
  { clave: 'nombre', etiqueta: 'Nombre', tipo: 'text' },
  { clave: 'dni', etiqueta: 'DNI', tipo: 'text' },
  { clave: 'numero', etiqueta: 'Número', tipo: 'text' },
  { clave: 'desde', etiqueta: 'Desde', tipo: 'date' },
  { clave: 'hasta', etiqueta: 'Hasta', tipo: 'date' },
] as const

const ESTADOS = ['Autorizada', 'Pendiente', 'Bloqueada', 'Descartada']

const CLASE_ESTADO: Record<string, string> = {
  Autorizada: 'etiqueta-final',
  Pendiente: 'etiqueta-borrador',
  Bloqueada: 'etiqueta-error',
  Descartada: 'etiqueta-error',
}

type Filtros = Record<'estado' | (typeof TEXTOS)[number]['clave'], string>

/** Facturas en todos sus estados, buscables por estado, fechas, número y datos del cliente (FR-039). */
export function ListadoFacturas() {
  const [filtros, setFiltros] = useState<Filtros>({ estado: '', apellido: '', nombre: '', dni: '', numero: '', desde: '', hasta: '' })
  const [resultados, setResultados] = useState<FacturaResumen[]>()

  useEffect(() => {
    pedir<FacturaResumen[]>('/api/facturas').then(setResultados)
  }, [])

  async function buscar(e: FormEvent) {
    e.preventDefault()
    const consulta = new URLSearchParams(Object.entries(filtros).filter(([, v]) => v.trim() !== '')).toString()
    setResultados(await pedir<FacturaResumen[]>(`/api/facturas${consulta ? `?${consulta}` : ''}`))
  }

  return (
    <>
      <form className="tarjeta" onSubmit={buscar}>
        <h1>Facturas</h1>
        <div className="fila">
          <div className="campo">
            <label htmlFor="filtro-estado">Estado</label>
            <select id="filtro-estado" value={filtros.estado} onChange={(e) => setFiltros((f) => ({ ...f, estado: e.target.value }))}>
              <option value="">Todos</option>
              {ESTADOS.map((e) => (
                <option key={e} value={e}>
                  {e}
                </option>
              ))}
            </select>
          </div>
          {TEXTOS.map((t) => (
            <div className="campo" key={t.clave}>
              <label htmlFor={`factura-${t.clave}`}>{t.etiqueta}</label>
              <input
                id={`factura-${t.clave}`}
                type={t.tipo}
                value={filtros[t.clave]}
                onChange={(e) => setFiltros((f) => ({ ...f, [t.clave]: e.target.value }))}
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
            <p className="aviso aviso-info">No hay facturas que cumplan todos los filtros.</p>
          ) : (
            <table className="grilla">
              <thead>
                <tr>
                  <th>Comprobante</th>
                  <th>Fecha</th>
                  <th>Cliente</th>
                  <th>Estado</th>
                  <th className="numero">Total</th>
                  <th className="numero">Presupuesto</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {resultados.map((f) => {
                  const numero = numeroComprobante(f.puntoVenta, f.numero)
                  return (
                    <tr key={f.id}>
                      <td>{`${f.tipo} ${numero}`}</td>
                      <td>{formatearFecha(f.fecha)}</td>
                      <td>{`${f.apellido}, ${f.nombre}`}</td>
                      <td>
                        <span className={`etiqueta ${CLASE_ESTADO[f.estado]}`}>{f.estado}</span>
                      </td>
                      <td className="numero">{formatearImporte(aCentavos(f.total))}</td>
                      <td className="numero">{f.presupuestoNumero}</td>
                      <td>
                        <Link className="boton" to={`/facturas/${f.id}`} aria-label={`Ver factura ${numero}`}>
                          Ver
                        </Link>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
          {resultados.length >= 200 && <p className="aviso aviso-info">Se muestran las 200 más recientes. Agregá filtros para encontrar otras.</p>}
        </div>
      )}
    </>
  )
}

import { useEffect, useState, type FormEvent } from 'react'
import { ErrorApi, pedir } from '../api/cliente'
import type { ImportacionHistorial, Proveedor, ResumenImportacion } from '../api/tipos'
import { formatearFecha } from '../comunes/fechas'

/** Importa la planilla de precios del proveedor elegido (US3, contracts/planilla-proveedor.md). */
export function Importacion() {
  const [proveedores, setProveedores] = useState<Proveedor[]>([])
  const [proveedorId, setProveedorId] = useState('')
  const [archivo, setArchivo] = useState<File>()
  const [resumen, setResumen] = useState<ResumenImportacion>()
  const [error, setError] = useState<string>()
  const [historial, setHistorial] = useState<ImportacionHistorial[]>([])
  const [importando, setImportando] = useState(false)

  useEffect(() => {
    pedir<Proveedor[]>('/api/proveedores').then((lista) => {
      setProveedores(lista)
      if (lista.length) setProveedorId(String(lista[0].id))
    })
  }, [])

  useEffect(() => {
    if (proveedorId) void pedir<ImportacionHistorial[]>(`/api/proveedores/${proveedorId}/importaciones`).then(setHistorial)
  }, [proveedorId, resumen])

  async function importar(e: FormEvent) {
    e.preventDefault()
    if (!archivo) {
      setError('Elegí la planilla a importar.')
      return
    }
    const datos = new FormData()
    datos.append('archivo', archivo)
    setImportando(true)
    setError(undefined)
    setResumen(undefined)
    try {
      setResumen(await pedir<ResumenImportacion>(`/api/proveedores/${proveedorId}/importaciones`, { method: 'POST', body: datos }))
    } catch (err) {
      if (err instanceof ErrorApi && err.titulo) setError([err.titulo, err.datos.detail].filter(Boolean).join(' '))
      else setError('No se pudo importar la planilla. Volvé a intentar.')
    } finally {
      setImportando(false)
    }
  }

  return (
    <>
      <form className="tarjeta" onSubmit={importar}>
        <h1>Importar precios</h1>
        <p>
          La planilla tiene que tener, en la primera fila, las columnas <strong>Código en el proveedor</strong>,{' '}
          <strong>Descripción</strong> y <strong>Precio de Costo</strong>. Los precios tienen que estar en celdas numéricas.
        </p>
        {error && <p className="aviso aviso-error" role="alert">{error}</p>}
        <div className="fila">
          <div className="campo">
            <label htmlFor="proveedor-importacion">Proveedor</label>
            <select id="proveedor-importacion" value={proveedorId} onChange={(e) => setProveedorId(e.target.value)}>
              {proveedores.map((p) => (
                <option key={p.id} value={String(p.id)}>
                  {p.nombre}
                </option>
              ))}
            </select>
          </div>
          <div className="campo">
            <label htmlFor="archivo-importacion">Planilla (.xlsx)</label>
            <input
              id="archivo-importacion"
              type="file"
              accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              onChange={(e) => setArchivo(e.target.files?.[0])}
            />
          </div>
          <button className="boton boton-primario" type="submit" disabled={importando || !proveedorId}>
            Importar
          </button>
        </div>
      </form>

      {resumen && (
        <div className="tarjeta">
          <p className="aviso aviso-exito" role="status">
            Se crearon {resumen.creados} artículos y se actualizaron {resumen.actualizados}.
          </p>
          {resumen.filas.length > 0 && (
            <table className="grilla">
              <caption>Filas para revisar</caption>
              <thead>
                <tr>
                  <th className="numero">Fila</th>
                  <th>Código</th>
                  <th>Motivo</th>
                </tr>
              </thead>
              <tbody>
                {resumen.filas.map((f) => (
                  <tr key={f.numeroFila}>
                    <td className="numero">{f.numeroFila}</td>
                    <td>{f.codigoProveedor}</td>
                    <td>
                      <span className={`etiqueta ${f.resultado === 'NoProcesada' ? 'etiqueta-error' : 'etiqueta-borrador'}`}>{f.razon}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      <div className="tarjeta">
        <h2>Importaciones anteriores</h2>
        <table className="grilla">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Archivo</th>
              <th className="numero">Creados</th>
              <th className="numero">Actualizados</th>
              <th className="numero">No procesados</th>
            </tr>
          </thead>
          <tbody>
            {historial.map((h) => (
              <tr key={h.id}>
                <td>{formatearFecha(h.fecha)}</td>
                <td>{h.nombreArchivo}</td>
                <td className="numero">{h.creados}</td>
                <td className="numero">{h.actualizados}</td>
                <td className="numero">{h.noProcesados}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

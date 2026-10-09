import { useEffect, useState, type FormEvent } from 'react'
import { ErrorValidacion, pedir } from '../api/cliente'
import type { CondicionFiscal, Configuracion as Datos } from '../api/tipos'
import { CampoConError } from '../comunes/CampoConError'
import { aNumero, aTexto } from '../comunes/numeros'

const ALICUOTAS = [0, 2.5, 5, 10.5, 21, 27]

/**
 * Parámetros de negocio (RF-63). El certificado y el punto de venta de ARCA no se muestran ni se editan
 * acá: se configuran fuera de la aplicación (FR-008).
 */
export function Configuracion() {
  const [alicuota, setAlicuota] = useState('21')
  const [condicion, setCondicion] = useState<CondicionFiscal>('ResponsableInscripto')
  const [tope, setTope] = useState('')
  const [multiplo, setMultiplo] = useState('')
  const [margen, setMargen] = useState('')
  const [errores, setErrores] = useState<Record<string, string>>({})
  const [aviso, setAviso] = useState<string>()
  const [cargada, setCargada] = useState(false)

  useEffect(() => {
    pedir<Datos>('/api/configuracion').then((c) => {
      setAlicuota(String(c.alicuotaIva))
      setCondicion(c.condicionFiscal)
      setTope(aTexto(c.topeIdentificacion))
      setMultiplo(aTexto(c.multiploRedondeo))
      setMargen(aTexto(c.margenPredeterminado))
      setCargada(true)
    })
  }, [])

  async function grabar(e: FormEvent) {
    e.preventDefault()
    setAviso(undefined)
    const numeros = { topeIdentificacion: aNumero(tope), multiploRedondeo: aNumero(multiplo), margenPredeterminado: aNumero(margen) }
    const invalidos = Object.entries(numeros).filter(([, v]) => Number.isNaN(v))
    if (invalidos.length) {
      setErrores(Object.fromEntries(invalidos.map(([k]) => [k, 'Ingresá un número, con coma para los decimales.'])))
      return
    }
    try {
      const c = await pedir<Datos>('/api/configuracion', {
        method: 'PUT',
        body: { alicuotaIva: Number(alicuota), condicionFiscal: condicion, ...numeros },
      })
      setErrores({})
      const recalculados = c.articulosRecalculados ?? 0
      setAviso(`Configuración grabada.${recalculados > 0 ? ` Se recalcularon ${recalculados} artículos.` : ''}`)
    } catch (error) {
      if (error instanceof ErrorValidacion) setErrores(error.errores)
      else throw error
    }
  }

  if (!cargada) return null
  return (
    <form className="tarjeta" onSubmit={grabar}>
      <h1>Configuración</h1>
      {aviso && <p className="aviso aviso-exito" role="status">{aviso}</p>}
      <div className="fila">
        <div className="campo">
          <label htmlFor="alicuota">Alícuota de IVA</label>
          <select id="alicuota" value={alicuota} onChange={(e) => setAlicuota(e.target.value)} aria-invalid={errores.alicuotaIva ? 'true' : undefined}>
            {ALICUOTAS.map((a) => (
              <option key={a} value={String(a)}>
                {aTexto(a)} %
              </option>
            ))}
          </select>
          {errores.alicuotaIva && <span className="campo-error">{errores.alicuotaIva}</span>}
        </div>
        <div className="campo">
          <label htmlFor="condicion">Condición fiscal</label>
          <select id="condicion" value={condicion} onChange={(e) => setCondicion(e.target.value as CondicionFiscal)}>
            <option value="ResponsableInscripto">Responsable Inscripto (Factura B)</option>
            <option value="Monotributo">Monotributo (Factura C)</option>
          </select>
        </div>
        <CampoConError etiqueta="Tope de identificación" clave="topeIdentificacion" errores={errores} inputMode="decimal" value={tope} onChange={(e) => setTope(e.target.value)} />
        <CampoConError etiqueta="Múltiplo de redondeo" clave="multiploRedondeo" errores={errores} inputMode="decimal" value={multiplo} onChange={(e) => setMultiplo(e.target.value)} />
        <CampoConError
          etiqueta="Margen predeterminado para artículos nuevos (%)"
          clave="margenPredeterminado"
          errores={errores}
          inputMode="decimal"
          value={margen}
          onChange={(e) => setMargen(e.target.value)}
        />
      </div>
      <p>
        <button className="boton boton-primario" type="submit">
          Grabar configuración
        </button>
      </p>
    </form>
  )
}

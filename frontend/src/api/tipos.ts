// Tipos de contracts/api-http.md.

export type CondicionFiscal = 'ResponsableInscripto' | 'Monotributo'

export interface Configuracion {
  alicuotaIva: number
  condicionFiscal: CondicionFiscal
  topeIdentificacion: number
  multiploRedondeo: number
  margenPredeterminado: number | null
  articulosRecalculados?: number
}

export interface Proveedor {
  id: number
  nombre: string
}

export interface Articulo {
  codigo: number
  proveedorId: number
  proveedor: string
  codigoProveedor: string
  descripcion: string
  precioCosto: number
  margen: number
  precioVenta: number
}

export interface FilaInformada {
  numeroFila: number
  codigoProveedor: string
  resultado: 'NoProcesada' | 'ActualizadoPrecioNegativoOCero' | 'CreadoPrecioNegativoOCero'
  razon: string
}

export interface ResumenImportacion {
  id: number
  creados: number
  actualizados: number
  filas: FilaInformada[]
}

export interface ImportacionHistorial {
  id: number
  fecha: string
  nombreArchivo: string
  creados: number
  actualizados: number
  noProcesados: number
}

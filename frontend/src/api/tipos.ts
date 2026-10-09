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

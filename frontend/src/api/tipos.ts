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

export type EstadoPresupuesto = 'Borrador' | 'Final'

export interface ClientePresupuesto {
  apellido: string
  nombre: string
  dni: string
  domicilio: string | null
  email: string | null
  telefono: string | null
}

export interface LineaPresupuesto {
  id: number
  orden: number
  articuloCodigo: number
  descripcion: string
  precioUnitario: number
  cantidad: number
  descuento: number
  precioConDescuento: number
  precioFinal: number
}

export type EstadoEmision = 'Pendiente' | 'Bloqueada' | 'Autorizada' | 'Descartada'

export interface Presupuesto {
  id: number
  numero: number
  fecha: string
  estado: EstadoPresupuesto
  cliente: ClientePresupuesto
  lineas: LineaPresupuesto[]
  total: number
  emision?: { facturaId: number; estado: EstadoEmision } | null
}

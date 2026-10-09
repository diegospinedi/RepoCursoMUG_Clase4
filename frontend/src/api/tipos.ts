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

export interface PresupuestoResumen {
  id: number
  numero: number
  fecha: string
  estado: EstadoPresupuesto
  apellido: string
  nombre: string
  dni: string
  total: number
}

export interface FacturaEmitida {
  facturaId: number
  estado: EstadoEmision
  tipo: 'B' | 'C'
  puntoVenta: number
  numero: number
  cae: string | null
  vencimientoCae: string | null
}

export interface FacturaDetalle {
  id: number
  estado: EstadoEmision
  tipo: 'B' | 'C'
  puntoVenta: number
  numero: number
  fecha: string
  receptorDocTipo: number
  receptorDocNro: string
  total: number
  neto: number | null
  iva: number | null
  alicuotaIva: number | null
  cae: string | null
  vencimientoCae: string | null
  presupuesto: { id: number; numero: number }
  apellido: string
  nombre: string
  dni: string
  lineas: { descripcion: string; cantidad: number; precioFinal: number }[]
}

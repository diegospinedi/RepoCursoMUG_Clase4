# Data Model: Presupuestos y Facturación Electrónica a Consumidor Final

**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md) | **Date**: 2026-10-09

Convenciones (research R2): los importes se guardan en centavos (`INTEGER`) y los porcentajes en
centésimos (`INTEGER`); en el dominio son `decimal`. Todas las fechas se guardan en hora local de
Argentina. "Requerido" significa NOT NULL.

## Acceso

Fila única (Id = 1).

| Campo | Tipo | Reglas |
|---|---|---|
| HashContrasena | texto | null hasta la primera definición; PBKDF2 con sal (FR-004) |
| IntentosFallidos | entero | 0–5; vuelve a 0 con un ingreso correcto |
| BloqueadoHasta | fecha-hora | null o ahora + 5 minutos tras el 5.º fallo (FR-002) |
| SelloSeguridad | texto | cambia al cambiar o restablecer la contraseña; invalida las demás sesiones (FR-005c) |

## Configuracion

Fila única (Id = 1).

| Campo | Tipo | Reglas |
|---|---|---|
| AlicuotaIva | porcentaje | requerido; solo 0; 2,5; 5; 10,5; 21 o 27 (FR-007a) |
| CondicionFiscal | enum | requerido; `ResponsableInscripto` o `Monotributo` |
| TopeIdentificacion | importe | requerido; > 0 y ≤ 999.999.999,99; inicial 10.000.000,00 |
| MultiploRedondeo | importe | requerido; 0,01 a 1.000 (FR-007, R16); inicial 0,01 |
| MargenPredeterminado | porcentaje | null (sin configurar) o 0 a 1000 (FR-006, FR-045) |

Al grabar un cambio de `MultiploRedondeo` se recalculan todos los artículos (FR-009). Los cambios de
alícuota y condición fiscal no tocan precios.

## Proveedor

| Campo | Tipo | Reglas |
|---|---|---|
| Id | entero | autonumérico |
| Nombre | texto | requerido; 1–100 caracteres; único sin distinguir mayúsculas (FR-011a) |

Sin baja (no hay bajas de artículos ni de proveedores en esta versión).

## Articulo

| Campo | Tipo | Reglas |
|---|---|---|
| Codigo | entero | autonumérico; es el "Código" visible (RF-20) |
| ProveedorId | FK Proveedor | requerido |
| CodigoProveedor | texto | requerido; 1–50 caracteres; único por `(ProveedorId, CodigoProveedor)` con comparación binaria, sensible a mayúsculas y espacios (FR-011, FR-045a) |
| Descripcion | texto | requerido; 1–200 caracteres |
| PrecioCosto | importe | requerido; 2 decimales; admite negativo o cero solo si viene de una importación (FR-047); a mano, ≥ 0 |
| Margen | porcentaje | requerido; 0 a 1000, hasta 2 decimales (FR-015) |
| PrecioVenta | importe | requerido; calculado y redondeado hacia +∞ al múltiplo (FR-012, FR-013); de solo lectura |
| DescripcionBusqueda | texto | derivado: minúsculas, sin acentos |

Siempre activo: no hay baja ni desactivación.

## Importacion

| Campo | Tipo | Reglas |
|---|---|---|
| Id | entero | autonumérico |
| ProveedorId | FK Proveedor | requerido |
| Fecha | fecha-hora | requerido |
| NombreArchivo | texto | requerido |
| Creados, Actualizados, NoProcesados | entero | ≥ 0 |

**FilaImportacion** (solo filas no procesadas o con precio negativo o cero): ImportacionId, NumeroFila
(número de fila de la planilla), CodigoProveedor (texto tal cual, puede estar vacío), Resultado
(`NoProcesada`, `ActualizadoPrecioNegativoOCero` o `CreadoPrecioNegativoOCero`) y Razon ("precio
inválido", "falta la descripción", "falta el código", "código repetido en la planilla", "actualizado con
precio negativo o cero" o "creado con precio negativo o cero").

Una planilla rechazada por formato o por límites no genera `Importacion` (no cambia nada).

## Presupuesto

| Campo | Tipo | Reglas |
|---|---|---|
| Id | entero | autonumérico interno |
| Numero | entero | requerido; único; último + 1, empezando en 1; asignado en `BEGIN IMMEDIATE` (FR-017) |
| Fecha | fecha | requerido; fecha de la primera grabación |
| Estado | enum | `Borrador` o `Final` |
| Apellido, Nombre | texto | requeridos; 1–100 caracteres |
| Dni | texto | requerido; 7 u 8 dígitos, se admiten puntos al cargarlo y se guarda solo con dígitos (CHK006) |
| Domicilio | texto | opcional; hasta 200 caracteres |
| Email | texto | opcional; formato de email si se carga |
| Telefono | texto | opcional; hasta 30 caracteres |
| Total | importe | suma de los precios finales redondeados (FR-021) |
| ApellidoBusqueda, NombreBusqueda | texto | derivados: minúsculas, sin acentos |

Debe tener al menos una línea para grabarse.

**Transiciones**: (nuevo) → `Borrador` al grabar → `Final` al grabar con estado Final. `Final` no cambia
ni vuelve a `Borrador`; los triggers bloquean `UPDATE` y `DELETE` (FR-018, R13). No hay eliminación de
presupuestos.

## LineaPresupuesto

| Campo | Tipo | Reglas |
|---|---|---|
| PresupuestoId | FK | requerido |
| Orden | entero | posición en la grilla |
| ArticuloCodigo | entero | código del artículo seleccionado |
| Descripcion | texto | copiada del catálogo en líneas nuevas; conservada en las existentes (FR-024) |
| PrecioUnitario | importe | copiado del precio de venta en líneas nuevas; ≥ 0, se admite 0 (FR-023); no editable |
| Cantidad | entero | 1 a 9.999 (FR-020, CHK034) |
| Descuento | porcentaje | 0 a 100, hasta 2 decimales |
| PrecioConDescuento | importe | round(PrecioUnitario × (1 − Descuento/100), 2) |
| PrecioFinal | importe | round(PrecioConDescuento × Cantidad, 2) |

## Factura (emisión)

| Campo | Tipo | Reglas |
|---|---|---|
| Id | entero | autonumérico |
| PresupuestoId | FK | requerido; único entre las emisiones Pendiente, Bloqueada y Autorizada (índice parcial, R10) |
| Estado | enum | `Pendiente`, `Bloqueada`, `Autorizada` o `Descartada` |
| Tipo | enum | `B` (6) o `C` (11), según la condición fiscal al registrar |
| PuntoVenta | entero | de `Arca:PuntoVenta` |
| Numero | entero | último autorizado + 1, registrado antes de enviar (FR-036) |
| Fecha | fecha | la del último envío a ARCA |
| ReceptorDocTipo | entero | 99 (sin identificar) o 96 (DNI) (FR-029) |
| ReceptorDocNro | texto | "0" o el DNI |
| Total | importe | igual al del presupuesto |
| Neto, Iva | importe | solo Factura B: neto = round(total / (1 + alícuota/100), 2), IVA = total − neto; null en C |
| AlicuotaIva | porcentaje | la vigente al registrar; null en C |
| Cae | texto | requerido si Autorizada |
| VencimientoCae | fecha | requerido si Autorizada |
| NumeroBusqueda | texto | derivado: punto de venta y número solo con dígitos |
| Apellido, Nombre, Dni (búsqueda) | texto | copiados del presupuesto al registrar, normalizados |

Índice único: `(PuntoVenta, Tipo, Numero)`. Las líneas del PDF se toman del presupuesto de origen, que es
inmutable.

**Transiciones** (FR-038, FR-038b):

```text
(Facturar) ──registra──▶ Pendiente ──ARCA autoriza──▶ Autorizada (final)
                            │  ├──ARCA rechaza──▶ (se elimina: no registrada)
                            │  └──sin respuesta──▶ Pendiente (Reintentar)
Reintentar: consulta del número ──mismo total──▶ Autorizada (CAE recuperado)
                                 ──no existe──▶ envío normal
                                 ──otro total──▶ Bloqueada ──confirmación──▶ Descartada (final)
```

Los triggers impiden `UPDATE` y `DELETE` sobre Autorizada y Descartada (FR-031).

# Contrato HTTP: API de Óptica Sistema

**Feature**: [spec.md](../spec.md) | **Data model**: [data-model.md](../data-model.md)

Contrato entre el frontend y el backend (principio V de la constitución). Base: `/api`. JSON en
`camelCase`. Importes como número con 2 decimales (`1815.00`); porcentajes como número (`21`, `10.5`).
Fechas `YYYY-MM-DD`.

## Convenciones comunes

- **Autenticación**: cookie de sesión. Sin sesión, todo endpoint salvo los de `/api/acceso` responde
  `401` sin cuerpo con datos (FR-005).
- **Validación**: `400` con `ValidationProblemDetails`. `errors` usa la ruta del campo como clave
  (`cliente.dni`, `lineas[0].cantidad`, `alicuotaIva`) y mensajes que dicen cómo corregir (FR-041).
  Mensajes fijados por la spec: "Ingresá el DNI del cliente", "La cantidad debe ser un número entero mayor
  a 0", "El descuento debe estar entre 0 y 100", "El precio unitario no puede ser negativo".
- **Regla de negocio violada** (presupuesto Final, factura cerrada, emisión en curso): `409` con
  `ProblemDetails` (`title` legible, `type` con un código estable como `presupuesto-cerrado`).
- **No encontrado**: `404`. **Solo local**: `403` con `type` = `solo-pc-local`.

## Acceso

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `GET /api/acceso/estado` | — | `200 { definida: bool, sesionIniciada: bool, bloqueadoHasta: fecha-hora? }` |
| `POST /api/acceso/definir` | `{ contrasena }` | `204`; `400` si tiene menos de 8 caracteres; `403` si no es local; `409` si ya está definida |
| `POST /api/acceso/ingresar` | `{ contrasena }` | `204` + cookie; `401` si es incorrecta; `423 { bloqueadoHasta }` si está bloqueado |
| `POST /api/acceso/salir` | — | `204` |
| `POST /api/acceso/cambiar` | `{ actual, nueva }` | `204` (cierra las otras sesiones); `400` si la actual es incorrecta o la nueva es corta |
| `POST /api/acceso/restablecer` | `{ nueva }` | `204`; `403` si no es local; `400` si es corta |

## Configuración

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `GET /api/configuracion` | — | `200 { alicuotaIva, condicionFiscal, topeIdentificacion, multiploRedondeo, margenPredeterminado? }` |
| `PUT /api/configuracion` | el mismo objeto | `200` con el objeto grabado y `{ articulosRecalculados: n }`; `400` por campo (FR-007, FR-007a, R16) |

`condicionFiscal`: `"ResponsableInscripto"` o `"Monotributo"`. Nunca se exponen el certificado ni el
punto de venta (FR-008).

## Proveedores

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `GET /api/proveedores` | — | `200 [{ id, nombre }]` |
| `POST /api/proveedores` | `{ nombre }` | `201`; `400` si está vacío o repetido |
| `PUT /api/proveedores/{id}` | `{ nombre }` | `200`; `400`; `404` |

## Artículos

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `GET /api/articulos?texto=&proveedorId=` | — | `200 [{ codigo, proveedorId, proveedor, codigoProveedor, descripcion, precioCosto, margen, precioVenta }]`; `texto` busca en código, código en el proveedor y descripción; máximo 50 resultados |
| `GET /api/articulos/{codigo}` | — | `200` artículo; `404` |
| `POST /api/articulos` | `{ proveedorId, codigoProveedor, descripcion, precioCosto, margen }` | `201` con `precioVenta` calculado; `400` (código repetido en el proveedor, margen fuera de rango, costo negativo) |
| `PUT /api/articulos/{codigo}` | el mismo cuerpo | `200` con `precioVenta` recalculado; `400`; `404` |

`precioVenta` en el cuerpo se ignora: es de solo lectura (FR-014).

## Importación

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `POST /api/proveedores/{id}/importaciones` | `multipart/form-data` con `archivo` (.xlsx) | `200` con el resumen; `400 { type: "formato-planilla" }` si el formato o los límites no son válidos (no cambia nada); `409 { type: "margen-sin-configurar" }`; `409 { type: "importacion-en-curso" }` |
| `GET /api/proveedores/{id}/importaciones` | — | `200 [{ id, fecha, nombreArchivo, creados, actualizados, noProcesados }]` |
| `GET /api/importaciones/{id}` | — | `200` resumen |

Resumen: `{ id, creados, actualizados, filas: [{ numeroFila, codigoProveedor, resultado, razon }] }`.
Formato de la planilla: [planilla-proveedor.md](planilla-proveedor.md).

## Presupuestos

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `GET /api/presupuestos?apellido=&nombre=&dni=&desde=&hasta=` | — | `200 [{ id, numero, fecha, estado, apellido, nombre, dni, total, emision? }]`; filtros combinados con "Y" (FR-026) |
| `GET /api/presupuestos/{id}` | — | `200` presupuesto completo con líneas y `emision: { facturaId, estado }?` |
| `POST /api/presupuestos` | ver abajo | `201 { id, numero, estado }`; `400` |
| `PUT /api/presupuestos/{id}` | ver abajo | `200`; `400`; `409 presupuesto-cerrado` si está en Final |
| `GET /api/presupuestos/{id}/pdf` | — | `200 application/pdf`; `409 presupuesto-borrador` si está en Borrador (FR-025) |

Cuerpo de grabación:

```json
{
  "estado": "Borrador",
  "cliente": { "apellido": "", "nombre": "", "dni": "", "domicilio": null, "email": null, "telefono": null },
  "lineas": [ { "id": null, "articuloCodigo": 12, "cantidad": 3, "descuento": 10 } ]
}
```

La pantalla envía solo artículo, cantidad y descuento (`AGENTS.md`). En líneas nuevas (`id` null) el
servidor toma descripción y precio del catálogo; en las existentes conserva los grabados (FR-019,
FR-024). La respuesta devuelve las líneas con los importes calculados por el servidor.

## Facturas

| Método y ruta | Cuerpo | Respuestas |
|---|---|---|
| `POST /api/presupuestos/{id}/factura` | — | `201 { facturaId, estado: "Autorizada", tipo, puntoVenta, numero, cae, vencimientoCae }`; `409 presupuesto-borrador`; `409 ya-facturado`; `502 { type: "arca-rechazo", codigo, descripcion }` (no registra nada, FR-034); `504 { type: "arca-sin-respuesta", facturaId }` (queda Pendiente, FR-035) |
| `POST /api/facturas/{id}/reintentar` | — | `200` Autorizada (recuperada o emitida); `409 { type: "emision-bloqueada" }` si aparece otro total; `502` o `504` como arriba; `409` si no está Pendiente |
| `POST /api/facturas/{id}/confirmar-revision` | — | `200 { estado: "Descartada" }`; `409` si no está Bloqueada (FR-038b) |
| `GET /api/facturas?estado=&apellido=&nombre=&dni=&numero=&desde=&hasta=` | — | `200 [{ id, estado, tipo, puntoVenta, numero, fecha, apellido, nombre, total, presupuestoNumero }]` (FR-039) |
| `GET /api/facturas/{id}` | — | `200` detalle con presupuesto de origen |
| `GET /api/facturas/{id}/pdf` | — | `200 application/pdf`; `409` si no está Autorizada |

No existen `PUT` ni `DELETE` para facturas (FR-031).

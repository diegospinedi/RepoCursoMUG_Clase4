# Contrato: Planilla de precios del proveedor

**Feature**: [spec.md](../spec.md) (FR-042 a FR-050) | **Research**: [research.md](../research.md) (R6, R7)

## Formato

- Archivo `.xlsx`, de hasta 10 MB y 20.000 filas de datos.
- Se lee solo la primera hoja.
- Fila 1: exactamente tres encabezados, en este orden y escritos igual:

| Columna | Encabezado | Tipo de celda aceptado |
|---|---|---|
| A | `Código en el proveedor` | texto o número; se toma el valor tal cual, sin recortar espacios |
| B | `Descripción` | texto; obligatoria solo para códigos nuevos; se ignora en códigos existentes |
| C | `Precio de Costo` | solo número (precio final del proveedor, con IVA) |

Una columna de más, una faltante o un encabezado distinto rechaza la planilla completa (FR-043). Lo
mismo pasa si se superan los límites.

## Resultado por fila

| Situación | Resultado | Razón informada |
|---|---|---|
| Fila completamente vacía | se ignora | — |
| Código vacío con otros datos | no procesada | "falta el código" |
| Código repetido en la planilla | no procesadas todas sus filas | "código repetido en la planilla" |
| Precio vacío o celda de texto (incluido "1.210,50") | no procesada | "precio inválido" |
| Código nuevo sin descripción | no procesada | "falta la descripción" |
| Código existente, precio válido | actualiza costo y precio de venta | — |
| Código nuevo, precio y descripción válidos | crea el artículo con el margen predeterminado | — |
| Precio negativo o cero (en una fila procesada) | procesada y listada | "actualizado con precio negativo o cero" |

Los artículos del proveedor que no figuran en la planilla no cambian. La importación es todo o nada
(FR-050) y requiere el margen predeterminado configurado (FR-045).

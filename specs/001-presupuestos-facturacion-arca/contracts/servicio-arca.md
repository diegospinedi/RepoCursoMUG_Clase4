# Contrato: Puerto `IServicioArca`

**Feature**: [spec.md](../spec.md) (FR-030, FR-034 a FR-038b) | **Research**: [research.md](../research.md) (R9, R10)

Interfaz del módulo `Arca`, la única puerta hacia WSFEv1. El módulo `Facturacion` depende solo de este
contrato. Implementación actual: `ArcaSimulado`.

## Operaciones

| Operación | Equivalente WSFEv1 | Entrada | Salida |
|---|---|---|---|
| `UltimoAutorizado` | `FECompUltimoAutorizado` | punto de venta, tipo | número (0 si no hay) |
| `SolicitarCae` | `FECAESolicitar` | `SolicitudComprobante` | `Autorizado { cae, vencimiento }` o `Rechazado { codigo, descripcion }` |
| `Consultar` | `FECompConsultar` | punto de venta, tipo, número | `NoExiste` o `Existe { total, cae, vencimiento }` |

Todas reciben un `CancellationToken`. Si se cancela por el tiempo de espera (`Arca:TiempoEsperaSegundos`,
30 por defecto), la operación lanza `ArcaSinRespuestaException`: el llamador no sabe si ARCA autorizó.

`SolicitudComprobante`: punto de venta, tipo (6 = B, 11 = C), número, fecha, concepto 1, receptor
(DocTipo 99 + DocNro 0, o DocTipo 96 + DNI), condición IVA del receptor 5, total, neto, IVA y alícuota
(id de ARCA: 0 % = 3, 2,5 % = 9, 5 % = 8, 10,5 % = 4, 21 % = 5, 27 % = 6). En Factura C: neto = total,
IVA 0 y sin alícuota.

## Simulador (`Arca:Simulador:Modo`)

| Modo | `SolicitarCae` |
|---|---|
| `Normal` | autoriza y guarda el comprobante en `arca-simulado.json` |
| `Rechazar` | devuelve `Rechazado` con un código y una descripción de ejemplo |
| `SinRespuesta` | no autoriza y espera hasta el tiempo límite |
| `AutorizarSinResponder` | guarda el comprobante como autorizado y espera hasta el tiempo límite (reproduce AC-64) |

`Consultar` y `UltimoAutorizado` responden siempre a partir de `arca-simulado.json`. Ese archivo vive
fuera del repositorio.

## Garantías que el llamador debe respetar

- Registrar la emisión Pendiente con su número antes de llamar a `SolicitarCae` (FR-036).
- Llamar a `Consultar` antes de reenviar una emisión Pendiente (FR-037).
- No llamar nunca a ARCA producción en desarrollo ni en pruebas (`AGENTS.md`).

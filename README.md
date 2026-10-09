# Óptica Sistema — Presupuestos y Factura Electrónica

Aplicación web local para Óptica Sistema (La Plata). Arma presupuestos con un catálogo de artículos cuyo
precio de venta se calcula a partir del costo del proveedor y del margen. Desde un presupuesto en estado
Final emite la factura electrónica a consumidor final (Factura B o C) contra ARCA, hoy mediante un
**simulador**, hasta tener el certificado de homologación.

Se desarrolló con **Spec-Driven Development** (Spec Kit) y un ciclo **test-first**: cada comportamiento tiene
primero un test en rojo, después la implementación y un commit por tarea.

## Stack

- **Backend:** .NET 10 (ASP.NET Core minimal APIs) + EF Core 10 sobre **SQLite** en modo WAL.
- **Frontend:** React 19 + TypeScript + Vite, sobre Node 22.
- **PDF:** QuestPDF (licencia Community) · **QR:** QRCoder · **Planillas:** ClosedXML.
- **Tests:** xUnit con tests de integración (`WebApplicationFactory`, reloj falso, espía de ARCA), Vitest +
  Testing Library.

## Cómo correrlo

Todo desde WSL. Los detalles y los problemas conocidos están en [AGENTS.md](AGENTS.md#cómo-correr).

```bash
# Backend: http://localhost:5220 (crea la base y aplica las migraciones al arrancar)
dotnet run --project backend/Optica.Api

# Frontend: http://localhost:5173, con proxy de /api al backend
cd frontend && npm install && npm run dev
```

La primera vez la aplicación pide definir la contraseña de acceso. Solo se acepta desde la PC donde corre la
API.

En producción, en la PC del local, la API sirve también el frontend compilado:

```bash
cd frontend && npm run build          # genera backend/Optica.Api/wwwroot
cd ../backend/Optica.Api && ASPNETCORE_ENVIRONMENT=Production dotnet run --no-launch-profile --urls http://0.0.0.0:5220
```

## Verificación

```bash
dotnet test Optica.slnx                                   # 212 tests, incluidos 4 de rendimiento
dotnet test Optica.slnx --filter Categoria=Rendimiento    # solo RNF-01, RNF-11, RNF-12 y SC-009
cd frontend && npm test && npm run build && npm run lint  # 85 tests; el build también revisa tipos
```

## Qué hace

| Historia | Qué cubre | Backend | Frontend |
|---|---|---|---|
| US1 Acceso | Contraseña única (hash PBKDF2), bloqueo de 5 min tras 5 fallos, sesión de 60 min, cambio y restablecimiento solo desde la PC del sistema, registro de seguridad | `Acceso/` | `acceso/` |
| US2 Catálogo y configuración | Proveedores, artículos con precio de venta = costo × (1 + margen) redondeado hacia arriba al múltiplo, alícuotas de ARCA, recálculo del catálogo | `Catalogo/`, `Configuracion/` | `articulos/`, `proveedores/`, `configuracion/` |
| US3 Importación | Planilla .xlsx por proveedor: alta y actualización, lectura estricta, todo o nada, una a la vez, historial | `Catalogo/Importacion*`, `LectorPlanilla` | `importacion/` |
| US4 Presupuestos | Cliente y líneas del catálogo, cálculos en centavos iguales en ambos lados, numeración sin repetidos, Borrador → Final inmutable | `Presupuestos/` | `presupuestos/` |
| US5 PDF y búsqueda | PDF del presupuesto Final y búsqueda por cliente y fechas | `Presupuestos/PdfPresupuesto`, `BusquedaPresupuestos` | `presupuestos/BuscarPresupuestos` |
| US6 Facturación | Factura B (neto e IVA) o C, receptor según el tope, CAE, PDF con QR | `Facturacion/` | `facturas/` |
| US7 Fallas de ARCA | Rechazo, sin respuesta (30 s), reintento con consulta previa, estados Pendiente / Bloqueada / Descartada | `Facturacion/ServicioEmision` | `facturas/EstadoEmision` |
| US8 Consulta de facturas | Listado y búsqueda por estado, número, cliente y fechas | `Facturacion/BusquedaFacturas` | `facturas/ListadoFacturas` |

**Fuera de alcance:** Facturas A, notas de crédito y débito, envío automático por email, ABM de clientes,
usuarios con roles, backups y conexión real con ARCA. Ver [AGENTS.md](AGENTS.md#qué-no-hacer).

## Estructura

```text
PRD.md                      requerimientos originales (RF, RNF, AC)
AGENTS.md / CLAUDE.md       instrucciones operativas para agentes de código
.specify/memory/constitution.md   principios del proyecto (Spec Kit)
specs/001-presupuestos-facturacion-arca/
  spec.md · plan.md · research.md · data-model.md · tasks.md · quickstart.md
  contracts/                API HTTP, planilla del proveedor, puerto IServicioArca
  checklists/               calidad de la spec y de los requisitos críticos
backend/Optica.Api/         un módulo por carpeta; Datos/ con DbContext, migraciones y cálculos
tests/Optica.Tests/         tests de integración por módulo; AppDePrueba y EspiaArca
tests/vectores-calculo.json casos de cálculo compartidos por backend y frontend
frontend/src/               una carpeta por pantalla, con sus tests al lado
Marca/                      logo y branding.json (única fuente de colores)
```

## Para quien corrige (personas o agentes)

Esta sección sirve para evaluar el trabajo sin recorrer todo el historial.

### Recorrido sugerido

1. [`PRD.md`](PRD.md): el problema y los requerimientos de origen.
2. [`.specify/memory/constitution.md`](.specify/memory/constitution.md): los cinco principios contra los que
   se evalúa todo el resto.
3. [`specs/001-presupuestos-facturacion-arca/spec.md`](specs/001-presupuestos-facturacion-arca/spec.md):
   historias, requisitos FR-xxx y criterios SC-xxx. La sección **Clarifications** registra cada decisión
   tomada con el responsable.
4. [`plan.md`](specs/001-presupuestos-facturacion-arca/plan.md) y
   [`research.md`](specs/001-presupuestos-facturacion-arca/research.md): decisiones técnicas R1–R17, con
   sus alternativas.
5. [`tasks.md`](specs/001-presupuestos-facturacion-arca/tasks.md): 115 tareas, todas `[X]`. Cada test va
   antes de su implementación.
6. [`quickstart.md`](specs/001-presupuestos-facturacion-arca/quickstart.md): recorridos de validación y sus
   resultados en vivo.

### Cómo verificar cada principio de la constitución

| Principio | Dónde comprobarlo |
|---|---|
| I. Test-first | `git log --oneline`: en cada historia, los tests se commitean en rojo (`test(...)`, con "Test en rojo" en el cuerpo) antes que la implementación. `tasks.md` indica qué AC y FR cubre cada archivo de test. |
| II. IA aislada | No aplica: la feature no usa modelos de IA. El patrón equivalente es ARCA detrás de `Arca/IServicioArca.cs`. |
| III. Fuente de verdad y revisión humana | Precios desde el catálogo en el servidor (`ServicioPresupuestos`); CAE solo de ARCA; "otro total" → Bloqueada hasta que una persona confirme (`ServicioEmision.ReintentarAsync`); sin margen predeterminado no se importa. Triggers de SQLite en las migraciones `CierrePresupuestos` y `FacturasInmutablesYBusqueda`. |
| IV. Sin secretos | `appsettings.json` sin credenciales; `.gitignore` excluye base, WAL, certificados y logs; contraseña solo como hash (`Acceso/ServicioAcceso.cs`). |
| V. Contrato backend/frontend | [`contracts/api-http.md`](specs/001-presupuestos-facturacion-arca/contracts/api-http.md); cálculos validados en los dos lados con `tests/vectores-calculo.json`. Excepción conocida: varios endpoints y sus pantallas quedaron en commits separados porque el responsable pidió un commit por tarea. |

### Trazabilidad

- Requisito → tarea → test: `tasks.md` cita los AC y FR de cada tarea de test junto con el archivo que los
  prueba (por ejemplo, AC-64 → T102 → `tests/Optica.Tests/Facturacion/FallasArcaTests.cs`).
- Los casos numéricos del PRD están en `tests/vectores-calculo.json` con su AC; los tests del frontend
  también citan el AC en el nombre del caso.
- Tarea → commit: los mensajes siguen Conventional Commits y el cuerpo empieza con el ID (`T097.`).

### Decisiones y desvíos documentados

- La importación Excel por proveedor fue una **enmienda al PRD** pedida por el responsable (inicio de
  `spec.md`).
- Decisiones de implementación que no estaban en la spec: búsquedas limitadas a los 200 resultados más
  recientes; dos razones extra de rechazo en la importación (código de más de 50 caracteres, descripción de
  más de 200); oxlint en lugar de ESLint.
- Pendientes con el responsable: datos reales del emisor (`Emisor:*`, hoy "COMPLETAR"), tope inicial y datos
  del PDF con el contador, criterio legal sobre la Ley 25.326 y conexión real con ARCA.

### Comprobaciones rápidas

```bash
grep -c "^- \[X\]" specs/001-presupuestos-facturacion-arca/tasks.md   # 115 tareas completas
git log --oneline | grep -c " test("                                   # commits de tests (48)
grep -n "AC-64" specs/001-presupuestos-facturacion-arca/tasks.md       # requisito → tarea → archivo de test
```

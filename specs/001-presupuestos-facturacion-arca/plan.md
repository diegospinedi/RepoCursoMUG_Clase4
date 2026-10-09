# Implementation Plan: Presupuestos y Facturación Electrónica a Consumidor Final

**Branch**: `001-presupuestos-facturacion-arca` | **Date**: 2026-10-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-presupuestos-facturacion-arca/spec.md`

## Summary

Aplicación web local para Óptica Sistema. Tiene un catálogo por proveedor, que se carga desde la planilla
Excel de cada proveedor, con el precio de venta calculado (costo × (1 + margen), redondeado hacia arriba
al múltiplo comercial). Permite armar presupuestos con estados Borrador y Final, numerados y con PDF, y
emitir Factura B o C a consumidor final contra ARCA, con una emisión segura ante rechazos y falta de
respuesta. El backend ASP.NET Core concentra las reglas y los cálculos con valor legal sobre SQLite; el
frontend React reproduce los cálculos de línea en centavos para la experiencia de uso. ARCA queda aislada
detrás de `IServicioArca`, hoy implementada por un simulador.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5 / React 19 sobre Node 22 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (proveedor SQLite), ClosedXML (planillas),
QuestPDF (PDF), QRCoder (QR de ARCA); Vite, React Router, `@fontsource` (fuentes de la marca). Ver
[research.md](research.md).

**Storage**: SQLite en modo WAL (archivo local `optica.db`), migraciones de EF Core aplicadas al arrancar.
Importes en centavos y porcentajes en centésimos como `INTEGER` (R2).

**Testing**: xUnit con tests de integración sobre `AppDePrueba` (base SQLite temporal, reloj falso,
espía de ARCA); Vitest + Testing Library en el frontend; vectores de cálculo compartidos
(`tests/vectores-calculo.json`).

**Target Platform**: la PC del local (WSL/Linux o Windows) sirviendo la API y el frontend compilado;
clientes Chrome y Edge, estable y anterior, en la misma PC o en la red local.

**Project Type**: aplicación web (backend + frontend).

**Performance Goals**: búsqueda de presupuestos < 2 s en p95 con 10.000 registros (RNF-01); tiempo propio
de emisión < 2 s en p95 (RNF-11); importación de 10.000 filas < 120 s (RNF-12).

**Constraints**: presupuestos y catálogo funcionan sin internet; 2 sesiones simultáneas sin errores de
bloqueo (RNF-13); espera máxima de ARCA 30 s sin reintento automático (RNF-10); sin emisión contra ARCA
producción; sin secretos en el repositorio.

**Scale/Scope**: 2 operadoras; hasta 10.000 artículos y 10.000 presupuestos; unas 8 pantallas (ingreso,
configuración, proveedores, artículos, importación, presupuesto, búsqueda de presupuestos, facturas).

Sin NEEDS CLARIFICATION abiertos: los puntos técnicos pendientes se resolvieron en research.md (R1 a R17).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cómo lo cumple el plan | Estado |
|---|---|---|
| I. Test-First | `/speckit-tasks` ordena cada historia como test que falla → implementación → refactor. Los AC del PRD con números (cálculos, importación, emisión) pasan a tests de integración; los vectores de cálculo se escriben antes que el código | ✅ |
| II. IA aislada | La feature no usa modelos de IA. No aplica; si se agrega IA, va en un módulo propio con su interfaz | ✅ N/A |
| III. Fuente de verdad y revisión humana | Precios y descripciones salen del catálogo en el servidor (la pantalla no envía precios). El CAE solo sale de ARCA. Una respuesta incierta deja la emisión Pendiente y "otro total" la deja Bloqueada hasta que una persona confirme. Sin margen predeterminado no se importa. Triggers que impiden modificar registros cerrados | ✅ |
| IV. Sin secretos | Contraseña solo como hash PBKDF2. Certificado y clave por user-secrets o variables de entorno. `appsettings.json` sin secretos. `.gitignore` para la base, el WAL y `arca-simulado.json`. Nada sensible en el frontend | ✅ |
| V. Contrato backend/frontend | [contracts/api-http.md](contracts/api-http.md) define endpoints, errores y claves de validación. Las reglas definitivas están en el servidor; el frontend replica el cálculo de línea en centavos y lo valida con los mismos vectores | ✅ |
| Flujo de desarrollo | Un commit por cambio lógico con test e implementación juntos; verificación completa (`dotnet test`, `npm test`, `build`, `lint`) antes de cada commit | ✅ |

**Re-check post-design**: el data model, los contratos y el quickstart no introducen violaciones.
Complexity Tracking queda vacío.

## Project Structure

### Documentation (this feature)

```text
specs/001-presupuestos-facturacion-arca/
├── plan.md              # Este archivo
├── research.md          # Fase 0: decisiones R1–R17
├── data-model.md        # Fase 1: entidades, reglas y estados
├── quickstart.md        # Fase 1: guía de validación
├── contracts/
│   ├── api-http.md          # API entre frontend y backend
│   ├── planilla-proveedor.md # formato de la planilla Excel
│   └── servicio-arca.md      # puerto IServicioArca y simulador
├── checklists/
│   ├── requirements.md
│   └── requisitos-criticos.md
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Source Code (repository root)

```text
Optica.slnx
Marca/                         # branding.json y logo.png (fuente única de la marca)
backend/Optica.Api/
├── Program.cs                 # composición, auth por cookie, migraciones al arrancar
├── appsettings.json           # Arca y Emisor (sin secretos)
├── Acceso/                    # contraseña, ingreso, bloqueo, sesión, restablecimiento local
├── Configuracion/             # parámetros de negocio y recálculo del catálogo
├── Catalogo/                  # proveedores, artículos, cálculo de precio de venta, importación Excel
├── Presupuestos/              # presupuestos, líneas, numeración, búsqueda, PDF
├── Facturacion/               # armado del comprobante, emisión con reintento, estados, PDF y QR
├── Arca/                      # IServicioArca, ArcaSimulado (sin lógica de negocio)
├── Datos/                     # OpticaDbContext, converters, normalización, Migraciones/
└── Recursos/                  # fuentes para los PDF
tests/
├── vectores-calculo.json      # casos compartidos backend/frontend
└── Optica.Tests/
    ├── AppDePrueba.cs         # host de prueba: SQLite temporal, reloj falso, espía de ARCA
    ├── Acceso/  Configuracion/  Catalogo/  Presupuestos/  Facturacion/
    └── Planillas/             # .xlsx de prueba generados en el test
frontend/
├── src/
│   ├── marca.ts               # lee Marca/branding.json
│   ├── estilos/               # tokens.css, componentes.css
│   ├── api/                   # cliente HTTP tipado según contracts/api-http.md
│   ├── calculos/              # cálculo de líneas en centavos
│   ├── acceso/  configuracion/  proveedores/  articulos/  importacion/
│   ├── presupuestos/          # edición y búsqueda
│   └── facturas/              # listado, detalle, reintento, confirmación de revisión
└── tests en cada carpeta (*.test.ts[x])
```

**Structure Decision**: aplicación web con backend modular (una carpeta por módulo, como fija
`AGENTS.md`) y frontend con una carpeta por pantalla. Los tests de integración del backend viven en
`tests/Optica.Tests`; los del frontend, junto a cada pantalla. En producción la API sirve el frontend
compilado desde `wwwroot`, así la PC del local corre un solo proceso y la detección de pedidos locales
es confiable (research R4).

## Riesgos y dependencias para la implementación

- `Marca/` ya está en el repositorio (`branding.json`, `logo.png` y el original `Marca.jpg`). Faltan las
  fuentes Montserrat y Barlow en TTF para los PDF (`backend/Optica.Api/Recursos/`, licencia OFL); el
  frontend las toma de `@fontsource` (FR-040).
- El dominio y el JSON del QR de ARCA se validan contra la especificación vigente antes de pasar a
  homologación (R11).
- Pendiente del responsable: el criterio para pedidos de rectificación o supresión de datos (Ley 25.326)
  frente a los registros inmutables (R17). El tope inicial y los datos de RF-30, a validar con el
  contador.

## Complexity Tracking

Sin violaciones de la constitución que justificar.

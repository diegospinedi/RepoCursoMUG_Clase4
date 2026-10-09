---

description: "Task list for feature 001: Presupuestos y Facturación Electrónica a Consumidor Final"
---

# Tasks: Presupuestos y Facturación Electrónica a Consumidor Final

**Input**: Design documents from `/specs/001-presupuestos-facturacion-arca/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: OBLIGATORIOS. La constitución (principio I, test-first) exige escribir cada test antes del
código y verlo fallar (rojo → verde → refactor). En cada historia, las tareas de "Tests" van primero y
deben fallar antes de empezar las de "Implementation".

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Commits**: un commit por tarea o grupo lógico (test + implementación que lo hace pasar), con
Conventional Commits. Un cambio de endpoint MUST ir en el mismo commit que su cliente en
`frontend/src/api/cliente.ts`, la pantalla que lo usa y los tests de los dos lados (constitución,
principio V): las tareas de backend y frontend de una misma historia que tocan un endpoint se commitean
juntas, después de pasar `dotnet test Optica.slnx` y, en `frontend/`, `npm test`,
`npm run build` y `npm run lint` (constitución, Flujo de desarrollo).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US8, as in spec.md)
- Include exact file paths in descriptions

## Path Conventions

- Backend: `backend/Optica.Api/<Módulo>/` (módulos `Acceso`, `Configuracion`, `Catalogo`, `Presupuestos`,
  `Facturacion`, `Arca`, `Datos`)
- Tests de backend: `tests/Optica.Tests/<Módulo>/`
- Frontend: `frontend/src/<pantalla>/`, con los tests `*.test.ts(x)` junto a cada archivo
- Migraciones: `backend/Optica.Api/Datos/Migraciones/`, con
  `dotnet ef migrations add <Nombre> --project backend/Optica.Api -o Datos/Migraciones`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create the solution `Optica.slnx` with an ASP.NET Core Web API project `backend/Optica.Api/Optica.Api.csproj` (net10.0, nullable enabled, warnings as errors) and an xUnit project `tests/Optica.Tests/Optica.Tests.csproj` that references it
- [X] T002 Add NuGet packages to `backend/Optica.Api/Optica.Api.csproj`: Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design, ClosedXML, QuestPDF, QRCoder; and to `tests/Optica.Tests/Optica.Tests.csproj`: Microsoft.AspNetCore.Mvc.Testing, Microsoft.Extensions.TimeProvider.Testing, UglyToad.PdfPig (to read PDF text in tests)
- [X] T003 [P] Create the local tool manifest `.config/dotnet-tools.json` with `dotnet-ef` 10.x
- [X] T004 [P] Create `.gitignore` at the repo root excluding `bin/`, `obj/`, `node_modules/`, `frontend/dist/`, `backend/Optica.Api/wwwroot/`, `*.db`, `*.db-wal`, `*.db-shm`, `arca-simulado.json`, `*.pfx`, `*.p12`, `*.key`, `*.crt`, `logs/` (research R14)
- [X] T005 [P] Scaffold `frontend/` with Vite + React 19 + TypeScript (`frontend/package.json`, `frontend/tsconfig.json`, `frontend/vite.config.ts` with proxy `/api` → `http://localhost:5220` and `test.environment = "jsdom"`, `frontend/eslint.config.js`); scripts `dev`, `build`, `test` (vitest run), `lint`
- [X] T006 [P] Add frontend dependencies in `frontend/package.json`: react-router, @fontsource/montserrat, @fontsource/barlow; dev: vitest, jsdom, @testing-library/react, @testing-library/user-event, @testing-library/jest-dom
- [X] T007 [P] Create `backend/Optica.Api/appsettings.json` with `Arca` (`Entorno: "Simulado"`, `PuntoVenta`, `TiempoEsperaSegundos: 30`, `Simulador: { Modo: "Normal", Archivo: "arca-simulado.json" }`) and `Emisor` (razón social, domicilio, CUIT, condición IVA, ingresos brutos, inicio de actividades, all "COMPLETAR"); no secrets (research R14)
- [X] T008 [P] Add Montserrat (600, 700) and Barlow (400, 500, 600) TTF files (OFL) to `backend/Optica.Api/Recursos/` and link `Marca/logo.png` and `Marca/branding.json` as content files copied to output in `backend/Optica.Api/Optica.Api.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cálculos compartidos, persistencia, puerto de ARCA, host de prueba y base del frontend

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational (write first, must fail)

- [X] T009 [P] Create the shared calculation vectors `tests/vectores-calculo.json`: precio de venta (costo 1000/margen 50/múltiplo 0,01 → 1500,00; 1210/50/0,01 → 1815,00; calculado 1815,37 con múltiplo 50 → 1850,00; calculado 1666,656 con 0,01 → 1666,66; 1210/60 → 1936,00; 2420/50 → 3630,00; 1105/50 → 1657,50; costo −1210,25/50/50 → −1800,00), líneas (1000 con 0 % → 1000,00; 1000 con 10 % → 900,00; 900 × 3 → 2700,00; con descuento sin redondear 3,335 × 3 → 3,34 y 10,02), total (10,02 + 2700,00 → 2710,02) y desglose Factura B con IVA 21 (1815,00 → neto 1500,00 + IVA 315,00; 1000,00 → 826,45 + 173,55)
- [X] T010 [P] Write failing tests that run every vector of `tests/vectores-calculo.json` against `Calculadora` in `tests/Optica.Tests/Datos/CalculadoraTests.cs`
- [X] T011 [P] Write failing tests for normalization (`"González"` → `"gonzalez"`, `"23.456.789"` → `"23456789"`, `"0003-00034561"` → `"000300034561"`) in `tests/Optica.Tests/Datos/NormalizacionTests.cs`
- [X] T012 [P] Write failing tests for the value converters (importe ↔ centavos, porcentaje ↔ centésimos, sin pérdida con 2 decimales) in `tests/Optica.Tests/Datos/ConversoresTests.cs`
- [X] T013 [P] Write failing tests for `ArcaSimulado` per `contracts/servicio-arca.md`: `Normal` autoriza y persiste, `Rechazar` devuelve código y descripción, `SinRespuesta` lanza `ArcaSinRespuestaException` al cancelarse, `AutorizarSinResponder` persiste y lanza, `Consultar` y `UltimoAutorizado` leen el archivo, in `tests/Optica.Tests/Arca/ArcaSimuladoTests.cs`
- [X] T014 [P] Write failing Vitest tests that run every line/total vector of `tests/vectores-calculo.json` against the centavos calculator in `frontend/src/calculos/lineas.test.ts`
- [X] T015 [P] Write failing tests for the API client (maps `ValidationProblemDetails.errors` by field key, raises a session-expired event on 401, exposes `ProblemDetails.type` on 409/502/504) in `frontend/src/api/cliente.test.ts`
- [X] T016 [P] Write failing tests for the brand: `marca.ts` returns the logo and colors of `Marca/branding.json`; the app shell shows the logo and applies the primary color from branding (AC-33, FR-040); and a test that fails if any file in `frontend/src` other than `estilos/tokens.css` contains a hex color literal, in `frontend/src/marca.test.ts`, `frontend/src/App.test.tsx` and `frontend/src/estilos/sinColoresSueltos.test.ts`

### Implementation for Foundational

- [X] T017 [P] Implement `Calculadora` (precio de venta = costo × (1 + margen/100) redondeado hacia +∞ al múltiplo; líneas y totales con `MidpointRounding.AwayFromZero`; desglose B neto = round(total / (1 + alícuota/100), 2), IVA = total − neto) per research R3 in `backend/Optica.Api/Datos/Calculadora.cs`
- [X] T018 [P] Implement `Normalizacion` (minúsculas sin diacríticos; solo dígitos) in `backend/Optica.Api/Datos/Normalizacion.cs`
- [X] T019 [P] Implement the EF Core value converters (importe → `INTEGER` centavos, porcentaje → `INTEGER` centésimos) in `backend/Optica.Api/Datos/Conversores.cs`
- [X] T020 Create `OpticaDbContext` with the converters applied by convention and a connection interceptor that sets `PRAGMA journal_mode=WAL` and `PRAGMA busy_timeout=5000` in `backend/Optica.Api/Datos/OpticaDbContext.cs` (depends on T019)
- [X] T021 [P] Define the ARCA port (`IServicioArca` with `UltimoAutorizado`, `SolicitarCae`, `Consultar`; `SolicitudComprobante`; result types; `ArcaSinRespuestaException`) per `contracts/servicio-arca.md` in `backend/Optica.Api/Arca/IServicioArca.cs`
- [X] T022 Implement `ArcaSimulado` with the four modes and JSON persistence outside the repo in `backend/Optica.Api/Arca/ArcaSimulado.cs` (depends on T021)
- [X] T023 Wire `backend/Optica.Api/Program.cs`: DbContext, apply migrations at startup, `TimeProvider.System`, ProblemDetails, `ValidationProblem` with field-path keys (`cliente.dni`, `lineas[0].cantidad`), register `IServicioArca` → `ArcaSimulado` and fail startup if `Arca:Entorno` ≠ `Simulado`, static files from `wwwroot` and an SPA fallback endpoint marked `AllowAnonymous` so the login screen loads without a session, no `ForwardedHeaders` (research R4)
- [X] T024 Create the test host `AppDePrueba` (WebApplicationFactory with a temporary SQLite file per test, `FakeTimeProvider`, `EspiaArca` implementing `IServicioArca` with programmable responses and recorded calls, `TiempoEsperaSegundos` reducido, a way to set `RemoteIpAddress` per request — loopback by default, a LAN address on demand — so tests can exercise `solo-pc-local`, and `IngresarAsync()` helper filled in by US1) in `tests/Optica.Tests/AppDePrueba.cs`
- [X] T025 [P] Implement the centavos line calculator (precio con descuento, precio final, total; no `Math.round` sobre importes en `number`) in `frontend/src/calculos/lineas.ts`
- [X] T026 [P] Implement the typed API client in `frontend/src/api/cliente.ts`
- [X] T027 [P] Create `frontend/src/marca.ts` (reads `Marca/branding.json`), `frontend/src/estilos/tokens.css` and `frontend/src/estilos/componentes.css` following the `frontend-design` skill (colors only from branding, tokens only)
- [X] T028 [P] Create `CampoConError` (label, input, mensaje de error junto al campo por clave, FR-041) in `frontend/src/comunes/CampoConError.tsx`; write the failing test `frontend/src/comunes/CampoConError.test.tsx` first
- [X] T029 Create the app shell (logo from `marca.ts`, navegación, rutas vacías por pantalla, fonts imported from @fontsource) in `frontend/src/App.tsx` and `frontend/src/main.tsx`

**Checkpoint**: vectores en verde en backend y frontend; host de prueba y ARCA simulado listos.

---

## Phase 3: User Story 1 - Acceso protegido a la aplicación (Priority: P1) 🎯 MVP

**Goal**: contraseña única con definición inicial local, ingreso, bloqueo, sesión de 60 minutos, cambio
y restablecimiento local; sin sesión no se devuelve ningún dato.

**Independent Test**: definir la contraseña, ingresar con incorrectas y correctas, esperar el bloqueo y
la inactividad con reloj falso, y comprobar que sin sesión todo endpoint responde 401.

### Tests for User Story 1 (write first, must fail)

- [X] T030 [P] [US1] Write failing tests for `POST /api/acceso/definir`: 7 caracteres → 400 "la longitud mínima es de 8 caracteres" (AC-57), pedido no local → 403 `solo-pc-local`, ya definida → 409, el hash guardado no contiene la contraseña en texto plano (AC-80) in `tests/Optica.Tests/Acceso/DefinirContrasenaTests.cs`
- [X] T031 [P] [US1] Write failing tests for `POST /api/acceso/ingresar`: correcta → 204 + cookie; incorrecta → 401; 5 fallos seguidos → 423 durante 5 minutos aun con la correcta, y con `FakeTimeProvider` avanzado 5 minutos vuelve a admitir (AC-58); each failure and the lockout leave an entry with date and time in the security log, without the password (FR-005d) in `tests/Optica.Tests/Acceso/IngresoTests.cs`
- [X] T032 [P] [US1] Write failing tests for the session: 60 minutos sin pedidos → 401 (AC-59); un pedido a los 59 minutos renueva la sesión; every `/api/*` endpoint in `EndpointDataSource` except `/api/acceso/*` returns 401 without data when there is no session (AC-79, FR-005), while the SPA fallback `/` loads anonymously in `tests/Optica.Tests/Acceso/SesionTests.cs`
- [X] T033 [P] [US1] Write failing tests for `cambiar` (actual incorrecta → 400; correcta → 204 y las otras sesiones reciben 401) and `restablecer` (local → 204 sin conocer la anterior; no local → 403; libera un bloqueo vigente) (FR-005a/b/c) in `tests/Optica.Tests/Acceso/CambioRestablecimientoTests.cs`
- [X] T034 [P] [US1] Write failing tests for the login screen (error de contraseña, aviso de bloqueo con hora) and the first-time definition screen (mínimo 8) in `frontend/src/acceso/Ingreso.test.tsx` and `frontend/src/acceso/DefinirContrasena.test.tsx`, and for the change and reset screens (contraseña actual incorrecta, mínimo 8, aviso cuando el pedido no es local) in `frontend/src/acceso/CambiarContrasena.test.tsx` and `frontend/src/acceso/RestablecerContrasena.test.tsx`

### Implementation for User Story 1

- [X] T035 [US1] Create the `Acceso` entity (single row Id = 1; `HashContrasena` "null hasta la primera definición; PBKDF2 con sal", `IntentosFallidos` "0–5", `BloqueadoHasta` "null o ahora + 5 minutos tras el 5.º fallo", `SelloSeguridad`) in `backend/Optica.Api/Acceso/EstadoAcceso.cs`, register it in `OpticaDbContext` and add migration `Acceso`
- [X] T036 [P] [US1] Implement `EsPedidoLocal` (loopback check on `HttpContext.Connection.RemoteIpAddress`, never forwarded headers) in `backend/Optica.Api/Acceso/EsPedidoLocal.cs`
- [X] T037 [US1] Implement `ServicioAcceso` (`PasswordHasher<T>` PBKDF2, lockout with `TimeProvider`, `SelloSeguridad` rotation on cambio/restablecimiento, security log entries without secrets or personal data per research R15) in `backend/Optica.Api/Acceso/ServicioAcceso.cs`
- [X] T038 [US1] Configure cookie authentication in `backend/Optica.Api/Program.cs` (`HttpOnly`, `SameSite=Strict`, sliding expiration 60 minutes using the registered `TimeProvider` via `CookieAuthenticationOptions.TimeProvider` so `FakeTimeProvider` controls the expiry, 401 instead of redirect, `OnValidatePrincipal` compares `SelloSeguridad`, fallback policy requiring an authenticated user for `/api/*`; `/api/acceso/*` and the SPA fallback are anonymous)
- [X] T039 [US1] Implement the endpoints `estado`, `definir`, `ingresar`, `salir`, `cambiar`, `restablecer` per `contracts/api-http.md` in `backend/Optica.Api/Acceso/EndpointsAcceso.cs` and complete `IngresarAsync()` in `tests/Optica.Tests/AppDePrueba.cs`
- [X] T040 [P] [US1] Implement `Ingreso.tsx`, `DefinirContrasena.tsx`, `CambiarContrasena.tsx` and `RestablecerContrasena.tsx` in `frontend/src/acceso/`
- [X] T041 [US1] Implement `SesionContexto.tsx` (route guard by `/api/acceso/estado`; on 401 opens the login dialog and keeps the open form in memory, nothing in browser storage, research R5) in `frontend/src/acceso/SesionContexto.tsx`; write the failing test `frontend/src/acceso/SesionContexto.test.tsx` first

**Checkpoint**: US1 funciona sola; el resto de las historias usa `IngresarAsync()` en sus tests.

---

## Phase 4: User Story 2 - Catálogo con precio de venta calculado y configuración del negocio (Priority: P1)

**Goal**: Configuración de negocio, alta de proveedores y carga manual de artículos con precio de venta
calculado, redondeado y de solo lectura.

**Independent Test**: configurar parámetros, cargar artículos y verificar los precios contra los
ejemplos numéricos del PRD.

### Tests for User Story 2 (write first, must fail)

- [X] T042 [P] [US2] Write failing tests for `PUT /api/configuracion`: alícuota fuera de {0; 2,5; 5; 10,5; 21; 27} → 400 en `alicuotaIva` (FR-007a); múltiplo 0,001 → 400 "el valor mínimo es 0,01" (AC-82) y > 1000 → 400; margen predeterminado fuera de 0–1000 → 400, null aceptado; tope ≤ 0 o mayor a 999.999.999,99 → 400; valores con más de 2 decimales → 400 (FR-010a); cambiar el múltiplo a 50 pasa un artículo de 1815,00 a 1850,00 sin editarlo (AC-85, AC-74); cambiar alícuota o condición fiscal no cambia ningún precio (FR-009); la respuesta nunca incluye certificado ni punto de venta (FR-008) in `tests/Optica.Tests/Configuracion/ConfiguracionTests.cs`
- [X] T043 [P] [US2] Write failing tests for proveedores: nombre vacío → 400; nombre repetido sin distinguir mayúsculas → 400; modificar el nombre → 200 (FR-011a) in `tests/Optica.Tests/Catalogo/ProveedoresTests.cs`
- [X] T044 [P] [US2] Write failing tests for artículos: alta asigna código autonumérico y precio de venta (AC-30); con múltiplo 0,01 costo 1000/margen 50 → 1500,00 (AC-13); RI e IVA 21, 1210/50 → 1815,00 (AC-36); Monotributo 1210/50 → 1815,00 (AC-83); IVA 10,5, 1105/50 → 1657,50 (AC-72); margen 60 → 1936,00 (AC-70); costo 2420 → 3630,00 (AC-71); `precioVenta` enviado en el cuerpo se ignora (AC-81); margen 1000,01 → 400 "entre 0 y 1000"; costo negativo a mano → 400; costo o margen con 3 decimales → 400 "como máximo 2 decimales" (FR-010a); "ABC-1" repetido en el mismo proveedor → 400, "abc-1" y "ABC-1" en otro proveedor → 201 (FR-011); `GET /api/articulos?texto=` busca por código, código en el proveedor y descripción y devuelve como máximo 50 in `tests/Optica.Tests/Catalogo/ArticulosTests.cs`
- [X] T045 [P] [US2] Write a failing performance test (trait `Rendimiento`): con 10.000 artículos, cambiar el múltiplo de redondeo recalcula todo el catálogo en menos de 30 segundos (SC-009) in `tests/Optica.Tests/Configuracion/RecalculoRendimientoTests.cs`
- [X] T046 [P] [US2] Write failing tests for the screens: Configuración muestra los cinco parámetros y ningún campo de certificado ni punto de venta (AC-50); el editor de artículo muestra el precio de venta en solo lectura (AC-81); el listado de artículos busca por código, código en el proveedor y descripción, in `frontend/src/configuracion/Configuracion.test.tsx`, `frontend/src/articulos/EditorArticulo.test.tsx` and `frontend/src/articulos/Articulos.test.tsx`

### Implementation for User Story 2

- [X] T047 [P] [US2] Create the `Configuracion` entity (single row Id = 1; `AlicuotaIva` "requerido; solo 0; 2,5; 5; 10,5; 21 o 27", `CondicionFiscal` "`ResponsableInscripto` o `Monotributo`", `TopeIdentificacion` "> 0 y ≤ 999.999.999,99; inicial 10.000.000,00", `MultiploRedondeo` "0,01 a 1.000; inicial 0,01", `MargenPredeterminado` "null (sin configurar) o 0 a 1000") with seed RI e IVA 21 in `backend/Optica.Api/Configuracion/Configuracion.cs`
- [X] T048 [P] [US2] Create the `Proveedor` entity (`Nombre` "requerido; 1–100 caracteres; único sin distinguir mayúsculas") in `backend/Optica.Api/Catalogo/Proveedor.cs`
- [X] T049 [P] [US2] Create the `Articulo` entity (`Codigo` autonumérico; `CodigoProveedor` "requerido; 1–50 caracteres; único por `(ProveedorId, CodigoProveedor)` con comparación binaria, sensible a mayúsculas y espacios"; `Descripcion` "requerido; 1–200 caracteres"; `PrecioCosto` "2 decimales; a mano, ≥ 0"; `Margen` "0 a 1000, hasta 2 decimales"; `PrecioVenta` calculado; `DescripcionBusqueda` derivado) in `backend/Optica.Api/Catalogo/Articulo.cs`
- [X] T050 [US2] Register Configuracion, Proveedor and Articulo in `backend/Optica.Api/Datos/OpticaDbContext.cs` (unique index `(ProveedorId, CodigoProveedor)` with `COLLATE BINARY`) and add migration `CatalogoYConfiguracion`
- [X] T051 [US2] Implement `ServicioPrecios` (precio de venta via `Calculadora` with the current múltiplo; recálculo de todo el catálogo en una transacción) in `backend/Optica.Api/Catalogo/ServicioPrecios.cs`
- [X] T052 [US2] Implement `GET/PUT /api/configuracion` with validations and recálculo only when the múltiplo changes, returning `articulosRecalculados`, logging the change (R15) in `backend/Optica.Api/Configuracion/EndpointsConfiguracion.cs`
- [X] T053 [P] [US2] Implement `GET/POST/PUT /api/proveedores` in `backend/Optica.Api/Catalogo/EndpointsProveedores.cs`
- [X] T054 [US2] Implement `GET/POST/PUT /api/articulos` per `contracts/api-http.md` in `backend/Optica.Api/Catalogo/EndpointsArticulos.cs`
- [X] T055 [P] [US2] Implement the Configuración screen in `frontend/src/configuracion/Configuracion.tsx`
- [X] T056 [P] [US2] Implement the Proveedores screen in `frontend/src/proveedores/Proveedores.tsx`; write the failing test `frontend/src/proveedores/Proveedores.test.tsx` first
- [X] T057 [US2] Implement the article list with search and the editor (precio de venta de solo lectura) in `frontend/src/articulos/Articulos.tsx` and `frontend/src/articulos/EditorArticulo.tsx`

**Checkpoint**: catálogo y configuración completos y probados.

---

## Phase 5: User Story 3 - Importar la planilla de precios de un proveedor (Priority: P1)

**Goal**: importar la planilla Excel de un proveedor: alta de artículos nuevos, actualización de costos,
resumen con filas no procesadas, todo o nada.

**Independent Test**: con un proveedor dado de alta y el margen predeterminado configurado, importar
planillas con filas nuevas, existentes, inválidas y con formato incorrecto, y verificar catálogo y resumen.

### Tests for User Story 3 (write first, must fail)

- [X] T058 [P] [US3] Create the test helper that builds `.xlsx` files in memory with ClosedXML (headers, numeric and text cells, empty rows, extra columns, N rows) in `tests/Optica.Tests/Planillas/GeneradorPlanillas.cs`
- [X] T059 [US3] Write failing tests for the import per `contracts/planilla-proveedor.md` in `tests/Optica.Tests/Catalogo/ImportacionTests.cs`: alta "ABC-1"/"Armazón metal"/1210 con margen predeterminado 50 → venta 1815,00 y "creado"; existente 1210 → 2420 → venta 3630,00, margen y descripción sin cambios (AC-14); "ABC-1" en dos proveedores → solo cambia el elegido; precio vacío, "abc" y texto "1.210,50" → "precio inválido" (AC-75); código nuevo sin descripción → "falta la descripción"; código vacío con datos → "falta el código"; código repetido → todas sus filas "código repetido en la planilla"; precios −100 y 0 en códigos existentes → "actualizado con precio negativo o cero" (AC-90) y en códigos nuevos → "creado con precio negativo o cero" (FR-047); columna de más, faltante o encabezado distinto, incluido "precio de costo" en minúsculas → 400 `formato-planilla` sin cambios (AC-76, FR-043); más de 20.000 filas o más de 10 MB → 400 sin cambios; filas vacías ignoradas; "ABC-1 " (con espacio) es un código distinto; artículo ausente de la planilla sin cambios; margen predeterminado null → 409 `margen-sin-configurar`; una falla simulada a mitad de la importación deja el catálogo igual, no queda en el historial e informa que no se aplicó (FR-050, FR-048a); dos importaciones simultáneas → la segunda 409 `importacion-en-curso`; el historial guarda la importación y sus filas
- [X] T060 [P] [US3] Write a failing performance test (trait `Rendimiento`): catálogo de 10.000 artículos y planilla de 10.000 filas en menos de 120 segundos (AC-62, SC-010) in `tests/Optica.Tests/Catalogo/ImportacionRendimientoTests.cs`
- [X] T061 [P] [US3] Write failing tests for the import screen (elegir proveedor, subir archivo, resumen con creados, actualizados y filas con número, código y razón; mensaje de formato no esperado) in `frontend/src/importacion/Importacion.test.tsx`

### Implementation for User Story 3

- [X] T062 [P] [US3] Create the `Importacion` entity (ProveedorId, Fecha, NombreArchivo, Creados, Actualizados, NoProcesados "≥ 0") and `FilaImportacion` (NumeroFila, CodigoProveedor "texto tal cual, puede estar vacío", Resultado "`NoProcesada`, `ActualizadoPrecioNegativoOCero` o `CreadoPrecioNegativoOCero`", Razon) in `backend/Optica.Api/Catalogo/Importacion.cs`, register them and add migration `Importaciones`
- [X] T063 [P] [US3] Implement `LectorPlanilla` (first sheet; exact headers `Código en el proveedor`, `Descripción`, `Precio de Costo`; price only if `DataType == Number`; code via `GetString()` without trimming; ignore fully empty rows; limits 10 MB and 20.000 rows) per research R6 in `backend/Optica.Api/Catalogo/LectorPlanilla.cs`
- [X] T064 [US3] Implement `ServicioImportacion` (`SemaphoreSlim` for one import at a time; `BEGIN IMMEDIATE` transaction; dictionary of the supplier's articles; per-row rules; recálculo via `ServicioPrecios`; saves `Importacion` and `FilaImportacion`) per research R7–R8 in `backend/Optica.Api/Catalogo/ServicioImportacion.cs`
- [X] T065 [US3] Implement `POST/GET /api/proveedores/{id}/importaciones` and `GET /api/importaciones/{id}` per `contracts/api-http.md` in `backend/Optica.Api/Catalogo/EndpointsImportacion.cs`
- [X] T066 [US3] Implement the import screen with the summary and the history list in `frontend/src/importacion/Importacion.tsx`

**Checkpoint**: la planilla del proveedor alimenta el catálogo.

---

## Phase 6: User Story 4 - Armar y cerrar un presupuesto (Priority: P1)

**Goal**: presupuestos con cliente y líneas tomadas del catálogo, cálculos idénticos en pantalla y
servidor, numeración sin repetidos, Borrador editable y Final inmutable.

**Independent Test**: con un catálogo cargado, crear un presupuesto, verificar cálculos y numeración,
modificarlo en Borrador, pasarlo a Final y comprobar que no admite cambios por ninguna vía.

### Tests for User Story 4 (write first, must fail)

- [X] T067 [P] [US4] Write failing tests for grabar in `tests/Optica.Tests/Presupuestos/GrabarPresupuestoTests.cs`: con los seis datos y una línea → 201, Borrador y número 1; si el último es 154 → 155 (AC-01, AC-04, AC-06, AC-23); sin DNI → 400 en `cliente.dni` "Ingresá el DNI del cliente" (AC-34); sin apellido o nombre → 400 en ese campo (AC-44); sin domicilio, email ni teléfono → 201 (AC-45); DNI con letras o de 6 dígitos → 400; DNI "23.456.789" se guarda "23456789"; sin líneas → 400; modificar el domicilio en Borrador → 200 y se ve al reabrir (AC-07); pasar a Final → Final (AC-05); agregar una línea en Borrador → queda en la grilla (AC-24)
- [X] T068 [P] [US4] Write failing tests for lines in `tests/Optica.Tests/Presupuestos/LineasTests.cs`: la línea nueva toma descripción y precio de venta del catálogo aunque el cuerpo no los envíe (AC-10, AC-40); cantidad 0, −1 o 2,5 → 400 en `lineas[0].cantidad` "La cantidad debe ser un número entero mayor a 0" (AC-12, AC-43); descuento −1 o 101 → 400 "El descuento debe estar entre 0 y 100" (AC-42); descuento con 3 decimales → 400 (FR-010a); un total mayor a 999.999.999,99 → 400 indicando el máximo (FR-021); artículo con precio de venta negativo → 400 "El precio unitario no puede ser negativo" (AC-12); importes del servidor iguales a los vectores (AC-26, AC-27, AC-29, AC-35, AC-11); artículo con precio de venta 0 → se acepta (FR-023); tras recalcular el catálogo, la línea grabada conserva 1815,00 (AC-86, FR-024)
- [X] T069 [P] [US4] Write failing tests for closure in `tests/Optica.Tests/Presupuestos/CierreTests.cs`: un Final rechaza `PUT` con 409 `presupuesto-cerrado` y queda igual (AC-66); pedir estado Borrador sobre un Final → 409 (AC-67); un `UPDATE` o `DELETE` por SQL directo sobre un Final o sus líneas falla por trigger
- [X] T070 [P] [US4] Write a failing concurrency test: dos grabaciones simultáneas de presupuestos nuevos → números distintos y consecutivos, sin errores de bloqueo (AC-63) in `tests/Optica.Tests/Presupuestos/NumeracionConcurrenteTests.cs`
- [X] T071 [P] [US4] Write failing tests for the editor in `frontend/src/presupuestos/EditorPresupuesto.test.tsx`: seleccionar un artículo completa código, descripción y precio (AC-10); cambiar cantidad y descuento recalcula la línea (AC-46); mensajes de validación junto al campo (AC-12, AC-42, AC-43); ningún importe discrimina IVA (AC-28); un Final se muestra sin controles de edición

### Implementation for User Story 4

- [X] T072 [P] [US4] Create the `Presupuesto` entity (`Numero` "requerido; único; último + 1, empezando en 1"; `Estado` "`Borrador` o `Final`"; `Apellido`, `Nombre` "requeridos; 1–100 caracteres"; `Dni` "7 u 8 dígitos, se guarda solo con dígitos"; `Domicilio` "opcional; hasta 200"; `Email` "opcional; formato de email si se carga"; `Telefono` "opcional; hasta 30"; `Total`; `ApellidoBusqueda`, `NombreBusqueda`) in `backend/Optica.Api/Presupuestos/Presupuesto.cs`
- [X] T073 [P] [US4] Create the `LineaPresupuesto` entity (`Orden`, `ArticuloCodigo`, `Descripcion`, `PrecioUnitario` "≥ 0; no editable", `Cantidad` "1 a 9.999", `Descuento` "0 a 100, hasta 2 decimales", `PrecioConDescuento`, `PrecioFinal`) in `backend/Optica.Api/Presupuestos/LineaPresupuesto.cs`
- [X] T074 [US4] Register both entities, add migration `Presupuestos` and migration `CierrePresupuestos` with SQLite triggers that `RAISE(ABORT)` on `UPDATE`/`DELETE` of Final presupuestos and their lines in `backend/Optica.Api/Datos/Migraciones/`
- [X] T075 [US4] Implement `Numerador` (next number inside a `BEGIN IMMEDIATE` transaction) in `backend/Optica.Api/Presupuestos/Numerador.cs`
- [X] T076 [US4] Implement `ServicioPresupuestos` (validaciones con claves de campo; líneas nuevas desde el catálogo, existentes conservadas; cálculos via `Calculadora`; transición Borrador → Final; rechazo de cambios sobre Final) in `backend/Optica.Api/Presupuestos/ServicioPresupuestos.cs`
- [X] T077 [US4] Implement `GET /api/presupuestos/{id}`, `POST` and `PUT /api/presupuestos` per `contracts/api-http.md` in `backend/Optica.Api/Presupuestos/EndpointsPresupuestos.cs`
- [X] T078 [P] [US4] Implement the article picker (search by code or description, max 50) in `frontend/src/presupuestos/BuscadorArticulos.tsx`; write the failing test `frontend/src/presupuestos/BuscadorArticulos.test.tsx` first (búsqueda por código y por descripción, selección devuelve el artículo)
- [X] T079 [P] [US4] Implement the lines grid with the seven columns (AC-25) using `frontend/src/calculos/lineas.ts` in `frontend/src/presupuestos/GrillaLineas.tsx`; write the failing test `frontend/src/presupuestos/GrillaLineas.test.tsx` first (siete columnas, importes con formato `$ 1.815,00`, recálculo al cambiar cantidad o descuento)
- [X] T080 [US4] Implement the budget editor (cliente, líneas, total, estado, Grabar, solo lectura en Final) in `frontend/src/presupuestos/EditorPresupuesto.tsx`

**Checkpoint**: las cuatro historias P1 forman el MVP.

---

## Phase 7: User Story 5 - PDF y búsqueda de presupuestos (Priority: P2)

**Goal**: PDF del presupuesto Final con marca y leyenda; búsqueda por fecha y datos del cliente.

**Independent Test**: con presupuestos grabados, descargar el PDF de uno Final, comprobar que un
Borrador no lo permite y ejecutar las búsquedas de los ejemplos.

### Tests for User Story 5 (write first, must fail)

- [X] T081 [P] [US5] Write failing tests for search in `tests/Optica.Tests/Presupuestos/BusquedaPresupuestosTests.cs`: "ONZALEZ" devuelve solo "González" (AC-03); "González" con Desde 15/03/2026 → solo el del 20/03 (AC-68); Desde 10/03 Hasta 20/03 → solo 10 y 20 (AC-69); DNI "3456" → el cliente 23.456.789 (AC-87)
- [X] T082 [P] [US5] Write failing tests for the PDF in `tests/Optica.Tests/Presupuestos/PdfPresupuestoTests.cs`: Borrador → 409 `presupuesto-borrador` sin generar PDF (AC-08); Final → `application/pdf` whose text (read with PdfPig) contains "Precios finales, IVA incluido", the number, the client and the lines (AC-02, AC-09, AC-37), the first page contains the logo image (FR-040) and no text discriminates IVA (FR-022)
- [X] T083 [P] [US5] Write a failing performance test (trait `Rendimiento`): con 10.000 presupuestos, 19 de 20 búsquedas por apellido responden en menos de 2 segundos (RNF-01; la medición en pantalla queda en el quickstart) in `tests/Optica.Tests/Presupuestos/BusquedaRendimientoTests.cs`
- [X] T084 [P] [US5] Write failing tests for the search screen and the PDF button (deshabilitado en Borrador) in `frontend/src/presupuestos/BuscarPresupuestos.test.tsx`

### Implementation for User Story 5

- [X] T085 [US5] Implement `BusquedaPresupuestos` (filtros combinados con "Y" sobre columnas normalizadas, `LIKE '%texto%'`, rango de fechas inclusive con un solo límite admitido) and `GET /api/presupuestos` in `backend/Optica.Api/Presupuestos/BusquedaPresupuestos.cs` and `backend/Optica.Api/Presupuestos/EndpointsPresupuestos.cs`
- [X] T086 [P] [US5] Implement `PdfPresupuesto` with QuestPDF (logo `Marca/logo.png`, color primario from `branding.json`, fonts from `Recursos/`, importes `$ 1.815,00`, leyenda "Precios finales, IVA incluido") in `backend/Optica.Api/Presupuestos/PdfPresupuesto.cs`
- [X] T087 [US5] Implement `GET /api/presupuestos/{id}/pdf` (409 for Borrador) in `backend/Optica.Api/Presupuestos/EndpointsPresupuestos.cs`
- [X] T088 [P] [US5] Implement the search screen in `frontend/src/presupuestos/BuscarPresupuestos.tsx` and the "Descargar PDF" button (disabled in Borrador) in `frontend/src/presupuestos/EditorPresupuesto.tsx`

---

## Phase 8: User Story 6 - Facturar un presupuesto Final (Priority: P2)

**Goal**: emitir Factura B o C a consumidor final desde un presupuesto Final, con CAE, vínculo al
presupuesto, PDF con QR y leyenda de simulado.

**Independent Test**: con un presupuesto Final y el simulador en `Normal`, facturar y verificar tipo,
receptor, importes enviados, CAE guardado y PDF.

### Tests for User Story 6 (write first, must fail)

- [X] T089 [P] [US6] Write failing tests for emission in `tests/Optica.Tests/Facturacion/EmisionTests.cs` using `EspiaArca`: Final con 3 líneas → Autorizada, total enviado igual al del presupuesto (AC-16); Borrador → 409 `presupuesto-borrador` sin llamadas a ARCA (AC-17); guarda número, CAE, vencimiento y presupuesto de origen (AC-18); RI → tipo 6 con neto 1500,00 e IVA 315,00 para 1815,00 y 826,45 + 173,55 para 1000,00 (AC-31, AC-47, AC-48); Monotributo → tipo 11 con total y sin IVA (AC-73, AC-84); total ≤ tope → DocTipo 99 aunque haya DNI, igual al tope incluido (AC-41, AC-77); total > tope → DocTipo 96 con el DNI (AC-21); cambiar el tope se aplica en la siguiente (AC-49); segundo pedido sobre el mismo presupuesto → 409 `ya-facturado`; dos pedidos simultáneos → un solo comprobante; el número solicitado es último autorizado + 1 y queda registrado antes de llamar a `SolicitarCae` (FR-036)
- [X] T090 [P] [US6] Write failing tests for immutability: no existen `PUT` ni `DELETE` sobre `/api/facturas/*` (405/404) y un `UPDATE`/`DELETE` por SQL sobre una Autorizada falla por trigger (AC-32) in `tests/Optica.Tests/Facturacion/InmutabilidadFacturasTests.cs`
- [X] T091 [P] [US6] Write failing tests for the invoice PDF (PdfPig text): datos del emisor, tipo, punto de venta, número, fecha, receptor, líneas, total, CAE, vencimiento (AC-19) and "COMPROBANTE SIMULADO — SIN VALIDEZ FISCAL" (FR-033), with the logo image on the first page (FR-040); and for `QrArca` (JSON with `ver`, `fecha`, `cuit`, `ptoVta`, `tipoCmp`, `nroCmp`, `importe`, `moneda`, `ctz`, `tipoDocRec`, `nroDocRec`, `tipoCodAut` "E", `codAut`, base64 in the ARCA URL) in `tests/Optica.Tests/Facturacion/PdfFacturaTests.cs`
- [X] T092 [P] [US6] Write a failing performance test (trait `Rendimiento`): con `EspiaArca` registrando su propio tiempo de respuesta, en 20 emisiones al menos 19 tienen un tiempo propio del sistema (total menos espera de ARCA) menor a 2 segundos (AC-78, SC-004, RNF-11) in `tests/Optica.Tests/Facturacion/EmisionRendimientoTests.cs`
- [X] T093 [P] [US6] Write failing tests for the Facturar button (deshabilitado en Borrador, confirma con el número de comprobante) in `frontend/src/facturas/Facturar.test.tsx`, and for the invoice detail (datos del comprobante, presupuesto de origen, Descargar PDF solo si está Autorizada) in `frontend/src/facturas/DetalleFactura.test.tsx`

### Implementation for User Story 6

- [X] T094 [US6] Create the `Factura` entity (`Estado` "`Pendiente`, `Bloqueada`, `Autorizada` o `Descartada`"; `Tipo` "`B` (6) o `C` (11)"; `PuntoVenta`; `Numero` "último autorizado + 1, registrado antes de enviar"; `Fecha`; `ReceptorDocTipo` "99 o 96"; `ReceptorDocNro`; `Total`; `Neto`, `Iva`, `AlicuotaIva` "solo Factura B; null en C"; `Cae`, `VencimientoCae` "requeridos si Autorizada"; `NumeroBusqueda`; datos de búsqueda del cliente) in `backend/Optica.Api/Facturacion/Factura.cs`
- [X] T095 [US6] Register `Factura` and add migration `FacturasInmutablesYBusqueda` with unique index `(PuntoVenta, Tipo, Numero)`, partial unique index on `PresupuestoId` for Pendiente, Bloqueada and Autorizada, and triggers that abort `UPDATE`/`DELETE` of Autorizada and Descartada in `backend/Optica.Api/Datos/Migraciones/`
- [X] T096 [US6] Implement `ArmadorComprobante` (tipo por condición fiscal, receptor por tope, desglose B via `Calculadora`, ids de alícuota de ARCA) in `backend/Optica.Api/Facturacion/ArmadorComprobante.cs`
- [X] T097 [US6] Implement `ServicioEmision.Facturar` (`SemaphoreSlim(1)`; registra Pendiente con la foto del comprobante; `SolicitarCae` con `CancellationToken` de `Arca:TiempoEsperaSegundos`; Autorizada con CAE; rechazo elimina la Pendiente) per research R10 in `backend/Optica.Api/Facturacion/ServicioEmision.cs`
- [X] T098 [P] [US6] Implement `QrArca` with QRCoder per research R11 in `backend/Optica.Api/Facturacion/QrArca.cs`
- [X] T099 [P] [US6] Implement `PdfFactura` with QuestPDF (datos de RF-30, logo y colores, QR, leyenda de simulado when `Arca:Entorno = Simulado`) in `backend/Optica.Api/Facturacion/PdfFactura.cs`
- [X] T100 [US6] Implement `POST /api/presupuestos/{id}/factura`, `GET /api/facturas/{id}` and `GET /api/facturas/{id}/pdf` per `contracts/api-http.md` in `backend/Optica.Api/Facturacion/EndpointsFacturas.cs`
- [X] T101 [US6] Implement the Facturar action in `frontend/src/presupuestos/EditorPresupuesto.tsx` and the invoice detail with PDF download in `frontend/src/facturas/DetalleFactura.tsx`

---

## Phase 9: User Story 7 - Emisión segura ante fallas de ARCA (Priority: P2)

**Goal**: rechazo informado, espera máxima de 30 s, reintento con consulta previa, estados Pendiente,
Bloqueada y Descartada, sin duplicados.

**Independent Test**: con el espía de ARCA (o el simulador) configurado para rechazar, no responder o
autorizar sin responder, facturar y reintentar, verificando cada estado.

### Tests for User Story 7 (write first, must fail)

- [X] T102 [US7] Write failing tests in `tests/Optica.Tests/Facturacion/FallasArcaTests.cs`: rechazo → 502 `arca-rechazo` con código y descripción y ninguna factura registrada (AC-20); sin respuesta → 504 `arca-sin-respuesta` con un mensaje que dice que ARCA no respondió, que la emisión quedó pendiente y que se puede reintentar (FR-035), Pendiente y ninguna llamada extra a ARCA (AC-60); reintento de una autorizada con el mismo total → Autorizada con el CAE recuperado y sin un segundo `SolicitarCae` (AC-64); reintento de una no autorizada → `Consultar` primero y recién después `SolicitarCae` (AC-65); número autorizado con otro total → 409 `emision-bloqueada`, estado Bloqueada, sin emitir ni recuperar (AC-89); reintentar una Bloqueada → 409; `confirmar-revision` → Descartada y un nuevo Facturar pide el número siguiente al último autorizado (FR-038b); el reintento usa la foto aunque la condición fiscal haya cambiado; la fecha enviada es la del reintento (FakeTimeProvider)
- [X] T103 [P] [US7] Write failing tests for the emission state component (Pendiente con Reintentar; Bloqueada con aviso de revisar el punto de venta y botón de confirmación; código y descripción del rechazo) in `frontend/src/facturas/EstadoEmision.test.tsx`

### Implementation for User Story 7

- [X] T104 [US7] Implement `ServicioEmision.Reintentar` (consulta previa; mismo total → recupera; no existe → envía; otro total → Bloqueada) and `ServicioEmision.ConfirmarRevision` (Bloqueada → Descartada) in `backend/Optica.Api/Facturacion/ServicioEmision.cs`
- [X] T105 [US7] Implement `POST /api/facturas/{id}/reintentar` and `POST /api/facturas/{id}/confirmar-revision`, and include `emision: { facturaId, estado }` in `GET /api/presupuestos/{id}`, in `backend/Optica.Api/Facturacion/EndpointsFacturas.cs` and `backend/Optica.Api/Presupuestos/EndpointsPresupuestos.cs`
- [X] T106 [US7] Implement `EstadoEmision.tsx` and show it in the budget editor (FR-038a) in `frontend/src/facturas/EstadoEmision.tsx` and `frontend/src/presupuestos/EditorPresupuesto.tsx`

---

## Phase 10: User Story 8 - Consultar facturas emitidas (Priority: P3)

**Goal**: listado y búsqueda de facturas por estado, fechas, número y datos del cliente.

**Independent Test**: con facturas emitidas en distintos estados, ejecutar las búsquedas de los ejemplos.

### Tests for User Story 8 (write first, must fail)

- [X] T107 [P] [US8] Write failing tests in `tests/Optica.Tests/Facturacion/BusquedaFacturasTests.cs`: "gonzalez" con Desde 15/03/2026 → solo la de González del 20/03 (AC-22); número "34561" → 0003-00034561 (AC-88); DNI "3456" → las del cliente 23.456.789 (AC-87); sin filtro de estado el listado incluye Autorizadas, Pendientes, Bloqueadas y Descartadas con su estado (FR-039); el filtro por estado devuelve solo ese estado; cada fila incluye el número del presupuesto de origen
- [X] T108 [P] [US8] Write failing tests for the invoice list screen (filtros, estado visible, acceso al detalle) in `frontend/src/facturas/ListadoFacturas.test.tsx`

### Implementation for User Story 8

- [ ] T109 [US8] Implement `BusquedaFacturas` and `GET /api/facturas` per `contracts/api-http.md` in `backend/Optica.Api/Facturacion/BusquedaFacturas.cs` and `backend/Optica.Api/Facturacion/EndpointsFacturas.cs`
- [ ] T110 [US8] Implement the invoice list screen in `frontend/src/facturas/ListadoFacturas.tsx`

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T111 [P] Write and pass a test that the security log records login failures, lockouts, password changes and resets and configuration changes with date and time (FR-005d), and never contains passwords, DNI or client names; and that ARCA and import error responses expose no other clients' data or stack traces (FR-041a) in `tests/Optica.Tests/Acceso/RegistroSeguridadTests.cs`
- [ ] T112 Configure the production build: Vite `build.outDir` → `backend/Optica.Api/wwwroot` in `frontend/vite.config.ts`, and verify the API serves the SPA with fallback (research R4)
- [ ] T113 [P] Update `AGENTS.md` ("Cómo correr" para producción con el frontend servido por la API; nuevas dependencias ClosedXML, QRCoder y PdfPig)
- [ ] T114 Run the full verification (`dotnet test Optica.slnx`, including traits `Rendimiento`; `cd frontend && npm test && npm run build && npm run lint`) and fix failures
- [ ] T115 Run the manual walkthroughs 1–12 of `specs/001-presupuestos-facturacion-arca/quickstart.md` in Chrome and Edge (stable and previous, RNF-03, SC-008) and record the results in `specs/001-presupuestos-facturacion-arca/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup; BLOCKS all user stories.
- **User stories**: all depend on Foundational. Order and dependencies between stories:
  - **US1 (Acceso)**: first; the other stories log in through `AppDePrueba.IngresarAsync()` and the session guard.
  - **US2 (Catálogo y configuración)**: after US1.
  - **US3 (Importación)**: after US2 (uses `Articulo`, `Proveedor`, `ServicioPrecios`).
  - **US4 (Presupuestos)**: after US2 (lines come from the catalog); independent of US3.
  - **US5 (PDF y búsqueda)**: after US4.
  - **US6 (Facturar)**: after US4; independent of US5.
  - **US7 (Fallas de ARCA)**: after US6.
  - **US8 (Consulta de facturas)**: after US6; best after US7 to list Pendientes and Bloqueadas.
- **Polish (Phase 11)**: after the desired stories.

```text
Setup → Foundational → US1 → US2 ─┬─ US3
                                  └─ US4 ─┬─ US5
                                          └─ US6 → US7 → US8
```

### Within Each User Story

- Tests MUST be written and FAIL before implementation (constitution, principle I).
- Entities → migrations → services → endpoints → screens.
- Commit each test + implementation pair once the full verification passes; an endpoint change ships in
  the same commit as its frontend client, screen and tests (constitution, principle V).

### Parallel Opportunities

- Setup: T003–T008 in parallel after T001–T002.
- Foundational: all tests T009–T016 in parallel; T017–T019, T021, T025–T028 in parallel.
- After US2: US3 and US4 in parallel. After US4: US5 and US6 in parallel.
- Within each story, every test task marked [P] and the entities marked [P] can run in parallel.

---

## Parallel Example: User Story 4

```bash
# Tests (all different files):
Task: "T067 GrabarPresupuestoTests.cs"
Task: "T068 LineasTests.cs"
Task: "T069 CierreTests.cs"
Task: "T070 NumeracionConcurrenteTests.cs"
Task: "T071 EditorPresupuesto.test.tsx"

# Entities:
Task: "T072 Presupuesto.cs"
Task: "T073 LineaPresupuesto.cs"

# Screens after the endpoints:
Task: "T078 BuscadorArticulos.tsx"
Task: "T079 GrillaLineas.tsx"
```

## Parallel Example: User Story 6

```bash
Task: "T089 EmisionTests.cs"
Task: "T090 InmutabilidadFacturasTests.cs"
Task: "T091 PdfFacturaTests.cs"
Task: "T093 Facturar.test.tsx"
# After T094–T097:
Task: "T098 QrArca.cs"
Task: "T099 PdfFactura.cs"
```

---

## Implementation Strategy

### MVP First (P1 stories)

1. Phase 1 Setup + Phase 2 Foundational.
2. US1 → US2 → US3 → US4 (las cuatro P1).
3. **STOP and VALIDATE**: quickstart recorridos 1–6. Con esto las operadoras ya arman presupuestos
   con precios del catálogo cargado desde las planillas.

### Incremental Delivery

1. MVP (US1–US4).
2. US5: PDF y búsqueda de presupuestos.
3. US6 + US7: facturación con el simulador de ARCA (siempre juntas antes de usarla de verdad: sin US7
   una falta de respuesta no tiene reintento).
4. US8: consulta de facturas.
5. Polish.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Verify tests fail before implementing (constitution, principle I)
- No task may add endpoints, migrations or fixes that modify a Final presupuesto or an Autorizada factura (`AGENTS.md`)
- Never point to ARCA production; all emission goes through `IServicioArca` (simulator or test spy)

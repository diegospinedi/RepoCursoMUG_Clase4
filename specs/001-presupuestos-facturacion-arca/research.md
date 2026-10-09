# Research: Presupuestos y Facturación Electrónica a Consumidor Final

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-09

Cada decisión resuelve una incógnita del Technical Context o un ítem abierto del checklist
`checklists/requisitos-criticos.md` (CHKxxx) que la spec dejó para el plan.

## R1. Stack

- **Decision**: .NET 10 (ASP.NET Core Web API) + EF Core 10 + SQLite; React 19 + TypeScript + Vite sobre
  Node 22. Solución `Optica.slnx`.
- **Rationale**: decidido en `AGENTS.md` (19/09/2026). .NET 10 es LTS; SQLite no necesita servidor en la
  PC del local y permite operar sin internet.
- **Alternatives considered**: .NET 9 (STS, sin soporte desde mayo de 2026); PostgreSQL (requiere un
  servicio instalado y administrado, sin beneficio para 2 usuarias).

## R2. Representación de importes y porcentajes

- **Decision**: en el dominio, `decimal`. En la base, importes como `INTEGER` en centavos y porcentajes
  como `INTEGER` en centésimos (21 % → 2100), con value converters de EF Core. Precisión (CHK016):
  costo, precios e importes con 2 decimales; margen y descuento con hasta 2 decimales; cantidad entera
  de 1 a 9.999 (CHK034).
- **Rationale**: el proveedor SQLite de EF Core guarda `decimal` como TEXT, que no se puede sumar ni
  comparar en SQL. Los enteros son exactos, ordenables y coinciden con el redondeo en centavos que exige
  `AGENTS.md` para el frontend.
- **Alternatives considered**: `decimal` como TEXT (sin agregaciones en SQL); `double` (errores binarios
  inaceptables en importes fiscales).

## R3. Reglas de cálculo y redondeo

- **Decision**:
  - Precio de venta = costo × (1 + margen / 100) (FR-012), redondeado **hacia +∞** al múltiplo comercial:
    `Math.Ceiling(precio / múltiplo) × múltiplo`. Para precios negativos (FR-047), hacia +∞ significa
    acercarse a cero (−1.815,37 con múltiplo 50 → −1.800) (CHK033).
  - Líneas y totales: `Math.Round(x, 2, MidpointRounding.AwayFromZero)` ("mitad hacia arriba" para
    importes positivos, que son los únicos admitidos en líneas).
  - Factura B: neto = round(total / (1 + alícuota / 100), 2) e IVA = total − neto (FR-028).
  - Frontend: los mismos cálculos en enteros (centavos), con un archivo de vectores de prueba compartido
    (`tests/vectores-calculo.json`) que corren tanto xUnit como Vitest.
- **Rationale**: el principio V de la constitución exige el mismo resultado en ambos lados; los vectores
  compartidos lo vuelven verificable.
- **Alternatives considered**: calcular solo en el servidor y que el frontend pida cada línea (latencia
  en cada tecla); `MidpointRounding.ToEven` (no es el redondeo pedido por RF-19).

## R4. Acceso: contraseña, sesión y operaciones locales

- **Decision**:
  - Hash con `PasswordHasher<T>` de ASP.NET Core (PBKDF2-HMAC-SHA512, 100.000 iteraciones, sal
    aleatoria) sin el resto de Identity.
  - Sesión con cookie de autenticación (`HttpOnly`, `SameSite=Strict`) y vencimiento deslizante de 60
    minutos. Cuenta como actividad cualquier pedido autenticado a la API (CHK020).
  - Bloqueo: contador y "bloqueado hasta" en la tabla `Acceso`; se reinicia con un ingreso correcto, al
    cambiar o restablecer la contraseña (FR-005c). Un bloqueo provocado desde otro equipo de la LAN
    afecta a todos (CHK035): riesgo aceptado, porque dura 5 minutos y la red es del local.
  - Cambio y restablecimiento de contraseña incrementan un `SelloSeguridad` guardado en `Acceso`; la
    validación de la cookie lo compara y cierra las demás sesiones (FR-005c).
  - "Solo desde la PC del sistema" (FR-001, FR-005b, CHK021) = `RemoteIpAddress` de loopback. En
    producción la API sirve el frontend compilado desde `wwwroot` (mismo origen, sin proxy) y **no** se
    habilita `ForwardedHeaders`, para que la IP no se pueda falsificar. Con el proxy de Vite en desarrollo
    todo pedido parece local: es aceptable solo en desarrollo y se documenta en el quickstart.
- **Rationale**: cumple RNF-04, RNF-08, RNF-09 y RNF-14 con componentes del framework, sin secretos.
- **Alternatives considered**: ASP.NET Core Identity completo (usuarios y roles fuera de alcance); JWT en
  el navegador (expuesto a XSS y sin cierre de sesión del lado del servidor).

## R5. Sesión vencida con un presupuesto a medio cargar (CHK011)

- **Decision**: el frontend conserva en memoria el formulario abierto. Ante un 401, muestra el ingreso en
  un diálogo y, tras ingresar, permite volver a grabar sin perder lo cargado. Nada se guarda en el
  almacenamiento del navegador.
- **Rationale**: no se persisten datos personales fuera de la base (Ley 25.326).
- **Alternatives considered**: guardar el borrador en `localStorage` (deja DNI y domicilio en el navegador).

## R6. Lectura de la planilla Excel

- **Decision**: ClosedXML (MIT). Se lee la primera hoja; la fila 1 debe tener exactamente los encabezados
  `Código en el proveedor`, `Descripción` y `Precio de Costo`, comparados de forma exacta (CHK015). El
  precio se acepta solo si `DataType == Number` (FR-045a). El código se toma con `GetString()` del valor
  de la celda sin recortar espacios. Se ignoran las filas completamente vacías; una fila con código vacío
  y otros datos se informa como "falta el código" (CHK007). Límites: archivo de hasta 10 MB y 20.000 filas
  de datos; si los supera, se rechaza completa (CHK008).
- **Rationale**: ClosedXML expone el tipo de cada celda, que es lo que exige la lectura estricta, y su
  licencia es libre.
- **Alternatives considered**: EPPlus (licencia comercial desde la versión 5); ExcelDataReader (más
  rápido, pero orientado a lectura secuencial y con menos detalle del tipo de celda); NPOI (API más
  verbosa).

## R7. Importación atómica y concurrencia (FR-050, CHK031)

- **Decision**: toda la importación ocurre en una transacción `BEGIN IMMEDIATE`. Primero se validan todas
  las filas en memoria contra un diccionario de los artículos del proveedor, luego se aplican los cambios
  y se confirma. Solo corre una importación a la vez (un `SemaphoreSlim` en el servicio); una segunda
  recibe "hay una importación en curso". Los presupuestos no se bloquean: el servidor toma el precio del
  catálogo al grabar líneas nuevas, así que una línea grabada después de una importación usa el precio
  nuevo, y la pantalla lo muestra al volver de grabar.
- **Rationale**: 10.000 filas se procesan en segundos en memoria; una sola transacción garantiza "todo o
  nada".
- **Alternatives considered**: transacciones por lote (deja el catálogo a medio actualizar).

## R8. Historial de importaciones (CHK009)

- **Decision**: cada importación confirmada se guarda (`Importacion` y `FilaImportacion` solo para las
  filas no procesadas o con precio negativo o cero) y se puede consultar en la pantalla de importación.
- **Rationale**: la entidad Importación de la spec ya lo sugiere y permite revisar después qué quedó
  afuera.

## R9. ARCA (WSFEv1) y simulador

- **Decision**: interfaz `IServicioArca` en el módulo `Arca` con tres operaciones (último autorizado,
  solicitar CAE, consultar comprobante) que reflejan `FECompUltimoAutorizado`, `FECAESolicitar` y
  `FECompConsultar`. Implementación `ArcaSimulado` (persistida en `arca-simulado.json`, fuera del repo)
  con modos `Normal`, `Rechazar`, `SinRespuesta` y `AutorizarSinResponder`. `Arca:Entorno` solo acepta
  `Simulado`; cualquier otro valor impide arrancar (la conexión real con WSAA queda para cuando haya
  certificado de homologación). Valores de dominio: Factura B = tipo 6, Factura C = tipo 11; receptor sin
  identificar = DocTipo 99 y DocNro 0; con DNI = DocTipo 96; condición IVA del receptor = 5 (Consumidor
  Final); concepto 1 (productos); ids de alícuota 0 % = 3, 2,5 % = 9, 5 % = 8, 10,5 % = 4, 21 % = 5 y
  27 % = 6. La Factura C va sin arreglo de IVA, con ImpNeto = total.
- **Rationale**: aísla los cambios normativos (riesgo del PRD) y permite probar todos los casos de error
  sin certificado.
- **Alternatives considered**: llamar a WSFEv1 desde el módulo Facturación (mezcla protocolo y negocio).

## R10. Emisión, reintento y estados (FR-036 a FR-038b)

- **Decision**:
  - Las emisiones son secuenciales (un `SemaphoreSlim(1)` en el servicio de emisión) y la base las
    respalda con un índice único `(PuntoVenta, Tipo, Numero)` y un índice único parcial sobre
    `PresupuestoId` para los estados Pendiente, Bloqueada y Autorizada (CHK005).
  - Antes de enviar se graba la emisión `Pendiente` con número, tipo, receptor e importes: es la
    "foto" del comprobante. Un reintento usa esa foto aunque la Configuración haya cambiado (CHK032).
  - La fecha del comprobante es la del envío efectivo; en un reintento de otro día se usa la del nuevo
    envío (CHK002). WSFEv1 admite hasta 5 días hacia atrás para productos, así que el día del primer
    envío no condiciona el reintento.
  - Un rechazo borra la emisión Pendiente (no queda registrada, FR-034). La falta de respuesta a los 30
    segundos (`CancellationToken` con `TiempoEsperaSegundos`, contado desde el envío del pedido de CAE)
    la deja Pendiente (CHK019).
  - Reintento: `FECompConsultar` del número registrado → mismo total: Autorizada con el CAE recuperado;
    no existe: se envía; otro total: Bloqueada. Una Bloqueada pasa a Descartada solo con la confirmación
    de la operadora.
- **Rationale**: implementa RF-65 a RF-92 sin riesgo de duplicado y con la acción humana que exige el
  principio III de la constitución en el único caso ambiguo.

## R11. PDF y QR

- **Decision**: QuestPDF (licencia Community) para presupuesto y factura; QRCoder (MIT) para el QR. El QR
  codifica la URL de ARCA para comprobantes (`https://www.arca.gob.ar/fe/qr/?p=` + JSON en base64 con
  `ver`, `fecha`, `cuit`, `ptoVta`, `tipoCmp`, `nroCmp`, `importe`, `moneda`, `ctz`, `tipoDocRec`,
  `nroDocRec`, `tipoCodAut` = "E" y `codAut`) (CHK040). El dominio y la versión del JSON se validan contra
  la especificación vigente de ARCA antes de pasar a homologación. Con `Entorno = Simulado`, el PDF lleva
  la leyenda "COMPROBANTE SIMULADO — SIN VALIDEZ FISCAL" (FR-033). Las fuentes de la marca van
  embebidas desde `backend/Optica.Api/Recursos/`.
- **Rationale**: QuestPDF ya está decidido en `AGENTS.md`; QRCoder no tiene dependencias nativas.

## R12. Búsquedas sin acentos (RF-82, RF-88)

- **Decision**: columnas normalizadas que se calculan al grabar: `ApellidoBusqueda` y `NombreBusqueda` en
  minúsculas y sin diacríticos, y `DniBusqueda` y `NumeroBusqueda` solo con dígitos. Las búsquedas usan
  `LIKE '%texto%'` sobre esas columnas, con índices en la fecha.
- **Rationale**: SQLite no trae una intercalación sin acentos; con 10.000 presupuestos un recorrido con
  `LIKE` responde en milisegundos, muy por debajo de los 2 segundos de RNF-01.
- **Alternatives considered**: FTS5 (más complejo, y no resuelve la coincidencia parcial de DNI).

## R13. Inmutabilidad de registros cerrados

- **Decision**: además de la regla en el servicio, triggers de SQLite que hacen fallar cualquier
  `UPDATE` o `DELETE` sobre presupuestos Final y sus líneas, y sobre facturas Autorizadas o Descartadas
  (migraciones `CierrePresupuestos` y `FacturasInmutablesYBusqueda`, que nombra `AGENTS.md`).
- **Rationale**: es la defensa en profundidad que pide `AGENTS.md`; ninguna vía (ni un endpoint mal
  hecho) puede modificarlos.

## R14. Secretos y configuración

- **Decision**: `appsettings.json` versionado solo con valores no sensibles (`Arca:Entorno`,
  `PuntoVenta`, `TiempoEsperaSegundos`, `Simulador:Modo` y los datos públicos del emisor). La ruta y la
  clave del certificado, cuando haya uno, van por user-secrets en desarrollo y por variables de entorno
  en la PC del local (CHK037). El frontend no tiene secretos. `.gitignore` excluye la base, `*-wal`,
  `*-shm`, `arca-simulado.json` y los certificados.
- **Rationale**: principio IV de la constitución.

## R15. Registro de eventos de seguridad (CHK038)

- **Decision**: logging estándar de ASP.NET Core a archivo local con ingresos fallidos, bloqueos, cambios
  y restablecimientos de contraseña y cambios de Configuración. Nunca se registran contraseñas, DNI ni
  datos de clientes (CHK039). No hay pantalla de consulta: se lee el archivo si hace falta.

## R16. Límites de Configuración (CHK017)

- **Decision**: múltiplo de redondeo de 0,01 a 1.000; tope de identificación mayor a 0 y hasta
  999.999.999,99; margen predeterminado de 0 a 1000 % o sin configurar (null).

## R17. Ley 25.326 y registros inmutables (CHK036)

- **Decision**: fuera de este plan. Rectificar o suprimir datos de un presupuesto Final o de una factura
  contradice RF-08, RF-32 y la regla de `AGENTS.md`; cualquier pedido de un titular se resuelve por fuera
  del sistema hasta que el responsable del proyecto defina un criterio con asesoramiento legal.
  Registrado como pendiente; no se implementa ningún mecanismo de edición.

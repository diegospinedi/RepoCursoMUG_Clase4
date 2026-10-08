# Óptica Sistema Constitution

## Core Principles

### I. Test-First (NO NEGOCIABLE)

- Todo cambio de comportamiento MUST empezar por un test que lo describa y que falle (rojo)
  antes de escribir el código de producción.
- Se escribe el mínimo código necesario para que el test pase (verde) y recién después se
  refactoriza con todos los tests en verde (refactor).
- Aplica a backend (`dotnet test`, tests de integración con `AppDePrueba`) y a frontend
  (Vitest). Un cambio sin test que lo cubra no se considera terminado.
- Los criterios de aceptación del PRD (AC-xx) MUST traducirse en tests automatizados cuando
  sean verificables por código.
- Corregir un bug MUST incluir primero un test que lo reproduzca.

Rationale: el código lo genera en buena parte una IA que puede interpretar mal un requerimiento;
el test escrito antes es la especificación ejecutable que lo detecta.

### II. IA aislada en un módulo dedicado

- Toda llamada a modelos de IA (prompts, clientes de API, parseo de respuestas, reintentos)
  MUST vivir en un módulo propio detrás de una interfaz, del mismo modo que ARCA vive detrás
  de `IServicioArca`.
- La lógica de negocio (presupuestos, precios, facturación) MUST NOT invocar un modelo de IA
  directamente ni depender de tipos de un SDK de IA; solo conoce la interfaz.
- El módulo de IA MUST poder reemplazarse por un doble de prueba, de modo que los tests de
  negocio no dependan de un modelo real ni de la red.
- Hoy el alcance no incluye funciones de IA; si se agregan, rige este principio y aplica la
  regla de no ampliar el alcance sin consultar (AGENTS.md).

Rationale: los modelos cambian, fallan y no son deterministas; aislarlos mantiene la lógica de
negocio testeable y permite cambiar de proveedor sin tocarla.

### III. Fuente de verdad y revisión humana

- El sistema MUST NOT inventar, completar ni estimar datos que no estén en su fuente de verdad:
  precios y descripciones salen del catálogo, los parámetros fiscales de Configuración, los
  datos del emisor de `appsettings.json` y el CAE y la numeración de la respuesta de ARCA.
- Ante un dato faltante, ambiguo o una respuesta incierta (por ejemplo, ARCA sin respuesta o
  una salida de IA no verificable), el sistema MUST detener la operación y derivarla a revisión
  humana con un estado y un mensaje explícitos (como `EmisionPendiente`), nunca suponer un
  resultado.
- Las salidas de IA, si existieran, son sugerencias: MUST NOT grabarse como dato definitivo ni
  disparar una emisión fiscal sin confirmación de una persona.
- Los registros cerrados (presupuesto Final, factura con CAE) no se modifican (RF-08, RF-32).

Rationale: un precio o un comprobante fiscal inventado tiene consecuencias económicas y legales
que no se pueden revertir desde el sistema.

### IV. Sin secretos en el código

- Contraseñas, claves privadas, certificados, tokens y credenciales MUST NOT aparecer en el
  código fuente, en archivos versionados ni en los tests (los tests usan valores ficticios).
- Los secretos se cargan desde fuera del repositorio (variables de entorno, user-secrets de
  .NET o archivos excluidos por `.gitignore`); `appsettings.json` solo guarda configuración no
  sensible y, como mucho, la ruta a un secreto.
- La contraseña de acceso se guarda solo como hash con sal (RNF-14) y el certificado de ARCA no
  se expone en pantalla (RF-64).
- El frontend MUST NOT contener secretos: todo lo que se compila con Vite es público.
- Un secreto commiteado por error se considera comprometido y MUST rotarse; borrarlo del
  historial no alcanza.

Rationale: la base guarda datos personales (Ley 25.326) y el certificado permite emitir
comprobantes fiscales reales en nombre de la empresa.

### V. Backend y frontend separados por un contrato

- El sistema tiene dos partes: backend (ASP.NET Core Web API en `backend/`) y frontend (React +
  TypeScript en `frontend/`), que se comunican solo por la API HTTP bajo `/api`.
- Las reglas de negocio, los cálculos con valor legal y las validaciones definitivas MUST
  residir en el backend; el frontend puede anticiparlas para la experiencia de uso, pero MUST
  dar el mismo resultado que el servidor (por ejemplo, el redondeo en centavos).
- Todo cambio en un endpoint MUST actualizarse en ambos lados en el mismo cambio, con tests en
  ambos lados.
- Los errores de validación viajan como `ValidationProblem` con la clave del campo (RF-35).

Rationale: el servidor es la única fuente de verdad para precios y facturas; duplicar reglas
sin contrato produce diferencias de un centavo que terminan en un comprobante mal emitido.

## Restricciones adicionales

- Stack: .NET 10 + EF Core 10, SQLite, React 19 + TypeScript + Vite sobre Node 22.
- Todo desarrollo y prueba contra ARCA va al simulador o a homologación, nunca a producción.
- Las restricciones de alcance, convenciones de código y decisiones tomadas de `AGENTS.md`
  forman parte de esta constitución por referencia.

## Flujo de desarrollo

- Cada cambio MUST cerrarse con su propio commit al repositorio: un commit por cambio lógico,
  que incluya juntos el test y la implementación que lo hace pasar.
- Los mensajes de commit siguen Conventional Commits (`feat:`, `fix:`, `test:`, `refactor:`,
  `docs:`, `chore:`).
- Antes de cada commit MUST pasar la verificación completa: `dotnet test Optica.slnx` y, en
  `frontend/`, `npm test`, `npm run build` y `npm run lint`.
- No se commitea código con tests en rojo, salvo el commit explícito de un test rojo dentro del
  ciclo test-first, identificado como tal en el mensaje.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica del proyecto. `AGENTS.md` es la guía
  operativa de detalle; si ambos se contradicen, se corrige el que esté desactualizado.
- Las enmiendas se hacen con `/speckit-constitution`, quedan registradas en su propio commit e
  incrementan la versión según semver: MAJOR al quitar o redefinir un principio, MINOR al agregar
  un principio o sección o ampliar una regla, PATCH para aclaraciones de redacción.
- Cada plan (`/speckit-plan`) y cada revisión de código MUST verificar el cumplimiento de estos
  principios; cualquier excepción se justifica por escrito en el plan.

**Version**: 1.0.0 | **Ratified**: 2026-10-08 | **Last Amended**: 2026-10-08

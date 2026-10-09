# Feature Specification: Presupuestos y Facturación Electrónica a Consumidor Final

**Feature Branch**: `001-presupuestos-facturacion-arca`

**Created**: 2026-10-08

**Status**: Draft

**Input**: User description: "@PRD.md" (PRD-001: Presupuestos y Facturación Electrónica Online para Óptica
Sistema). Los identificadores RF-xx, RNF-xx y AC-xx remiten a ese documento, que es la fuente de verdad
de los requerimientos; esta especificación los organiza por recorrido de usuario.
Enmienda al PRD (pedido del usuario, 2026-10-08): la planilla Excel por proveedor es la fuente de ingreso
del catálogo. Da de alta artículos nuevos además de actualizar costos, el código en el proveedor es único
por proveedor y los artículos nuevos toman un margen predeterminado de Configuración. Esto amplía RF-20 a
RF-24 y cambia el formato de RF-23 (agrega la columna Descripción).

## Clarifications

### Session 2026-10-08

- Q: ¿Hasta qué porcentaje se puede cargar el margen de utilidad de un artículo (y el margen
  predeterminado de Configuración)? → A: de 0 a 1000 %; admite márgenes altos y frena errores groseros.
- Q: ¿Qué valores puede tomar la alícuota de IVA en la pantalla Configuración? → A: solo las que acepta
  ARCA: 0; 2,5; 5; 10,5; 21 y 27.
- Q: ¿Qué pasa con un artículo que el proveedor dejó de vender? → A: no hay bajas; los artículos quedan
  siempre activos y se corrigen a mano.
- Q: Cuando una planilla trae un artículo que ya existe, ¿se actualiza también su descripción o solo el
  precio de costo? → A: solo el precio de costo; la descripción se mantiene.
- Q: ¿Con qué número tiene que arrancar la numeración de los presupuestos? → A: 1.

### Session 2026-10-09

- Q: ¿Cambiar la alícuota de IVA o la condición fiscal tiene que modificar los precios de venta del
  catálogo? → A: no; el precio de venta depende solo del costo, el margen y el múltiplo de redondeo. La
  alícuota se usa solo para el desglose de la Factura B y el catálogo se recalcula solo al cambiar el
  múltiplo.
- Q: En una Factura B, ¿cómo se calculan el neto y el IVA a partir del total con IVA incluido? → A: sobre
  el total: neto = total / (1 + alícuota/100) redondeado a 2 decimales (mitad hacia arriba), e IVA =
  total − neto.
- Q: ¿Cómo tiene que leer el sistema los precios y los códigos de la planilla Excel? → A: estricta; solo
  se aceptan precios en celdas numéricas (los escritos como texto son "precio inválido") y los códigos se
  comparan exactamente, espacios incluidos.
- Q: ¿Cómo se cambia la contraseña de acceso y qué se hace si se olvida? → A: se cambia desde la
  aplicación ingresando la actual; si se olvida, se restablece solo desde la PC donde corre el sistema,
  definiendo una nueva sin conocer la anterior.
- Q: ¿Cómo encuentra la operadora una factura que quedó pendiente porque ARCA no respondió, y qué pasa si
  al reintentar aparece el caso de "número autorizado con otro total"? → A: el presupuesto muestra
  "Emisión pendiente" con Reintentar y el listado de facturas incluye las pendientes, filtrables por
  estado. En el caso de otro total, la emisión queda bloqueada hasta que la operadora confirme que revisó
  el punto de venta en ARCA; recién entonces se descarta y el presupuesto vuelve a poder facturarse.

## User Scenarios & Testing *(mandatory)*

Las usuarias son las dos dueñas de la óptica, que atienden el local ("la operadora"). Hoy arman los
presupuestos a mano con el folleto de costos del proveedor y facturan cargando los datos en el sitio de
ARCA. El sistema reemplaza ambas tareas.

### User Story 1 - Acceso protegido a la aplicación (Priority: P1)

La operadora define una contraseña la primera vez y desde entonces la necesita para entrar. Sin sesión
iniciada no se ve ningún dato de clientes, presupuestos, facturas ni configuración.

**Why this priority**: la base guarda datos personales de clientes (Ley 25.326). Ninguna otra pantalla
puede habilitarse sin este control.

**Independent Test**: definir la contraseña, cerrar sesión, intentar entrar con contraseñas incorrectas y
correctas, y comprobar que sin sesión no se obtiene ningún dato.

**Acceptance Scenarios**:

1. **Given** la pantalla de definición de contraseña, **When** ingreso una de 7 caracteres, **Then** el
   sistema no la acepta e indica que el mínimo es 8 (AC-57).
2. **Given** que no inicié sesión, **When** abro cualquier pantalla o pido cualquier dato, **Then** el
   sistema me lleva al ingreso o rechaza el pedido sin devolver datos (AC-79).
3. **Given** la pantalla de ingreso, **When** fallo 5 veces seguidas, **Then** el acceso queda bloqueado
   5 minutos (incluso con la contraseña correcta) y después se libera solo (AC-58).
4. **Given** una sesión iniciada, **When** pasan 60 minutos sin actividad, **Then** la sesión se cierra y
   se exige la contraseña de nuevo (AC-59).
5. **Given** una contraseña definida, **When** inspecciono los datos y la configuración guardados,
   **Then** la contraseña no aparece en texto plano (AC-80).
6. **Given** una sesión iniciada, **When** cambio la contraseña ingresando la actual y una nueva de al
   menos 8 caracteres, **Then** la nueva queda vigente y las demás sesiones se cierran; con la actual
   incorrecta, el cambio se rechaza.
7. **Given** una contraseña olvidada, **When** la restablezco desde la PC donde corre el sistema, **Then**
   puedo definir una nueva sin conocer la anterior; desde otro equipo de la red, el restablecimiento se
   rechaza.

---

### User Story 2 - Catálogo con precio de venta calculado y configuración del negocio (Priority: P1)

La operadora da de alta los proveedores y puede cargar o corregir artículos a mano, con su proveedor, su
costo (precio final, IVA incluido) y su margen; el sistema calcula y guarda el precio de venta. Desde la
pantalla de Configuración administra la alícuota de IVA, la condición fiscal, el tope de identificación
del receptor, el múltiplo de redondeo comercial y el margen predeterminado para artículos nuevos.

**Why this priority**: los presupuestos toman sus precios del catálogo; sin catálogo no hay presupuesto.
Es además el origen del error de precio que el sistema viene a eliminar.

**Independent Test**: configurar los parámetros, cargar artículos y verificar el precio de venta guardado
contra los ejemplos numéricos del PRD.

**Acceptance Scenarios**:

1. **Given** Responsable Inscripto, IVA 21 y múltiplo $0,01, **When** grabo un artículo con costo $1.210 y
   margen 50 %, **Then** el precio de venta guardado es $1.815,00 (AC-36).
2. **Given** Monotributo y múltiplo $0,01, **When** grabo un artículo con costo $1.210 y margen 50 %,
   **Then** el precio de venta guardado es $1.815,00 (AC-83).
3. **Given** múltiplo $50 y un precio calculado de $1.815,37, **When** grabo el artículo, **Then** se guarda
   $1.850,00; con múltiplo $0,01 y calculado $1.666,656 se guarda $1.666,66 (AC-38, AC-39).
4. **Given** un artículo con costo $1.210, margen 50 % y venta $1.815,00, **When** cambio el margen a 60 %
   o el costo a $2.420 y grabo, **Then** la venta pasa a $1.936,00 o $3.630,00 respectivamente (AC-70,
   AC-71).
5. **Given** IVA 21, **When** cambio la alícuota a 10,5 y grabo un artículo con costo $1.105 y margen 50 %,
   **Then** el precio de venta guardado es $1.657,50 (AC-72).
6. **Given** un artículo con venta $1.815,00 y múltiplo $0,01, **When** cambio el múltiplo a $50 en
   Configuración, **Then** el artículo pasa a $1.850,00 sin editarlo (AC-85).
7. **Given** un artículo grabado, **When** lo abro para editar, **Then** el precio de venta se muestra y no
   se puede modificar (AC-81).
8. **Given** la pantalla de Configuración, **When** ingreso un múltiplo de 0,001, **Then** no se acepta y
   se indica que el mínimo es 0,01 (AC-82).
9. **Given** un artículo del proveedor "Lentes SA" con código "ABC-1", **When** intento grabar otro de
   "Lentes SA" con el mismo código, **Then** el sistema lo rechaza; "abc-1" en "Lentes SA" o "ABC-1" en
   otro proveedor se aceptan como artículos distintos.

---

### User Story 3 - Importar la planilla de precios de un proveedor (Priority: P1)

La operadora elige un proveedor y carga la planilla Excel que ese proveedor le envía. El sistema da de
alta los artículos nuevos, actualiza el costo de los que ya existen, recalcula los precios de venta y
muestra un resumen con lo creado, lo actualizado y lo que no pudo procesar, con la razón.

**Why this priority**: la planilla es la fuente de ingreso del catálogo. Sin ella, los artículos se
cargarían uno por uno y volvería el error de transcripción que el sistema viene a eliminar.

**Independent Test**: con un proveedor dado de alta, importar planillas con filas nuevas, existentes,
inválidas y con formato incorrecto, y verificar el catálogo resultante y el resumen.

**Acceptance Scenarios**:

1. **Given** el proveedor "Lentes SA" sin artículos, margen predeterminado 50 %, Responsable Inscripto, IVA
   21 y múltiplo $0,01, **When** importo una planilla con la fila "ABC-1", "Armazón metal", $1.210,
   **Then** se crea el artículo de "Lentes SA" con esa descripción, costo $1.210,00, margen 50 % y precio de
   venta $1.815,00, y el resumen lo informa como creado.
2. **Given** IVA 21, múltiplo $0,01 y un artículo "ABC-1" de "Lentes SA" con costo $1.210 y margen 50 %,
   **When** importo para "Lentes SA" la fila "ABC-1" con $2.420, **Then** el costo pasa a $2.420,00, la
   venta a $3.630,00, el margen y la descripción no cambian y el resumen lo informa como actualizado
   (AC-14).
3. **Given** "ABC-1" existe en "Lentes SA" y en "Ópticos SRL", **When** importo una planilla para "Lentes
   SA", **Then** solo cambia el artículo de "Lentes SA".
4. **Given** una planilla con una fila de precio vacío, otra con precio "abc", otra con el texto
   "1.210,50" y otras válidas, **When** la importo, **Then** se procesan las válidas y las tres se listan
   como no procesadas con la razón "precio inválido" (AC-75).
5. **Given** una planilla con una fila de código nuevo y descripción vacía, **When** la importo, **Then**
   esa fila no se procesa y se lista con la razón "falta la descripción".
6. **Given** una planilla con una columna de más, una faltante o encabezados distintos, **When** la
   importo, **Then** no se crea ni actualiza ningún artículo y se informa que el formato no es el esperado
   (AC-76).
7. **Given** una planilla con filas de precio -100 y 0, **When** la importo, **Then** se procesan y se
   listan con la indicación "actualizado con precio negativo o cero" (AC-90).
8. **Given** una planilla con el mismo código en dos filas, **When** la importo, **Then** ninguna de esas
   filas se procesa y se listan con la razón "código repetido en la planilla".
9. **Given** un catálogo de 10.000 artículos y una planilla de 10.000 filas, **When** la importo, **Then**
   termina en menos de 120 segundos (AC-62).
10. **Given** un artículo de "Lentes SA" que no figura en la planilla, **When** la importo, **Then** el
    artículo queda sin cambios.
11. **Given** el margen predeterminado sin configurar, **When** intento importar, **Then** el sistema no
    procesa la planilla e indica que primero hay que configurar el margen predeterminado.

---

### User Story 4 - Armar y cerrar un presupuesto (Priority: P1)

La operadora carga los datos del cliente, agrega líneas buscando artículos del catálogo, ajusta cantidad y
descuento, y graba. El presupuesto queda en Borrador (editable) hasta que lo pasa a Final, momento en que
queda cerrado para siempre.

**Why this priority**: es el uso diario principal y el primer objetivo del PRD (registro centralizado y
sin errores de precio).

**Independent Test**: con un catálogo cargado, crear un presupuesto, verificar cálculos y numeración,
modificarlo en Borrador, pasarlo a Final y comprobar que ya no admite cambios.

**Acceptance Scenarios**:

1. **Given** un presupuesto nuevo con Apellido, Nombre y DNI y al menos una línea, **When** grabo por
   primera vez, **Then** queda en Borrador, con número igual al último + 1 (154 → 155) y un mensaje que
   confirma la grabación y el estado (AC-01, AC-04, AC-06).
2. **Given** un presupuesto sin DNI, **When** grabo, **Then** no se graba y junto al campo DNI aparece
   "Ingresá el DNI del cliente"; Domicilio, Email y Teléfono no se exigen (AC-34, AC-44, AC-45).
3. **Given** una línea, **When** busco un artículo por código o descripción y lo selecciono, **Then** se
   completan código, descripción y precio unitario con el precio de venta guardado, sin recalcularlo
   (AC-10, AC-40).
4. **Given** una línea con precio unitario $1.000, **When** cargo descuento 0 % o 10 %, **Then** el precio
   con descuento es $1.000,00 o $900,00, y con cantidad 3 el precio final es $2.700,00 (AC-26, AC-35,
   AC-27).
5. **Given** una línea con precio con descuento sin redondear $3,335 y cantidad 3, **When** agrego otra
   línea de $2.700,00, **Then** la línea muestra $3,34 y $10,02 y el total es $2.710,02 (AC-29, AC-11).
6. **Given** una línea, **When** ingreso cantidad 0, negativa o 2,5, o descuento fuera de 0–100, **Then** el
   valor no se acepta y junto al campo aparece "La cantidad debe ser un número entero mayor a 0" o "El
   descuento debe estar entre 0 y 100" (AC-12, AC-42, AC-43).
7. **Given** un presupuesto en Borrador, **When** modifico su Domicilio y grabo, **Then** el cambio queda
   guardado y se ve al reabrirlo (AC-07).
8. **Given** un presupuesto en Borrador, **When** lo paso a Final y grabo, **Then** queda en Final (AC-05).
9. **Given** un presupuesto en Final, **When** intento modificar datos, líneas o volverlo a Borrador por
   cualquier vía, **Then** el sistema lo rechaza y el presupuesto queda igual (AC-66, AC-67).
10. **Given** dos sesiones, **When** ambas graban un presupuesto nuevo al mismo tiempo, **Then** los dos
    quedan grabados con números distintos y consecutivos, sin errores (AC-63).
11. **Given** un presupuesto grabado con una línea a $1.815,00, **When** el catálogo se recalcula y el
    artículo pasa a $1.850,00, **Then** la línea conserva $1.815,00 (AC-86).
12. **Given** un presupuesto con líneas, **When** lo veo en pantalla, **Then** ningún importe discrimina IVA
    (AC-28).

---

### User Story 5 - PDF y búsqueda de presupuestos (Priority: P2)

La operadora descarga el PDF de un presupuesto Final para entregarlo o enviarlo a mano por email o
WhatsApp, y encuentra presupuestos anteriores por fecha o datos del cliente.

**Why this priority**: completa el ciclo del presupuesto, pero el registro ya aporta valor sin el PDF.

**Independent Test**: con presupuestos grabados, descargar el PDF de uno Final, comprobar que uno en
Borrador no lo permite y ejecutar las búsquedas de los ejemplos.

**Acceptance Scenarios**:

1. **Given** un presupuesto en Final, **When** presiono Descargar PDF, **Then** se descarga un PDF con el
   logo, los colores de la óptica y la leyenda "Precios finales, IVA incluido" (AC-02, AC-09, AC-37).
2. **Given** un presupuesto en Borrador, **When** lo abro o pido su PDF por cualquier vía, **Then** la
   descarga está deshabilitada y el pedido se rechaza sin generar el PDF (AC-08).
3. **Given** clientes "González" y "Gómez", **When** busco el apellido "ONZALEZ", **Then** solo aparece
   "González" (AC-03).
4. **Given** presupuestos del 09, 10, 20 y 21/03/2026, **When** busco Desde 10/03 Hasta 20/03, **Then**
   aparecen solo los del 10 y del 20; con apellido "González" y solo Desde 15/03 aparece solo el del 20/03
   (AC-69, AC-68).
5. **Given** un cliente con DNI 23.456.789, **When** busco "3456", **Then** aparecen sus presupuestos
   (AC-87).

---

### User Story 6 - Facturar un presupuesto Final (Priority: P2)

Desde un presupuesto Final, la operadora presiona Facturar; el sistema arma el comprobante a consumidor
final (Factura B o C según la condición fiscal), obtiene el CAE de ARCA y guarda la factura vinculada al
presupuesto. La operadora descarga el PDF de la factura.

**Why this priority**: elimina la carga manual en el sitio de ARCA, pero depende de que existan
presupuestos Final (historias 2 a 4).

**Independent Test**: con un presupuesto Final y ARCA respondiendo (o su simulador), facturar y verificar
tipo de comprobante, identificación del receptor, importes enviados, CAE guardado y PDF.

**Acceptance Scenarios**:

1. **Given** un presupuesto Final con 3 líneas, **When** presiono Facturar, **Then** la factura contiene
   las 3 líneas, el total enviado es igual al del presupuesto y no se vuelve a cargar ningún dato (AC-16).
2. **Given** un presupuesto en Borrador, **When** pido facturarlo por cualquier vía, **Then** la acción
   está deshabilitada y se rechaza sin contactar a ARCA (AC-17).
3. **Given** ARCA autoriza, **When** facturo, **Then** se guarda la factura con número, CAE y vencimiento,
   vinculada al presupuesto, y se confirma con el número de comprobante (AC-18).
4. **Given** Responsable Inscripto, IVA 21 y un presupuesto de $1.815,00, **When** facturo, **Then** se
   solicita Factura B por $1.815,00 con neto $1.500,00 e IVA $315,00; con un total de $1.000,00, neto
   $826,45 e IVA $173,55 (AC-31, AC-47, AC-48).
5. **Given** Monotributo y un presupuesto de $1.815,00, **When** facturo, **Then** se solicita Factura C por
   $1.815,00 sin desglose de neto e IVA (AC-73, AC-84).
6. **Given** un total menor o igual al tope configurado, **When** facturo, **Then** el receptor se
   identifica como "Consumidor Final" sin identificar, aunque haya DNI; si el total supera el tope, se
   envía el DNI (AC-41, AC-77, AC-21).
7. **Given** una factura autorizada, **When** descargo su PDF, **Then** contiene todos los datos de RF-30,
   incluido el código QR de ARCA (AC-19).
8. **Given** una factura con CAE, **When** intento modificarla o eliminarla por cualquier vía, **Then** el
   sistema no lo permite y la factura queda igual (AC-32).
9. **Given** la pantalla de Configuración, **When** cambio el tope y grabo, **Then** la siguiente
   facturación aplica el nuevo tope (AC-49).

---

### User Story 7 - Emisión segura ante fallas de ARCA (Priority: P2)

Si ARCA rechaza el pedido o no responde, la operadora ve el motivo y decide cuándo reintentar. El sistema
nunca deja un comprobante duplicado ni inventa un resultado: ante la duda, verifica en ARCA o pide
revisión humana.

**Why this priority**: un comprobante duplicado o mal registrado no se puede anular desde el sistema (las
notas de crédito están fuera de alcance).

**Independent Test**: con el simulador de ARCA configurado para rechazar, no responder o autorizar sin
responder, facturar y reintentar, verificando cada resultado.

**Acceptance Scenarios**:

1. **Given** ARCA rechaza, **When** facturo, **Then** no se registra factura, se muestran el código y la
   descripción del error y se permite reintentar (AC-20).
2. **Given** ARCA no responde, **When** facturo, **Then** la espera termina a los 30 segundos, no se
   reintenta automáticamente y se muestra el motivo (AC-60).
3. **Given** una emisión sin respuesta que ARCA sí autorizó con el número registrado y el mismo total,
   **When** reintento, **Then** se recupera y guarda ese CAE sin emitir otro comprobante (AC-64).
4. **Given** una emisión sin respuesta que no llegó a autorizarse, **When** reintento, **Then** el sistema
   verifica que el número registrado no figura autorizado y recién entonces envía el pedido (AC-65).
5. **Given** una emisión sin respuesta cuyo número figura autorizado con otro total, **When** reintento,
   **Then** no se emite ni recupera nada, la emisión queda "Bloqueada" y se avisa que hay que revisar el
   punto de venta en ARCA (AC-89).
6. **Given** una emisión sin respuesta, **When** abro el presupuesto o el listado de facturas, **Then** veo
   la emisión como "Pendiente" con la acción Reintentar, y puedo filtrar el listado por ese estado.
7. **Given** una emisión "Bloqueada", **When** intento reintentarla, **Then** el sistema no lo permite;
   **When** confirmo que revisé el punto de venta en ARCA, **Then** la emisión queda "Descartada" y el
   presupuesto puede volver a facturarse con un número nuevo.

---

### User Story 8 - Consultar facturas emitidas (Priority: P3)

La operadora lista y busca las facturas por fecha, número de comprobante y datos del cliente, y ve el
presupuesto de origen de cada una.

**Why this priority**: consulta posterior; la emisión ya funciona sin ella.

**Independent Test**: con facturas emitidas, ejecutar las búsquedas de los ejemplos.

**Acceptance Scenarios**:

1. **Given** facturas a "González" del 10/03 y 20/03/2026 y a "Gómez" del 20/03/2026, **When** busco
   "gonzalez" Desde 15/03/2026, **Then** aparece solo la de "González" del 20/03 (AC-22).
2. **Given** la factura 0003-00034561, **When** busco el número "34561", **Then** aparece esa factura
   (AC-88).
3. **Given** un cliente con DNI 23.456.789, **When** busco "3456", **Then** aparecen sus facturas (AC-87).

---

### Edge Cases

- Artículo con precio de venta negativo o cero (por ejemplo, por un costo mal cargado): no puede usarse
  en una línea con precio negativo; se informa "El precio unitario no puede ser negativo" (RF-17, AC-12).
- Planilla vacía (solo encabezados): no cambia nada y el resumen lo informa.
- Error inesperado a mitad de la importación: no se aplica ningún cambio de esa planilla, para no dejar
  el catálogo a medio actualizar.
- Cambio de margen predeterminado: afecta solo a los artículos que se creen después, no a los existentes.
- Total del presupuesto exactamente igual al tope de identificación: receptor sin identificar (AC-77).
- Corte de internet: catálogo y presupuestos siguen operando; solo la facturación queda pendiente hasta
  reintentar.
- Cambio de múltiplo de redondeo: recalcula todo el catálogo pero no los presupuestos ya grabados (RF-86,
  RF-87). Cambiar la alícuota o la condición fiscal no modifica ningún precio de venta.
- Cambio de condición fiscal con presupuestos Final aún sin facturar: se factura con la condición vigente
  al momento de emitir.
- Dos operadoras facturando a la vez: las emisiones se procesan de a una para no duplicar números.
- Búsquedas con acentos, mayúsculas, puntos o guiones: se ignoran según RF-82 y RF-88.
- Intento de facturar dos veces el mismo presupuesto: se rechaza, porque un presupuesto origina como
  máximo una factura (ver Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

**Acceso**

- **FR-001**: El sistema MUST exigir una contraseña de al menos 8 caracteres para acceder, definida la
  primera vez solo desde la PC donde corre el sistema (RNF-04).
- **FR-002**: El sistema MUST bloquear el ingreso 5 minutos tras 5 intentos fallidos consecutivos y
  liberarlo solo (RNF-08).
- **FR-003**: El sistema MUST cerrar la sesión tras 60 minutos de inactividad (RNF-09).
- **FR-004**: El sistema MUST guardar la contraseña solo como hash con sal, nunca en texto plano (RNF-14).
- **FR-005**: El sistema MUST rechazar sin devolver datos todo pedido sin sesión iniciada (AC-79).
- **FR-005a**: El sistema MUST permitir cambiar la contraseña desde una sesión iniciada, exigiendo la
  contraseña actual y aplicando las reglas de FR-001.
- **FR-005b**: El sistema MUST permitir restablecer una contraseña olvidada, sin conocer la anterior,
  únicamente desde la PC donde corre el sistema; el pedido desde cualquier otro equipo MUST rechazarse.
- **FR-005c**: Al cambiar o restablecer la contraseña, el sistema MUST cerrar todas las demás sesiones
  abiertas y liberar un bloqueo por intentos fallidos vigente.

**Configuración**

- **FR-006**: El sistema MUST permitir administrar desde una pantalla protegida la alícuota de IVA, la
  condición fiscal (Responsable Inscripto o Monotributo), el tope de identificación del receptor, el
  múltiplo de redondeo comercial y el margen predeterminado para artículos nuevos (0 a 1000 %) (RF-63).
- **FR-007**: El sistema MUST rechazar un múltiplo de redondeo menor a 0,01 (RF-72).
- **FR-007a**: El sistema MUST admitir como alícuota de IVA únicamente 0; 2,5; 5; 10,5; 21 o 27, las que
  acepta ARCA, y rechazar cualquier otro valor indicando los valores válidos.
- **FR-008**: El sistema MUST NOT mostrar ni permitir editar en pantalla el certificado digital ni el punto
  de venta de ARCA (RF-64).
- **FR-009**: Al grabar un cambio del múltiplo de redondeo, el sistema MUST recalcular y guardar el precio
  de venta de todo el catálogo, sin modificar líneas de presupuestos ya grabados (RF-86, RF-87). Un cambio
  de alícuota o de condición fiscal MUST NOT modificar precios de venta: solo afecta a las facturas que se
  emitan después (RF-86, acotado por clarificación).

**Catálogo**

- **FR-010**: El sistema MUST permitir cargar y modificar artículos con código autonumérico, proveedor,
  código en el proveedor, descripción, precio de costo (final, con IVA) y margen de utilidad (RF-20).
- **FR-011**: El código en el proveedor MUST ser único dentro de cada proveedor y distinguir mayúsculas de
  minúsculas ("ABC-1" y "abc-1" son artículos distintos); proveedores distintos pueden repetir códigos.
- **FR-011a**: El sistema MUST permitir dar de alta proveedores y modificar su nombre, que es obligatorio
  y único.
- **FR-012**: El sistema MUST calcular el precio de venta como costo × (1 + margen / 100) en las dos
  condiciones fiscales, sin intervención de la alícuota de IVA. Con Responsable Inscripto es el resultado
  de los tres pasos de RF-21 con alícuota única; con Monotributo, la fórmula de RF-84 (RF-21, RF-84).
- **FR-013**: El sistema MUST redondear el precio de venta hacia arriba al múltiplo de redondeo comercial
  configurado y guardarlo ya redondeado (RF-56, RF-58).
- **FR-014**: El sistema MUST recalcular el precio de venta al modificar costo o margen, y mostrarlo como
  dato de solo lectura (RF-45, RF-70).
- **FR-015**: El sistema MUST aceptar márgenes de utilidad entre 0 % y 1000 %, ambos inclusive, y
  rechazar valores fuera de ese rango indicando el rango válido (RF-21, ampliado por clarificación).

**Importación de planillas por proveedor**

- **FR-042**: El sistema MUST permitir importar una planilla Excel para un proveedor elegido por la
  operadora, con tres columnas en este orden y con estos encabezados: Código en el proveedor, Descripción
  y Precio de Costo (precio final del proveedor, con IVA) (RF-22, RF-23, enmendado).
- **FR-043**: El sistema MUST rechazar completa, sin cambiar ningún artículo, una planilla con columnas
  faltantes, columnas de más o encabezados distintos, e informar que el formato no es el esperado (RF-78,
  RF-79).
- **FR-044**: Por cada fila cuyo código ya exista en ese proveedor, el sistema MUST actualizar el precio de
  costo y recalcular el precio de venta, sin cambiar descripción ni margen (RF-47). En esas filas la
  descripción de la planilla se ignora y puede venir vacía.
- **FR-045**: Por cada fila cuyo código no exista en ese proveedor, el sistema MUST crear el artículo con
  la descripción de la planilla, el precio de costo y el margen predeterminado vigente, y calcular su
  precio de venta. Si el margen predeterminado no está configurado, el sistema MUST NOT iniciar la
  importación e indica que se configure primero.
- **FR-045a**: El sistema MUST aceptar como precio solo celdas de tipo numérico; una celda vacía o de texto
  es "precio inválido", aunque el texto parezca un número ("1.210,50", "1210.50"). El código en el
  proveedor MUST tomarse tal como está en la celda y compararse exactamente, sin recortar espacios: "ABC-1"
  y "ABC-1 " son códigos distintos. Una celda de código numérica se toma por su valor ("00123" con formato
  de número se lee 123).
- **FR-046**: El sistema MUST NOT procesar una fila con precio inválido según FR-045a, una fila nueva sin
  descripción ("falta la descripción") ni las filas cuyo código aparezca más de una vez en la planilla
  ("código repetido en la planilla") (RF-80).
- **FR-047**: El sistema MUST aceptar precios negativos o cero y listar esas filas con la indicación
  "actualizado con precio negativo o cero" (RF-80, RF-93).
- **FR-048**: Al terminar, el sistema MUST mostrar un resumen con la cantidad de artículos creados y
  actualizados y la lista de filas no procesadas con su número de fila, código y razón (RF-24).
- **FR-049**: Los artículos del proveedor que no figuren en la planilla MUST quedar sin cambios, y los de
  otros proveedores no se tocan.
- **FR-050**: Una importación MUST aplicarse completa o no aplicarse: si se interrumpe, el catálogo queda
  como estaba antes.

**Presupuestos**

- **FR-016**: El sistema MUST grabar presupuestos con los datos del cliente (Apellido, Nombre, DNI
  obligatorios; Domicilio, Email y Teléfono opcionales) guardados dentro del presupuesto (RF-05, RF-38,
  RF-61, RF-74).
- **FR-017**: El sistema MUST numerar cada presupuesto al grabarlo por primera vez, empezando por 1, con el
  último número + 1, sin repetir números con grabaciones simultáneas (RF-06, RNF-13).
- **FR-018**: El sistema MUST manejar los estados Borrador y Final; solo se modifica en Borrador, y un
  presupuesto Final MUST NOT modificarse ni volver a Borrador por ninguna vía (RF-04, RF-07, RF-08, RF-67).
- **FR-019**: El sistema MUST permitir buscar artículos por código o descripción y, al seleccionar uno,
  completar código, descripción y precio unitario con el precio de venta guardado. El precio unitario de
  la línea no es editable: para bajarlo se usa el descuento (RF-11, RF-39, RF-68, RF-73).
- **FR-020**: Cada línea MUST registrar código, descripción, precio unitario, cantidad (entera, mayor a 0),
  porcentaje de descuento (0 a 100), precio con descuento y precio final (RF-12, RF-16, RF-59, RF-60).
- **FR-021**: El sistema MUST calcular precio con descuento = precio unitario × (1 − descuento / 100)
  redondeado a 2 decimales (mitad hacia arriba); precio final = precio con descuento redondeado ×
  cantidad, redondeado; total = suma de precios finales redondeados (RF-13, RF-14, RF-15, RF-19, RF-44).
- **FR-022**: Los importes en pantalla y en el PDF del presupuesto MUST expresarse con IVA incluido y sin
  discriminar IVA (RF-18, RF-42).
- **FR-023**: El sistema MUST rechazar una línea cuyo precio unitario sea negativo (RF-17).
- **FR-024**: Una línea ya grabada MUST conservar su descripción y precio unitario aunque el artículo
  cambie en el catálogo (RF-87).

**PDF y búsqueda de presupuestos**

- **FR-025**: El sistema MUST generar y permitir descargar el PDF de un presupuesto Final, con logo, colores
  de la óptica y la leyenda "Precios finales, IVA incluido", y MUST rechazarlo para un Borrador (RF-02,
  RF-09, RF-36, RF-37, RF-55).
- **FR-026**: El sistema MUST buscar presupuestos por rango de fechas (Desde/Hasta inclusive, uno solo
  admitido), apellido, nombre y DNI, combinando filtros con "Y"; apellido y nombre por coincidencia parcial
  sin distinguir mayúsculas ni acentos; DNI por coincidencia parcial ignorando puntos y guiones (RF-03,
  RF-81, RF-82, RF-83, RF-88).

**Facturación**

- **FR-027**: El sistema MUST permitir facturar solo presupuestos en estado Final, tomando sus datos y
  líneas sin volver a cargarlos (RF-25, RF-62).
- **FR-028**: El sistema MUST emitir Factura B con Responsable Inscripto y Factura C con Monotributo (solo
  con el total) (RF-26, RF-43, RF-46, RF-76, RF-85). En la Factura B, el desglose se calcula una vez sobre
  el total al emitir: neto = total / (1 + alícuota / 100), redondeado a 2 decimales (mitad hacia arriba), e
  IVA = total − neto, de modo que neto + IVA es siempre igual al total.
- **FR-029**: El sistema MUST identificar al receptor como "Consumidor Final" sin identificar si el total
  es menor o igual al tope configurado, y con su DNI si lo supera (RF-27, RF-49, RF-71).
- **FR-030**: El sistema MUST solicitar el CAE a ARCA y guardar la factura autorizada con número (por punto
  de venta), CAE, vencimiento del CAE y vínculo al presupuesto de origen (RF-28, RF-29).
- **FR-031**: Una factura con CAE MUST NOT modificarse ni eliminarse por ninguna vía (RF-32).
- **FR-032**: El sistema MUST generar y permitir descargar el PDF de la factura con los datos de RF-30
  (emisor, tipo, punto de venta, número, fecha, receptor, líneas, total, CAE, vencimiento y QR de ARCA)
  (RF-30, RF-50, RF-55).
- **FR-033**: Mientras la autorización se obtenga de un entorno simulado, el PDF de la factura MUST mostrar
  la leyenda "COMPROBANTE SIMULADO — SIN VALIDEZ FISCAL".

**Fallas de ARCA**

- **FR-034**: Si ARCA rechaza el pedido, el sistema MUST NOT registrar la factura y MUST mostrar el código
  y la descripción del error (RF-31, RF-51).
- **FR-035**: El sistema MUST dar por no disponible a ARCA tras 30 segundos sin respuesta y MUST NOT
  reintentar por su cuenta; el reintento lo decide la operadora (RF-52, RNF-10).
- **FR-036**: Antes de enviar cada pedido, el sistema MUST registrar el número de comprobante que solicita
  (último autorizado + 1), y emitir de a una factura por vez (RF-89).
- **FR-037**: Al reintentar una emisión sin respuesta, el sistema MUST consultar a ARCA el número
  registrado: si figura autorizado con el mismo total, recupera y guarda su CAE sin emitir otro; si no
  figura, envía el pedido; si figura con otro total, no emite ni recupera nada y avisa que se revise el
  punto de venta en ARCA (RF-65, RF-66, RF-75, RF-90, RF-91, RF-92).
- **FR-038**: Cada emisión MUST estar en uno de estos estados: Pendiente (sin respuesta de ARCA, se puede
  reintentar), Bloqueada (caso de otro total de FR-037, no se puede reintentar), Autorizada (con CAE,
  final) o Descartada (final, sin comprobante). Un rechazo de ARCA no registra ninguna emisión (FR-034),
  con lo que se cumplen los dos resultados de RF-53: autorizada con CAE o no registrada (RF-53).
- **FR-038a**: Un presupuesto con una emisión Pendiente MUST mostrar "Emisión pendiente" y la acción
  Reintentar; con una emisión Bloqueada, el aviso de revisar el punto de venta en ARCA.
- **FR-038b**: Una emisión Bloqueada MUST pasar a Descartada solo cuando la operadora confirma
  expresamente que revisó el punto de venta en ARCA. Una emisión Descartada libera el presupuesto para
  una nueva emisión, que solicita el número siguiente al último autorizado.

**Consulta de facturas e interfaz**

- **FR-039**: El sistema MUST listar facturas, incluidas las emisiones Pendientes y Bloqueadas con su
  estado, y buscarlas por estado, rango de fechas, número de comprobante y datos del cliente, con los
  mismos criterios de FR-026 (RF-33, RF-54, RF-81, RF-82, RF-83, RF-88).
- **FR-040**: El sistema MUST mostrar el logo y la paleta de colores de la óptica (primario #0903A0 sobre
  #FFFFFF) en todas las pantallas y PDF (RF-34, RF-55).
- **FR-041**: Todo error de validación MUST indicar junto al campo afectado qué hacer para corregirlo
  (RF-35).

### Key Entities *(include if feature involves data)*

- **Proveedor**: nombre único. Agrupa sus artículos y es el destino de cada importación.
- **Artículo**: código autonumérico, proveedor, código en el proveedor (único dentro del proveedor,
  sensible a mayúsculas), descripción, precio de costo, margen de utilidad y precio de venta calculado y
  redondeado.
- **Importación**: proveedor, fecha, cantidades de creados y actualizados, y filas no procesadas o con
  precio negativo o cero, con su razón. Es el resumen que ve la operadora.
- **Configuración**: alícuota de IVA, condición fiscal, tope de identificación, múltiplo de redondeo y
  margen predeterminado para artículos nuevos. Valores únicos para todo el sistema.
- **Presupuesto**: número, fecha, estado (Borrador/Final), datos del cliente (Apellido, Nombre, DNI,
  Domicilio, Email, Teléfono) y total. Cerrado e inmutable al pasar a Final.
- **Línea de presupuesto**: código, descripción y precio unitario copiados del artículo al cargarla;
  cantidad, descuento, precio con descuento y precio final.
- **Factura**: tipo (B o C), punto de venta, número, fecha, identificación del receptor, importes (total;
  neto e IVA en Factura B), CAE, vencimiento, estado de emisión (Pendiente, Bloqueada, Autorizada,
  Descartada) y presupuesto de origen. Inmutable una vez Autorizada o Descartada.
- **Acceso**: hash de la contraseña, contador de intentos fallidos y bloqueo temporal.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100 % de los precios unitarios de los presupuestos coincide con el precio de venta del
  catálogo vigente al cargar la línea: no hay transcripción manual de precios.
- **SC-002**: Todos los ejemplos numéricos del PRD (AC-11, AC-13, AC-14, AC-29,
  AC-36, AC-38, AC-39, AC-70 a AC-74, AC-83 a AC-85) dan exactamente el importe esperado, al centavo.
- **SC-003**: Con 10.000 presupuestos guardados, 19 de cada 20 búsquedas por apellido muestran resultados
  en menos de 2 segundos desde que se presiona Buscar (RNF-01).
- **SC-004**: Sin contar la espera de ARCA, 19 de cada 20 facturas se confirman en menos de 2 segundos
  desde que se presiona Facturar (RNF-11).
- **SC-005**: Ninguna secuencia de rechazo, falta de respuesta y reintento produce un comprobante
  duplicado, ni una factura Autorizada sin CAE, ni un presupuesto trabado sin una acción visible para la
  operadora (Reintentar o confirmar la revisión).
- **SC-006**: Dos operadoras grabando presupuestos a la vez nunca reciben errores ni números repetidos.
- **SC-007**: La operadora emite una factura a partir de un presupuesto Final sin volver a ingresar ningún
  dato del cliente ni de los artículos.
- **SC-008**: Todos los escenarios de aceptación pasan en Chrome y Edge, versión estable vigente y la
  inmediata anterior (RNF-03).
- **SC-010**: Una planilla de proveedor de hasta 10.000 filas se importa en menos de 120 segundos, y la
  operadora no carga a mano ningún artículo que venga en la planilla (RNF-12).
- **SC-011**: El 100 % de las filas no procesadas aparece en el resumen con su razón: ninguna fila se
  pierde sin aviso.
- **SC-009**: Un catálogo de 10.000 artículos se recalcula completo tras un cambio del múltiplo de redondeo
  sin que la operadora deba editar ningún artículo.

## Assumptions

- **Fuera de esta especificación** (quedan para una feature posterior): backups con su aviso (RNF-02,
  RNF-05, RNF-06, RF-77, AC-52 a AC-55).
- La planilla es un archivo .xlsx con los datos en la primera hoja y los encabezados en la primera fila.
  Como cada proveedor envía su propio formato, la operadora lo adapta a las tres columnas de FR-042 antes
  de importar.
- AC-15 del PRD (código inexistente = no actualizado) queda reemplazado: un código inexistente ahora da de
  alta el artículo.
- No hay bajas ni desactivación de artículos en esta versión: todos quedan siempre activos y disponibles
  para cargar líneas. Ni la carga manual ni la importación eliminan artículos; un discontinuado se corrige
  a mano (por ejemplo, en su descripción).
- El margen predeterminado no tiene valor inicial: hasta que la operadora lo configure, la importación se
  bloquea en lugar de crear artículos con un margen supuesto (constitución, principio III).
- Fuera de alcance según el PRD: Facturas A, notas de crédito/débito y anulaciones, envío automático por
  email o WhatsApp, ABM de clientes y de usuarios con roles, eliminación de presupuestos, stock, cobros e
  impresión directa.
- Hasta contar con el certificado de homologación, la autorización de comprobantes se desarrolla y prueba
  contra un simulador de ARCA que permite reproducir autorización, rechazo y falta de respuesta. Nunca se
  emite contra ARCA producción durante el desarrollo.
- La numeración de presupuestos arranca en 1 (confirmado en la clarificación del 2026-10-08).
- El tope de identificación inicial es $10.000.000 y la lista de datos del PDF de factura (RF-30) se valida
  con el contador de la óptica.
- Un presupuesto Final origina como máximo una factura autorizada; mientras tenga una emisión Pendiente,
  Bloqueada o Autorizada no se puede iniciar otra.
- Los datos del emisor (razón social, domicilio, CUIT, ingresos brutos, inicio de actividades), el
  certificado y el punto de venta se configuran fuera de la interfaz.
- El sistema corre en la PC del local y se usa solo desde esa PC o la red local; no se publica en internet.
- Los datos de clientes se guardan sin cifrado en reposo: riesgo asumido por el responsable del proyecto
  (19/09/2026); el control es el acceso con contraseña.
- El logo y la paleta de la óptica están disponibles en los archivos de marca del proyecto.

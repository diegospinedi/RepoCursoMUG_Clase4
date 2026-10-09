# Quickstart: validar la feature 001

**Feature**: [spec.md](spec.md) | **Contratos**: [contracts/](contracts/) | **Data model**: [data-model.md](data-model.md)

Guía para comprobar de punta a punta que la feature funciona. Todo se corre desde WSL (`AGENTS.md`).

## Requisitos

- SDK de .NET 10 en `~/.dotnet`, en el `PATH`.
- Node 22 y npm.
- `Marca/branding.json`, `Marca/logo.png` y las fuentes de la marca en `backend/Optica.Api/Recursos/`.
- `appsettings.json` con `Arca:Entorno = Simulado`.

## Verificación automática

```bash
dotnet test Optica.slnx
cd frontend && npm test && npm run build && npm run lint
```

Las cuatro deben pasar antes de cada commit (constitución, Flujo de desarrollo). Los tests de cálculo
del backend y del frontend usan los mismos vectores (`tests/vectores-calculo.json`).

## Arranque

```bash
dotnet run --project backend/Optica.Api      # http://localhost:5220
cd frontend && npm install && npm run dev    # http://localhost:5173
```

En desarrollo, el proxy de Vite hace que todo pedido parezca local: la restricción "solo desde la PC del
sistema" se prueba con los tests de integración, no desde el navegador (research R4).

## Recorridos manuales

| # | Historia | Pasos | Resultado esperado |
|---|---|---|---|
| 1 | US1 | Definir una contraseña de 7 caracteres, después una de 8; ingresar 5 veces con una incorrecta | Rechaza la de 7; tras 5 fallos bloquea 5 minutos |
| 2 | US2 | Configuración: RI, IVA 21, múltiplo 0,01, margen predeterminado 50. Crear el proveedor "Lentes SA" y el artículo ABC-1 con costo 1.210 y margen 50 | Precio de venta $1.815,00 |
| 3 | US2 | Cambiar el múltiplo a 50 | ABC-1 pasa a $1.850,00 sin editarlo |
| 4 | US3 | Importar para "Lentes SA" una planilla con una fila nueva, ABC-1 a 2.420, un precio "1.210,50" como texto y una columna de más (en otra planilla) | Resumen: 1 creado, 1 actualizado, 1 "precio inválido"; la planilla con columna de más se rechaza sin cambios |
| 5 | US4 | Presupuesto con cliente sin DNI; después con DNI, una línea de ABC-1, cantidad 3, descuento 10 | Error junto al DNI; luego número 1, Borrador, importes calculados |
| 6 | US4 | Pasarlo a Final e intentar editarlo | Rechazado; queda igual |
| 7 | US5 | Descargar el PDF del Final; intentarlo con un Borrador | PDF con logo y leyenda; el Borrador no lo permite |
| 8 | US6 | Facturar el Final con `Simulador:Modo = Normal` | Factura B autorizada con CAE; PDF con QR y "COMPROBANTE SIMULADO — SIN VALIDEZ FISCAL" |
| 9 | US7 | Con modo `Rechazar`, facturar otro Final | Código y descripción del error; no se registra factura |
| 10 | US7 | Con modo `AutorizarSinResponder`, facturar; volver a `Normal` y Reintentar | A los 30 s queda Pendiente; el reintento recupera el CAE sin emitir otro |
| 11 | US7 | Con modo `SinRespuesta`, facturar; volver a `Normal` y Reintentar | Pendiente; el reintento verifica que no existe y emite |
| 12 | US8 | Buscar facturas por "gonzalez", por parte del DNI y por parte del número | Coincidencias parciales, sin importar acentos ni puntos |

El caso "otro total" (Bloqueada → Descartada) se reproduce solo con tests de integración, editando el
espía de ARCA de `AppDePrueba`.

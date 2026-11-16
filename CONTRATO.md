# Contrato de AeroTaller

Como en un parcial, algunos nombres **son obligatorios**: el verificador (`dotnet run --project verificador`) y la
corrección automática de GitHub Classroom los buscan tal cual. El resto (la implementación interna, los nombres de
variables, los métodos privados, los estilos) queda a criterio de quien resuelve el TP.

## Proyecto y configuración

| Qué | Valor obligatorio |
|---|---|
| Carpeta y proyecto | `AeroTaller/AeroTaller.csproj` (no lo renombres) |
| Script de la base | `AeroTaller/aerotaller.sql`, copiado a la salida (`CopyToOutputDirectory`) |
| Cadena de conexión | `ConnectionStrings:AeroTaller` en `appsettings.json` → `Data Source=aerotaller.db;Foreign Keys=True` |
| Crear la base | al arrancar, si el archivo de la cadena no existe, se ejecuta `aerotaller.sql` |
| Origen de datos (desafío 7) | clave `OrigenDeDatos`: `Sqlite` (por defecto) o `Falso` (repositorios en memoria, **sin tocar la base**) |
| Logs (desafío 8) | Serilog a archivo en `logs/` (dentro de `AeroTaller/`), un archivo `.txt` por día |

> El verificador ejecuta la aplicación con **una base temporal propia**: le pasa otra cadena por variable de entorno
> (`ConnectionStrings__AeroTaller`). Por eso la cadena debe leerse de la configuración y no escribirse en el código.

## Base de datos (desafío 1)

Nombres de tablas y columnas **exactos** (si no, `material/datos.sql` no se puede cargar):

| Tabla | Columnas | Restricciones |
|---|---|---|
| `Aeropuerto` | `Id`, `Codigo`, `Ciudad`, `Nombre` | `Codigo` único |
| `Vuelo` | `Id`, `Numero`, `OrigenId`, `DestinoId`, `Salida`, `DuracionMinutos`, `Filas`, `Precio` | `Numero` único · FK `OrigenId` y `DestinoId` → `Aeropuerto` |
| `Pasajero` | `Id`, `Nombre`, `Dni`, `Email` | `Dni` único y obligatorio · `Email` opcional |
| `Reserva` | `Id`, `VueloId`, `PasajeroId`, `Asiento`, `Fecha`, `Estado` | FK a `Vuelo` y `Pasajero` · `Estado` es `'Confirmada'` o `'Cancelada'` |
| `Usuario` (desafío 9) | `Id`, `NombreUsuario`, `Nombre`, `Clave`, `Rol` | `NombreUsuario` único · `Rol` es `'Admin'` o `'Mostrador'` |
| `Equipaje` (simulacro) | `Id`, `ReservaId`, `Tipo`, `PesoKg` | FK a `Reserva` · `Tipo` es `'Mano'` o `'Bodega'` |

- Fechas como texto `'AAAA-MM-DD HH:MM'`. Cada fila del avión tiene 6 asientos: `A B C | D E F`. Capacidad = `Filas × 6`.
- Desafío 8: un **índice único** que impida dos reservas *confirmadas* en el mismo asiento del mismo vuelo.

## Pantallas y rutas

| Ruta | Qué muestra / hace | Desde |
|---|---|---|
| `GET /` | tablero: los próximos vuelos (los que todavía no salieron) con su ocupación | 3 |
| `GET /Vuelos` | listado de vuelos (desde el 3); filtro opcional `?DestinoId=…&Desde=…` (desde el 4) | 3 · 4 |
| `GET /Vuelos/Detalle/{id}` | datos del vuelo + **mapa de asientos**: cada asiento libre es un link a `/Reservas/Crear?vueloId={id}&asiento={codigo}`; cada ocupado lleva la clase CSS `ocupado` | 3 · 4 |
| `GET /Vuelos/Detalle/{id-inexistente}` | 404 | 3 |
| `GET /Pasajeros` | listado; búsqueda `?buscar=texto` por nombre o DNI | 3 |
| `GET /Pasajeros/Detalle/{id}` | datos del pasajero y sus reservas | 3 |
| `GET/POST /Pasajeros/Crear` | formulario con campos `Nombre`, `Dni`, `Email` | 5 |
| `GET /Reservas` | listado (vuelo, pasajero, asiento, estado) | 4 |
| `GET/POST /Reservas/Crear` | formulario con `VueloId` (select), `PasajeroId` (select), `Asiento` (texto) | 4 · 5 |
| `POST /Reservas/Cancelar/{id}` | cancela (no borra: `Estado = 'Cancelada'`) | 6 |
| `GET/POST /Vuelos/Crear` | alta de vuelo: `Numero`, `OrigenId`, `DestinoId`, `Salida`, `DuracionMinutos`, `Filas`, `Precio` | 5 · 9 (solo Admin) |
| `GET/POST /Login` · `POST /Login/Salir` | iniciar y cerrar sesión: campos `NombreUsuario`, `Clave` | 9 |
| `GET /Reservas/Detalle/{id}` | la reserva con su equipaje y el total de kg | simulacro |
| `GET/POST /Equipaje/Crear?reservaId={id}` | campos `Tipo`, `PesoKg` | simulacro |

Reglas de los formularios:

- Si un POST sale bien: **Redirect** (POST → Redirect → GET). Si falla: la misma vista con los errores
  (`asp-validation-for` → elementos con la clase `field-validation-error`).
- Todos los formularios POST con `[ValidateAntiForgeryToken]`.

## Reglas del negocio (desafíos 5 y 6)

| Regla | Dónde se muestra el error |
|---|---|
| El asiento tiene formato fila + letra A–F (`12C`) | campo `Asiento` |
| El asiento existe en ese avión (fila ≤ `Filas`) | campo `Asiento` |
| El asiento está libre | campo `Asiento` |
| No se reserva un vuelo que ya salió | campo `VueloId` |
| No hay overbooking (confirmadas < capacidad) | campo `VueloId` |
| Un pasajero no tiene dos reservas confirmadas en el mismo vuelo | campo `PasajeroId` |
| DNI: 7 u 8 dígitos y único | campo `Dni` |
| Vuelo: salida futura, origen ≠ destino, número `AT` + 4 dígitos y único | el campo correspondiente |

## Clases y archivos

| Qué | Nombre obligatorio | Desde |
|---|---|---|
| Interfaces de repositorios | `IAeropuertoRepository`, `IVueloRepository`, `IPasajeroRepository`, `IReservaRepository` (+ `IUsuarioRepository`, `IEquipajeRepository`) | 7 |
| Implementaciones | `VueloRepository`, `PasajeroRepository`, `ReservaRepository`, `AeropuertoRepository` … | 2 |
| Cadena inyectada | `record ConexionDb(string Cadena)` registrada como Singleton | 7 |
| Repositorios falsos | una clase por interfaz, con `Falso` en el nombre (`VueloRepositoryFalso`) | 7 |
| Servicio de reservas | `IReservaService` / `ReservaService` | 6 |
| Excepción propia | `ReglaDeNegocioException` (hereda de `Exception`, con el nombre del campo) | 6 |
| Atributo de validación | `FechaFuturaAttribute` | 5 |
| ViewModels | carpeta `ViewModels/`, nombres terminados en `ViewModel` (`ReservaCrearViewModel`, `VueloDetalleViewModel` …) | 4 |
| Login | `IAutenticacionService` / `AutenticacionService`, filtros `RequiereLoginAttribute` y `RequiereRolAttribute`, vista `SinPermiso` | 9 |

## Usuarios de prueba (desafío 9)

| Usuario | Clave | Rol |
|---|---|---|
| `admin` | `aero2026` | Admin: además crea vuelos y elimina pasajeros |
| `mostrador` | `vuelos2026` | Mostrador: pasajeros, reservas, equipaje |

Sin sesión solo se ven `/`, `/Vuelos` y `/Vuelos/Detalle/{id}`. Con sesión pero sin el rol: vista `SinPermiso`.

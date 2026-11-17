## :plane: El proyecto

**AeroTaller** es una aerolínea regional que vuela entre ciudades de la Argentina. Cada **Aeropuerto** se identifica por
su código internacional (`TUC`, `AEP`, `COR`…). Un **Vuelo** une un aeropuerto de origen con uno de destino, sale un día a
una hora determinada, dura cierta cantidad de minutos, tiene un precio y se realiza en un avión con una cantidad de filas de
seis asientos cada una.

Cada persona que viaja es un **Pasajero**: se registra una sola vez con su nombre, su DNI y, opcionalmente, un correo.
Cuando un pasajero ocupa un lugar en un vuelo se genera una **Reserva**, que guarda el vuelo, el pasajero y el asiento
elegido en el mapa del avión. Una reserva puede cancelarse: el asiento vuelve a quedar libre y la reserva queda registrada
como cancelada.

El sistema no está destinado a los pasajeros, sino al personal de la aerolínea. Cada **Usuario** inicia sesión con su nombre de
usuario y su clave, y tiene un rol: el personal de mostrador (`Mostrador`) registra pasajeros y reservas; la administración
(`Admin`), además, crea vuelos y elimina pasajeros.

Las reglas del negocio son las habituales en una aerolínea: no se reserva un asiento ocupado, ni uno que no existe en ese avión, ni un vuelo
que ya salió o que está completo. En el simulacro de parcial se agrega el **Equipaje** de cada reserva, con sus propias
reglas de peso y cantidad de piezas.

En el TP se construye esa aplicación por capas, de la base de datos a las pantallas, en diez desafíos. El resultado tiene
la misma estructura que se pide en el parcial.

## :network: Entidades y relaciones

Cuatro entidades forman el negocio y una quinta controla el acceso. Cada caja se despliega con un clic para ver sus
propiedades; el selector cambia entre la vista de **clases** (lo que se escribe en C# en el desafío 2) y la de **tablas**
(lo que ya define `aerotaller.sql`). Donde la tabla almacena el `Id` de otra tabla, la clase contiene el objeto
completo: la tabla `Vuelo` tiene `OrigenId`, y la clase `Vuelo` tiene `Origen`.

:::detalle Qué significa cada propiedad
#### Aeropuerto

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | identificador que asigna la base |
| `Codigo` | `string` | código internacional de tres letras (`TUC`); único |
| `Ciudad` | `string` | ciudad donde está el aeropuerto |
| `Nombre` | `string` | nombre del aeropuerto (`Teniente Benjamín Matienzo`) |

#### Vuelo

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | identificador que asigna la base |
| `Numero` | `string` | número de vuelo (`AT1001`); único |
| `Origen` | `Aeropuerto` | de dónde sale; en la base, la columna `OrigenId` |
| `Destino` | `Aeropuerto` | a dónde llega; en la base, la columna `DestinoId` (distinto del origen) |
| `Salida` | `DateTime` | día y hora de salida |
| `DuracionMinutos` | `int` | duración del vuelo |
| `Filas` | `int` | filas del avión; cada una tiene seis asientos, `A B C` y `D E F` |
| `Precio` | `decimal` | precio del pasaje |
| `Llegada` | `DateTime` | calculada: `Salida` + `DuracionMinutos` |
| `Capacidad` | `int` | calculada: `Filas` × 6 |
| `YaSalio` | `bool` | calculada: la salida ya pasó |

#### Pasajero

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | identificador que asigna la base |
| `Nombre` | `string` | nombre y apellido |
| `Dni` | `string` | documento; obligatorio y único |
| `Email` | `string?` | correo; opcional |

#### Reserva

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | identificador que asigna la base |
| `Vuelo` | `Vuelo` | el vuelo reservado; en la base, la columna `VueloId` |
| `Pasajero` | `Pasajero` | quién viaja; en la base, la columna `PasajeroId` |
| `Asiento` | `string` | fila y letra (`12A`) |
| `Fecha` | `DateTime` | cuándo se realizó la reserva |
| `Estado` | `EstadoReserva` | `Confirmada` o `Cancelada` |

#### Usuario

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | identificador que asigna la base |
| `NombreUsuario` | `string` | lo que se escribe para iniciar sesión; único |
| `Nombre` | `string` | nombre para mostrar (`Marta`) |
| `Clave` | `string` | contraseña; en texto plano solo para el TP |
| `Rol` | `string` | `Admin` o `Mostrador` |

#### Equipaje (simulacro)

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | identificador que asigna la base |
| `ReservaId` | `int` | la reserva a la que pertenece |
| `Tipo` | `TipoEquipaje` | `Mano` o `Bodega` |
| `PesoKg` | `decimal` | peso de la pieza, mayor que cero |
:::

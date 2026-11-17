## :target: Misión

Una aerolínea sin datos es un hangar vacío. La plantilla incluye **la base de AeroTaller ya diseñada**: el script
`AeroTaller/aerotaller.sql` crea aeropuertos, vuelos, pasajeros, reservas y usuarios, con sus claves, sus restricciones y
datos de ejemplo. En este desafío se **lee y se entiende** ese script, se crea `aerotaller.db` con un botón, y se conecta la
aplicación: cuando se clona el repositorio (o el día del parcial, o al correr el verificador) la app se inicia, detecta que la
base no existe y la crea automáticamente a partir del mismo script.

Al terminar debe quedar:

1. El paquete `Microsoft.Data.Sqlite` en el proyecto.
2. `aerotaller.db` creada con el botón **Crear base de datos** de esta página y revisada en **DB Browser for SQLite**.
3. La cadena de conexión en `appsettings.json` y `Program.cs` creando la base si no existe.
4. Seis consultas probadas en DB Browser: las mismas que luego se escriben desde C#.

:::nota
Todavía no hay ninguna pantalla nueva: la app se ve igual que en el desafío 0. Lo que cambia es que ahora existe un archivo
`aerotaller.db` en la carpeta `AeroTaller/`. Es el cimiento de todo lo que sigue.
:::

## :book-open-text: Repaso

En el tema 07 se construyó la base de la clínica y en el tema 08 se la usó desde C#. AeroTaller es **el mismo problema con
otro dominio** (es exactamente la situación que plantea el parcial):

| En la clínica | En AeroTaller | Qué cambia |
|---|---|---|
| `Paciente` | `Pasajero` | el DNI ahora es **obligatorio** y único |
| `Medico` | `Vuelo` | tiene **dos** claves foráneas a la misma tabla (origen y destino) |
| `Turno` | `Reserva` | además tiene un `Estado` (`'Confirmada'` o `'Cancelada'`) |
| — | `Aeropuerto` | tabla pequeña, pero requiere un JOIN doble |

Los conceptos que se usan en este desafío:

- **Clave primaria (PK)**: el `Id` que asigna la base (`INTEGER PRIMARY KEY AUTOINCREMENT`). No tiene significado propio y nunca cambia.
- **Clave foránea (FK)**: una columna que guarda la PK de otra tabla. `Reserva.VueloId` hace referencia a `Vuelo.Id`.
- **Integridad referencial**: con las claves foráneas activadas, la base **rechaza** una reserva para el vuelo 99 si
  no existe, o el borrado de un pasajero que tiene reservas: `FOREIGN KEY constraint failed`.
- **Fechas como texto** `'AAAA-MM-DD HH:MM'`: SQLite no tiene tipo fecha. Con ese formato, ordenar textos es ordenar fechas.
- **Cadena de conexión** en `appsettings.json`, leída con `GetConnectionString`: se cambia sin recompilar.

Las relaciones de AeroTaller, en una tabla:

| Tabla | Se relaciona con | Cómo | Se lee |
|---|---|---|---|
| `Vuelo` | `Aeropuerto` | `OrigenId` → `Aeropuerto.Id` | un aeropuerto es origen de muchos vuelos (1 a N) |
| `Vuelo` | `Aeropuerto` | `DestinoId` → `Aeropuerto.Id` | …y destino de muchos otros (otra relación 1 a N) |
| `Reserva` | `Vuelo` | `VueloId` → `Vuelo.Id` | un vuelo tiene muchas reservas |
| `Reserva` | `Pasajero` | `PasajeroId` → `Pasajero.Id` | un pasajero tiene muchas reservas |

:::catedra
Lo visto en clase está en las diapositivas del [tema 07](https://taller-de-lenguajes-2.github.io/clases/tema-07-bases-de-datos/index.html)
(PK, FK, `CREATE TABLE`, `SELECT`, `JOIN`, `GROUP BY`) y del [tema 08](https://taller-de-lenguajes-2.github.io/clases/tema-08-ado-net/index.html)
(cadena de conexión y `appsettings.json`). El script de AeroTaller usa además tres elementos que no estaban en las diapositivas: `CHECK`
y `DEFAULT` en el `CREATE TABLE`, y un índice único parcial; y la consulta 4 usa `LEFT JOIN`. Cada uno se explica en el paso correspondiente.
:::

### :lightbulb: Tips y recursos

**¿Cómo nace la base?**

| Forma | Cómo | A favor | En contra | Cuándo |
|---|---|---|---|---|
| Script + `CrearBaseSiNoExiste` (la de la cátedra) | la app ejecuta `aerotaller.sql` si el archivo no existe (y en este TP, también el botón de la Torre) | el repositorio contiene solo texto; la base puede regenerarse en cualquier equipo; la corrección automática parte de cero | si se cambia el script, es necesario recrear la base | TP, parcial, proyectos pequeños |
| Base creada manualmente en DB Browser y commiteada | se sube `aerotaller.db` al repositorio | no requiere código | un binario en git: no se distinguen los cambios, genera conflictos entre integrantes, las fechas quedan desactualizadas | nunca en este TP |
| `CREATE TABLE IF NOT EXISTS` en cada arranque | se ejecuta siempre, sin `File.Exists` | tolera una base creada parcialmente | los `INSERT` de ejemplo se repetirían cada vez: es necesario separarlos | cuando la estructura crece de forma incremental |
| Migraciones (Entity Framework Core) | el esquema se versiona como código C# | cambios ordenados en bases reales | requiere otra herramienta completa; la materia usa ADO.NET | proyectos profesionales (para investigar) |

**¿Cómo se lee la cadena?**

| Forma | Si falta la cadena… | Comentario |
|---|---|---|
| `GetConnectionString("AeroTaller")!` (proyecto del tema 10) | continúa con `null` y falla más adelante, lejos del origen del problema | el `!` solo suprime la advertencia del compilador |
| `GetConnectionString("AeroTaller") ?? throw new InvalidOperationException("…")` | la app no arranca e indica **qué** falta | la que se usa en AeroTaller |
| `"Data Source=aerotaller.db"` escrito en el código | — | el verificador no puede cambiar la base: falla |

:::catedra
En el proyecto de ejemplo del [tema 10](https://taller-de-lenguajes-2.github.io/clases/tema-10-mvc/index.html) la cadena
se lee con `GetConnectionString("Clinica")!` y la base se crea con la misma `CrearBaseSiNoExiste`. En AeroTaller se reemplaza
el `!` por `?? throw` para que, si falta la configuración, el error indique exactamente qué ocurre. Las dos formas son válidas
en el parcial.
:::

## :code-xml: Paso a paso

### Paso 1 · Agregar el proveedor de SQLite

Desde la raíz del repositorio:

```bash
cd AeroTaller
dotnet add package Microsoft.Data.Sqlite
cd ..
```

Abrir `AeroTaller/AeroTaller.csproj`: debe aparecer un `<PackageReference Include="Microsoft.Data.Sqlite" … />`.
Es el mismo paquete del tema 08 (importante: no `System.Data.SQLite`, que es otra biblioteca con nombres similares).

### Paso 2 · Leer el script: `AeroTaller/aerotaller.sql`

Abrir `AeroTaller/aerotaller.sql` junto con el `CONTRATO.md`. **No se modifica**: los nombres de tablas y columnas son los
del contrato y el verificador depende de esos datos (por ejemplo, de que el asiento 3A del AT1001 esté ocupado). El objetivo es
entender cada parte, porque en el parcial el script lo escribe cada estudiante.

El script comienza eliminando las tablas existentes, **de las hijas a las madres** (primero la tabla que contiene la FK):

```sql
PRAGMA foreign_keys = ON;

-- Primero las tablas que apuntan a otras: si se borra Vuelo antes que Reserva,
-- la clave foránea de Reserva lo impide
DROP TABLE IF EXISTS Reserva;
DROP TABLE IF EXISTS Vuelo;
DROP TABLE IF EXISTS Pasajero;
DROP TABLE IF EXISTS Aeropuerto;
DROP TABLE IF EXISTS Usuario;
```

La tabla más relevante es `Vuelo`, con **dos** claves foráneas a la misma tabla:

```sql
CREATE TABLE Vuelo (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Numero          TEXT NOT NULL UNIQUE,  -- 'AT1001'
    OrigenId        INTEGER NOT NULL,
    DestinoId       INTEGER NOT NULL,
    Salida          TEXT NOT NULL,         -- 'AAAA-MM-DD HH:MM'
    DuracionMinutos INTEGER NOT NULL,
    Filas           INTEGER NOT NULL,      -- cada fila tiene 6 asientos: A B C | D E F
    Precio          REAL NOT NULL,
    FOREIGN KEY (OrigenId)  REFERENCES Aeropuerto(Id),
    FOREIGN KEY (DestinoId) REFERENCES Aeropuerto(Id),
    CHECK (OrigenId <> DestinoId)          -- un vuelo no va de TUC a TUC
);
```

Elementos que aparecen en el script y conviene poder explicar:

| En el script | Qué hace |
|---|---|
| `CHECK (condición)` | la base rechaza cualquier fila que no la cumpla. Es una regla **en la base**, que vale aunque el dato no provenga de la app (por ejemplo, si se carga manualmente en DB Browser) |
| `DEFAULT 'Confirmada'` en `Reserva.Estado` | si el `INSERT` no incluye esa columna, se usa ese valor |
| `Estado … CHECK (Estado IN ('Confirmada', 'Cancelada'))` | el `IN` del tema 07, ahora dentro de una restricción |
| `Dni TEXT NOT NULL UNIQUE` | DNI obligatorio y sin repetidos |
| `CREATE UNIQUE INDEX UX_Reserva_AsientoConfirmado … WHERE Estado = 'Confirmada'` | índice único **parcial**: no puede haber dos reservas confirmadas del mismo asiento en el mismo vuelo. Se utiliza en el desafío 8 |
| `CREATE TABLE Usuario` | los usuarios que inician sesión (no los pasajeros). Se usa en el desafío 9 |

:::nota
`Dni` es `TEXT` y no `INTEGER` porque con un DNI no se realizan operaciones aritméticas; además, como texto conserva los
ceros a la izquierda y no se confunde con un número. Lo mismo vale para teléfonos, códigos postales y legajos.
:::

:::nota
El script cubre todo el TP salvo el simulacro: allí se le agrega la tabla `Equipaje`, escrita desde cero, como en el parcial.
:::

Así se cargan las fechas de los vuelos:

```sql
strftime('%Y-%m-%d %H:%M', 'now', 'localtime', '+12 days', 'start of day', '+7 hours', '+30 minutes')
```

Se lee de izquierda a derecha: *ahora*, en hora local, *más doce días*, *al principio de ese día* (00:00), *más 7 horas
y 30 minutos* → dentro de 12 días a las 07:30, con el formato `'AAAA-MM-DD HH:MM'`. Las fechas se calculan **a partir del día
en que se ejecuta el script**: así siempre hay vuelos futuros para reservar, y el `AT0999` siempre salió ayer.

### Paso 3 · Crear la base y abrirla en DB Browser

Más abajo en esta página está el panel **:database: Base de datos**. El botón **Crear base de datos** ejecuta `aerotaller.sql` y
deja `aerotaller.db` en la carpeta `AeroTaller/` del repositorio (la que figura en la tarjeta de embarque). El panel muestra
cada tabla con su cantidad de filas: deben verse 8 aeropuertos, 9 vuelos, 8 pasajeros, 15 reservas y 2 usuarios.

A continuación, abrir el archivo en DB Browser for SQLite (**Abrir base de datos** → `AeroTaller/aerotaller.db`) y recorrerlo:

1. Pestaña **Estructura de la base de datos**: las cinco tablas, el índice `UX_Reserva_AsientoConfirmado` y, dentro de cada
   tabla, sus columnas con el tipo.
2. Pestaña **Hoja de datos**: elegir `Vuelo` y ubicar `AT0999` (ya salió) y `AT0500` (la avioneta de una sola fila).
3. Pestaña **Ejecutar SQL**: `PRAGMA foreign_key_list(Vuelo);` muestra las dos claves foráneas hacia `Aeropuerto`.

:::cuidado
Las fechas quedan fijas desde el momento en que se creó la base: si se creó hace una semana, hoy varios vuelos ya "salieron".
Cuando las fechas no sean coherentes, o después de experimentar con los datos, usar **Recrear base de datos**: vuelve a los
datos del script con fechas calculadas desde hoy. También sirve el método manual: **app detenida → borrar `aerotaller.db` →
arrancar** (una vez terminado el paso 5).
:::

### Paso 4 · Copiar el script junto al ejecutable

Cuando la app se ejecuta, el ejecutable está en `AeroTaller/bin/Debug/net10.0/`. El script debe estar **en esa carpeta**, por lo
que se le indica al compilador que lo copie. En `AeroTaller.csproj`, dentro de `<Project>`:

```xml
<ItemGroup>
  <!-- El script de la base se copia junto al ejecutable: Program.cs lo usa si aerotaller.db no existe -->
  <None Update="aerotaller.sql" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

`PreserveNewest` copia el archivo solo cuando cambió. Compilar (`dotnet build AeroTaller`) y buscar `aerotaller.sql` en
`bin/Debug/net10.0/`: debe estar.

### Paso 5 · La cadena de conexión y `CrearBaseSiNoExiste`

En `appsettings.json`, agregar la sección (respetando las comas del JSON):

```json
{
  "ConnectionStrings": {
    "AeroTaller": "Data Source=aerotaller.db;Foreign Keys=True"
  },
  "Logging": { … lo que ya estaba … }
}
```

`Foreign Keys=True` activa las claves foráneas en **cada** conexión. `Microsoft.Data.Sqlite` ya suele activarlas, pero
así no se depende de ese comportamiento (lo indica el tema 08).

En `Program.cs`, donde dice `// Desafío 1`, leer la cadena y crear la base:

```csharp
using System.Globalization;
using Microsoft.Data.Sqlite;

// (estas líneas de cultura ya vienen en el proyecto base: se dejan como están)
var cultura = (CultureInfo)CultureInfo.GetCultureInfo("es-AR").Clone();
cultura.NumberFormat.NumberDecimalSeparator = ".";
cultura.NumberFormat.NumberGroupSeparator = "";
CultureInfo.DefaultThreadCurrentCulture = cultura;

var builder = WebApplication.CreateBuilder(args);

// La cadena se lee UNA vez, al arrancar. Si falta, la app no se inicia (es preferible a un error confuso más adelante)
string cadenaConexion = builder.Configuration.GetConnectionString("AeroTaller")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'AeroTaller' en appsettings.json");
CrearBaseSiNoExiste(cadenaConexion);

builder.Services.AddControllersWithViews();
// … el resto, igual que antes …
```

Y al final del archivo, **después** de `app.Run();`, la función (es la misma idea del proyecto de ejemplo del tema 10):

```csharp
// Si la base no existe, la crea con aerotaller.sql
static void CrearBaseSiNoExiste(string cadenaConexion)
{
    // De la cadena se toma solo el archivo: "aerotaller.db" (o la ruta que indique el verificador)
    var archivo = new SqliteConnectionStringBuilder(cadenaConexion).DataSource;
    if (File.Exists(archivo)) return;

    using var conexion = new SqliteConnection(cadenaConexion);
    conexion.Open();                       // SQLite crea el archivo vacío al abrir

    using var comando = conexion.CreateCommand();
    // El script está junto al ejecutable (bin/…), no en la carpeta del proyecto
    comando.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "aerotaller.sql"));
    comando.ExecuteNonQuery();             // ejecuta TODO el script: DROP, CREATE e INSERT
}
```

Tres decisiones que conviene entender (y que se preguntan en el parcial):

| Decisión | Por qué |
|---|---|
| La cadena se lee de la configuración, no se escribe en el código | el verificador inicia la app con **su propia** base temporal y le pasa otra cadena por variable de entorno |
| `DataSource` del builder, no `"aerotaller.db"` escrito de forma fija | si la cadena cambia, el archivo que se consulta en `File.Exists` cambia con ella |
| `AppContext.BaseDirectory` para el script | es la carpeta del ejecutable (`bin/Debug/net10.0/`), donde lo copió el paso 4; la carpeta "actual" puede ser otra |

### Paso 6 · Arrancar y comprobar

La base del paso 3 la creó la Torre. Ahora se comprueba que la **aplicación** también puede crearla, que es lo que ocurre
al clonar el repositorio en otra computadora, el día del parcial y en cada ejecución del verificador:

1. Con la app detenida, borrar `AeroTaller/aerotaller.db`.
2. Correr la app (`dotnet run --project AeroTaller` o F5). Debe aparecer de nuevo `AeroTaller/aerotaller.db`.
3. Volver a esta página y usar **Actualizar** en el panel de la base: las cinco tablas, con los mismos datos.

:::cuidado
Antes de arrancar la app, **cerrar la base en DB Browser** (o al menos usar *Escribir cambios*). Si DB Browser tiene
cambios sin escribir, la base queda bloqueada para otros procesos y la app recibe `SQLite Error 5: 'database is locked'`.
:::

:::nota
`aerotaller.db` **no se commitea**: está en el `.gitignore`. Lo que está en el repositorio es el script. La base es un
resultado, como los archivos de `bin/`: puede regenerarse con el botón de la Torre o al iniciar la app.
:::

### Paso 7 · Seis consultas en DB Browser

Con `aerotaller.db` abierta en DB Browser, pestaña **Ejecutar SQL**. Intentar resolver cada una antes de abrir la solución:
estas consultas son las que se escriben desde C# en el desafío 2.

**1 · Listado ordenado.** Número, salida y precio de todos los vuelos, del que sale primero al último.

:::solucion Consulta 1
```sql
SELECT Numero, Salida, Precio
FROM Vuelo
ORDER BY Salida;
```
Primero aparece `AT0999` (salió ayer) y después `AT0500`, el próximo en salir. Como las fechas son `'AAAA-MM-DD HH:MM'`, ordenar
el texto es ordenar por fecha.
:::

**2 · Buscar con LIKE.** Los pasajeros cuyo nombre termina en "ez".

:::solucion Consulta 2
```sql
SELECT Nombre, Dni
FROM Pasajero
WHERE Nombre LIKE '%ez';
```
Devuelve Ana Gómez y Facundo López. Probar también `LIKE '%gom%'`: **no devuelve a Gómez**. El `LIKE` de SQLite ignora
mayúsculas solo en letras sin tilde, y `o` no es `ó`. Tenerlo en cuenta al probar el buscador de pasajeros.
:::

**3 · JOIN doble con alias.** Cada vuelo con el **código** del aeropuerto de origen y el de destino (`TUC → AEP`).

La dificultad consiste en unir `Vuelo` con `Aeropuerto` **dos veces**, una por cada clave foránea. Como es la misma
tabla, cada aparición necesita su propio alias: `o` para el origen y `d` para el destino.

:::solucion Consulta 3
```sql
SELECT v.Numero, o.Codigo AS Origen, d.Codigo AS Destino, v.Salida
FROM Vuelo v
INNER JOIN Aeropuerto o ON v.OrigenId  = o.Id
INNER JOIN Aeropuerto d ON v.DestinoId = d.Id
ORDER BY v.Salida;
```
Sin alias, SQLite no puede determinar a cuál de los dos `Aeropuerto` corresponde la columna (`ambiguous column name`).
:::

**4 · Ocupación con LEFT JOIN y GROUP BY.** Para **cada** vuelo: número, capacidad (`Filas * 6`) y cuántas reservas
**confirmadas** tiene. Los vuelos sin reservas deben aparecer con 0.

El `INNER JOIN` del tema 07 solo devuelve las filas que tienen correspondencia: un vuelo sin reservas queda excluido. El **`LEFT JOIN`**
conserva todas las filas de la tabla de la izquierda (`Vuelo`) y, si no hay correspondencia, completa las columnas de la derecha
(`Reserva`) con `NULL`.

:::solucion Consulta 4
```sql
SELECT v.Numero, v.Filas * 6 AS Capacidad, COUNT(r.Id) AS Ocupados
FROM Vuelo v
LEFT JOIN Reserva r ON r.VueloId = v.Id AND r.Estado = 'Confirmada'
GROUP BY v.Id, v.Numero, v.Filas
ORDER BY v.Salida;
```
`AT4001` y `AT3110` aparecen con 0, y `AT0500` con 5 de 6 (es la avioneta casi llena). Dos detalles importantes:

- La condición del estado va en el **ON**, no en el `WHERE`: en el `WHERE` descartaría los vuelos sin reservas
  (sus columnas de `r` son `NULL`).
- `COUNT(r.Id)`, no `COUNT(*)`: `COUNT(*)` cuenta filas, y el vuelo sin reservas tiene una fila (con `NULL`): daría 1.
:::

**5 · UPDATE con WHERE.** Bruno Díaz (Id 2) no tiene email: asignarle `bruno.diaz@correo.com`.

:::solucion Consulta 5
```sql
UPDATE Pasajero
SET Email = 'bruno.diaz@correo.com'
WHERE Id = 2;
```
DB Browser informa cuántas filas cambió: debe ser 1.
:::

**6 · DELETE con WHERE (y la integridad referencial).** Primero intentar borrar a Ana Gómez (Id 1), que tiene reservas.
Después borrar la reserva cancelada (Id 5).

:::solucion Consulta 6
```sql
DELETE FROM Pasajero WHERE Id = 1;   -- FOREIGN KEY constraint failed: Ana tiene reservas
DELETE FROM Reserva  WHERE Id = 5;   -- esta sí: 1 fila borrada
```
:::

:::cuidado
Un `UPDATE` o un `DELETE` **sin `WHERE` se aplica a todas las filas**. `UPDATE Reserva SET Estado = 'Cancelada';` cancela
las 15 reservas sin pedir confirmación. En DB Browser se puede deshacer mientras no se use *Escribir cambios*; en una app real, no.
Para volver a los datos originales: **Recrear base de datos** en el panel de esta página (o, con la app detenida, borrar `aerotaller.db` y arrancarla).
:::

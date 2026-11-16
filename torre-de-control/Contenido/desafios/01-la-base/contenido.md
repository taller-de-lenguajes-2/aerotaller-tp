## 🎯 Misión

Una aerolínea sin datos es un hangar vacío. En este desafío se arma **la base de AeroTaller**: aeropuertos, vuelos,
pasajeros y reservas, con sus claves y sus relaciones. Y se la deja **automática**: cuando alguien clona el repositorio (o
el día del parcial, o al correr el verificador) la app arranca, detecta que no hay base y la crea sola a partir de un script.

Al terminar debe quedar:

1. El paquete `Microsoft.Data.Sqlite` en el proyecto.
2. `AeroTaller/aerotaller.sql` con las cuatro tablas del contrato y los datos de ejemplo de `material/datos.sql`.
3. La cadena de conexión en `appsettings.json` y `Program.cs` creando la base si no existe.
4. Seis consultas probadas en **DB Browser for SQLite**: las mismas que luego se escriben desde C#.

:::nota
Todavía no hay ninguna pantalla nueva: la app se ve igual que en el desafío 0. Lo que cambia es que ahora, al arrancar,
aparece un archivo `aerotaller.db` en la carpeta `AeroTaller/`. Es el cimiento de todo lo que sigue.
:::

## 📚 Repaso

En el tema 07 se armó la base de la clínica y en el tema 08 se la usó desde C#. AeroTaller es **el mismo problema con
otro dominio** (es exactamente la situación que plantea el parcial):

| En la clínica | En AeroTaller | Qué cambia |
|---|---|---|
| `Paciente` | `Pasajero` | el DNI ahora es **obligatorio** y único |
| `Medico` | `Vuelo` | tiene **dos** claves foráneas a la misma tabla (origen y destino) |
| `Turno` | `Reserva` | además tiene un `Estado` (`'Confirmada'` o `'Cancelada'`) |
| — | `Aeropuerto` | tabla chica, pero obliga a hacer un JOIN doble |

Los conceptos que se usan en este desafío:

- **Clave primaria (PK)**: el `Id` que asigna la base (`INTEGER PRIMARY KEY AUTOINCREMENT`). No significa nada y nunca cambia.
- **Clave foránea (FK)**: una columna que guarda la PK de otra tabla. `Reserva.VueloId` apunta a `Vuelo.Id`.
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
(cadena de conexión y `appsettings.json`). En este desafío se suman tres elementos que no estaban en las diapositivas: `CHECK`
y `DEFAULT` en el `CREATE TABLE`, y el `LEFT JOIN`. Cada uno se explica en el paso correspondiente.
:::

## 💻 Paso a paso

### Paso 1 · Agregar el proveedor de SQLite

Desde la raíz del repositorio:

```bash
cd AeroTaller
dotnet add package Microsoft.Data.Sqlite
cd ..
```

Abrir `AeroTaller/AeroTaller.csproj`: debe aparecer un `<PackageReference Include="Microsoft.Data.Sqlite" … />`.
Es el mismo paquete del tema 08 (importante: no `System.Data.SQLite`, que es otra librería con nombres parecidos).

### Paso 2 · Las tablas: `AeroTaller/aerotaller.sql`

Crear el archivo `AeroTaller/aerotaller.sql` (junto al `.csproj`). Antes de consultar la solución, conviene intentarlo con
el `CONTRATO.md` abierto: los nombres de tablas y columnas son **obligatorios**, letra por letra.

Requisitos:

- `Aeropuerto`: `Codigo` único (es el código IATA: `'TUC'`, `'AEP'`).
- `Vuelo`: `Numero` único; `OrigenId` y `DestinoId` son FK a `Aeropuerto`; `Salida` es texto; `Precio` es `REAL`.
- `Pasajero`: `Dni` **obligatorio y único**; `Email` opcional (puede ser `NULL`).
- `Reserva`: FK a `Vuelo` y a `Pasajero`; `Estado` solo puede ser `'Confirmada'` o `'Cancelada'`.

El script empieza borrando lo que haya, **de las hijas a las madres** (primero la tabla que tiene la FK):

```sql
PRAGMA foreign_keys = ON;

-- Primero las tablas que apuntan a otras: si se borra Vuelo antes que Reserva,
-- la clave foránea de Reserva lo impide
DROP TABLE IF EXISTS Reserva;
DROP TABLE IF EXISTS Vuelo;
DROP TABLE IF EXISTS Pasajero;
DROP TABLE IF EXISTS Aeropuerto;
```

Y la tabla más interesante, `Vuelo`, con **dos** claves foráneas a la misma tabla:

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

Dos palabras nuevas:

- `CHECK (condición)`: la base rechaza cualquier fila que no la cumpla. Es una regla **en la base**, que vale aunque el
  dato no provenga de la app (por ejemplo, si alguien lo carga a mano en DB Browser).
- `DEFAULT 'valor'`: si el `INSERT` no envía esa columna, se usa ese valor. Resulta útil para `Reserva.Estado`.

:::pista Cómo escribir Estado con CHECK y DEFAULT
`Estado TEXT NOT NULL DEFAULT 'Confirmada' CHECK (Estado IN ('Confirmada', 'Cancelada'))`.
El `IN` es el mismo del tema 07, ahora dentro de una restricción.
:::

:::solucion Ver las cuatro tablas
```sql
-- ============================================================
-- aerotaller.sql — base del TP AeroTaller
-- Se puede ejecutar entero en DB Browser for SQLite (pestaña "Ejecutar SQL").
-- La aplicación lo ejecuta sola la primera vez (CrearBaseSiNoExiste en Program.cs).
-- ============================================================

PRAGMA foreign_keys = ON;

DROP TABLE IF EXISTS Reserva;
DROP TABLE IF EXISTS Vuelo;
DROP TABLE IF EXISTS Pasajero;
DROP TABLE IF EXISTS Aeropuerto;

CREATE TABLE Aeropuerto (
    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,          -- código IATA: 'TUC', 'AEP'…
    Ciudad TEXT NOT NULL,
    Nombre TEXT NOT NULL
);

CREATE TABLE Vuelo (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Numero          TEXT NOT NULL UNIQUE,  -- 'AT1001'
    OrigenId        INTEGER NOT NULL,
    DestinoId       INTEGER NOT NULL,
    Salida          TEXT NOT NULL,         -- 'AAAA-MM-DD HH:MM' (hora local)
    DuracionMinutos INTEGER NOT NULL,
    Filas           INTEGER NOT NULL,      -- cada fila tiene 6 asientos: A B C | D E F
    Precio          REAL NOT NULL,
    FOREIGN KEY (OrigenId)  REFERENCES Aeropuerto(Id),
    FOREIGN KEY (DestinoId) REFERENCES Aeropuerto(Id),
    CHECK (OrigenId <> DestinoId)
);

CREATE TABLE Pasajero (
    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre TEXT NOT NULL,
    Dni    TEXT NOT NULL UNIQUE,
    Email  TEXT                            -- opcional: puede ser NULL
);

CREATE TABLE Reserva (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    VueloId    INTEGER NOT NULL,
    PasajeroId INTEGER NOT NULL,
    Asiento    TEXT NOT NULL,              -- '12A'
    Fecha      TEXT NOT NULL,              -- cuándo se hizo la reserva
    Estado     TEXT NOT NULL DEFAULT 'Confirmada' CHECK (Estado IN ('Confirmada', 'Cancelada')),
    FOREIGN KEY (VueloId)    REFERENCES Vuelo(Id),
    FOREIGN KEY (PasajeroId) REFERENCES Pasajero(Id)
);

-- ── Datos de ejemplo: a continuación va TODO material/datos.sql ──
```
:::

:::nota
Este script no queda cerrado: en el desafío 8 se le agrega un índice único, en el 9 la tabla `Usuario` y en el
simulacro la tabla `Equipaje`. Por ahora, solo las cuatro tablas del contrato.
:::

:::nota
¿Por qué `Dni` es `TEXT` y no `INTEGER`? Porque con un DNI no se hacen cuentas, y como texto no pierde ceros adelante ni
se confunde con un número. Lo mismo vale para teléfonos, códigos postales y legajos.
:::

### Paso 3 · Los datos y la prueba en DB Browser

Abrir `material/datos.sql` y pegarlo **entero, al final** de `aerotaller.sql`. No modificarlo: el verificador
cuenta con esos datos (por ejemplo, que el asiento 3A del AT1001 está ocupado).

A continuación, probar el script **antes** de usarlo desde C#, como en el tema 07:

1. En DB Browser for SQLite: **Nueva base de datos** → guardarla en cualquier ubicación como `prueba.db` (cancelar el asistente de tablas).
2. Pestaña **Ejecutar SQL** → pegar todo `aerotaller.sql` → ▶ (ejecutar todo).
3. Si un `INSERT` falla, el problema casi siempre está en el `CREATE TABLE` propio: un nombre de columna distinto al del contrato.
   Corregir y volver a ejecutar (los `DROP TABLE IF EXISTS` limpian lo anterior) hasta que no haya errores.
4. Pestaña **Hoja de datos**: deben verse 8 aeropuertos, 9 vuelos, 8 pasajeros y 15 reservas.

Así se cargan las fechas de los vuelos:

```sql
strftime('%Y-%m-%d %H:%M', 'now', 'localtime', '+12 days', 'start of day', '+7 hours', '+30 minutes')
```

Se lee de izquierda a derecha: *ahora*, en hora local, *más doce días*, *al principio de ese día* (00:00), *más 7 horas
y 30 minutos* → dentro de 12 días a las 07:30, con el formato `'AAAA-MM-DD HH:MM'`. Las fechas se calculan **a partir del día
en que se ejecuta el script**: así siempre hay vuelos futuros para reservar, y el `AT0999` siempre salió ayer.

:::cuidado
La contracara: si la base se creó hace una semana, hoy varios vuelos ya "salieron". Cuando las fechas no sean coherentes
(o cuando se modifique el script), **borrar `aerotaller.db` con la app detenida**: la próxima vez que arranque, se recrea con
fechas nuevas.
:::

### Paso 4 · Que el script viaje con la app

Cuando la app corre, el ejecutable está en `AeroTaller/bin/Debug/net10.0/`. El script debe estar **ahí**, así que
se le indica al compilador que lo copie. En `AeroTaller.csproj`, dentro de `<Project>`:

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
así no se depende de eso (lo indica el tema 08).

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

// La cadena se lee UNA vez, al arrancar. Si falta, la app no levanta (mejor que un error confuso después)
string cadenaConexion = builder.Configuration.GetConnectionString("AeroTaller")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'AeroTaller' en appsettings.json");
CrearBaseSiNoExiste(cadenaConexion);

builder.Services.AddControllersWithViews();
// … el resto, igual que antes …
```

Y al final del archivo, **después** de `app.Run();`, la función (es la misma idea del proyecto de ejemplo del tema 10):

```csharp
// Si la base no existe, la arma con aerotaller.sql
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
| La cadena sale de la configuración, no escrita en el código | el verificador levanta la app con **su** base temporal pasándole otra cadena por variable de entorno |
| `DataSource` del builder, no `"aerotaller.db"` a mano | si la cadena cambia, el archivo que se consulta en `File.Exists` cambia con ella |
| `AppContext.BaseDirectory` para el script | es la carpeta del ejecutable (`bin/Debug/net10.0/`), donde lo copió el paso 4; la carpeta "actual" puede ser otra |

### Paso 6 · Arrancar y comprobar

Correr la app (`dotnet run --project AeroTaller` o F5). Debe aparecer `AeroTaller/aerotaller.db`. Detenerla, abrir ese
archivo con DB Browser (**Abrir base de datos**) y confirmar que están las cuatro tablas con sus datos.

Hacer la prueba definitiva: **app detenida → borrar `aerotaller.db` → arrancar**. La base debe volver a crearse.

:::cuidado
Antes de arrancar la app, **cerrar la base en DB Browser** (o al menos usar *Escribir cambios*). Si DB Browser tiene
cambios sin escribir, la base queda bloqueada para los demás y la app recibe `SQLite Error 5: 'database is locked'`.
:::

:::nota
`aerotaller.db` **no se commitea**: está en el `.gitignore`. Lo que se commitea es el script. La base es un resultado,
como los archivos de `bin/`: cualquiera la regenera arrancando la app.
:::

### Paso 7 · Seis consultas en DB Browser

Con `aerotaller.db` abierta en DB Browser, pestaña **Ejecutar SQL**. Intentar cada una antes de abrir la solución:
estas consultas son las que se escriben desde C# en el desafío 2.

**1 · Listado ordenado.** Número, salida y precio de todos los vuelos, del que sale primero al último.

:::solucion Consulta 1
```sql
SELECT Numero, Salida, Precio
FROM Vuelo
ORDER BY Salida;
```
Primero aparece `AT0999` (salió ayer) y después `AT0500` (sale mañana). Como las fechas son `'AAAA-MM-DD HH:MM'`, ordenar
el texto es ordenar por fecha.
:::

**2 · Buscar con LIKE.** Los pasajeros cuyo nombre termina en "ez".

:::solucion Consulta 2
```sql
SELECT Nombre, Dni
FROM Pasajero
WHERE Nombre LIKE '%ez';
```
Devuelve Ana Gómez y Facundo López. Probar también `LIKE '%gom%'`: **no trae a Gómez**. El `LIKE` de SQLite ignora
mayúsculas solo en letras sin tilde, y `o` no es `ó`. Tenerlo en cuenta al probar el buscador de pasajeros.
:::

**3 · JOIN doble con alias.** Cada vuelo con el **código** del aeropuerto de origen y el de destino (`TUC → AEP`).

Aquí está la dificultad: hay que unir `Vuelo` con `Aeropuerto` **dos veces**, una por cada clave foránea. Como es la misma
tabla, cada aparición necesita su propio alias: `o` para el origen y `d` para el destino.

:::solucion Consulta 3
```sql
SELECT v.Numero, o.Codigo AS Origen, d.Codigo AS Destino, v.Salida
FROM Vuelo v
INNER JOIN Aeropuerto o ON v.OrigenId  = o.Id
INNER JOIN Aeropuerto d ON v.DestinoId = d.Id
ORDER BY v.Salida;
```
Sin alias, SQLite no sabría a cuál de los dos `Aeropuerto` se refiere la columna (`ambiguous column name`).
:::

**4 · Ocupación con LEFT JOIN y GROUP BY.** Para **cada** vuelo: número, capacidad (`Filas * 6`) y cuántas reservas
**confirmadas** tiene. Los vuelos sin reservas deben aparecer con 0.

El `INNER JOIN` del tema 07 solo devuelve las filas que tienen pareja: un vuelo sin reservas desaparece. El **`LEFT JOIN`**
conserva todas las filas de la tabla de la izquierda (`Vuelo`) y, si no hay pareja, completa las columnas de la derecha
(`Reserva`) con `NULL`.

:::solucion Consulta 4
```sql
SELECT v.Numero, v.Filas * 6 AS Capacidad, COUNT(r.Id) AS Ocupados
FROM Vuelo v
LEFT JOIN Reserva r ON r.VueloId = v.Id AND r.Estado = 'Confirmada'
GROUP BY v.Id, v.Numero, v.Filas
ORDER BY v.Salida;
```
`AT4001` y `AT3110` aparecen con 0, y `AT0500` con 5 de 6 (es la avioneta casi llena). Dos detalles finos:

- La condición del estado va en el **ON**, no en el `WHERE`: en el `WHERE` descartaría los vuelos sin reservas
  (sus columnas de `r` son `NULL`).
- `COUNT(r.Id)`, no `COUNT(*)`: `COUNT(*)` cuenta filas, y el vuelo sin reservas tiene una fila (con `NULL`): daría 1.
:::

**5 · UPDATE con WHERE.** Bruno Díaz (Id 2) no tiene email: cargarle `bruno.diaz@correo.com`.

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
las 15 reservas sin preguntar. En DB Browser se puede deshacer mientras no se use *Escribir cambios*; en una app real, no.
Para volver a los datos originales: cerrar DB Browser, borrar `aerotaller.db` y arrancar la app.
:::

## 🔴 Breakpoints

:::breakpoint Program.cs · CrearBaseSiNoExiste
Colocar el punto en `if (File.Exists(archivo)) return;` y arrancar con F5 **sin** `aerotaller.db`. Observar en Variables
`cadenaConexion` y `archivo`: ¿qué parte de la cadena es `archivo`? Agregar al Watch `AppContext.BaseDirectory` y
`Directory.GetCurrentDirectory()`: ¿por qué son distintas? Con F10, ¿entra al `return` o sigue de largo?
Volver a arrancar ahora que la base existe: ¿qué cambia?
:::

:::breakpoint Program.cs · comando.ExecuteNonQuery()
Detener la ejecución justo antes de ejecutar el script. Observar `comando.CommandText`: es el `aerotaller.sql` entero, como un solo string.
Abrir la carpeta `AeroTaller/` en el explorador: `aerotaller.db` **ya existe** (lo creó `conexion.Open()`), pero pesa 0 bytes.
Presionar F10 y volver a observar el tamaño. ¿Qué pasaría si el script fallara justo en este punto?
:::

:::breakpoint Program.cs · la línea de GetConnectionString
Arrancar desde la terminal pasándole otra cadena, como hace el verificador:
`dotnet run --project AeroTaller -- "--ConnectionStrings:AeroTaller=Data Source=prueba.db"`.
(Para depurarlo con F5, puede agregarse ese argumento en `"args"` del `launch.json`.) ¿Qué archivo se crea ahora?
¿Fue necesario cambiar alguna línea de código?
:::

Preguntas para analizar:

- Si el script tiene un error en el último `INSERT`, ¿qué queda en `aerotaller.db`? ¿Y en el siguiente arranque?
- ¿Por qué `CrearBaseSiNoExiste` es `static`? ¿Usa algo del `builder` o de la `app`?

## 🔀 Alternativas

**¿Cómo nace la base?**

| Forma | Cómo | A favor | En contra | Cuándo |
|---|---|---|---|---|
| Script + `CrearBaseSiNoExiste` (la de la cátedra) | la app ejecuta `aerotaller.sql` si el archivo no existe | el repositorio tiene solo texto; cualquiera la regenera; la corrección automática arranca de cero | si se cambia el script, hay que borrar la base a mano | TP, parcial, proyectos chicos |
| Base armada a mano en DB Browser y commiteada | se sube `aerotaller.db` al repositorio | cero código | un binario en git: no se ve qué cambió, genera conflictos entre compañeros, las fechas quedan viejas | nunca en este TP |
| `CREATE TABLE IF NOT EXISTS` en cada arranque | se ejecuta siempre, sin `File.Exists` | sobrevive a una base a medio crear | los `INSERT` de ejemplo se repetirían cada vez: hay que separarlos | cuando la estructura crece de a poco |
| Migraciones (Entity Framework Core) | el esquema se versiona como código C# | cambios ordenados en bases reales | otra herramienta entera; la materia usa ADO.NET | proyectos profesionales (para investigar) |

**¿Cómo se lee la cadena?**

| Forma | Si falta la cadena… | Comentario |
|---|---|---|
| `GetConnectionString("AeroTaller")!` (proyecto del tema 10) | sigue con `null` y falla después, lejos del problema | el `!` solo silencia al compilador |
| `GetConnectionString("AeroTaller") ?? throw new InvalidOperationException("…")` | la app no arranca e indica **qué** falta | la que se usa en AeroTaller |
| `"Data Source=aerotaller.db"` escrito en el código | — | el verificador no puede cambiar la base: falla |

:::catedra
En el proyecto de ejemplo del [tema 10](https://taller-de-lenguajes-2.github.io/clases/tema-10-mvc/index.html) la cadena
se lee con `GetConnectionString("Clinica")!` y la base se crea con la misma `CrearBaseSiNoExiste`. En AeroTaller se reemplaza
el `!` por `?? throw` para que, si falta la configuración, el error indique exactamente qué ocurre. Las dos formas son válidas
en el parcial.
:::

## ⚠️ Errores típicos

- **Un nombre distinto al contrato** (`DNI` en vez de `Dni`, `Duracion` en vez de `DuracionMinutos`): los `INSERT` de
  `datos.sql` fallan con `table Vuelo has no column named …`. Corregir el `CREATE TABLE`, no los datos.
- **El script no se copió a la salida**: `FileNotFoundException: Could not find file '…/bin/Debug/net10.0/aerotaller.sql'`.
  Y lo peor: `conexion.Open()` ya creó un `aerotaller.db` de 0 bytes, así que la próxima vez `CrearBaseSiNoExiste` sale
  por el `return` y la app queda con una base sin tablas: la primera consulta (desde el desafío 2) falla con
  `SQLite Error 1: 'no such table: Vuelo'`. Corregir el `.csproj` **y** borrar la base.
- **"El script cambió y no pasa nada"**: la base ya existía, `CrearBaseSiNoExiste` salió por el `return`. Borrar `aerotaller.db`.
- **`DROP TABLE` en el orden equivocado**: borrar `Vuelo` antes que `Reserva` con datos da `FOREIGN KEY constraint failed`.
  Primero las tablas hijas.
- **`SQLite Error 5: 'database is locked'`**: DB Browser tiene la base abierta con cambios sin escribir. Escribir los cambios o cerrar.
- **`COUNT(*)` con `LEFT JOIN`**: los vuelos sin reservas dan 1 en vez de 0. Contar una columna de la tabla de la derecha.
- **`UPDATE` o `DELETE` sin `WHERE`**: cambia o borra **toda** la tabla. En el parcial, leer dos veces antes de ejecutar.

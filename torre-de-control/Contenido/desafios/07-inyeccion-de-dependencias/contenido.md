## 🎯 Misión

AeroTaller ya reserva, cancela y valida. Pero cada pieza **sabe demasiado** de las otras:

- `VuelosController` pide un `VueloRepository`: sabe que atrás hay SQLite.
- `ReservaService` pide un `ReservaRepository`, un `VueloRepository` y un `PasajeroRepository`: también.
- `Program.cs` arma cada repositorio a mano: `AddScoped(_ => new VueloRepository(cadenaConexion))`.

¿Y si mañana la base es otra? ¿Y si queremos probar la app **sin base** (porque se rompió, porque todavía no existe, o
para una demo)? Hoy habría que tocar controladores y servicios. La idea de este desafío es que eso no pase nunca más.

Al terminar, AeroTaller va a poder correr de dos formas, y **lo único que cambia entre una y otra es el bloque de
registros de `Program.cs`**:

| `OrigenDeDatos` | Repositorios | ¿Usa `aerotaller.db`? |
|---|---|---|
| `Sqlite` (por defecto) | `VueloRepository`, `PasajeroRepository`… (ADO.NET) | sí |
| `Falso` | `VueloRepositoryFalso`, `PasajeroRepositoryFalso`… (listas en memoria) | **no**: ni la crea ni la abre |

Controladores, servicios y vistas: **los mismos**, sin una sola línea distinta. Es como la toma de energía de tierra de un
avión en la plataforma: el avión tiene un enchufe estándar y no le importa qué equipo lo alimenta, mientras respete el enchufe.
En C#, el enchufe es una **interfaz**.

Lo que tiene que quedar andando:

1. Una interfaz por repositorio (`IVueloRepository`…) en `Repositorios/Interfaces.cs`.
2. La cadena de conexión envuelta en `record ConexionDb(string Cadena)`, registrada como **Singleton**.
3. Los registros `AddScoped<IVueloRepository, VueloRepository>()`, y controladores y servicios que piden **interfaces**.
4. `Repositorios/Falsos/`: una `BaseFalsa` (Singleton) y un repositorio falso por interfaz.
5. El interruptor `OrigenDeDatos` en `Program.cs`, y un aviso en el layout cuando los datos son falsos.
6. Los **tiempos de vida** (Transient, Scoped, Singleton) vistos en vivo, rompiendo cosas a propósito.

:::nota
El verificador revisa algo muy concreto: con `OrigenDeDatos=Falso` la app tiene que andar **sin crear ni abrir** la base.
Si `CrearBaseSiNoExiste` se ejecuta igual, no pasa. Lo vas a ver en el paso 7.
:::

## 📚 Repaso

**Una dependencia** es una clase que otra necesita para funcionar: `ReservaService` depende de los repositorios, los
repositorios dependen de la cadena de conexión. No es malo depender; la pregunta es **cómo consigue cada clase lo que necesita**:

| Forma | Ejemplo | Problema |
|---|---|---|
| La crea ella | `private readonly VueloRepository vuelos = new("Data Source=aerotaller.db");` | sabe que es SQLite, dónde está el archivo y cómo se arma |
| **Se la dan** (inyección) | `public class VuelosController(IVueloRepository vuelos) : Controller` | ninguno: solo sabe *qué* puede hacer con lo que recibe |

Eso es la **D de SOLID** (inversión de dependencias): lo que decide (controladores, servicios) no depende de los detalles
(SQLite); los dos dependen de una abstracción (la interfaz).

**El contenedor** de .NET hace el trabajo en dos pasos:

1. **Registrar**, en `Program.cs`: «para `IVueloRepository`, entregá un `VueloRepository`, uno por request».
2. **Recibir**, en el constructor: la clase pide `IVueloRepository` y el contenedor se la pasa.

Para atender `GET /Vuelos/Detalle/3`, el contenedor lee el constructor de `VuelosController`, ve que pide repositorios,
lee el constructor de cada repositorio, ve que piden `ConexionDb`… y crea todo **de abajo hacia arriba**. Lo único que
armamos a mano es `new ConexionDb(cadena)`.

**Los tres tiempos de vida**, con ejemplos de AeroTaller:

| Tiempo de vida | Cuántos objetos | En AeroTaller |
|---|---|---|
| `AddTransient` | uno nuevo **cada vez** que alguien lo pide | un servicio sin estado que arma un texto (no tenemos ninguno todavía) |
| `AddScoped` | uno **por request**, compartido por todos los que lo piden en ese request | repositorios, servicios |
| `AddSingleton` | uno **para toda la aplicación**, hasta que se reinicia | `ConexionDb`, `BaseFalsa` |

Y la regla que no se negocia: **un servicio solo puede recibir servicios que vivan igual o más que él**. Un Scoped puede
recibir un Singleton (el repositorio recibe `ConexionDb`); un Singleton no puede recibir un Scoped.

:::catedra
Todo esto es el [tema 14](https://taller-de-lenguajes-2.github.io/clases/tema-14-inyeccion-de-dependencias/index.html):
la impresora y el cartucho, `record ConexionDb`, «Cambiar una implementación» (`PacienteRepository` → `PacienteRepositoryJson`
cambiando una línea) y la página `/TiemposDeVida` del proyecto de ejemplo. Si no la probaste, corré
`tema-14-inyeccion-de-dependencias/proyecto-ejemplo` y recargá `/TiemposDeVida` varias veces antes de seguir: es la mejor forma de
entender los tiempos de vida.
:::

## 💻 Paso a paso

### Paso 1 · Las interfaces: qué se puede hacer con los datos

Creá `Repositorios/Interfaces.cs`. Para cada repositorio, copiá la **firma** de cada método público (sin el cuerpo).
La interfaz dice **qué** se puede hacer; la clase dice **cómo**.

```csharp
// Repositorios/Interfaces.cs
// Las abstracciones de las que dependen los servicios y los controladores.

public interface IVueloRepository
{
    List<Vuelo> Listar();
    List<Vuelo> Buscar(int? destinoId, DateTime? desde);
    Vuelo? Obtener(int id);
    bool ExisteNumero(string numero);
    int Insertar(Vuelo vuelo);
    List<OcupacionVuelo> ListarOcupacion();
}

public interface IReservaRepository
{
    List<Reserva> Listar(EstadoReserva? estado);
    List<Reserva> ListarPorPasajero(int pasajeroId);
    Reserva? Obtener(int id);
    List<string> AsientosOcupados(int vueloId);
    bool PasajeroTieneReserva(int vueloId, int pasajeroId);
    int ContarConfirmadas(int vueloId);
    int Insertar(Reserva reserva);
    bool CambiarEstado(int id, EstadoReserva estado);
}
```

Hacé lo mismo con `IAeropuertoRepository` y `IPasajeroRepository`, y después agregá `: IXRepository` a cada clase:

```csharp
public class VueloRepository(...) : IVueloRepository
```

Si te olvidaste un método en la clase, el compilador te avisa:
`'VueloRepository' does not implement interface member 'IVueloRepository.ExisteNumero(string)'`.

:::nota
Los métodos de **tus** interfaces son los que tengan **tus** repositorios (el contrato solo exige los nombres de las
interfaces). Si tu `Listar` de reservas no recibe el estado, tu interfaz tampoco. Lo que no puede pasar es que un
controlador o un servicio use un método que la interfaz no tiene.
:::

:::solucion Ver las otras dos interfaces
```csharp
public interface IAeropuertoRepository
{
    List<Aeropuerto> Listar();
    Aeropuerto? Obtener(int id);
}

public interface IPasajeroRepository
{
    List<Pasajero> Listar();
    List<Pasajero> Buscar(string texto);
    Pasajero? Obtener(int id);
    bool ExisteDni(string dni, int? excepto = null);
    int Insertar(Pasajero pasajero);
    bool Actualizar(Pasajero pasajero);
    bool Eliminar(int id);
}
```
:::

### Paso 2 · ConexionDb: la cadena con nombre propio

Hasta ahora los repositorios recibían un `string`. El contenedor busca las dependencias **por tipo**: si un repositorio
pide un `string`, ¿cuál le da? ¿La cadena de conexión, el nombre de la app, un email? No sabe. Por eso la envolvemos en un
tipo propio:

```csharp
// Repositorios/ConexionDb.cs
// La cadena de conexión envuelta en un tipo propio: el contenedor busca por TIPO,
// y con ConexionDb no hay dudas de qué se pide.
public record ConexionDb(string Cadena);
```

Y en cada repositorio cambia **solo el constructor**. El truco es guardar la cadena en un campo con el mismo nombre que
tenía el parámetro: así el resto de los métodos no se tocan.

```csharp
// Antes (desafíos 2 a 6)
public class VueloRepository(string cadenaConexion)
{
    ...
}

// Ahora
public class VueloRepository(ConexionDb conexionDb) : IVueloRepository
{
    private readonly string cadenaConexion = conexionDb.Cadena;   // ← el resto del archivo sigue igual

    public List<Vuelo> Listar()
    {
        using var conexion = new SqliteConnection(cadenaConexion);
        ...
    }
}
```

Repetilo en los cuatro repositorios. Va a dejar de compilar `Program.cs` (los `new VueloRepository(cadenaConexion)`):
es lo que arreglamos en el paso que sigue.

### Paso 3 · Registrar en el contenedor

En `Program.cs`, reemplazá los registros «a mano» por registros **interfaz → clase**:

```csharp
// Antes (desafío 3): el registro nombra la clase concreta y la arma con una lambda
builder.Services.AddScoped(_ => new VueloRepository(cadenaConexion));

// Ahora: la cadena, envuelta, una sola para toda la aplicación (es un dato que no cambia)
builder.Services.AddSingleton(new ConexionDb(cadenaConexion));

// Repositorios: uno por request. El contenedor ve que piden ConexionDb y se la pasa solo
builder.Services.AddScoped<IAeropuertoRepository, AeropuertoRepository>();
builder.Services.AddScoped<IVueloRepository, VueloRepository>();
builder.Services.AddScoped<IPasajeroRepository, PasajeroRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
```

Leelo así: «cuando alguien pida un `IVueloRepository`, creá un `VueloRepository`, uno por request».
Los servicios ya estaban registrados así desde el desafío 6 (`AddScoped<IReservaService, ReservaService>()`): ahora los
repositorios juegan con las mismas reglas.

:::cuidado
`AddSingleton(new ConexionDb(cadenaConexion))` lleva el **objeto ya creado**. Si escribís `AddSingleton<ConexionDb>()`,
el contenedor intenta crearlo él, ve que el constructor pide un `string` y explota:
`Unable to resolve service for type 'System.String' while attempting to activate 'ConexionDb'.`
:::

### Paso 4 · Que nadie pida clases concretas

Si corrés ahora, la app **ni arranca**:

```text
System.AggregateException: Some services are not able to be constructed
(Error while validating the service descriptor 'ServiceType: IReservaService Lifetime: Scoped ImplementationType: ReservaService':
Unable to resolve service for type 'ReservaRepository' while attempting to activate 'ReservaService'.) …
```

¡Bien! Es el contenedor diciendo la verdad: `ReservaService` pide un `ReservaRepository`, pero en `Program.cs` ya no hay
nadie registrado con **ese** tipo (ahora está registrado `IReservaRepository`). En Development, .NET revisa al arrancar que
todos los servicios registrados se puedan construir. Cambiá los constructores de **todos** los servicios y controladores
para que pidan interfaces:

```csharp
public class VuelosController(
    IVueloRepository vuelos,
    IAeropuertoRepository aeropuertos,
    IReservaRepository reservas,
    IVueloService vueloService) : Controller
{
    // las acciones NO cambian: vuelos.Listar(), reservas.AsientosOcupados(id)… son métodos de la interfaz
}

public class ReservaService(
    IReservaRepository reservas,
    IVueloRepository vuelos,
    IPasajeroRepository pasajeros) : IReservaService
{
    // las reglas tampoco cambian
}
```

Para no olvidarte de ninguno, usá **Buscar en archivos** (`Ctrl/Cmd + Shift + F`) con expresión regular activada
(el botón `.*`) y en «archivos para incluir» poné `Controllers, Servicios`:

- `(Aeropuerto|Vuelo|Pasajero|Reserva)Repository \w+` → parámetros de tipo concreto (tienen que dar **cero**).
- `new \w+Repository\(` → alguien creando un repositorio a mano (también **cero**).

:::nota
Ojo con los **controladores**: no están registrados en el contenedor como los servicios, así que .NET no los revisa al
arrancar. Si arreglaste los servicios y te olvidaste de uno, la app arranca bien… y recién cuando entrás a esa pantalla aparece
`InvalidOperationException: Unable to resolve service for type 'VueloRepository' while attempting to activate 'VuelosController'.`
Por eso conviene recorrer **todas** las pantallas después de un cambio así.
:::

Corré la app y recorré todo: vuelos, detalle con el mapa, reservar, cancelar, pasajeros. Tiene que andar **igual que antes**.
Si anda igual, lo hiciste bien: cambiaste la arquitectura sin cambiar el comportamiento.

### Paso 5 · La base falsa: una "base de datos" en memoria

Ahora la parte divertida: una segunda implementación de las mismas interfaces, sin SQL. Primero, el lugar donde viven
los datos. Creá `Repositorios/Falsos/BaseFalsa.cs`:

```csharp
// Una "base de datos" en memoria para los repositorios falsos.
// Se registra como Singleton: las listas viven mientras corre la app (al reiniciar, vuelven a estos datos).
// Todos los repositorios falsos reciben ESTA misma instancia por el constructor.
public class BaseFalsa
{
    public List<Aeropuerto> Aeropuertos { get; }
    public List<Vuelo> Vuelos { get; }
    public List<Pasajero> Pasajeros { get; }
    public List<Reserva> Reservas { get; }

    // Las listas no son seguras para varios requests a la vez: todos los repos falsos usan este candado
    public object Candado { get; } = new();

    public BaseFalsa()
    {
        var tuc = new Aeropuerto { Id = 1, Codigo = "TUC", Ciudad = "San Miguel de Tucumán", Nombre = "Teniente Benjamín Matienzo" };
        var aep = new Aeropuerto { Id = 2, Codigo = "AEP", Ciudad = "Buenos Aires", Nombre = "Aeroparque Jorge Newbery" };
        var brc = new Aeropuerto { Id = 3, Codigo = "BRC", Ciudad = "San Carlos de Bariloche", Nombre = "Teniente Luis Candelaria" };
        Aeropuertos = [tuc, aep, brc];

        DateTime hoy = DateTime.Today;
        Vuelos =
        [
            new Vuelo { Id = 1, Numero = "FK0001", Origen = tuc, Destino = aep, Salida = hoy.AddDays(1).AddHours(8), DuracionMinutos = 125, Filas = 4, Precio = 1 },
            new Vuelo { Id = 2, Numero = "FK0002", Origen = aep, Destino = brc, Salida = hoy.AddDays(2).AddHours(10), DuracionMinutos = 140, Filas = 4, Precio = 1 }
        ];

        Pasajeros =
        [
            new Pasajero { Id = 1, Nombre = "Pasajera de Prueba", Dni = "11111111", Email = "prueba@falso.com" },
            new Pasajero { Id = 2, Nombre = "Pasajero de Prueba", Dni = "22222222" }
        ];

        Reservas =
        [
            new Reserva { Id = 1, Vuelo = Vuelos[0], Pasajero = Pasajeros[0], Asiento = "1A", Fecha = hoy }
        ];
    }

    // El "AUTOINCREMENT" de la memoria
    public static int SiguienteId<T>(List<T> lista, Func<T, int> id) => lista.Count == 0 ? 1 : lista.Max(id) + 1;
}
```

Tres cosas para mirar:

- **Composición** (tema 02): la reserva apunta al **mismo objeto** `Vuelos[0]`, no a un `VueloId` suelto. En memoria no
  hay JOIN: el objeto ya está ahí.
- Los números `FK…` y el precio 1 son a propósito: de un vistazo sabés que estás mirando datos falsos.
- **El candado.** Un Singleton lo comparten **todos los requests al mismo tiempo** (dos mostradores reservando a la vez).
  `List<T>` no está preparada para eso, así que cada repositorio falso hace `lock (db.Candado) { ... }` mientras toca las listas.

### Paso 6 · Los repositorios falsos: el mismo enchufe, otra instalación

Creá `Repositorios/Falsos/RepositoriosFalsos.cs` con una clase por interfaz. El nombre tiene que llevar `Falso`
(es parte del contrato). Cada una recibe la `BaseFalsa` por el constructor y hace con **LINQ** lo que la verdadera hace con SQL:

```csharp
public class VueloRepositoryFalso(BaseFalsa db) : IVueloRepository
{
    public List<Vuelo> Listar()
    {
        lock (db.Candado) return db.Vuelos.OrderBy(v => v.Salida).ToList();   // ORDER BY v.Salida
    }

    public Vuelo? Obtener(int id)
    {
        lock (db.Candado) return db.Vuelos.FirstOrDefault(v => v.Id == id);   // WHERE v.Id = @id
    }

    public int Insertar(Vuelo vuelo)
    {
        lock (db.Candado)
        {
            vuelo.Id = BaseFalsa.SiguienteId(db.Vuelos, v => v.Id);           // INSERT … RETURNING Id
            db.Vuelos.Add(vuelo);
            return vuelo.Id;
        }
    }

    // El mismo LEFT JOIN + GROUP BY del repositorio SQLite, hecho con LINQ
    public List<OcupacionVuelo> ListarOcupacion()
    {
        lock (db.Candado)
        {
            return db.Vuelos
                .Where(v => !v.YaSalio)
                .OrderBy(v => v.Salida)
                .Select(v => new OcupacionVuelo(
                    v.Id, v.Numero, $"{v.Origen.Codigo} → {v.Destino.Codigo}", v.Salida, v.Capacidad,
                    db.Reservas.Count(r => r.Vuelo.Id == v.Id && r.Estado == EstadoReserva.Confirmada)))
                .ToList();
        }
    }

    // … Buscar y ExisteNumero: intentalos vos
}
```

Intentá vos los otros tres. Pensá cada método como «¿qué hace el SQL?» → «¿cómo lo hago con `Where`, `Any`, `Count`, `Select`?».
Ojo: `ReservaRepositoryFalso.CambiarEstado` **no borra** la reserva; le cambia el `Estado` (igual que el `UPDATE`).

:::solucion Ver los cuatro repositorios falsos completos
```csharp
// Implementaciones "falsas" de las mismas interfaces: sin SQL, sin archivo .db.
// Los servicios y los controladores NO se enteran del cambio: dependen de la interfaz.

public class AeropuertoRepositoryFalso(BaseFalsa db) : IAeropuertoRepository
{
    public List<Aeropuerto> Listar()
    {
        lock (db.Candado) return db.Aeropuertos.OrderBy(a => a.Ciudad).ToList();
    }

    public Aeropuerto? Obtener(int id)
    {
        lock (db.Candado) return db.Aeropuertos.FirstOrDefault(a => a.Id == id);
    }
}

public class VueloRepositoryFalso(BaseFalsa db) : IVueloRepository
{
    public List<Vuelo> Listar()
    {
        lock (db.Candado) return db.Vuelos.OrderBy(v => v.Salida).ToList();
    }

    public List<Vuelo> Buscar(int? destinoId, DateTime? desde)
    {
        lock (db.Candado)
        {
            return db.Vuelos
                .Where(v => destinoId is null || v.Destino.Id == destinoId)
                .Where(v => desde is null || v.Salida >= desde)
                .OrderBy(v => v.Salida)
                .ToList();
        }
    }

    public Vuelo? Obtener(int id)
    {
        lock (db.Candado) return db.Vuelos.FirstOrDefault(v => v.Id == id);
    }

    public bool ExisteNumero(string numero)
    {
        lock (db.Candado) return db.Vuelos.Any(v => v.Numero == numero);
    }

    public int Insertar(Vuelo vuelo)
    {
        lock (db.Candado)
        {
            vuelo.Id = BaseFalsa.SiguienteId(db.Vuelos, v => v.Id);
            db.Vuelos.Add(vuelo);
            return vuelo.Id;
        }
    }

    public List<OcupacionVuelo> ListarOcupacion()
    {
        lock (db.Candado)
        {
            return db.Vuelos
                .Where(v => !v.YaSalio)
                .OrderBy(v => v.Salida)
                .Select(v => new OcupacionVuelo(
                    v.Id, v.Numero, $"{v.Origen.Codigo} → {v.Destino.Codigo}", v.Salida, v.Capacidad,
                    db.Reservas.Count(r => r.Vuelo.Id == v.Id && r.Estado == EstadoReserva.Confirmada)))
                .ToList();
        }
    }
}

public class PasajeroRepositoryFalso(BaseFalsa db) : IPasajeroRepository
{
    public List<Pasajero> Listar()
    {
        lock (db.Candado) return db.Pasajeros.OrderBy(p => p.Nombre).ToList();
    }

    public List<Pasajero> Buscar(string texto)
    {
        lock (db.Candado)
        {
            return db.Pasajeros
                .Where(p => p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) || p.Dni.Contains(texto))
                .OrderBy(p => p.Nombre)
                .ToList();
        }
    }

    public Pasajero? Obtener(int id)
    {
        lock (db.Candado) return db.Pasajeros.FirstOrDefault(p => p.Id == id);
    }

    public bool ExisteDni(string dni, int? excepto = null)
    {
        lock (db.Candado) return db.Pasajeros.Any(p => p.Dni == dni && p.Id != excepto);
    }

    public int Insertar(Pasajero pasajero)
    {
        lock (db.Candado)
        {
            pasajero.Id = BaseFalsa.SiguienteId(db.Pasajeros, p => p.Id);
            db.Pasajeros.Add(pasajero);
            return pasajero.Id;
        }
    }

    public bool Actualizar(Pasajero pasajero)
    {
        lock (db.Candado)
        {
            int i = db.Pasajeros.FindIndex(p => p.Id == pasajero.Id);
            if (i < 0) return false;
            db.Pasajeros[i] = pasajero;
            return true;
        }
    }

    public bool Eliminar(int id)
    {
        lock (db.Candado) return db.Pasajeros.RemoveAll(p => p.Id == id) > 0;
    }
}

public class ReservaRepositoryFalso(BaseFalsa db) : IReservaRepository
{
    public List<Reserva> Listar(EstadoReserva? estado)
    {
        lock (db.Candado)
        {
            return db.Reservas
                .Where(r => estado is null || r.Estado == estado)
                .OrderBy(r => r.Vuelo.Salida).ThenBy(r => r.Asiento)
                .ToList();
        }
    }

    public List<Reserva> ListarPorPasajero(int pasajeroId)
    {
        lock (db.Candado) return db.Reservas.Where(r => r.Pasajero.Id == pasajeroId).OrderBy(r => r.Vuelo.Salida).ToList();
    }

    public Reserva? Obtener(int id)
    {
        lock (db.Candado) return db.Reservas.FirstOrDefault(r => r.Id == id);
    }

    public List<string> AsientosOcupados(int vueloId)
    {
        lock (db.Candado)
        {
            return db.Reservas
                .Where(r => r.Vuelo.Id == vueloId && r.Estado == EstadoReserva.Confirmada)
                .Select(r => r.Asiento)
                .ToList();
        }
    }

    public bool PasajeroTieneReserva(int vueloId, int pasajeroId)
    {
        lock (db.Candado)
        {
            return db.Reservas.Any(r => r.Vuelo.Id == vueloId && r.Pasajero.Id == pasajeroId
                                        && r.Estado == EstadoReserva.Confirmada);
        }
    }

    public int ContarConfirmadas(int vueloId)
    {
        lock (db.Candado) return db.Reservas.Count(r => r.Vuelo.Id == vueloId && r.Estado == EstadoReserva.Confirmada);
    }

    public int Insertar(Reserva reserva)
    {
        lock (db.Candado)
        {
            reserva.Id = BaseFalsa.SiguienteId(db.Reservas, r => r.Id);
            db.Reservas.Add(reserva);
            return reserva.Id;
        }
    }

    public bool CambiarEstado(int id, EstadoReserva estado)
    {
        lock (db.Candado)
        {
            Reserva? reserva = db.Reservas.FirstOrDefault(r => r.Id == id);
            if (reserva is null) return false;
            reserva.Estado = estado;
            return true;
        }
    }
}
```
:::

### Paso 7 · El interruptor: OrigenDeDatos

Este es **el** paso del desafío. Agregá la clave en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AeroTaller": "Data Source=aerotaller.db;Foreign Keys=True"
  },
  "OrigenDeDatos": "Sqlite"
}
```

Y en `Program.cs`, el bloque de datos queda así:

```csharp
// "OrigenDeDatos" sale de appsettings.json, o de la línea de comandos: --OrigenDeDatos=Falso
// Fijate que SOLO cambia este bloque: controladores, servicios y vistas no se enteran.
string origenDeDatos = builder.Configuration["OrigenDeDatos"] ?? "Sqlite";

if (origenDeDatos == "Falso")
{
    // Todo en memoria: una sola BaseFalsa para toda la app
    builder.Services.AddSingleton<BaseFalsa>();
    builder.Services.AddScoped<IAeropuertoRepository, AeropuertoRepositoryFalso>();
    builder.Services.AddScoped<IVueloRepository, VueloRepositoryFalso>();
    builder.Services.AddScoped<IPasajeroRepository, PasajeroRepositoryFalso>();
    builder.Services.AddScoped<IReservaRepository, ReservaRepositoryFalso>();
}
else
{
    // La cadena y la creación de la base van ADENTRO del else: con datos falsos no se toca el archivo
    string cadenaConexion = builder.Configuration.GetConnectionString("AeroTaller")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'AeroTaller' en appsettings.json");
    CrearBaseSiNoExiste(cadenaConexion);

    builder.Services.AddSingleton(new ConexionDb(cadenaConexion));
    builder.Services.AddScoped<IAeropuertoRepository, AeropuertoRepository>();
    builder.Services.AddScoped<IVueloRepository, VueloRepository>();
    builder.Services.AddScoped<IPasajeroRepository, PasajeroRepository>();
    builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
}

// Los servicios van AFUERA del if: son los mismos para los dos orígenes
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IPasajeroService, PasajeroService>();
builder.Services.AddScoped<IVueloService, VueloService>();
```

Mirá bien qué hay adentro del `if` y qué no: **solo los repositorios**. Ese es todo el «cableado» que cambia.

Para que se note cuándo estás con datos falsos, agregá un aviso arriba de `<main>` en `Views/Shared/_Layout.cshtml`.
Una vista también puede pedirle cosas al contenedor, con `@inject`:

```cshtml
@inject IConfiguration Configuracion
@{
    bool datosFalsos = Configuracion["OrigenDeDatos"] == "Falso";
}
...
@if (datosFalsos)
{
    <div class="aviso-falso">Datos de prueba en memoria (OrigenDeDatos = Falso): al reiniciar la app se pierden los cambios.</div>
}
```

(La clase `aviso-falso` ya está en `sitio.css`.)

**Probalo de verdad**, que es el punto del desafío:

1. Apagá la app y renombrá `AeroTaller/aerotaller.db` (por ejemplo, a `aerotaller.db.bak`).
2. Arrancá con datos falsos, de cualquiera de estas formas:
   - VS Code: en **Ejecutar y depurar** elegí **AeroTaller (datos falsos)** y F5 (abrí vos `http://localhost:5210`).
   - Terminal: `dotnet run --project AeroTaller --launch-profile datos-falsos`
   - Terminal: `dotnet run --project AeroTaller -- --OrigenDeDatos=Falso`
3. Tenés que ver el aviso amarillo, los vuelos `FK0001` y `FK0002`, y el 1A ocupado en el mapa del primero.
4. Creá un pasajero, reservale un asiento, cancelá la reserva: **todo anda**, con las mismas reglas de `ReservaService`.
5. Mirá la carpeta `AeroTaller/`: **no apareció** ningún `aerotaller.db` nuevo.
6. Apagá, volvé el nombre a `aerotaller.db` y arrancá normal: tus datos de siempre, como si nada.

Ahora preguntate: entre una corrida y la otra, **¿qué archivo cambió?** Ninguno. Cambió un valor de configuración, y
`Program.cs` eligió otras clases para las mismas interfaces. Eso es inyección de dependencias.

:::nota
Pensá qué pasaría si mezclás: `IPasajeroRepository` falso y el resto con SQLite. Compila y arranca (los tipos están bien),
pero los datos no cierran: reservarías con pasajeros que en la base no existen. El contenedor garantiza que cada clase
recibe algo que **cumple la interfaz**; que los datos sean coherentes sigue siendo responsabilidad tuya.
:::

### Paso 8 · Tiempos de vida en vivo: rompé a propósito

Ahora que todo anda, vamos a romperlo para entender **por qué** cada registro tiene el tiempo de vida que tiene.
Arrancá siempre con **datos falsos** y deshacé cada cambio antes de pasar al siguiente.

**Experimento 1 · `BaseFalsa` como Scoped.** Cambiá `AddSingleton<BaseFalsa>()` por `AddScoped<BaseFalsa>()`.
Creá un pasajero: si tu POST redirige al detalle, te lleva a `/Pasajeros/Detalle/3`… y da **404** (si redirige al listado,
el pasajero nuevo directamente no aparece). Se guardó en la `BaseFalsa` del POST, pero el GET del redirect es **otro request**,
con otra `BaseFalsa` recién construida (con los dos pasajeros de siempre).

**Experimento 2 · `BaseFalsa` como Transient.** Ponela `AddTransient<BaseFalsa>()` y poné un breakpoint en la primera línea
del constructor de `BaseFalsa`. Abrí `/Vuelos/Detalle/1` y contá cuántas veces frena **en un solo request**: una por cada
repositorio falso que se creó para ese request. Cada repositorio tiene **su propia** base: lo que guarda uno, el otro no lo ve.

**Experimento 3 · Dependencia cautiva.** Volvé `BaseFalsa` a Singleton y ahora cambiá
`AddScoped<IReservaService, ReservaService>()` por `AddSingleton<IReservaService, ReservaService>()`. La app **no arranca**:

```text
System.AggregateException: Some services are not able to be constructed
(Error while validating the service descriptor 'ServiceType: IReservaService Lifetime: Singleton ImplementationType: ReservaService':
Cannot consume scoped service 'IReservaRepository' from singleton 'IReservaService'.)
```

Un Singleton que recibe un Scoped se quedaría con el repositorio del **primer** request para siempre. En Development, .NET lo
detecta al arrancar y no te deja seguir.

| Registro | Resultado | Por qué |
|---|---|---|
| `BaseFalsa` Singleton | ✅ los datos duran hasta reiniciar | una sola lista para todos, como una base de verdad |
| `BaseFalsa` Scoped | ❌ lo nuevo desaparece en el request siguiente | una base nueva por request |
| `BaseFalsa` Transient | ❌ ni siquiera dentro del mismo request coinciden | una base por cada repositorio que la pide |
| Servicio Singleton con repositorio Scoped | 💥 no arranca | dependencia cautiva |

:::cuidado
Antes de commitear: `BaseFalsa` en **Singleton**, `ReservaService` en **Scoped** y `"OrigenDeDatos": "Sqlite"` en
`appsettings.json`. El verificador prueba las dos configuraciones.
:::

## 🔴 Breakpoints

:::breakpoint Program.cs · if (origenDeDatos == "Falso")
Arrancá con **AeroTaller** y después con **AeroTaller (datos falsos)**. En Variables mirá `origenDeDatos`, y con F10 fijate
por qué rama entra. Frena **una sola vez**, al arrancar: registrar es al arrancar; crear los objetos es en cada request
(ya lo habías visto en el desafío 0 con `AddControllersWithViews`).
:::

:::breakpoint BaseFalsa.cs · constructor, primera línea
Con datos falsos, abrí `/Vuelos`. Mirá el panel **Pila de llamadas (Call Stack)**: ¿quién llama al constructor? Vos no:
vas a ver métodos de `Microsoft.Extensions.DependencyInjection` (el contenedor) y, más arriba, la creación del repositorio falso
y del controlador. Recargá la página: con Singleton **no vuelve a frenar**. Con Scoped (experimento 1), frena en cada request.
:::

:::breakpoint VuelosController.cs · Index, primera línea
Pasá el mouse sobre `vuelos` (o buscalo en Variables, adentro de `this`). Con **AeroTaller** el objeto es un `VueloRepository`;
con **datos falsos**, un `VueloRepositoryFalso`. El tipo declarado (`IVueloRepository`) es el mismo en los dos casos.
Si querés, agregá `vuelos.GetType().Name` en Inspección (Watch).
:::

Preguntate:

- ¿Cuántas veces se ejecuta `new ConexionDb(...)` mientras la app corre? ¿Y el constructor de `VueloRepository`?
- Si `ReservaService` y `VuelosController` piden `IVueloRepository` en el mismo request, ¿reciben el mismo objeto o dos distintos?
- ¿Por qué el repositorio falso puede ser Scoped si `BaseFalsa` es Singleton, y no al revés?

## 🔀 Alternativas

**¿Cómo le llega la cadena de conexión al repositorio?**

| Forma | Código | A favor | En contra | Cuándo |
|---|---|---|---|---|
| Lambda con `new` (tema 10, desafíos 3–6) | `AddScoped(_ => new VueloRepository(cadena))` | explícito, fácil de leer | el registro nombra la clase concreta; si el repo pide algo más, hay que cambiar la lambda | proyectos chicos sin interfaces |
| **`ConexionDb` inyectado** (lo nuestro) | `AddSingleton(new ConexionDb(cadena))` + `AddScoped<IVueloRepository, VueloRepository>()` | el contenedor arma todo; el repo recibe **solo** lo que usa, y el tipo dice qué es | un tipo más | lo que pide la cátedra y el parcial |
| `IConfiguration` en el repositorio | `VueloRepository(IConfiguration config)` y adentro `config.GetConnectionString("AeroTaller")` | cero código extra | el repo recibe **toda** la configuración y depende del nombre de la clave | prototipos rápidos |

**¿Cómo elegís la implementación?**

| Forma | A favor | En contra |
|---|---|---|
| **`if` con una clave de configuración** (lo nuestro) | se cambia sin recompilar: `appsettings.json`, línea de comandos o variable de entorno (`OrigenDeDatos=Falso`) | el `if` crece si hay muchas variantes |
| Comentar y descomentar líneas en `Program.cs` | simple, es lo que muestra la diapositiva | hay que recompilar, y es fácil commitear la línea equivocada |
| Un `appsettings.Development.json` con `"OrigenDeDatos": "Falso"` | cada entorno con su valor | menos visible: «¿por qué en mi máquina anda distinto?» |

:::catedra
En la diapositiva «Cambiar una implementación» del tema 14 el cambio es una línea: `PacienteRepository` → `PacienteRepositoryJson`.
Acá es lo mismo, pero con cuatro repositorios a la vez y elegido por configuración. Y en el ejemplo de la clase,
`UsuarioRepository` (los usuarios en una lista en memoria) se registra **Singleton** por el mismo motivo que `BaseFalsa`.
La diapositiva lee la cadena con `GetConnectionString("Clinica")!`; acá usamos `?? throw new InvalidOperationException(...)`
para que, si falta, el error diga qué falta.
:::

## ⚠️ Errores típicos

- **`Unable to resolve service for type 'VueloRepository' while attempting to activate 'VuelosController'`**: registraste
  la interfaz pero el controlador todavía pide la clase concreta. Al revés (`'IVueloRepository'`) es que falta el registro,
  muchas veces en **una sola rama** del `if` (anda con SQLite y explota con Falso, o viceversa).
- **`CrearBaseSiNoExiste` afuera del `else`**: con datos falsos la app igual crea `aerotaller.db`. Anda, pero el verificador
  lo marca: la idea es poder correr **sin** base.
- **`BaseFalsa` Scoped o Transient**: lo que creás desaparece en el request siguiente (el 404 del experimento 1).
- **Cada repositorio falso con sus propias listas** (`private static List<Reserva> reservas = ...` adentro de cada uno):
  la reserva que guarda `ReservaRepositoryFalso` no la ve `VueloRepositoryFalso`, y el tablero muestra 0 ocupados.
  Una sola `BaseFalsa` compartida lo resuelve.
- **`Cannot consume scoped service … from singleton …`**: registraste como Singleton algo que recibe un Scoped.
  Bajalo a Scoped.
- **`AddSingleton<ConexionDb>()` sin el objeto**: `Unable to resolve service for type 'System.String'…`.
  Va `AddSingleton(new ConexionDb(cadenaConexion))`.
- **`"OrigenDeDatos": "falso"`** con minúscula: `"falso" == "Falso"` es `false`, la app usa SQLite y no te avisa.
  Para eso está el aviso amarillo: si no lo ves, no estás con datos falsos.

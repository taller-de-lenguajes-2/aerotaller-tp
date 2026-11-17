using System.Text.Json;
using System.Text.RegularExpressions;

public class Desafio0 : Desafio
{
    public override int Numero => 0;
    public override string Titulo => "Check-in";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        informe.Probar("Program.cs registra MVC (AddControllersWithViews)", () =>
            Resultado(p.Leer("Program.cs").Contains("AddControllersWithViews"), "No se encontró builder.Services.AddControllersWithViews() en Program.cs."));

        using (Servidor s = ctx.Arrancar())
        using (Navegador nav = s.NuevoNavegador())
        {
            informe.Probar("La aplicación inicia y responde en /", () =>
            {
                Respuesta r = nav.Get("/");
                return Resultado(r.Codigo == 200, $"GET / {Describir(r)}");
            });
        }

        if (ctx.RevisarGit)
        {
            informe.Probar("Hay al menos un commit propio", () =>
            {
                (int codigo, string salida) = Comando.Correr("git", p.CarpetaRepo, TimeSpan.FromSeconds(10), "rev-list", "--count", "HEAD");
                if (codigo != 0) return (false, "La carpeta no es un repositorio git (el repositorio de Classroom debe clonarse).");
                int commits = int.Parse(salida.Trim());
                return Resultado(commits >= 2, "Solo existe el commit inicial de la plantilla: faltan el primer commit propio y su push.", $"{commits} commits");
            });
        }
    }
}

public class Desafio1 : Desafio
{
    public override int Numero => 1;
    public override string Titulo => "La base";

    private static readonly Dictionary<string, string[]> ColumnasEsperadas = new()
    {
        ["Aeropuerto"] = ["Id", "Codigo", "Ciudad", "Nombre"],
        ["Vuelo"] = ["Id", "Numero", "OrigenId", "DestinoId", "Salida", "DuracionMinutos", "Filas", "Precio"],
        ["Pasajero"] = ["Id", "Nombre", "Dni", "Email"],
        ["Reserva"] = ["Id", "VueloId", "PasajeroId", "Asiento", "Fecha", "Estado"]
    };

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        string csproj = File.ReadAllText(p.Csproj);

        informe.Probar("El proyecto usa el paquete Microsoft.Data.Sqlite", () =>
            Resultado(csproj.Contains("Microsoft.Data.Sqlite"), "Falta: dotnet add AeroTaller package Microsoft.Data.Sqlite"));
        informe.Probar("Existe AeroTaller/aerotaller.sql", () =>
            Resultado(p.Existe("aerotaller.sql"), "No se encontró el script aerotaller.sql en la carpeta AeroTaller/ (viene con la plantilla: recuperarlo con git checkout -- AeroTaller/aerotaller.sql)."));
        informe.Probar("aerotaller.sql se copia junto al ejecutable", () =>
            Resultado(Regex.IsMatch(csproj, @"aerotaller\.sql""[^>]*CopyToOutputDirectory|CopyToOutputDirectory[\s\S]{0,80}aerotaller\.sql", RegexOptions.IgnoreCase)
                      || Regex.IsMatch(csproj, @"Include=""aerotaller\.sql""[\s\S]{0,120}<CopyToOutputDirectory>", RegexOptions.IgnoreCase),
                "En el .csproj: <None Update=\"aerotaller.sql\" CopyToOutputDirectory=\"PreserveNewest\" />"));
        informe.Probar("appsettings.json tiene ConnectionStrings:AeroTaller", () =>
        {
            using var json = JsonDocument.Parse(p.Leer("appsettings.json"), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            bool ok = json.RootElement.TryGetProperty("ConnectionStrings", out var cs) && cs.TryGetProperty("AeroTaller", out _);
            return Resultado(ok, "Falta \"ConnectionStrings\": { \"AeroTaller\": \"Data Source=aerotaller.db;Foreign Keys=True\" }");
        });
        informe.Probar("Program.cs lee la cadena con GetConnectionString(\"AeroTaller\")", () =>
            Resultado(p.Leer("Program.cs").Contains("GetConnectionString(\"AeroTaller\")"), "La cadena debe leerse de la configuración, no escribirse en el código."));

        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);

        informe.Probar("Al iniciar con una base nueva, la aplicación la crea", () =>
            Resultado(db.Existe, "Con otra cadena de conexión la aplicación no creó la base. CrearBaseSiNoExiste debe usar la cadena de la configuración."));
        if (!db.Existe) return;

        foreach ((string tabla, string[] columnas) in ColumnasEsperadas)
        {
            informe.Probar($"Tabla {tabla} con sus columnas", () =>
            {
                if (!db.TieneTabla(tabla)) return (false, $"No existe la tabla {tabla}.");
                var faltan = columnas.Except(db.Columnas(tabla), StringComparer.OrdinalIgnoreCase).ToList();
                return Resultado(faltan.Count == 0, "Faltan columnas: " + string.Join(", ", faltan));
            });
        }

        informe.Probar("Claves foráneas de Vuelo (OrigenId y DestinoId → Aeropuerto)", () =>
        {
            var fks = db.ClavesForaneas("Vuelo");
            bool ok = fks.Any(f => f.Desde == "OrigenId" && f.Tabla == "Aeropuerto") && fks.Any(f => f.Desde == "DestinoId" && f.Tabla == "Aeropuerto");
            return Resultado(ok, "Faltan FOREIGN KEY (OrigenId) y/o (DestinoId) REFERENCES Aeropuerto(Id).");
        });
        informe.Probar("Claves foráneas de Reserva (VueloId → Vuelo, PasajeroId → Pasajero)", () =>
        {
            var fks = db.ClavesForaneas("Reserva");
            bool ok = fks.Any(f => f.Desde == "VueloId" && f.Tabla == "Vuelo") && fks.Any(f => f.Desde == "PasajeroId" && f.Tabla == "Pasajero");
            return Resultado(ok, "Faltan FOREIGN KEY (VueloId) REFERENCES Vuelo(Id) y/o (PasajeroId) REFERENCES Pasajero(Id).");
        });
        informe.Probar("El DNI del pasajero es único", () =>
            Resultado(db.EsUnica("Pasajero", "Dni"), "Dni TEXT NOT NULL UNIQUE"));
        informe.Probar("El número de vuelo es único", () =>
            Resultado(db.EsUnica("Vuelo", "Numero"), "Numero TEXT NOT NULL UNIQUE"));
        informe.Probar("Los datos de ejemplo de aerotaller.sql están cargados", () =>
        {
            long vuelos = db.Contar("SELECT COUNT(*) FROM Vuelo"), reservas = db.TotalReservas(), pasajeros = db.Contar("SELECT COUNT(*) FROM Pasajero");
            return Resultado(vuelos == 9 && reservas == 15 && pasajeros == 8,
                $"Se esperaban 9 vuelos, 8 pasajeros y 15 reservas; hay {vuelos}, {pasajeros} y {reservas}. Los datos de aerotaller.sql no deben modificarse.");
        });
    }
}

public class Desafio2 : Desafio
{
    public override int Numero => 2;
    public override string Titulo => "Modelos y repositorios";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;

        informe.Probar("Clases del dominio: Aeropuerto, Vuelo, Pasajero, Reserva y el enum EstadoReserva", () =>
        {
            var faltan = new[] { "Aeropuerto", "Vuelo", "Pasajero", "Reserva" }.Where(c => !p.DeclaraTipo(c, "class")).ToList();
            if (!p.DeclaraTipo("EstadoReserva", "enum")) faltan.Add("enum EstadoReserva");
            return Resultado(faltan.Count == 0, "Falta declarar: " + string.Join(", ", faltan));
        });
        informe.Probar("Composición: el Vuelo tiene Aeropuerto Origen y Destino; la Reserva tiene Vuelo y Pasajero", () =>
        {
            string todo = string.Join("\n", p.Cs.Select(f => f.Texto));
            var faltan = new List<string>();
            if (!Regex.IsMatch(todo, @"\bAeropuerto\??\s+Origen\b")) faltan.Add("Aeropuerto Origen");
            if (!Regex.IsMatch(todo, @"\bAeropuerto\??\s+Destino\b")) faltan.Add("Aeropuerto Destino");
            if (!Regex.IsMatch(todo, @"\bVuelo\??\s+Vuelo\s*\{")) faltan.Add("Vuelo Vuelo (en Reserva)");
            if (!Regex.IsMatch(todo, @"\bPasajero\??\s+Pasajero\s*\{")) faltan.Add("Pasajero Pasajero (en Reserva)");
            return Resultado(faltan.Count == 0, "Los objetos se componen de otros objetos, no de Id aislados. Falta: " + string.Join(", ", faltan));
        });
        informe.Probar("Repositorios: Aeropuerto, Vuelo, Pasajero y Reserva", () =>
        {
            var faltan = new[] { "AeropuertoRepository", "VueloRepository", "PasajeroRepository", "ReservaRepository" }
                .Where(c => !p.DeclaraTipo(c, "class")).ToList();
            return Resultado(faltan.Count == 0, "Falta: " + string.Join(", ", faltan));
        });
        informe.Probar("Las consultas usan parámetros (AddWithValue)", () =>
            Resultado(p.Coincidencias(@"Parameters\.Add(WithValue)?\(").Count > 0, "No se encontró ningún comando.Parameters.AddWithValue(...)."));
        informe.Probar("Ningún SQL armado con interpolación ($\"... {variable}\")", () =>
        {
            var hallazgos = SqlInterpolado(p);
            return Resultado(hallazgos.Count == 0, "Inyección SQL posible en: " + string.Join(" · ", hallazgos.Take(3)));
        });
        informe.Probar("Las conexiones se cierran con using", () =>
        {
            var sinUsing = p.Cs.SelectMany(f => Proyecto.SinComentarios(f.Texto).Split('\n').Select((l, i) => (f.Ruta, Linea: l, Num: i + 1)))
                .Where(x => x.Linea.Contains("new SqliteConnection(") && !x.Linea.Contains("using"))
                .Where(x => !Regex.IsMatch(x.Linea, @"^\s*(var\s+\w+\s*=|return)")) // un método Abrir() que la devuelve
                .Select(x => $"{x.Ruta}:{x.Num}")
                .ToList();
            bool hayUsing = p.Coincidencias(@"using\s*(var\s+\w+\s*=\s*)?\(?\s*(new\s+SqliteConnection|\w+\(\))").Count > 0;
            return Resultado(hayUsing && sinUsing.Count == 0,
                sinUsing.Count > 0 ? "Conexión sin using en " + string.Join(", ", sinUsing) : "Forma esperada: using var conexion = new SqliteConnection(cadenaConexion);");
        });
    }
}

public class Desafio3 : Desafio
{
    public override int Numero => 3;
    public override string Titulo => "Controladores y vistas";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        informe.Probar("Los controladores no contienen SQL (sin SqliteConnection)", () =>
        {
            var archivos = p.Coincidencias(@"\bSqliteConnection\b", r => r.StartsWith("Controllers/"));
            return Resultado(archivos.Count == 0, "SQL en: " + string.Join(", ", archivos) + ". El acceso a datos corresponde a los repositorios.");
        });

        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);
        using Navegador nav = s.NuevoNavegador();
        int at1001 = db.IdVuelo("AT1001");

        informe.Probar("El inicio muestra el tablero de próximas salidas", () =>
        {
            Respuesta r = nav.Get("/");
            return Resultado(r.Codigo == 200 && Contiene(r, "AT1001") && !Contiene(r, "AT0999"),
                $"GET / debe listar los vuelos futuros (AT1001 sí; AT0999, que ya salió, no). {Describir(r)}");
        });
        informe.Probar("GET /Vuelos lista los vuelos", () =>
        {
            Respuesta r = nav.Get("/Vuelos");
            bool ok = r.Codigo == 200 && new[] { "AT1001", "AT2020", "AT3005", "AT0500" }.All(n => Contiene(r, n));
            return Resultado(ok, $"Se esperaban AT1001, AT2020, AT3005 y AT0500. {Describir(r)}");
        });
        informe.Probar("GET /Vuelos/Detalle/{id} muestra el vuelo con su origen y destino", () =>
        {
            Respuesta r = nav.Get($"/Vuelos/Detalle/{at1001}");
            bool ok = r.Codigo == 200 && Contiene(r, "AT1001")
                && (Contiene(r, "TUC") || Contiene(r, "Tucumán")) && (Contiene(r, "AEP") || Contiene(r, "Buenos Aires"));
            return Resultado(ok, $"/Vuelos/Detalle/{at1001} (AT1001, TUC → AEP) {Describir(r)}");
        });
        informe.Probar("Un vuelo que no existe devuelve 404", () =>
        {
            Respuesta r = nav.Get("/Vuelos/Detalle/987654");
            return Resultado(r.Codigo == 404, $"Se esperaba 404 (return NotFound()); {Describir(r)}");
        });

        using Navegador mostrador = Mostrador(s, "/Pasajeros");
        informe.Probar("GET /Pasajeros lista los pasajeros", () =>
        {
            Respuesta r = mostrador.Get("/Pasajeros");
            return Resultado(r.Codigo == 200 && Contiene(r, "Ana Gómez") && Contiene(r, "Bruno Díaz"), Describir(r));
        });
        informe.Probar("GET /Pasajeros?buscar=ana filtra por nombre", () =>
        {
            Respuesta r = mostrador.Get("/Pasajeros?buscar=ana");
            return Resultado(r.Codigo == 200 && Contiene(r, "Ana Gómez") && !Contiene(r, "Bruno Díaz"),
                "Con ?buscar=ana debe aparecer Ana Gómez y no Bruno Díaz (LIKE con parámetro). " + Describir(r));
        });
        informe.Probar("GET /Pasajeros?buscar=2899 busca también por DNI", () =>
        {
            Respuesta r = mostrador.Get("/Pasajeros?buscar=2899");
            return Resultado(r.Codigo == 200 && Contiene(r, "Bruno Díaz") && !Contiene(r, "Ana Gómez"),
                "Con ?buscar=2899 debe aparecer Bruno Díaz (DNI 28999000). " + Describir(r));
        });
        informe.Probar("El detalle del pasajero muestra sus reservas", () =>
        {
            int ana = db.IdPasajero(DniAna);
            Respuesta r = mostrador.Get($"/Pasajeros/Detalle/{ana}");
            return Resultado(r.Codigo == 200 && Contiene(r, "Ana Gómez") && Contiene(r, "AT1001") && Contiene(r, "AT1002"),
                $"/Pasajeros/Detalle/{ana} debe mostrar a Ana Gómez con sus vuelos AT1001 y AT1002. {Describir(r)}");
        });
    }
}

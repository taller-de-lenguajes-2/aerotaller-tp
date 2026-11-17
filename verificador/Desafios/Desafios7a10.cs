using System.Text.RegularExpressions;

public class Desafio7 : Desafio
{
    public override int Numero => 7;
    public override string Titulo => "Inyección de dependencias";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        string program = Proyecto.SinComentarios(p.Leer("Program.cs"));

        informe.Probar("Interfaces de los repositorios", () =>
        {
            var faltan = new[] { "IAeropuertoRepository", "IVueloRepository", "IPasajeroRepository", "IReservaRepository" }
                .Where(i => !p.DeclaraTipo(i, "interface")).ToList();
            return Resultado(faltan.Count == 0, "Falta: " + string.Join(", ", faltan));
        });
        informe.Probar("ConexionDb registrada como Singleton", () =>
            Resultado(p.DeclaraTipo("ConexionDb", "(record|class)") && Regex.IsMatch(program, @"AddSingleton[^;]*ConexionDb"),
                "public record ConexionDb(string Cadena); y builder.Services.AddSingleton(new ConexionDb(cadena));"));
        informe.Probar("Program.cs registra los repositorios por su interfaz", () =>
        {
            var faltan = new[] { "IVueloRepository", "IPasajeroRepository", "IReservaRepository", "IAeropuertoRepository" }
                .Where(i => !Regex.IsMatch(program, $@"Add(Scoped|Transient|Singleton)<\s*{i}\s*,")).ToList();
            return Resultado(faltan.Count == 0, "Falta builder.Services.AddScoped<I…Repository, …>() para: " + string.Join(", ", faltan));
        });
        informe.Probar("Ningún controlador ni servicio instancia un repositorio con new", () =>
        {
            var archivos = p.Coincidencias(@"new\s+\w+Repository(Falso)?\s*\(", r => r.StartsWith("Controllers/") || r.StartsWith("Servicios/"));
            return Resultado(archivos.Count == 0, "Los repositorios deben recibirse por constructor desde el contenedor. Hay new en: " + string.Join(", ", archivos));
        });
        informe.Probar("Los controladores reciben interfaces, no clases concretas", () =>
        {
            var concretos = p.Cs.Where(f => f.Ruta.StartsWith("Controllers/"))
                .Where(f => Regex.IsMatch(Proyecto.SinComentarios(f.Texto), @"[(,]\s*(Aeropuerto|Vuelo|Pasajero|Reserva)Repository\s+\w+"))
                .Select(f => f.Ruta).ToList();
            return Resultado(concretos.Count == 0, "Reciben la clase concreta en: " + string.Join(", ", concretos));
        });
        informe.Probar("Hay repositorios falsos (en memoria)", () =>
        {
            int cantidad = p.Cs.Sum(f => Regex.Matches(f.Texto, @"class\s+\w+RepositoryFalso\b[^{]*:\s*I\w+Repository").Count);
            return Resultado(cantidad >= 4, $"Se encontraron {cantidad} clases …RepositoryFalso : I…Repository (se esperaban al menos 4).", $"{cantidad} falsos");
        });

        informe.Probar("Con OrigenDeDatos=Falso la aplicación funciona sin acceder a la base", () =>
        {
            string carpeta = ctx.NuevaCarpeta();
            string rutaProhibida = Path.Combine(carpeta, "no-tiene-que-existir.db");
            using Servidor s = Servidor.Arrancar(p, carpeta, cadena: $"Data Source={rutaProhibida};Foreign Keys=True",
                extra: new() { ["OrigenDeDatos"] = "Falso" });
            using Navegador nav = s.NuevoNavegador();
            Respuesta inicio = nav.Get("/"), vuelos = nav.Get("/Vuelos");
            if (inicio.Codigo != 200 || vuelos.Codigo != 200)
                return (false, $"Con datos falsos: GET / {Describir(inicio)} · GET /Vuelos {Describir(vuelos)}");
            return Resultado(!File.Exists(rutaProhibida),
                "Con OrigenDeDatos=Falso se creó o abrió la base de todos modos: revisar si CrearBaseSiNoExiste y ConexionDb quedaron fuera del if.");
        });
        informe.Probar("Con OrigenDeDatos=Sqlite la aplicación sigue usando la base", () =>
        {
            using Servidor s = ctx.Arrancar(new() { ["OrigenDeDatos"] = "Sqlite" });
            using Navegador nav = s.NuevoNavegador();
            Respuesta r = nav.Get("/Vuelos");
            return Resultado(r.Codigo == 200 && Contiene(r, "AT2020") && new BaseDatos(s.RutaBase).Existe, Describir(r));
        });
    }
}

public class Desafio8 : Desafio
{
    public override int Numero => 8;
    public override string Titulo => "Excepciones y logs";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        string program = Proyecto.SinComentarios(p.Leer("Program.cs"));

        informe.Probar("Serilog instalado y configurado", () =>
            Resultado(File.ReadAllText(p.Csproj).Contains("Serilog.AspNetCore") && Regex.IsMatch(program, @"AddSerilog|UseSerilog"),
                "dotnet add AeroTaller package Serilog.AspNetCore + builder.Services.AddSerilog(...) en Program.cs"));
        informe.Probar("Página de error: app.UseExceptionHandler(...)", () =>
            Resultado(program.Contains("UseExceptionHandler"), "app.UseExceptionHandler(\"/Home/Error\");"));
        informe.Probar("Los servicios registran eventos con ILogger<T>", () =>
            Resultado(p.Coincidencias(@"ILogger<\w+Service>", r => r.StartsWith("Servicios/")).Count > 0, "ILogger<ReservaService> debe recibirse por el constructor."));
        informe.Probar("Los logs usan plantillas, no $\"...\"", () =>
        {
            var archivos = p.Coincidencias(@"\.Log(Trace|Debug|Information|Warning|Error|Critical)\(\s*(\w+\s*,\s*)?\$""");
            return Resultado(archivos.Count == 0, "logger.LogInformation(\"Reserva {ReservaId}…\", id) en vez de $\"…\": " + string.Join(", ", archivos));
        });
        informe.Probar("Hay un catch (SqliteException …) when (…) para el conflicto de reservas", () =>
            Resultado(p.Coincidencias(@"catch\s*\(\s*SqliteException\s+\w+\s*\)\s*when").Count > 0, "catch (SqliteException ex) when (ex.SqliteErrorCode == 19)"));
        informe.Probar("Ningún catch vacío", () =>
        {
            var archivos = p.Coincidencias(@"catch(\s*\([^)]*\))?\s*\{\s*\}");
            return Resultado(archivos.Count == 0, "Un catch vacío oculta el error: " + string.Join(", ", archivos));
        });

        string carpeta = ctx.NuevaCarpeta();
        using (Servidor s = Servidor.Arrancar(p, carpeta))
        {
            var db = new BaseDatos(s.RutaBase);
            int at2020 = db.IdVuelo("AT2020"), at2010 = db.IdVuelo("AT2010");

            informe.Probar("Índice único: no puede haber dos reservas confirmadas en el mismo asiento", () =>
            {
                var indices = db.IndicesUnicosParciales("Reserva");
                bool ok = indices.Any(sql => Regex.IsMatch(sql, @"VueloId", RegexOptions.IgnoreCase) && Regex.IsMatch(sql, @"Asiento", RegexOptions.IgnoreCase) && sql.Contains("Confirmada"));
                return Resultado(ok, "Falta el índice UX_Reserva_AsientoConfirmado de aerotaller.sql: el script de la plantilla lo incluye y no debe quitarse.");
            });

            informe.Probar("Una reserva queda registrada en logs/", () =>
            {
                using Navegador nav = Mostrador(s);
                Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at2020, db.IdPasajero(DniDiego), "17D"));
                if (!r.EsRedireccion) return (false, "No fue posible reservar AT2020 17D: " + Describir(r));
                Thread.Sleep(1500);   // Serilog escribe el archivo de forma periódica
                return Resultado(Logs(p, s.Inicio).Any(l => l.Contains("17D") && !l.Contains("HTTP ")),
                    "No se encontró en AeroTaller/logs/*.txt una línea del servicio con el asiento 17D (ej.: \"Reserva {ReservaId} confirmada… asiento {Asiento}\").");
            });

            informe.Probar("8 reservas simultáneas del mismo asiento → queda una sola", () =>
            {
                using Navegador nav = Mostrador(s);
                string token = nav.Get("/Reservas/Crear").Token();
                string[] dnis = [DniAna, DniBruno, "35444555", DniDiego, DniFacundo, DniGabriela, DniHernan, "45678901"];
                var ids = dnis.Select(d => db.Escalar("SELECT Id FROM Pasajero WHERE Dni = @d", ("@d", d))).Where(x => x is not null).Select(Convert.ToInt32).ToList();
                Parallel.ForEach(ids, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                    id => nav.Post("/Reservas/Crear", Reserva(at2010, id, "15C"), token));
                long confirmadas = db.ReservasConfirmadas(at2010, "15C");
                return Resultado(confirmadas == 1, $"Quedaron {confirmadas} reservas confirmadas del 15C en AT2010.");
            });
        }

        informe.Probar("Con la base en solo lectura: página de error (500), sin detalles técnicos, y el error en el log", () =>
        {
            string rutaBase = Path.Combine(carpeta, "aerotaller.db");
            using Servidor s = Servidor.Arrancar(p, carpeta, cadena: $"Data Source={rutaBase};Foreign Keys=True;Mode=ReadOnly");
            var db = new BaseDatos(rutaBase);
            using Navegador nav = Mostrador(s);
            Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(db.IdVuelo("AT3110"), db.IdPasajero(DniHernan), "4A"));
            if (r.Codigo != 500) return (false, $"Se esperaba 500: {Describir(r)}");
            if (Contiene(r, "SqliteException") || r.Texto.Contains("   at ") || Contiene(r, "StackTrace"))
                return (false, "La página de error muestra detalles técnicos (excepción/stack trace): no deben mostrarse al usuario.");
            Thread.Sleep(1500);
            return Resultado(Logs(p, s.Inicio).Any(l => l.Contains("SqliteException") || l.Contains("readonly", StringComparison.OrdinalIgnoreCase)),
                "La página de error es correcta, pero el SqliteException no aparece en AeroTaller/logs/*.txt.");
        });
    }

    private static List<string> Logs(Proyecto p, DateTime desde)
    {
        string carpeta = Path.Combine(p.Carpeta, "logs");
        if (!Directory.Exists(carpeta)) return [];
        return Directory.EnumerateFiles(carpeta, "*.txt", SearchOption.AllDirectories)
            .Where(f => File.GetLastWriteTime(f) >= desde.AddSeconds(-2))
            .SelectMany(f =>
            {
                using var flujo = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var lector = new StreamReader(flujo);
                return lector.ReadToEnd().Split('\n');
            })
            .ToList();
    }
}

public class Desafio9 : Desafio
{
    public override int Numero => 9;
    public override string Titulo => "Autenticación";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        informe.Probar("Filtros RequiereLoginAttribute y RequiereRolAttribute", () =>
        {
            var faltan = new[] { "RequiereLoginAttribute", "RequiereRolAttribute" }.Where(n => !p.DeclaraTipo(n, "class")).ToList();
            return Resultado(faltan.Count == 0, "Falta: " + string.Join(", ", faltan));
        });
        informe.Probar("IAutenticacionService / AutenticacionService", () =>
            Resultado(p.DeclaraTipo("IAutenticacionService", "interface") && p.DeclaraTipo("AutenticacionService", "class"), "En Servicios/"));
        informe.Probar("La sesión se configura (AddSession + UseSession)", () =>
        {
            string program = Proyecto.SinComentarios(p.Leer("Program.cs"));
            return Resultado(program.Contains("AddSession") && program.Contains("UseSession"), "builder.Services.AddSession(...) y app.UseSession() antes del ruteo.");
        });

        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);

        informe.Probar("Tabla Usuario con admin y mostrador", () =>
        {
            if (!db.TieneTabla("Usuario")) return (false, "No existe la tabla Usuario: aerotaller.sql de la plantilla la incluye y no debe quitarse.");
            long cantidad = db.Contar("SELECT COUNT(*) FROM Usuario WHERE NombreUsuario IN ('admin', 'mostrador')");
            return Resultado(cantidad == 2, "Faltan los usuarios admin y mostrador de aerotaller.sql: los datos del script no deben modificarse.");
        });

        using (Navegador anonimo = s.NuevoNavegador())
        {
            informe.Probar("Sin sesión, /Reservas redirige al login (con returnUrl)", () =>
            {
                Respuesta r = anonimo.Get("/Reservas");
                return Resultado(r.EsRedireccion && r.Ubicacion!.Contains("/Login", StringComparison.OrdinalIgnoreCase)
                                 && r.Ubicacion.Contains("returnUrl", StringComparison.OrdinalIgnoreCase), Describir(r));
            });
            informe.Probar("Sin sesión, /Pasajeros también está protegido", () =>
            {
                Respuesta r = anonimo.Get("/Pasajeros");
                return Resultado(r.EsRedireccion && r.Ubicacion!.Contains("/Login", StringComparison.OrdinalIgnoreCase), Describir(r));
            });
            informe.Probar("Sin sesión, los vuelos se pueden ver", () =>
            {
                Respuesta r = anonimo.Get("/Vuelos");
                return Resultado(r.Codigo == 200, Describir(r));
            });
            informe.Probar("Login con clave incorrecta: muestra un error y no inicia sesión", () =>
            {
                Respuesta r = anonimo.IniciarSesion("mostrador", "clave-incorrecta");
                Respuesta despues = anonimo.Get("/Reservas");
                return Resultado(r.Codigo == 200 && (r.TieneResumenDeErrores || r.CantidadErroresDeCampo > 0) && despues.EsRedireccion, Describir(r));
            });
        }

        using (Navegador mostrador = s.NuevoNavegador())
        {
            informe.Probar("Login de mostrador: redirige y accede a /Reservas", () =>
            {
                Respuesta r = mostrador.IniciarSesion("mostrador", "vuelos2026");
                Respuesta reservas = mostrador.Get("/Reservas");
                return Resultado(r.EsRedireccion && reservas.Codigo == 200, $"POST /Login {Describir(r)} · GET /Reservas {Describir(reservas)}");
            });
            informe.Probar("La cookie de sesión es HttpOnly", () =>
            {
                Respuesta r = mostrador.Get("/Reservas");
                var setCookie = mostrador.Cookies.GetAllCookies().FirstOrDefault(c => c.Name.Contains("Session", StringComparison.OrdinalIgnoreCase));
                return Resultado(setCookie?.HttpOnly == true, "options.Cookie.HttpOnly = true en AddSession.");
            });
            informe.Probar("Mostrador no puede crear vuelos (SinPermiso)", () =>
            {
                Respuesta r = mostrador.Get("/Vuelos/Crear");
                return Resultado(r.Codigo is 200 or 403 && Contiene(r, "permiso") && !Regex.IsMatch(r.Html, @"name=""Numero"""),
                    $"Se esperaba la vista SinPermiso. {Describir(r)}");
            });
            informe.Probar("Salir cierra la sesión", () =>
            {
                string token = mostrador.Get("/Reservas").Token();
                if (string.IsNullOrEmpty(token)) token = mostrador.Get("/Reservas/Crear").Token();
                Respuesta salir = mostrador.Post("/Login/Salir", new(), token);
                Respuesta despues = mostrador.Get("/Reservas");
                return Resultado(salir.EsRedireccion && despues.EsRedireccion, $"POST /Login/Salir {Describir(salir)} · después GET /Reservas {Describir(despues)}");
            });
        }

        using (Navegador admin = s.NuevoNavegador())
        {
            informe.Probar("Admin accede a /Vuelos/Crear", () =>
            {
                admin.IniciarSesion("admin", "aero2026");
                Respuesta r = admin.Get("/Vuelos/Crear");
                return Resultado(r.Codigo == 200 && Regex.IsMatch(r.Html, @"name=""Numero"""), Describir(r));
            });
        }

        informe.Probar("returnUrl: después del login redirige a la página solicitada", () =>
        {
            using Navegador nav = s.NuevoNavegador();
            Respuesta login = nav.Get("/Login?returnUrl=%2FPasajeros");
            Respuesta r = nav.Post("/Login", new() { ["NombreUsuario"] = "mostrador", ["Clave"] = "vuelos2026", ["ReturnUrl"] = "/Pasajeros" }, login.Token());
            return Resultado(r.EsRedireccion && r.Ubicacion!.EndsWith("/Pasajeros", StringComparison.OrdinalIgnoreCase), Describir(r));
        });
    }
}

public class Desafio10 : Desafio
{
    public override int Numero => 10;
    public override string Titulo => "Simulacro: equipaje";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);

        informe.Probar("Tabla Equipaje (Id, ReservaId, Tipo, PesoKg) con FK a Reserva", () =>
        {
            if (!db.TieneTabla("Equipaje")) return (false, "No existe la tabla Equipaje (borrar aerotaller.db para recrearla).");
            var faltan = new[] { "Id", "ReservaId", "Tipo", "PesoKg" }.Except(db.Columnas("Equipaje"), StringComparer.OrdinalIgnoreCase).ToList();
            bool fk = db.ClavesForaneas("Equipaje").Any(f => f.Desde == "ReservaId" && f.Tabla == "Reserva");
            return Resultado(faltan.Count == 0 && fk, $"Faltan columnas: {string.Join(", ", faltan)} · FK a Reserva: {(fk ? "sí" : "no")}");
        });
        if (!db.TieneTabla("Equipaje")) return;

        int at1001 = db.IdVuelo("AT1001");
        // La reserva de Ana en el AT1001 (3A): según datos-equipaje.sql ya tiene 18.5 kg en bodega y 7 kg de mano
        int reserva = Convert.ToInt32(db.Escalar("SELECT Id FROM Reserva WHERE VueloId = @v AND Asiento = '3A'", ("@v", at1001)));
        using Navegador nav = Mostrador(s, $"/Equipaje/Crear?reservaId={reserva}");
        string pagina = $"/Equipaje/Crear?reservaId={reserva}";
        string bodega = "";

        informe.Probar("GET /Equipaje/Crear?reservaId= muestra el formulario (Tipo y PesoKg)", () =>
        {
            Respuesta r = nav.Get(pagina);
            if (r.Codigo != 200) return (false, Describir(r));
            Match opcion = Regex.Match(r.Html, @"<option[^>]*value=""([^""]+)""[^>]*>\s*Bodega\s*<", RegexOptions.IgnoreCase);
            bodega = opcion.Success ? opcion.Groups[1].Value : "Bodega";
            bool campos = Regex.IsMatch(r.Html, @"name=""Tipo""") && Regex.IsMatch(r.Html, @"name=""PesoKg""");
            return Resultado(campos, "Faltan los campos Tipo y/o PesoKg.");
        });

        long Piezas() => db.Contar("SELECT COUNT(*) FROM Equipaje WHERE ReservaId = @r", ("@r", reserva));
        Respuesta Agregar(string peso) => nav.EnviarFormulario(pagina, "/Equipaje/Crear",
            new() { ["ReservaId"] = reserva.ToString(), ["Tipo"] = bodega, ["PesoKg"] = peso });

        informe.Probar("Bodega con 25 kg: error en PesoKg", () =>
        {
            long antes = Piezas();
            Respuesta r = Agregar("25");
            return Resultado(r.Codigo == 200 && r.TieneErrorEn("PesoKg") && Piezas() == antes, Describir(r));
        });
        informe.Probar("Bodega con 20 kg: se agrega y redirige", () =>
        {
            long antes = Piezas();
            Respuesta r = Agregar("20");
            return Resultado(r.EsRedireccion && Piezas() == antes + 1, Describir(r));
        });
        informe.Probar("Tercera pieza de bodega: error", () =>
        {
            long antes = Piezas();
            Respuesta r = Agregar("5");
            return Resultado(r.Codigo == 200 && (r.CantidadErroresDeCampo > 0 || r.TieneResumenDeErrores) && Piezas() == antes, Describir(r));
        });
        informe.Probar("El detalle de la reserva muestra el equipaje y el total (45,5 kg)", () =>
        {
            Respuesta r = nav.Get($"/Reservas/Detalle/{reserva}");
            return Resultado(r.Codigo == 200 && (Contiene(r, "45,5") || Contiene(r, "45.5")), $"18,5 + 7 + 20 = 45,5 kg. {Describir(r)}");
        });
    }
}

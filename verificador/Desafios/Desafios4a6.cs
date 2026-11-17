using System.Text.RegularExpressions;

public class Desafio4 : Desafio
{
    public override int Numero => 4;
    public override string Titulo => "ViewModels";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        informe.Probar("Carpeta ViewModels/ con al menos 3 ViewModels", () =>
        {
            int cantidad = p.Cs.Where(f => f.Ruta.StartsWith("ViewModels/"))
                .Sum(f => Regex.Matches(f.Texto, @"\b(class|record)\s+\w+ViewModel\b").Count);
            return Resultado(cantidad >= 3, $"Se encontraron {cantidad} clases …ViewModel en ViewModels/.", $"{cantidad} ViewModels");
        });
        informe.Probar("Existen VueloDetalleViewModel y ReservaCrearViewModel", () =>
        {
            var faltan = new[] { "VueloDetalleViewModel", "ReservaCrearViewModel" }.Where(n => !p.DeclaraTipo(n)).ToList();
            return Resultado(faltan.Count == 0, "Falta: " + string.Join(", ", faltan));
        });
        informe.Probar("Las vistas de detalle de vuelo y de nueva reserva reciben un ViewModel", () =>
        {
            var mal = new List<string>();
            if (!Regex.IsMatch(p.Leer("Views/Vuelos/Detalle.cshtml"), @"@model\s+\w*ViewModel")) mal.Add("Views/Vuelos/Detalle.cshtml");
            if (!Regex.IsMatch(p.Leer("Views/Reservas/Crear.cshtml"), @"@model\s+ReservaCrearViewModel")) mal.Add("Views/Reservas/Crear.cshtml");
            return Resultado(mal.Count == 0, "Revisar el @model de: " + string.Join(", ", mal));
        });

        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);
        int at1001 = db.IdVuelo("AT1001");
        using Navegador anonimo = s.NuevoNavegador();

        informe.Probar("Mapa de asientos: un link por cada asiento libre", () =>
        {
            Respuesta r = anonimo.Get($"/Vuelos/Detalle/{at1001}");
            if (r.Codigo != 200) return (false, Describir(r));

            var libres = Regex.Matches(r.Texto, @"href=""[^""]*/Reservas/Crear\?([^""]+)""", RegexOptions.IgnoreCase)
                .Select(m => Parametros(m.Groups[1].Value))
                .Where(q => q.GetValueOrDefault("vueloid") == at1001.ToString())
                .Select(q => q.GetValueOrDefault("asiento", "").ToUpperInvariant())
                .ToHashSet();

            // AT1001: 20 filas × 6 = 120 asientos; ocupados 3A, 3B y 10F (1C está cancelada: está libre)
            var faltan = new[] { "1A", "1C", "4C", "20F" }.Where(a => !libres.Contains(a)).ToList();
            var sobran = new[] { "3A", "3B", "10F" }.Where(libres.Contains).ToList();
            if (faltan.Count > 0) return (false, $"Sin link a /Reservas/Crear?vueloId={at1001}&asiento=… para los asientos libres: {string.Join(", ", faltan)}");
            if (sobran.Count > 0) return (false, $"Los asientos ocupados no deben ser links: {string.Join(", ", sobran)}");
            return Resultado(libres.Count == 117, $"Se esperaban 117 asientos libres en AT1001 (120 − 3 ocupados); hay {libres.Count} enlaces.", "117 libres");
        });
        informe.Probar("Mapa de asientos: los ocupados llevan la clase ocupado", () =>
        {
            Respuesta r = anonimo.Get($"/Vuelos/Detalle/{at1001}");
            int ocupados = Regex.Matches(r.Html, @"class=""[^""]*\bocupado\b").Count;
            return Resultado(ocupados == 3, $"Se esperaban 3 asientos con class=\"… ocupado\" (3A, 3B, 10F); hay {ocupados}.");
        });

        using Navegador nav = Mostrador(s);
        informe.Probar("GET /Reservas lista las reservas (vuelo, pasajero y asiento)", () =>
        {
            Respuesta r = nav.Get("/Reservas");
            return Resultado(r.Codigo == 200 && Contiene(r, "AT1001") && Contiene(r, "Ana Gómez") && Contiene(r, "10F"), Describir(r));
        });
        informe.Probar("GET /Reservas/Crear tiene VueloId, PasajeroId y Asiento, y precarga el query string", () =>
        {
            Respuesta r = nav.Get($"/Reservas/Crear?vueloId={at1001}&asiento=4C");
            if (r.Codigo != 200) return (false, Describir(r));
            var faltan = new List<string>();
            if (!Regex.IsMatch(r.Html, @"<select[^>]*name=""VueloId""")) faltan.Add("select VueloId");
            if (!Regex.IsMatch(r.Html, @"<select[^>]*name=""PasajeroId""")) faltan.Add("select PasajeroId");
            if (!Regex.IsMatch(r.Html, @"<input[^>]*name=""Asiento""")) faltan.Add("input Asiento");
            if (faltan.Count > 0) return (false, "Falta en el formulario: " + string.Join(", ", faltan));

            bool vueloElegido = Regex.IsMatch(r.Html, $@"<option[^>]*selected=""selected""[^>]*value=""{at1001}""|<option[^>]*value=""{at1001}""[^>]*selected");
            bool asiento = Regex.IsMatch(r.Html, @"<input[^>]*name=""Asiento""[^>]*value=""4C""|<input[^>]*value=""4C""[^>]*name=""Asiento""");
            return Resultado(vueloElegido && asiento, "Con ?vueloId=…&asiento=4C el vuelo debe aparecer seleccionado y el asiento completado (desde el mapa).");
        });
        informe.Probar("POST /Reservas/Crear guarda la reserva y redirige", () =>
        {
            int elena = db.IdPasajero(DniElena);
            Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at1001, elena, "4C"));
            bool guardada = db.ReservasConfirmadas(at1001, "4C") == 1;
            return Resultado(r.EsRedireccion && guardada, $"Reserva de AT1001 4C para Elena Paz: {Describir(r)}; en la base {(guardada ? "está" : "no está")}.");
        });
    }

    private static Dictionary<string, string> Parametros(string query) =>
        query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(par => par.Split('=', 2))
            .Where(par => par.Length == 2)
            .GroupBy(par => par[0].ToLowerInvariant())
            .ToDictionary(g => g.Key, g => Uri.UnescapeDataString(g.First()[1]));
}

public class Desafio5 : Desafio
{
    public override int Numero => 5;
    public override string Titulo => "Validación";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        informe.Probar("Existe el atributo propio FechaFuturaAttribute", () =>
            Resultado(p.DeclaraTipo("FechaFuturaAttribute", "class") && p.Coincidencias(@"FechaFuturaAttribute\s*:\s*ValidationAttribute").Count > 0,
                "class FechaFuturaAttribute : ValidationAttribute (en Validaciones/)"));

        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);
        using Navegador nav = Mostrador(s);
        int at1001 = db.IdVuelo("AT1001");
        int elena = db.IdPasajero(DniElena);

        void Rechaza(string nombre, Dictionary<string, string> campos, string campoError, Func<long> contar)
        {
            informe.Probar(nombre, () =>
            {
                long antes = contar();
                Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", campos);
                if (r.EsRedireccion) return (false, $"Se aceptó (redirigió a {r.Ubicacion}).");
                if (contar() != antes) return (false, "Se mostró la vista, pero la reserva se guardó de todos modos en la base.");
                return Resultado(r.Codigo == 200 && r.TieneErrorEn(campoError),
                    $"Se esperaba la vista con el error junto al campo {campoError} (asp-validation-for). {Describir(r)}");
            });
        }

        informe.Probar("Reserva vacía: errores en VueloId, PasajeroId y Asiento", () =>
        {
            Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", new() { ["VueloId"] = "", ["PasajeroId"] = "", ["Asiento"] = "" });
            var faltan = new[] { "VueloId", "PasajeroId", "Asiento" }.Where(c => !r.TieneErrorEn(c)).ToList();
            return Resultado(r.Codigo == 200 && faltan.Count == 0, $"Sin error en: {string.Join(", ", faltan)}. {Describir(r)}");
        });
        Rechaza("Asiento con formato inválido (ZZ)", Reserva(at1001, elena, "ZZ"), "Asiento", db.TotalReservas);
        Rechaza("Asiento que no existe en el avión (30A en un avión de 20 filas)", Reserva(at1001, elena, "30A"), "Asiento", db.TotalReservas);
        Rechaza("Asiento ocupado (3A del AT1001)", Reserva(at1001, elena, "3A"), "Asiento", db.TotalReservas);

        informe.Probar("Reserva válida: redirige y queda en la base", () =>
        {
            Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at1001, elena, "5b"));
            bool guardada = db.ReservasConfirmadas(at1001, "5B") == 1;
            return Resultado(r.EsRedireccion && guardada,
                $"AT1001 5b para Elena Paz: {Describir(r)}; en la base {(guardada ? "está" : "no está")} como '5B' (el asiento debe convertirse a mayúsculas).");
        });
        informe.Probar("Un POST sin token antiforgery se rechaza", () =>
        {
            long antes = db.TotalReservas();
            Respuesta r = nav.Post("/Reservas/Crear", Reserva(at1001, db.IdPasajero(DniDiego), "6A"), token: null);
            return Resultado(r.Codigo == 400 && db.TotalReservas() == antes, $"Falta [ValidateAntiForgeryToken]: {Describir(r)}");
        });

        using Navegador pasajeros = Mostrador(s, "/Pasajeros/Crear");
        Respuesta CrearPasajero(string nombre, string dni) =>
            pasajeros.EnviarFormulario("/Pasajeros/Crear", "/Pasajeros/Crear", new() { ["Nombre"] = nombre, ["Dni"] = dni, ["Email"] = "" });

        informe.Probar("Pasajero con DNI inválido (12): error en Dni", () =>
        {
            Respuesta r = CrearPasajero("Prueba Verificador", "12");
            return Resultado(r.Codigo == 200 && r.TieneErrorEn("Dni"), Describir(r));
        });
        informe.Probar("Pasajero con DNI repetido: error en Dni", () =>
        {
            Respuesta r = CrearPasajero("Otra Ana", DniAna);
            return Resultado(r.Codigo == 200 && r.TieneErrorEn("Dni") && db.Contar("SELECT COUNT(*) FROM Pasajero WHERE Dni = @d", ("@d", DniAna)) == 1, Describir(r));
        });
        informe.Probar("Pasajero válido: redirige y queda en la base", () =>
        {
            Respuesta r = CrearPasajero("Ignacio Verificador", "45678901");
            return Resultado(r.EsRedireccion && db.Contar("SELECT COUNT(*) FROM Pasajero WHERE Dni = '45678901'") == 1, Describir(r));
        });

        using Navegador admin = Admin(s);
        Dictionary<string, string> Vuelo(string numero, string salida) => new()
        {
            ["Numero"] = numero,
            ["OrigenId"] = db.Escalar("SELECT Id FROM Aeropuerto WHERE Codigo = 'TUC'")!.ToString()!,
            ["DestinoId"] = db.Escalar("SELECT Id FROM Aeropuerto WHERE Codigo = 'MDZ'")!.ToString()!,
            ["Salida"] = salida,
            ["DuracionMinutos"] = "95",
            ["Filas"] = "10",
            ["Precio"] = "80000"
        };
        informe.Probar("Vuelo con salida en el pasado: error en Salida ([FechaFutura])", () =>
        {
            Respuesta r = admin.EnviarFormulario("/Vuelos/Crear", "/Vuelos/Crear", Vuelo("AT8801", "2020-03-01T10:00"));
            return Resultado(r.Codigo == 200 && r.TieneErrorEn("Salida") && db.Contar("SELECT COUNT(*) FROM Vuelo WHERE Numero = 'AT8801'") == 0, Describir(r));
        });
        informe.Probar("Vuelo válido: redirige y queda en la base", () =>
        {
            string salida = DateTime.Today.AddDays(30).AddHours(9).ToString("yyyy-MM-ddTHH:mm");
            Respuesta r = admin.EnviarFormulario("/Vuelos/Crear", "/Vuelos/Crear", Vuelo("AT8802", salida));
            return Resultado(r.EsRedireccion && db.Contar("SELECT COUNT(*) FROM Vuelo WHERE Numero = 'AT8802'") == 1, Describir(r));
        });
    }
}

public class Desafio6 : Desafio
{
    public override int Numero => 6;
    public override string Titulo => "Capa de servicios";

    public override void Verificar(Contexto ctx, Informe informe)
    {
        var p = ctx.Proyecto;
        informe.Probar("IReservaService y ReservaService (que la implementa)", () =>
            Resultado(p.DeclaraTipo("IReservaService", "interface") && p.Coincidencias(@"class\s+ReservaService\b[^{]*\bIReservaService\b").Count > 0,
                "interface IReservaService + class ReservaService(…) : IReservaService (en Servicios/)"));
        informe.Probar("ReglaDeNegocioException hereda de Exception", () =>
            Resultado(p.Coincidencias(@"class\s+ReglaDeNegocioException\b[^{]*:\s*Exception\b").Count > 0, "class ReglaDeNegocioException(…) : Exception(…)"));
        informe.Probar("ReservasController usa el servicio y no contiene las reglas", () =>
        {
            var controlador = p.DondeSeDeclara("ReservasController");
            if (controlador is null) return (false, "No se encontró ReservasController.");
            string codigo = Proyecto.SinComentarios(controlador.Value.Texto);
            if (!codigo.Contains("IReservaService")) return (false, "El controlador debe recibir IReservaService.");
            var reglas = new[] { "AsientosOcupados(", "ContarConfirmadas(", "PasajeroTieneReserva(", "TieneAsiento(" }.Where(codigo.Contains).ToList();
            return Resultado(reglas.Count == 0, "Estas reglas siguen en el controlador: " + string.Join(", ", reglas) + " → trasladarlas a ReservaService.");
        });

        using Servidor s = ctx.Arrancar();
        var db = new BaseDatos(s.RutaBase);
        using Navegador nav = Mostrador(s);
        int at1001 = db.IdVuelo("AT1001"), at0999 = db.IdVuelo("AT0999"), at0500 = db.IdVuelo("AT0500");

        informe.Probar("No se reserva un vuelo que ya salió (AT0999): error en VueloId", () =>
        {
            long antes = db.TotalReservas();
            Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at0999, db.IdPasajero(DniHernan), "8A"));
            return Resultado(r.Codigo == 200 && r.TieneErrorEn("VueloId") && db.TotalReservas() == antes, Describir(r));
        });
        informe.Probar("Un pasajero no reserva dos veces el mismo vuelo: error en PasajeroId", () =>
        {
            long antes = db.TotalReservas();
            Respuesta r = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at1001, db.IdPasajero(DniAna), "9A"));
            return Resultado(r.Codigo == 200 && r.TieneErrorEn("PasajeroId") && db.TotalReservas() == antes,
                $"Ana Gómez ya viaja en AT1001. {Describir(r)}");
        });
        informe.Probar("Sin overbooking: la avioneta AT0500 (6 asientos) se completa y no admite más reservas", () =>
        {
            Respuesta ultima = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at0500, db.IdPasajero(DniGabriela), "1F"));
            if (!ultima.EsRedireccion) return (false, $"El último asiento libre (1F) debe poder reservarse: {Describir(ultima)}");
            Respuesta otra = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at0500, db.IdPasajero(DniHernan), "1A"));
            long confirmadas = db.Contar("SELECT COUNT(*) FROM Reserva WHERE VueloId = @v AND Estado = 'Confirmada'", ("@v", at0500));
            return Resultado(!otra.EsRedireccion && confirmadas == 6, $"Con el vuelo lleno: {Describir(otra)}; confirmadas = {confirmadas}.");
        });
        informe.Probar("Cancelar una reserva cambia su estado (no la borra) y libera el asiento", () =>
        {
            int idBruno = Convert.ToInt32(db.Escalar("SELECT Id FROM Reserva WHERE VueloId = @v AND Asiento = '3B'", ("@v", at1001)));
            Respuesta formulario = nav.Get("/Reservas/Crear");
            Respuesta r = nav.Post($"/Reservas/Cancelar/{idBruno}", new(), formulario.Token());
            string? estado = db.Escalar("SELECT Estado FROM Reserva WHERE Id = @id", ("@id", idBruno))?.ToString();
            if (!r.EsRedireccion || estado != "Cancelada")
                return (false, $"POST /Reservas/Cancelar/{idBruno}: {Describir(r)}; Estado en la base: {estado ?? "(borrada)"}");
            Respuesta nueva = nav.EnviarFormulario("/Reservas/Crear", "/Reservas/Crear", Reserva(at1001, db.IdPasajero(DniFacundo), "3B"));
            return Resultado(nueva.EsRedireccion, $"Después de cancelar, el 3B debe quedar libre: {Describir(nueva)}");
        });
    }
}

// Las insignias se definen en Contenido/insignias.json; aquí se evalúa cada TIPO de regla.
// Para agregar una insignia nueva con un tipo que ya existe, basta con editar el JSON.
public class InsigniaService(
    IContenidoRepository contenido,
    IInsigniaRepository repositorio,
    IProgresoRepository progreso,
    IIntentoQuizRepository intentos,
    IBitacoraRepository bitacora,
    ILogger<InsigniaService> logger) : IInsigniaService
{
    public List<InsigniaVista> Listar(int alumnoId)
    {
        Dictionary<string, DateTime> obtenidas = repositorio.Listar(alumnoId).ToDictionary(i => i.InsigniaId, i => i.Fecha);
        return contenido.ListarInsignias()
            .Select(i => new InsigniaVista(i.Id, i.Nombre, i.Icono, i.Descripcion,
                obtenidas.ContainsKey(i.Id), obtenidas.TryGetValue(i.Id, out DateTime f) ? f : null))
            .ToList();
    }

    // Revisa todas las insignias que el alumno todavía no tiene y otorga las que ya cumple.
    // Devuelve las NUEVAS (para anunciarlas en pantalla).
    public List<Insignia> Evaluar(int alumnoId)
    {
        HashSet<string> obtenidas = repositorio.Listar(alumnoId).Select(i => i.InsigniaId).ToHashSet();
        var datos = new DatosAlumno(alumnoId, contenido, progreso, intentos, bitacora);
        var nuevas = new List<Insignia>();

        foreach (Insignia insignia in contenido.ListarInsignias().Where(i => !obtenidas.Contains(i.Id)))
        {
            if (Cumple(insignia.Regla, datos))
            {
                repositorio.Otorgar(alumnoId, insignia.Id);
                nuevas.Add(insignia);
                logger.LogInformation("Alumno {AlumnoId} obtuvo la insignia {Insignia}", alumnoId, insignia.Id);
            }
        }
        return nuevas;
    }

    private static bool Cumple(ReglaInsignia regla, DatosAlumno datos) => regla.Tipo switch
    {
        "desafio" => regla.DesafioId is not null && datos.Completados.Contains(regla.DesafioId),
        "quizPrimerIntento" => datos.QuizzesAlPrimerIntento >= regla.Cantidad,
        "depurador" => datos.PreguntasDepuradorAlPrimerIntento >= regla.Cantidad,
        "checkpoints" => datos.CheckpointsCompletos >= regla.Cantidad,
        "lecturas" => datos.Lecturas >= regla.Cantidad,
        "repoAlDia" => datos.DesafiosConRepoAlDia >= regla.Cantidad,
        "verificados" => datos.DesafiosVerificados >= regla.Cantidad,
        "dudas" => datos.Dudas >= regla.Cantidad,
        "perseverancia" => datos.MaximoIntentosHastaAprobar >= regla.Cantidad,
        "simulacroATiempo" => datos.SimulacroATiempo,
        "todos" => datos.Desafios.Count > 0 && datos.Desafios.All(d => datos.Completados.Contains(d.Id)),
        _ => false
    };

    // Reúne, de forma diferida y solo cuando se necesitan, los datos que evalúan las reglas
    private class DatosAlumno(int alumnoId, IContenidoRepository contenido, IProgresoRepository progreso,
        IIntentoQuizRepository intentos, IBitacoraRepository bitacora)
    {
        private readonly Lazy<List<Desafio>> desafios = new(contenido.ListarDesafios);
        private readonly Lazy<Dictionary<string, EstadoDesafio>> estados = new(() => progreso.Estados(alumnoId));
        private readonly Lazy<List<IntentoQuiz>> intentosQuiz = new(() => intentos.Listar(alumnoId));

        private HashSet<string>? completados;

        public List<Desafio> Desafios => desafios.Value;

        public HashSet<string> Completados => completados ??=
            estados.Value.Values.Where(e => e.Completado is not null).Select(e => e.DesafioId).ToHashSet();

        // Primer intento de cada cuestionario
        private IEnumerable<IntentoQuiz> PrimerosIntentos =>
            intentosQuiz.Value.GroupBy(i => i.DesafioId).Select(g => g.OrderBy(i => i.Id).First());

        public int QuizzesAlPrimerIntento => PrimerosIntentos.Count(i => i.Aprobado);

        public int PreguntasDepuradorAlPrimerIntento
        {
            get
            {
                var deDepurador = Desafios
                    .SelectMany(d => d.Quiz.Where(p => p.Etiquetas.Contains("depurador")).Select(p => (d.Id, p.Id)))
                    .ToHashSet();
                return PrimerosIntentos
                    .SelectMany(i => i.Respuestas.Where(r => r.Correcta).Select(r => (i.DesafioId, r.PreguntaId)))
                    .Count(deDepurador.Contains);
            }
        }

        public int CheckpointsCompletos =>
            Desafios.Count(d => d.Tipo == TiposDesafio.Checkpoint && Completados.Contains(d.Id));

        public int Lecturas => progreso.ContarLecturas(alumnoId);

        public int DesafiosConRepoAlDia => progreso.ContarDesafiosConRepoAlDia(alumnoId);

        public int DesafiosVerificados => progreso.ContarDesafiosVerificados(alumnoId);

        public int Dudas => bitacora.Listar(alumnoId).Count(n => n.Tipo == TiposNota.Duda);

        // Mayor cantidad de intentos necesarios para aprobar un cuestionario (solo se consideran los aprobados)
        public int MaximoIntentosHastaAprobar =>
            intentosQuiz.Value.GroupBy(i => i.DesafioId)
                .Where(g => g.Any(i => i.Aprobado))
                .Select(g => g.OrderBy(i => i.Id).TakeWhile(i => !i.Aprobado).Count() + 1)
                .DefaultIfEmpty(0)
                .Max();

        public bool SimulacroATiempo
        {
            get
            {
                Desafio? simulacro = Desafios.FirstOrDefault(d => d.Tipo == TiposDesafio.Simulacro);
                if (simulacro?.MinutosLimite is null) return false;
                if (!estados.Value.TryGetValue(simulacro.Id, out EstadoDesafio? estado) || estado.Completado is null) return false;
                return (estado.Completado.Value - estado.Iniciado).TotalMinutes <= simulacro.MinutosLimite;
            }
        }
    }
}

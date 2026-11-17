// Las reglas del avance:
//  · un desafío se desbloquea cuando el anterior está completo (salvo en modo libre),
//  · se completa con todas las tareas marcadas y el cuestionario aprobado,
//  · un checkpoint se completa registrando lo realizado y lo pendiente.
public class ProgresoService(
    IContenidoRepository contenido,
    IAlumnoRepository alumnos,
    IProgresoRepository progreso,
    IIntentoQuizRepository intentos,
    IBitacoraRepository bitacora,
    IInsigniaService insignias,
    ILogger<ProgresoService> logger) : IProgresoService
{
    public const string Bloqueado = "bloqueado";
    public const string Disponible = "disponible";
    public const string Completado = "completado";

    public Alumno ObtenerAlumno(int alumnoId) =>
        alumnos.Obtener(alumnoId) ?? throw new KeyNotFoundException($"No existe el alumno {alumnoId}");

    public List<EtapaMapa> Mapa(int alumnoId)
    {
        Alumno alumno = ObtenerAlumno(alumnoId);
        List<Desafio> desafios = contenido.ListarDesafios();
        Dictionary<string, EstadoDesafio> estados = progreso.Estados(alumnoId);
        Dictionary<string, int> tareas = progreso.ContarTareasPorDesafio(alumnoId);
        HashSet<string> aprobados = intentos.Listar(alumnoId).Where(i => i.Aprobado).Select(i => i.DesafioId).ToHashSet();
        Dictionary<string, UltimaVerificacion> verificaciones = progreso.UltimasVerificaciones(alumnoId);

        return desafios.Select(d => new EtapaMapa(
            d.Id, d.Orden, d.Codigo, d.Numero, d.Titulo, d.Subtitulo, d.Tipo, d.MinutosEstimados, d.Temas,
            Estado(alumno, desafios, estados, d.Id),
            Math.Min(tareas.GetValueOrDefault(d.Id), d.Tareas.Count), d.Tareas.Count,
            aprobados.Contains(d.Id), d.Quiz.Count > 0, d.Insignia,
            verificaciones.TryGetValue(d.Id, out UltimaVerificacion? v) ? v.Aprobado : null)).ToList();
    }

    public ResumenAlumno Resumen(int alumnoId)
    {
        Alumno alumno = ObtenerAlumno(alumnoId);
        List<EtapaMapa> mapa = Mapa(alumnoId);
        int obtenidas = insignias.Listar(alumnoId).Count(i => i.Obtenida);
        int totalInsignias = contenido.ListarInsignias().Count;
        string? actual = mapa.FirstOrDefault(e => e.Estado == Disponible)?.Id;
        return new ResumenAlumno(alumno, mapa.Count(e => e.Estado == Completado), mapa.Count, obtenidas, totalInsignias, actual);
    }

    public DesafioVista Detalle(int alumnoId, string desafioId)
    {
        Alumno alumno = ObtenerAlumno(alumnoId);
        List<Desafio> desafios = contenido.ListarDesafios();
        Desafio desafio = desafios.FirstOrDefault(d => d.Id == desafioId)
            ?? throw new KeyNotFoundException($"No existe el desafío {desafioId}");

        Dictionary<string, EstadoDesafio> estados = progreso.Estados(alumnoId);
        string estado = Estado(alumno, desafios, estados, desafioId);
        if (estado == Bloqueado)
            throw new SolicitudInvalidaException("Este desafío está bloqueado: primero debe completarse el anterior.");

        progreso.Iniciar(alumnoId, desafioId);
        estados = progreso.Estados(alumnoId);

        List<IntentoQuiz> intentosDesafio = intentos.Listar(alumnoId, desafioId);
        IntentoQuiz? ultimo = intentosDesafio.LastOrDefault();
        int indice = desafios.IndexOf(desafio);

        var vista = new ProgresoDesafio(
            estado,
            estados[desafioId].Iniciado,
            estados[desafioId].Completado,
            progreso.TareasMarcadas(alumnoId, desafioId),
            progreso.LecturasAbiertas(alumnoId, desafioId),
            intentosDesafio.Any(i => i.Aprobado),
            intentosDesafio.Count,
            ultimo?.Respuestas,
            progreso.UltimasVerificaciones(alumnoId).GetValueOrDefault(desafioId));

        return new DesafioVista(
            desafio.Id, desafio.Orden, desafio.Codigo, desafio.Titulo, desafio.Subtitulo, desafio.Tipo,
            desafio.MinutosEstimados, desafio.MinutosLimite, desafio.Temas, desafio.Insignia, desafio.Markdown,
            desafio.Tareas,
            // La respuesta correcta, la pista y la explicación NO se envían al navegador
            desafio.Quiz.Select(p => new PreguntaPublica(p.Id, p.Texto, p.Codigo, p.Opciones, p.Etiquetas)).ToList(),
            desafio.Lecturas, desafio.Commit, desafio.Numero, vista,
            indice > 0 ? desafios[indice - 1].Id : null,
            indice < desafios.Count - 1 ? desafios[indice + 1].Id : null,
            desafio.Herramientas);
    }

    public void MarcarTarea(int alumnoId, string desafioId, string tareaId, bool marcada)
    {
        Desafio desafio = DesafioDisponible(alumnoId, desafioId);
        if (desafio.Tareas.All(t => t.Id != tareaId))
            throw new KeyNotFoundException($"El desafío {desafioId} no tiene la tarea {tareaId}");

        progreso.MarcarTarea(alumnoId, desafioId, tareaId, marcada);
    }

    public ResultadoAccion Completar(int alumnoId, string desafioId)
    {
        Desafio desafio = DesafioDisponible(alumnoId, desafioId);
        if (desafio.Tipo == TiposDesafio.Checkpoint)
            throw new SolicitudInvalidaException("Los checkpoints se completan con el formulario de bitácora.");

        var faltantes = new List<string>();
        List<string> marcadas = progreso.TareasMarcadas(alumnoId, desafioId);
        foreach (Tarea tarea in desafio.Tareas.Where(t => !marcadas.Contains(t.Id)))
            faltantes.Add($"Tarea sin marcar: {tarea.Texto}");

        if (desafio.Quiz.Count > 0 && !intentos.Listar(alumnoId, desafioId).Any(i => i.Aprobado))
            faltantes.Add("Cuestionario sin aprobar.");

        if (faltantes.Count > 0)
            throw new SolicitudInvalidaException("No es posible completar la escala: quedan requisitos pendientes.", faltantes);

        progreso.Completar(alumnoId, desafioId);
        logger.LogInformation("Alumno {AlumnoId} completó {DesafioId}", alumnoId, desafioId);
        return new ResultadoAccion(true, $"{desafio.Titulo}: completado.", insignias.Evaluar(alumnoId));
    }

    public ResultadoAccion CompletarCheckpoint(int alumnoId, string desafioId, CheckpointEntrada entrada)
    {
        Desafio desafio = DesafioDisponible(alumnoId, desafioId);
        if (desafio.Tipo != TiposDesafio.Checkpoint)
            throw new SolicitudInvalidaException("El desafío indicado no es un checkpoint.");

        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(entrada.Hice)) faltantes.Add("Falta describir el trabajo realizado.");
        if (string.IsNullOrWhiteSpace(entrada.Falta)) faltantes.Add("Falta describir lo pendiente o los temas para repasar.");
        if (faltantes.Count > 0)
            throw new SolicitudInvalidaException("La bitácora está incompleta.", faltantes);

        DateTime ahora = DateTime.Now;
        bitacora.Insertar(new NotaBitacora { AlumnoId = alumnoId, DesafioId = desafioId, Tipo = TiposNota.Hice, Texto = entrada.Hice.Trim(), Fecha = ahora });
        bitacora.Insertar(new NotaBitacora { AlumnoId = alumnoId, DesafioId = desafioId, Tipo = TiposNota.Falta, Texto = entrada.Falta.Trim(), Fecha = ahora });
        if (!string.IsNullOrWhiteSpace(entrada.Dudas))
            bitacora.Insertar(new NotaBitacora { AlumnoId = alumnoId, DesafioId = desafioId, Tipo = TiposNota.Duda, Texto = entrada.Dudas.Trim(), Fecha = ahora });

        progreso.Completar(alumnoId, desafioId);
        return new ResultadoAccion(true, "Bitácora guardada.", insignias.Evaluar(alumnoId));
    }

    public ResultadoAccion AbrirLectura(int alumnoId, string desafioId, string lecturaId)
    {
        Desafio desafio = DesafioDisponible(alumnoId, desafioId);
        if (desafio.Lecturas.All(l => l.Id != lecturaId))
            throw new KeyNotFoundException($"El desafío {desafioId} no tiene la lectura {lecturaId}");

        progreso.AbrirLectura(alumnoId, desafioId, lecturaId);
        return new ResultadoAccion(true, "Lectura registrada.", insignias.Evaluar(alumnoId));
    }

    private Desafio DesafioDisponible(int alumnoId, string desafioId)
    {
        Alumno alumno = ObtenerAlumno(alumnoId);
        List<Desafio> desafios = contenido.ListarDesafios();
        Desafio desafio = desafios.FirstOrDefault(d => d.Id == desafioId)
            ?? throw new KeyNotFoundException($"No existe el desafío {desafioId}");

        if (Estado(alumno, desafios, progreso.Estados(alumnoId), desafioId) == Bloqueado)
            throw new SolicitudInvalidaException("Este desafío está bloqueado.");
        return desafio;
    }

    // Completado si tiene fecha de fin; disponible si es el primero, si el anterior está completo o si está en modo libre
    private static string Estado(Alumno alumno, List<Desafio> desafios, Dictionary<string, EstadoDesafio> estados, string desafioId)
    {
        if (estados.TryGetValue(desafioId, out EstadoDesafio? estado) && estado.Completado is not null)
            return Completado;

        int indice = desafios.FindIndex(d => d.Id == desafioId);
        if (indice <= 0 || alumno.ModoLibre) return Disponible;

        string anterior = desafios[indice - 1].Id;
        bool anteriorCompleto = estados.TryGetValue(anterior, out EstadoDesafio? previo) && previo.Completado is not null;
        return anteriorCompleto ? Disponible : Bloqueado;
    }
}

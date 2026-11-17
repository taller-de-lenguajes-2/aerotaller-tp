// Lo que la API recibe y devuelve (DTOs). Separado del modelo para no mandar al navegador,
// por ejemplo, la respuesta correcta de cada pregunta.

public record AlumnoEntrada(string Nombre, string? RutaProyecto, bool ModoLibre);

public record EtapaMapa(
    string Id, int Orden, string Codigo, int? Numero, string Titulo, string Subtitulo, string Tipo,
    int MinutosEstimados, List<TemaRef> Temas, string Estado,   // "bloqueado" · "disponible" · "completado"
    int TareasHechas, int TareasTotal, bool QuizAprobado, bool TieneQuiz, string? Insignia, bool? Verificado);

public record PreguntaPublica(string Id, string Texto, string? Codigo, List<Opcion> Opciones, List<string> Etiquetas);

public record DesafioVista(
    string Id, int Orden, string Codigo, string Titulo, string Subtitulo, string Tipo,
    int MinutosEstimados, int? MinutosLimite, List<TemaRef> Temas, string? Insignia, string Markdown,
    List<Tarea> Tareas, List<PreguntaPublica> Quiz, List<Lectura> Lecturas, Commit? Commit,
    int? Numero, ProgresoDesafio Progreso, string? Anterior, string? Siguiente, List<string> Herramientas);

public record ProgresoDesafio(
    string Estado, DateTime? Iniciado, DateTime? Completado,
    List<string> TareasMarcadas, List<string> LecturasAbiertas,
    bool QuizAprobado, int IntentosQuiz, List<RespuestaQuiz>? UltimasRespuestas,
    UltimaVerificacion? Verificacion);

public record UltimaVerificacion(bool Aprobado, DateTime Fecha);

public record MarcaTarea(bool Marcada);

public record RespuestasEntrada(Dictionary<string, string> Respuestas);

public record CorreccionPregunta(string PreguntaId, bool Correcta, string Mensaje);

public record ResultadoQuiz(bool Aprobado, int Correctas, int Total, int Intento, List<CorreccionPregunta> Detalle, List<Insignia> NuevasInsignias);

public record CheckpointEntrada(string Hice, string Falta, string? Dudas);

public record ResultadoAccion(bool Ok, string Mensaje, List<Insignia> NuevasInsignias);

public record ResultadoCheck(string Nombre, bool Ok, string Detalle);

public record EstadoRepoGit(bool EsRepo, string? Rama, bool TieneRemoto, int SinCommitear, int SinPushear, List<string> UltimosCommits, string Mensaje);

public record ResultadoVerificacion(bool Ejecutado, bool Aprobado, string? Error, List<ResultadoCheck> Chequeos, EstadoRepoGit? Repo, List<Insignia> NuevasInsignias);

public record InsigniaVista(string Id, string Nombre, string Icono, string Descripcion, bool Obtenida, DateTime? Fecha);

public record NotaEntrada(string Tipo, string Texto, string? DesafioId);

public record ResumenAlumno(Alumno Alumno, int Completados, int Total, int Insignias, int InsigniasTotal, string? Actual);

// La base de AeroTaller (aerotaller.db) en la carpeta del alumno, para el botón "Crear base de datos"
public record EstadoBaseAeroTaller(
    string Carpeta, string Archivo, bool HayScript, bool Existe, long Bytes, DateTime? Modificada,
    List<TablaBase> Tablas, string? Problema);

public record TablaBase(string Nombre, long Filas);

public record ResultadoCrearBase(bool Ok, string Mensaje, EstadoBaseAeroTaller Estado);

using Microsoft.AspNetCore.Mvc;

// Todo lo que el alumno hace dentro de un desafío. El controlador no decide nada: delega en los servicios.
[ApiController]
[Route("api/alumnos/{alumnoId:int}")]
public class ProgresoController(
    IProgresoService progreso,
    IQuizService quiz,
    IVerificadorService verificador,
    IInsigniaService insignias) : ControllerBase
{
    [HttpGet("mapa")]
    public ActionResult<List<EtapaMapa>> Mapa(int alumnoId) => progreso.Mapa(alumnoId);

    [HttpGet("desafios/{desafioId}")]
    public ActionResult<DesafioVista> Desafio(int alumnoId, string desafioId) => progreso.Detalle(alumnoId, desafioId);

    [HttpPut("desafios/{desafioId}/tareas/{tareaId}")]
    public IActionResult MarcarTarea(int alumnoId, string desafioId, string tareaId, MarcaTarea marca)
    {
        progreso.MarcarTarea(alumnoId, desafioId, tareaId, marca.Marcada);
        return NoContent();
    }

    [HttpPost("desafios/{desafioId}/quiz")]
    public ActionResult<ResultadoQuiz> Responder(int alumnoId, string desafioId, RespuestasEntrada entrada) =>
        quiz.Corregir(alumnoId, desafioId, entrada.Respuestas);

    [HttpPost("desafios/{desafioId}/completar")]
    public ActionResult<ResultadoAccion> Completar(int alumnoId, string desafioId) => progreso.Completar(alumnoId, desafioId);

    [HttpPost("desafios/{desafioId}/checkpoint")]
    public ActionResult<ResultadoAccion> Checkpoint(int alumnoId, string desafioId, CheckpointEntrada entrada) =>
        progreso.CompletarCheckpoint(alumnoId, desafioId, entrada);

    [HttpPost("desafios/{desafioId}/lecturas/{lecturaId}")]
    public ActionResult<ResultadoAccion> AbrirLectura(int alumnoId, string desafioId, string lecturaId) =>
        progreso.AbrirLectura(alumnoId, desafioId, lecturaId);

    [HttpPost("desafios/{desafioId}/verificar")]
    public ActionResult<ResultadoVerificacion> Verificar(int alumnoId, string desafioId) =>
        verificador.Verificar(alumnoId, desafioId);

    [HttpGet("repo")]
    public ActionResult<EstadoRepoGit> Repo(int alumnoId) => verificador.EstadoRepo(alumnoId);

    [HttpGet("insignias")]
    public ActionResult<List<InsigniaVista>> Insignias(int alumnoId)
    {
        progreso.ObtenerAlumno(alumnoId);
        return insignias.Listar(alumnoId);
    }
}

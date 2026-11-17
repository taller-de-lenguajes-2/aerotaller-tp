using Microsoft.AspNetCore.Mvc;

// La bitácora: notas personales (trabajo realizado, pendientes, dudas para la consulta)
[ApiController]
[Route("api/alumnos/{alumnoId:int}/bitacora")]
public class BitacoraController(IBitacoraRepository bitacora, IProgresoService progreso, IInsigniaService insignias) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<NotaBitacora>> Listar(int alumnoId)
    {
        progreso.ObtenerAlumno(alumnoId);
        return bitacora.Listar(alumnoId);
    }

    [HttpPost]
    public ActionResult<ResultadoAccion> Agregar(int alumnoId, NotaEntrada entrada)
    {
        progreso.ObtenerAlumno(alumnoId);
        if (string.IsNullOrWhiteSpace(entrada.Texto))
            throw new SolicitudInvalidaException("La nota está vacía.");
        if (!TiposNota.Todos.Contains(entrada.Tipo))
            throw new SolicitudInvalidaException($"Tipo de nota inválido: {entrada.Tipo}");

        bitacora.Insertar(new NotaBitacora
        {
            AlumnoId = alumnoId,
            DesafioId = entrada.DesafioId,
            Tipo = entrada.Tipo,
            Texto = entrada.Texto.Trim(),
            Fecha = DateTime.Now
        });
        return new ResultadoAccion(true, "Nota guardada.", insignias.Evaluar(alumnoId));
    }

    [HttpPut("{id:int}/resuelta")]
    public IActionResult Resolver(int alumnoId, int id, MarcaTarea marca)
    {
        if (!bitacora.CambiarResuelta(alumnoId, id, marca.Marcada)) throw new KeyNotFoundException($"No existe la nota {id}");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Eliminar(int alumnoId, int id)
    {
        if (!bitacora.Eliminar(alumnoId, id)) throw new KeyNotFoundException($"No existe la nota {id}");
        return NoContent();
    }
}

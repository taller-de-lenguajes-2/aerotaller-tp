using Microsoft.AspNetCore.Mvc;

// Las "tarjetas de embarque": perfiles de quienes usan el tutorial en esta computadora.
[ApiController]
[Route("api/alumnos")]
public class AlumnosController(IAlumnoRepository alumnos, IProgresoService progreso) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<Alumno>> Listar() => alumnos.Listar();

    [HttpGet("{id:int}")]
    public ActionResult<ResumenAlumno> Obtener(int id) => progreso.Resumen(id);

    [HttpPost]
    public ActionResult<Alumno> Crear(AlumnoEntrada entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nombre))
            throw new SolicitudInvalidaException("El nombre es obligatorio.");

        var alumno = new Alumno
        {
            Nombre = entrada.Nombre.Trim(),
            RutaProyecto = Limpiar(entrada.RutaProyecto),
            ModoLibre = entrada.ModoLibre,
            Creado = DateTime.Now
        };
        alumno.Id = alumnos.Insertar(alumno);
        return CreatedAtAction(nameof(Obtener), new { id = alumno.Id }, alumno);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Alumno> Actualizar(int id, AlumnoEntrada entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nombre))
            throw new SolicitudInvalidaException("El nombre es obligatorio.");

        Alumno alumno = progreso.ObtenerAlumno(id);
        alumno.Nombre = entrada.Nombre.Trim();
        alumno.RutaProyecto = Limpiar(entrada.RutaProyecto);
        alumno.ModoLibre = entrada.ModoLibre;
        alumnos.Actualizar(alumno);
        return alumno;
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}

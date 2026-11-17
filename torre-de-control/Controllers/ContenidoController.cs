using Microsoft.AspNetCore.Mvc;

// Páginas sueltas (manual de vuelo, glosario) y el catálogo de insignias
[ApiController]
[Route("api")]
public class ContenidoController(IContenidoRepository contenido, IVerificadorService verificador) : ControllerBase
{
    // Para la tarjeta de embarque: la carpeta del repositorio que se sugiere por defecto
    [HttpGet("sugerencias")]
    public IActionResult Sugerencias() => Ok(new { rutaProyecto = verificador.RutaPorDefecto });

    [HttpGet("paginas/{nombre}")]
    public IActionResult Pagina(string nombre)
    {
        string? markdown = contenido.ObtenerPagina(nombre);
        return markdown is null ? NotFound() : Content(markdown, "text/markdown; charset=utf-8");
    }

    [HttpGet("insignias")]
    public ActionResult<List<Insignia>> Insignias() => contenido.ListarInsignias();
}

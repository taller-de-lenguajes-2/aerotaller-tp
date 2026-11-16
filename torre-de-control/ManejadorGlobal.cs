using Microsoft.AspNetCore.Diagnostics;

// El manejador global del tema 17: traduce cada tipo de excepción a un código HTTP
// y devuelve un ProblemDetails (JSON) que el navegador muestra en un cartel.
public class ManejadorGlobal(IProblemDetailsService problemDetails, ILogger<ManejadorGlobal> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        http.Response.StatusCode = ex switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            SolicitudInvalidaException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (http.Response.StatusCode >= 500)
            logger.LogError(ex, "Error no controlado en {Ruta}", http.Request.Path);

        var detalle = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = http.Response.StatusCode >= 500 ? "Error interno en la Torre de Control" : ex.Message,
            Detail = http.Response.StatusCode >= 500 ? "El detalle está en la consola donde se ejecuta dotnet run." : null
        };
        if (ex is SolicitudInvalidaException invalida && invalida.Faltantes.Count > 0)
            detalle.Extensions["faltantes"] = invalida.Faltantes;

        return await problemDetails.TryWriteAsync(new() { HttpContext = http, Exception = ex, ProblemDetails = detalle });
    }
}

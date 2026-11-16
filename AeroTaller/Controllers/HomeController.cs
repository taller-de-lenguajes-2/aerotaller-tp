using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    // GET /  →  Views/Home/Index.cshtml
    public IActionResult Index()
    {
        // Desafío 0: colocar un punto de interrupción en la línea siguiente y recargar la página
        return View();
    }
}

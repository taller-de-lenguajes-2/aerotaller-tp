using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    // GET /  →  Views/Home/Index.cshtml
    public IActionResult Index()
    {
        // Desafío 0: poné un punto de interrupción en la línea de abajo y recargá la página
        return View();
    }
}

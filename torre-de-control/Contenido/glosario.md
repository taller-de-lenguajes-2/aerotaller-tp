## A–D

| Término | Qué es |
|---|---|
| **ADO.NET** | La forma "a mano" de hablar con una base desde .NET: `Connection`, `Command`, `DataReader`. |
| **Antiforgery token** | Valor oculto en cada formulario que prueba que el POST salió de *tu* página (evita CSRF). `[ValidateAntiForgeryToken]`. |
| **Autenticación** | Comprobar quién es el usuario (login). |
| **Autorización** | Decidir qué puede hacer (roles, filtros). 401 = no sé quién sos; 403 = sé quién sos y no podés. |
| **Breakpoint** | Punto de interrupción: el programa se frena ahí y podés mirar las variables. |
| **Cadena de conexión** | Texto que dice a qué base conectarse: `Data Source=aerotaller.db;Foreign Keys=True`. Va en `appsettings.json`. |
| **Captive dependency** | Error de tiempos de vida: un Singleton que recibe un Scoped y lo "captura" para siempre. |
| **Clave foránea (FK)** | Columna que apunta a la clave primaria de otra tabla (`Reserva.VueloId → Vuelo.Id`). |
| **Composición** | Un objeto formado por otros: la `Reserva` *tiene* un `Vuelo` y un `Pasajero`. |
| **Contenedor (DI)** | El que crea los objetos y les pasa sus dependencias según lo registrado en `Program.cs`. |
| **Controlador** | Clase que atiende requests: recibe datos, llama a servicios y elige la vista. |
| **Cookie** | Dato chico que guarda el navegador y manda en cada pedido. |
| **DataAnnotations** | Atributos de validación: `[Required]`, `[Range]`, `[RegularExpression]`… |

## E–M

| Término | Qué es |
|---|---|
| **Excepción propia** | Clase que hereda de `Exception` para un error del dominio: `ReglaDeNegocioException`. |
| **Filtro** | Código que corre antes (o después) de una acción: `[RequiereLogin]`. |
| **Índice único parcial** | `UNIQUE` que aplica solo a las filas que cumplen un `WHERE`: un asiento confirmado por vuelo. |
| **Inyección de dependencias** | Recibir lo que necesitás por el constructor en vez de hacer `new`. |
| **Inyección SQL** | Ataque que mete SQL en un dato del usuario. Se evita con parámetros (`@id`). |
| **Interfaz** | Contrato: dice *qué* se puede hacer, no *cómo*. `IVueloRepository`. |
| **JOIN** | Combinar filas de dos tablas por una columna en común. `LEFT JOIN` conserva las de la izquierda aunque no tengan pareja. |
| **Layout** | La "plantilla" común de todas las páginas (`_Layout.cshtml`): menú, pie, `@RenderBody()`. |
| **Log estructurado** | Log con datos con nombre: `"Reserva {ReservaId}"`, no texto pegado con `$`. |
| **Model binding** | ASP.NET llena los parámetros de la acción (o el ViewModel) con lo que viene en el formulario o la URL. |
| **ModelState** | Dónde quedan los errores de validación. `ModelState.IsValid`, `AddModelError`. |
| **MVC** | Modelo–Vista–Controlador: separar datos, presentación y coordinación. |

## O–Z

| Término | Qué es |
|---|---|
| **Overbooking** | Vender más asientos que la capacidad. Acá está prohibido. |
| **Overposting** | Que el usuario mande campos que no debería poder tocar. Se evita con ViewModels. |
| **PRG** | POST → Redirect → GET: después de guardar, redirigir (así F5 no vuelve a guardar). |
| **Primary constructor** | `class X(IDependencia dep)`: el constructor en la misma línea de la clase (C# 12). |
| **Razor** | El lenguaje de las vistas: HTML + `@` C#. |
| **Repositorio** | Clase que encapsula el acceso a datos de **una** entidad. |
| **Ruteo convencional** | `{controller=Home}/{action=Index}/{id?}`: la URL decide controlador, acción e id. |
| **Serilog** | Librería de logs que escribe a consola y a archivos. |
| **Servicio** | Capa con las reglas del negocio. |
| **Sesión** | Datos del usuario guardados en el servidor; el navegador tiene solo una cookie con el Id. |
| **Singleton / Scoped / Transient** | Tiempos de vida: uno para toda la app / uno por request / uno cada vez. |
| **Tag helper** | Atributo `asp-…` que Razor convierte en HTML correcto (`asp-for`, `asp-action`). |
| **TempData** | Dato que sobrevive exactamente un request más (mensajes después de un Redirect). |
| **ViewModel** | Clase con exactamente lo que una pantalla necesita. |

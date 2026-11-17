## A–D

| Término | Qué es |
|---|---|
| **ADO.NET** | Mecanismo de .NET para acceder manualmente a una base de datos: `Connection`, `Command`, `DataReader`. |
| **Antiforgery token** | Valor oculto en cada formulario que permite verificar que el POST proviene de la *propia* página (evita CSRF). `[ValidateAntiForgeryToken]`. |
| **Autenticación** | Comprobar quién es el usuario (login). |
| **Autorización** | Determinar qué acciones puede realizar el usuario (roles, filtros). 401 = usuario no identificado; 403 = usuario identificado, pero sin permiso. |
| **Breakpoint** | Punto de interrupción: el programa se detiene ahí y se pueden inspeccionar las variables. |
| **Cadena de conexión** | Texto que indica a qué base conectarse: `Data Source=aerotaller.db;Foreign Keys=True`. Se define en `appsettings.json`. |
| **Captive dependency** | Error de tiempos de vida: un Singleton que recibe un Scoped y lo "captura" durante toda la ejecución de la aplicación. |
| **Clave foránea (FK)** | Columna que referencia la clave primaria de otra tabla (`Reserva.VueloId → Vuelo.Id`). |
| **Composición** | Un objeto formado por otros: la `Reserva` *tiene* un `Vuelo` y un `Pasajero`. |
| **Contenedor (DI)** | Componente que crea los objetos y les provee sus dependencias según lo registrado en `Program.cs`. |
| **Controlador** | Clase que procesa requests: recibe datos, llama a servicios y selecciona la vista. |
| **Cookie** | Dato pequeño que el navegador almacena y envía en cada request. |
| **DataAnnotations** | Atributos de validación: `[Required]`, `[Range]`, `[RegularExpression]`… |

## E–M

| Término | Qué es |
|---|---|
| **Excepción propia** | Clase que hereda de `Exception` para un error del dominio: `ReglaDeNegocioException`. |
| **Filtro** | Código que se ejecuta antes (o después) de una acción: `[RequiereLogin]`. |
| **Índice único parcial** | `UNIQUE` que aplica solo a las filas que cumplen un `WHERE`: un asiento confirmado por vuelo. |
| **Inyección de dependencias** | Recibir las dependencias por el constructor en vez de crearlas con `new`. |
| **Inyección SQL** | Ataque que introduce SQL en un dato ingresado por el usuario. Se evita con parámetros (`@id`). |
| **Interfaz** | Contrato: define *qué* se puede hacer, no *cómo*. `IVueloRepository`. |
| **JOIN** | Combinar filas de dos tablas por una columna en común. `LEFT JOIN` conserva las de la izquierda aunque no tengan coincidencia. |
| **Layout** | La "plantilla" común de todas las páginas (`_Layout.cshtml`): menú, pie, `@RenderBody()`. |
| **Log estructurado** | Log con datos con nombre: `"Reserva {ReservaId}"`, no texto interpolado con `$`. |
| **Model binding** | Mecanismo por el cual ASP.NET completa los parámetros de la acción (o el ViewModel) con los valores recibidos en el formulario o la URL. |
| **ModelState** | Estructura donde se almacenan los errores de validación. `ModelState.IsValid`, `AddModelError`. |
| **MVC** | Modelo–Vista–Controlador: separar datos, presentación y coordinación. |

## O–Z

| Término | Qué es |
|---|---|
| **Overbooking** | Vender más asientos que la capacidad. En AeroTaller está prohibido. |
| **Overposting** | Envío, por parte del usuario, de campos que no debería poder modificar. Se evita con ViewModels. |
| **PRG** | POST → Redirect → GET: después de guardar, redirigir (así, al presionar F5 no se repite el guardado). |
| **Primary constructor** | `class X(IDependencia dep)`: el constructor en la misma línea de la clase (C# 12). |
| **Razor** | El lenguaje de las vistas: HTML + `@` C#. |
| **Repositorio** | Clase que encapsula el acceso a datos de **una** entidad. |
| **Ruteo convencional** | `{controller=Home}/{action=Index}/{id?}`: la URL determina controlador, acción e id. |
| **Serilog** | Biblioteca de logs que escribe a consola y a archivos. |
| **Servicio** | Capa con las reglas del negocio. |
| **Sesión** | Datos del usuario guardados en el servidor; el navegador conserva solo una cookie con el Id. |
| **Singleton / Scoped / Transient** | Tiempos de vida: uno para toda la aplicación / uno por request / uno cada vez. |
| **Tag helper** | Atributo `asp-…` que Razor convierte en HTML correcto (`asp-for`, `asp-action`). |
| **TempData** | Dato que se conserva durante exactamente un request más (mensajes después de un Redirect). |
| **ViewModel** | Clase que contiene exactamente los datos que requiere una pantalla. |

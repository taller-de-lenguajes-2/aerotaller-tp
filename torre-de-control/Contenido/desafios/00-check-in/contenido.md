## 🎯 Misión

El trabajo práctico consiste en construir **AeroTaller**, una aerolínea, de a una escala por vez. Al final se obtiene
una aplicación web completa: vuelos con mapa de asientos, pasajeros, reservas con reglas de negocio, logs y login con roles.
Es lo mismo que se pide en el parcial, pero con un dominio nuevo.

Antes de despegar, el check-in:

1. El repositorio de GitHub Classroom clonado en la máquina local.
2. El proyecto base `AeroTaller` en ejecución.
3. **El primer punto de interrupción**: detener el programa en medio de un request e inspeccionar su contenido.
4. El primer commit **y push**.

:::nota
Esta aplicación (la **Torre de Control**) también es ASP.NET: un Web API con repositorios, servicios
e inyección de dependencias, y la página está hecha con Vue. Al terminar el TP, en `torre-de-control/` pueden
reconocerse todos esos elementos.
:::

## 📚 Repaso

En el tema 10 se presentó el **viaje de un request** en MVC:

1. El navegador pide `GET /`.
2. El **ruteo convencional** (`{controller=Home}/{action=Index}/{id?}`) completa lo que falta: controlador `Home`, acción `Index`.
3. ASP.NET crea un `HomeController` y llama a `Index()`.
4. La acción devuelve `View()`: Razor dibuja `Views/Home/Index.cshtml` dentro del `_Layout.cshtml`.
5. Vuelve HTML al navegador.

Hasta ahora ese viaje se describió en teoría. En este desafío se lo **observa**: el depurador detiene el programa en el paso 3
y permite inspeccionar el request por dentro.

| En el repositorio | Qué es |
|---|---|
| `AeroTaller/` | el proyecto propio: se completa en cada desafío |
| `material/` | scripts SQL con datos de ejemplo (se usan en el desafío 1) |
| `torre-de-control/` | este tutorial |
| `verificador/` | el programa que revisa el TP (el mismo que usa la corrección automática) |
| `CONTRATO.md` | los nombres obligatorios (tablas, rutas, clases) |

## 💻 Paso a paso

### Paso 1 · Clonar el repositorio

Aceptar la tarea desde el enlace de GitHub Classroom provisto por la cátedra. Se crea un repositorio **propio** con este
proyecto adentro. Clonarlo:

```bash
git clone https://github.com/<organizacion>/<repositorio>.git
cd <repositorio>
code .
```

:::cuidado
Abrir en VS Code la carpeta **raíz del repositorio** (la que tiene `AeroTaller/`, `torre-de-control/` y `.vscode/`), no una subcarpeta.
De lo contrario, F5 no encuentra la configuración de depuración.
:::

### Paso 2 · Correr AeroTaller

En una terminal:

```bash
dotnet run --project AeroTaller
```

Abrir `http://localhost:5210`. Debe verse la página de inicio "AeroTaller · proyecto base". Los enlaces del menú (Vuelos, Reservas, Pasajeros)
todavía devuelven 404: esos controladores se escriben en los desafíos siguientes.

Detenerlo con `Ctrl + C`.

### Paso 3 · Preparar VS Code para depurar

Instalar la extensión **C# Dev Kit** (de Microsoft). El repositorio ya trae `.vscode/launch.json` con dos configuraciones:
`AeroTaller` y `AeroTaller (datos falsos)` (esta última se usa en el desafío 7).

Abrir el panel **Ejecutar y depurar** (`Ctrl/Cmd + Shift + D`) y elegir **AeroTaller** en el desplegable superior.

### Paso 4 · Primer punto de interrupción

Abrir `AeroTaller/Controllers/HomeController.cs` y hacer clic a la izquierda del número de línea del `return View();`.
Aparece un punto rojo: es el **breakpoint**.

```csharp
public IActionResult Index()
{
    // Desafío 0: colocar un punto de interrupción en la línea siguiente y recargar la página
    return View();   // ← 🔴 aquí
}
```

Presionar **F5**. Se compila, arranca y se abre el navegador. VS Code pasa al frente con la línea resaltada en amarillo:
**el programa está detenido en medio del request**. El navegador sigue "cargando" porque todavía no recibió respuesta.

### Paso 5 · Inspeccionar el request

Con el programa detenido:

- Panel **Variables**: desplegar `this` → `Request` → observar `Method`, `Path`, `QueryString`.
- Panel **Inspección (Watch)**: agregar `Request.Path` y `HttpContext.Request.Headers["User-Agent"]`.
- Panel **Pila de llamadas (Call Stack)**: ¿quién llamó a `Index()`? (aparece mucho código de ASP.NET; el método propio está en la parte superior).

Luego probar en el navegador `http://localhost:5210/?origen=TUC` (con el breakpoint todavía activo) y observar `Request.QueryString`.

| Tecla | Qué hace |
|---|---|
| **F5** | continuar hasta el próximo breakpoint |
| **F10** | ejecutar la línea actual entera y detenerse en la siguiente |
| **F11** | entrar al método que se llama en esta línea |
| **Shift + F11** | terminar el método actual y volver a quien lo llamó |
| **Shift + F5** | detener la depuración |

### Paso 6 · Primer commit y push

Escribir nombre y legajo en el `README.md` y enviarlo a GitHub:

```bash
git status                  # qué cambió
git add README.md
git commit -m "Desafío 0: check-in del proyecto base"
git push
```

Ingresar al repositorio en GitHub y verificar que el commit esté. **Ese es el procedimiento de cada desafío**: terminar, commit, push y verificación en GitHub.

:::nota
Cargar en la tarjeta de embarque (arriba a la derecha, ✏️) la carpeta del repositorio. Así la Torre puede consultar el `git status`
y correr el verificador sobre el proyecto.
:::

## 🔴 Breakpoints

:::breakpoint HomeController.cs · Index
Con el breakpoint en el `return View();`, abrir `http://localhost:5210/` y después `http://localhost:5210/Home/Index`.
¿Cambia `Request.Path`? ¿Cambia qué método se ejecuta?
:::

:::breakpoint Program.cs · builder.Services.AddControllersWithViews()
Colocar un breakpoint en esa línea y arrancar con F5. Se detiene **una sola vez**, al arrancar, y no en cada request.
`Program.cs` arma la aplicación; los controladores atienden cada pedido. Esa diferencia es clave para el desafío 7.
:::

Preguntas para analizar:

- Si el programa queda detenido mucho tiempo, ¿qué le ocurre al navegador?
- ¿Qué ocurre si se coloca un breakpoint en una línea de comentario?

## 🔀 Alternativas

Hay tres formas de correr el proyecto, y cada una sirve para otra cosa:

| Forma | Comando | Ventaja | Cuándo usarla |
|---|---|---|---|
| Correr | `dotnet run --project AeroTaller` | simple, sin IDE | probar rápido, o el día del parcial si el IDE se cuelga |
| Correr y recompilar automáticamente | `dotnet watch --project AeroTaller` | al guardar recarga sola (*hot reload*) | maquetar vistas y CSS |
| Depurar | F5 en VS Code | detener, inspeccionar variables, seguir el flujo | entender qué ocurre, encontrar un bug |

:::catedra
En el tema 06 se usaba `dotnet run` y `dotnet watch`. El depurador no está en las diapositivas, pero es la herramienta
que más tiempo ahorra en el parcial: en lugar de adivinar, **se observa**.
:::

## ⚠️ Errores típicos

- **"Address already in use"**: ya hay otra instancia corriendo en el puerto 5210 (otra terminal, o un F5 anterior). Detenerla.
- **F5 no hace nada / pide elegir entorno**: se abrió una subcarpeta en VS Code, o falta la extensión C# Dev Kit.
- **El breakpoint queda hueco (círculo vacío)**: el código en ejecución no es el que se está viendo. Detener todo y volver a presionar F5.
- **Commit sin push**: el docente (y la corrección automática) no ven el trabajo. Siempre verificar en GitHub después del push.
- **Commits gigantes al final**: un commit por desafío (o por paso) permite volver atrás si algo se rompe.

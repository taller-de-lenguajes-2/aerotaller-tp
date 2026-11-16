## 🎯 Misión

Bienvenido a bordo de **AeroTaller**, la aerolínea que vamos a construir de a una escala por vez. Al final vas a tener
una aplicación web completa: vuelos con mapa de asientos, pasajeros, reservas con reglas de negocio, logs y login con roles.
Es lo mismo que te va a pedir el parcial, pero con un dominio nuevo.

Antes de despegar, el check-in:

1. Tu repositorio de GitHub Classroom clonado en tu máquina.
2. El proyecto base `AeroTaller` corriendo.
3. **Tu primer punto de interrupción**: vas a frenar el programa en medio de un request y mirar qué tiene adentro.
4. Tu primer commit **y push**.

:::nota
Esta app que estás leyendo (la **Torre de Control**) también es ASP.NET: un Web API con repositorios, servicios
e inyección de dependencias, y la página está hecha con Vue. Cuando termines el TP, abrí `torre-de-control/` y vas a
reconocer todo.
:::

## 📚 Repaso

En el tema 10 vimos el **viaje de un request** en MVC:

1. El navegador pide `GET /`.
2. El **ruteo convencional** (`{controller=Home}/{action=Index}/{id?}`) completa lo que falta: controlador `Home`, acción `Index`.
3. ASP.NET crea un `HomeController` y llama a `Index()`.
4. La acción devuelve `View()`: Razor dibuja `Views/Home/Index.cshtml` adentro del `_Layout.cshtml`.
5. Vuelve HTML al navegador.

Hasta ahora ese viaje lo "creíamos". Hoy lo vamos a **ver**: el depurador frena el programa en el paso 3 y te deja mirar
el request por dentro.

| En tu repo | Qué es |
|---|---|
| `AeroTaller/` | **tu** proyecto: lo vas a ir completando en cada desafío |
| `material/` | scripts SQL con datos de ejemplo (los usás en el desafío 1) |
| `torre-de-control/` | este tutorial |
| `verificador/` | el programa que revisa tu TP (el mismo que usa la corrección automática) |
| `CONTRATO.md` | los nombres obligatorios (tablas, rutas, clases) |

## 💻 Paso a paso

### Paso 1 · Clonar tu repo

Aceptá la tarea desde el link de GitHub Classroom que te pasó la cátedra. Se crea un repo **tuyo** con este proyecto
adentro. Clonalo:

```bash
git clone https://github.com/<organizacion>/<tu-repo>.git
cd <tu-repo>
code .
```

:::cuidado
Abrí en VS Code la carpeta **raíz del repo** (la que tiene `AeroTaller/`, `torre-de-control/` y `.vscode/`), no una subcarpeta.
Si no, F5 no encuentra la configuración de depuración.
:::

### Paso 2 · Correr AeroTaller

En una terminal:

```bash
dotnet run --project AeroTaller
```

Abrí `http://localhost:5210`. Tenés que ver "Bienvenido a AeroTaller". Los links del menú (Vuelos, Reservas, Pasajeros)
todavía dan 404: esos controladores los vas a escribir vos.

Cortalo con `Ctrl + C`.

### Paso 3 · Preparar VS Code para depurar

Instalá la extensión **C# Dev Kit** (de Microsoft). El repo ya trae `.vscode/launch.json` con dos configuraciones:
`AeroTaller` y `AeroTaller (datos falsos)` (esa la vas a usar en el desafío 7).

Abrí el panel **Ejecutar y depurar** (`Ctrl/Cmd + Shift + D`) y elegí **AeroTaller** en el desplegable de arriba.

### Paso 4 · Tu primer punto de interrupción

Abrí `AeroTaller/Controllers/HomeController.cs` y hacé clic a la izquierda del número de línea del `return View();`.
Aparece un punto rojo: es el **breakpoint**.

```csharp
public IActionResult Index()
{
    // Desafío 0: poné un punto de interrupción en la línea de abajo y recargá la página
    return View();   // ← 🔴 acá
}
```

Apretá **F5**. Se compila, arranca y se abre el navegador. VS Code se pone al frente con la línea resaltada en amarillo:
**el programa está detenido en medio del request**. El navegador sigue "cargando" porque todavía no le respondimos.

### Paso 5 · Mirar adentro

Con el programa frenado:

- Panel **Variables**: desplegá `this` → `Request` → mirá `Method`, `Path`, `QueryString`.
- Panel **Inspección (Watch)**: agregá `Request.Path` y `HttpContext.Request.Headers["User-Agent"]`.
- Panel **Pila de llamadas (Call Stack)**: ¿quién llamó a `Index()`? (vas a ver mucho código de ASP.NET; tu método está arriba de todo).

Ahora probá en el navegador `http://localhost:5210/?origen=TUC` (con el breakpoint todavía puesto) y mirá `Request.QueryString`.

| Tecla | Qué hace |
|---|---|
| **F5** | continuar hasta el próximo breakpoint |
| **F10** | ejecutar la línea actual entera y frenar en la siguiente |
| **F11** | entrar al método que se llama en esta línea |
| **Shift + F11** | terminar el método actual y volver a quien lo llamó |
| **Shift + F5** | parar la depuración |

### Paso 6 · Primer commit y push

Escribí tu nombre y legajo en el `README.md` y mandalo a GitHub:

```bash
git status                  # qué cambió
git add README.md
git commit -m "Desafío 0: check-in del proyecto base"
git push
```

Entrá a tu repo en GitHub y verificá que el commit está. **Ese es el ritual de cada desafío**: terminar, commit, push y mirar en GitHub.

:::nota
Cargá en tu tarjeta de embarque (arriba a la derecha, ✏️) la carpeta de tu repo. Así la Torre puede mirar tu `git status`
y correr el verificador sobre tu proyecto.
:::

## 🔴 Breakpoints

:::breakpoint HomeController.cs · Index
Con el breakpoint en el `return View();`, abrí `http://localhost:5210/` y después `http://localhost:5210/Home/Index`.
¿Cambia `Request.Path`? ¿Cambia qué método se ejecuta?
:::

:::breakpoint Program.cs · builder.Services.AddControllersWithViews()
Poné un breakpoint en esa línea y arrancá con F5. Frena **una sola vez**, al arrancar, y no en cada request.
`Program.cs` arma la aplicación; los controladores atienden cada pedido. Esa diferencia es clave para el desafío 7.
:::

Preguntate:

- Si dejás el programa frenado mucho tiempo, ¿qué le pasa al navegador?
- ¿Qué pasa si ponés un breakpoint en una línea de comentario?

## 🔀 Alternativas

Hay tres formas de correr el proyecto, y cada una sirve para otra cosa:

| Forma | Comando | Ventaja | Cuándo usarla |
|---|---|---|---|
| Correr | `dotnet run --project AeroTaller` | simple, sin IDE | probar rápido, el día del parcial si el IDE se cuelga |
| Correr y recompilar solo | `dotnet watch --project AeroTaller` | al guardar recarga sola (*hot reload*) | maquetar vistas y CSS |
| Depurar | F5 en VS Code | frenar, mirar variables, seguir el flujo | entender qué pasa, encontrar un bug |

:::catedra
En el tema 06 se usaba `dotnet run` y `dotnet watch`. El depurador no está en las diapositivas, pero es la herramienta
que más tiempo te va a ahorrar en el parcial: en vez de adivinar, **mirás**.
:::

## ⚠️ Errores típicos

- **"Address already in use"**: ya hay otra instancia corriendo en el puerto 5210 (otra terminal, o un F5 anterior). Cortala.
- **F5 no hace nada / pide elegir entorno**: abriste una subcarpeta en VS Code, o falta la extensión C# Dev Kit.
- **El breakpoint queda hueco (círculo vacío)**: el código que corre no es el que estás viendo. Pará todo y volvé a F5.
- **Commit sin push**: el profe (y la corrección automática) no ven tu trabajo. Siempre mirá GitHub después de pushear.
- **Commits gigantes al final**: un commit por desafío (o por paso) te deja volver atrás si algo se rompe.

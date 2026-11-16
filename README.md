# AeroTaller — TP integrador de Taller de Lenguajes 2

Alumno/a: _(nombre y apellido)_

El TP consiste en construir, por etapas, la aplicación web de una aerolínea: vuelos con mapa de asientos, pasajeros,
reservas con reglas de negocio, registro de logs e inicio de sesión con roles. Se trabaja con ASP.NET Core MVC (.NET 10),
ADO.NET con SQLite, repositorios, servicios, inyección de dependencias, ViewModels y validación: los mismos contenidos que
evalúa el parcial.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --list-sdks`)
- [VS Code](https://code.visualstudio.com/) con la extensión **C# Dev Kit**
- Git y una cuenta de GitHub
- [DB Browser for SQLite](https://sqlitebrowser.org/) (para inspeccionar la base)

## Comandos

```bash
dotnet run --project torre-de-control      # tutorial interactivo  → http://localhost:5200
dotnet run --project AeroTaller            # aplicación del TP     → http://localhost:5210
dotnet run --project verificador -- 3      # verificación del desafío 3 (también 0-10 o todos)
```

Para **depurar** la aplicación: abrir la carpeta del repositorio en VS Code y presionar **F5** (configuración "AeroTaller").

## Torre de Control

Es el tutorial del TP. Presenta cada desafío con la misión, un repaso teórico, el paso a paso, los puntos de interrupción
sugeridos, alternativas, errores frecuentes, lecturas, una lista de tareas y un cuestionario breve que habilita la etapa
siguiente. Cada tres desafíos hay un checkpoint "¿Cómo vamos?" con una bitácora (lo realizado, lo pendiente y las dudas para
la consulta).

El progreso se guarda en `torre-de-control/progreso.db`, en la computadora local (no se sube al repositorio).

## Plan de vuelo

| Escala | Desafío | Temas |
|---|---|---|
| CHK | 0 · Check-in: repositorio, proyecto en ejecución, primer punto de interrupción | 00 · 10 |
| SQL | 1 · La base: tablas, claves, datos y `CrearBaseSiNoExiste` | 07 · 08 |
| REP | 2 · Modelos y repositorios con ADO.NET | 01 · 02 · 08 · 09 |
| CP1 | ¿Cómo vamos? | |
| MVC | 3 · Controladores, vistas y layout | 10 · 11 |
| VMD | 4 · ViewModels y mapa de asientos | 12 |
| VAL | 5 · Validación | 13 |
| CP2 | ¿Cómo vamos? | |
| SRV | 6 · Capa de servicios | 04 · 09 · 16 |
| DIN | 7 · Inyección de dependencias y repositorios falsos | 14 |
| LOG | 8 · Excepciones y logs | 16 · 17 |
| CP3 | ¿Cómo vamos? | |
| AUT | 9 · Autenticación: inicio de sesión, sesión y roles | 15 |
| SIM | 10 · Simulacro de parcial (equipaje, 2 h 30 min, sin puntaje: porcentaje de completitud) | todos |

## Corrección automática

Cada `git push` ejecuta el verificador en GitHub (pestaña **Actions** del repositorio) y asigna puntos por cada desafío
aprobado. Es el mismo programa que se ejecuta con `dotnet run --project verificador`. Los nombres que verifica están en
[CONTRATO.md](./CONTRATO.md).

No deben modificarse `verificador/`, `.github/` ni `material/datos.sql`: la cátedra vuelve a ejecutar el verificador
oficial al corregir.

## Contenido del repositorio

```
AeroTaller/          ← proyecto del TP (parte casi vacío)
material/            ← datos de ejemplo para la base (desafíos 1, 9 y simulacro)
torre-de-control/    ← tutorial (ASP.NET Web API + Vue)
verificador/         ← programa que verifica el TP
CONTRATO.md          ← nombres obligatorios
.vscode/             ← configuración de depuración (F5)
```

## Procedimiento sugerido para cada desafío

1. Leer la misión e intentar cada paso **antes** de consultar la solución.
2. Colocar los puntos de interrupción indicados y seguir el flujo de ejecución.
3. Ejecutar `dotnet run --project verificador -- N` hasta obtener todos los chequeos aprobados.
4. `git add -A` · `git commit -m "Desafío N: …"` · `git push` · comprobar el commit en GitHub.
5. Responder el cuestionario y continuar con la etapa siguiente.

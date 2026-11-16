# ✈ AeroTaller — TP integrador de Taller de Lenguajes 2

Alumno/a: _(tu nombre)_ · Legajo: _(tu legajo)_

Vas a construir, de a una escala por vez, la aplicación web de una aerolínea: vuelos con mapa de asientos, pasajeros,
reservas con reglas de negocio, logs y login con roles. ASP.NET Core MVC (.NET 10), ADO.NET con SQLite, repositorios,
servicios, inyección de dependencias, ViewModels y validación: **lo mismo que te va a pedir el parcial**.

## Antes de empezar

- [.NET SDK 10](https://dotnet.microsoft.com/download) → `dotnet --list-sdks`
- [VS Code](https://code.visualstudio.com/) con la extensión **C# Dev Kit**
- Git y tu cuenta de GitHub
- [DB Browser for SQLite](https://sqlitebrowser.org/) (para mirar la base)

## Tres comandos

```bash
dotnet run --project torre-de-control      # el tutorial interactivo → http://localhost:5200
dotnet run --project AeroTaller            # TU aplicación          → http://localhost:5210
dotnet run --project verificador -- 3      # revisa el desafío 3 (o 0-10, o todos)
```

Para **depurar** tu aplicación: abrí la carpeta del repo en VS Code y apretá **F5** (configuración "AeroTaller").

## La Torre de Control

Es el tutorial: te guía desafío por desafío con la misión, un repaso, el paso a paso, los breakpoints para poner,
alternativas, errores típicos de parcial, lecturas, una checklist y un cuestionario corto para desbloquear la siguiente escala.
Cada 3 desafíos hay un checkpoint "¿Cómo vamos?" con tu bitácora (qué hiciste, qué falta, dudas para la consulta).

Tu progreso se guarda en `torre-de-control/progreso.db` (en tu máquina; no se sube al repo).

## Plan de vuelo

| Escala | Desafío | Temas |
|---|---|---|
| CHK | 0 · Check-in: repo, proyecto corriendo, primer breakpoint | 00 · 10 |
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
| AUT | 9 · Autenticación: login, sesión y roles | 15 |
| SIM | 10 · Simulacro de parcial (equipaje, 120 minutos) | todos |

## Corrección automática

Cada `git push` corre el verificador en GitHub (pestaña **Actions** de tu repo) y suma puntos por cada desafío en verde.
Es exactamente el mismo programa que corrés con `dotnet run --project verificador`. Los nombres que busca están en
[CONTRATO.md](./CONTRATO.md).

⚠️ No modifiques `verificador/`, `.github/` ni `material/datos.sql`: la cátedra vuelve a correr el verificador oficial al corregir.

## Qué hay en el repo

```
AeroTaller/          ← tu proyecto (empieza casi vacío)
material/            ← datos de ejemplo para la base (desafíos 1, 9 y simulacro)
torre-de-control/    ← el tutorial (ASP.NET Web API + Vue)
verificador/         ← el programa que revisa tu TP
CONTRATO.md          ← nombres obligatorios
.vscode/             ← configuración para depurar con F5
```

## El ritual de cada desafío

1. Leé la misión. Intentá cada paso **antes** de abrir la solución.
2. Poné los breakpoints que propone el desafío y seguí el flujo.
3. `dotnet run --project verificador -- N` hasta que esté todo en verde.
4. `git add -A` · `git commit -m "Desafío N: …"` · `git push` · mirá GitHub.
5. Cuestionario y a la próxima escala.

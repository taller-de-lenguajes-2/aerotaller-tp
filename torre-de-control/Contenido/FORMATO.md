# Cómo se escribe un desafío

Cada desafío es una carpeta en `Contenido/desafios/` con dos archivos:

```
Contenido/desafios/05-validacion/
├── desafio.json    ← metadatos, tareas, cuestionario, lecturas, commit
└── contenido.md    ← el texto (Markdown)
```

Se leen en cada pedido: basta con editar el archivo y recargar la página (no hace falta reiniciar).
Para agregar un desafío nuevo, crear la carpeta y asignar un `orden` que lo ubique en el mapa.

## desafio.json

```jsonc
{
  "orden": 6,                         // posición en el mapa (los ids de carpeta no importan para el orden)
  "codigo": "VAL",                    // 3 letras, como un aeropuerto
  "numero": 5,                        // número de desafío para el verificador (null en checkpoints)
  "titulo": "Validación",
  "subtitulo": "Que nadie reserve el asiento 99Z",
  "tipo": "desafio",                  // "desafio" · "checkpoint" · "simulacro"
  "minutosEstimados": 90,
  "minutosLimite": null,              // solo simulacro
  "temas": [ { "numero": "13", "titulo": "Validación", "url": "https://taller-de-lenguajes-2.github.io/clases/tema-13-validacion/index.html" } ],
  "insignia": "validacion",           // id en insignias.json que se gana al completarlo
  "herramientas": ["base"],           // opcional: paneles extra; "base" = crear/recrear aerotaller.db con el script
  "tareas": [ { "id": "t1", "texto": "…", "ayuda": "opcional" } ],
  "quiz": [
    {
      "id": "p1",
      "texto": "Pregunta",
      "codigo": "opcional: un fragmento de código que se muestra con la pregunta",
      "opciones": [ { "id": "a", "texto": "…" }, { "id": "b", "texto": "…" } ],
      "correcta": "b",                // NUNCA llega al navegador: corrige el servidor
      "pista": "Se muestra ante una respuesta incorrecta (orienta, no da la respuesta)",
      "explicacion": "Se muestra ante una respuesta correcta (refuerza el porqué)",
      "etiquetas": ["depurador"]      // "depurador" = se responde mirando el debugger
    }
  ],
  "lecturas": [ { "id": "l1", "titulo": "…", "url": "https://…", "fuente": "Microsoft Learn", "pregunta": "Qué buscar al leerla" } ],
  "commit": { "mensaje": "Desafío 5: validación de reservas y pasajeros", "detalle": "opcional" }
}
```

Reglas: el desafío se completa con **todas las tareas marcadas** y el **cuestionario aprobado** (todas bien,
intentos ilimitados). El checkpoint se completa con el formulario de bitácora. El simulacro, con sus tareas.

## contenido.md

Secciones `##` con estos títulos exactos (el tutorial arma el índice lateral con ellos):

| Sección | Para qué |
|---|---|
| `## :target: Misión` | el problema de AeroTaller y qué debe quedar funcionando |
| `## :book-open-text: Repaso` | el concepto explicado en términos simples + "lo que vimos en clase" (enlace a la diapositiva); al final, `### :lightbulb: Tips y recursos` con alternativas y sus pros y contras. Debajo, el tutorial agrega las lecturas "Para investigar" del JSON |
| `## :code-xml: Paso a paso` | pasos `### Paso N · Título`, que se revelan de a uno |

Checkpoints: `## :plane-landing: ¿Cómo vamos?`, `## :circle-check: Lo que ya se sabe hacer`, `## :compass: Lo que viene`, `## :search: Repaso sugerido`.
Simulacro: `## :plane: Consigna`, `## :clipboard-list: Requisitos`, `## :timer: Reglas del simulacro`, `## :clipboard-check: Cómo se corrige`.

**Íconos.** No se usan emojis: los íconos son de [Lucide](https://lucide.dev) y se escriben `:nombre:`. Al comienzo de un título
`##` el ícono acompaña la sección y el índice; en el texto o en una tabla (`:check:`, `:x:`, `:circle-check:`) se dibuja en línea.
Solo están disponibles los íconos de `wwwroot/js/iconos-lucide.js`; un `:nombre:` desconocido queda como texto. Dentro de los
bloques de código no se reemplaza nada.

Lo que NO va en el `.md` porque lo arma el tutorial desde el JSON: checklist, cuestionario, lecturas ("Para investigar"),
commit y push, y verificación.

### Cajas

```
:::pista Título opcional
Se muestra plegada: se abre solo si hace falta.
:::

:::solucion Ver el código completo
Plegada también. Para el código "de referencia" de un paso.
:::

:::detalle Título de lo que se despliega
Plegada. Para información de consulta que no hace falta leer de corrido (por ejemplo, tablas largas).
:::

:::catedra
"Lo visto en clase": cómo lo muestra la diapositiva, con enlace.
:::

:::cuidado
Advertencia (errores que rompen todo).
:::

:::nota
Comentario al margen.
:::

:::breakpoint Archivo.cs · método
Un punto de interrupción sugerido: dónde colocarlo y qué observar.
:::
```

### Código

Bloques con lenguaje: ` ```csharp `, ` ```sql `, ` ```cshtml `, ` ```bash `, ` ```json `.

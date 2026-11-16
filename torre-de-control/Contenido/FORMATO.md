# Cómo se escribe un desafío

Cada desafío es una carpeta en `Contenido/desafios/` con dos archivos:

```
Contenido/desafios/05-validacion/
├── desafio.json    ← metadatos, tareas, cuestionario, lecturas, commit
└── contenido.md    ← el texto (Markdown)
```

Se leen en cada pedido: editás, recargás la página y listo (no hace falta reiniciar).
Para agregar un desafío nuevo, creá la carpeta y poné un `orden` que lo ubique en el mapa.

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
  "tareas": [ { "id": "t1", "texto": "…", "ayuda": "opcional" } ],
  "quiz": [
    {
      "id": "p1",
      "texto": "Pregunta",
      "codigo": "opcional: un fragmento de código que se muestra con la pregunta",
      "opciones": [ { "id": "a", "texto": "…" }, { "id": "b", "texto": "…" } ],
      "correcta": "b",                // NUNCA llega al navegador: corrige el servidor
      "pista": "Se muestra si contesta mal (orienta, no da la respuesta)",
      "explicacion": "Se muestra si contesta bien (refuerza el porqué)",
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
| `## 🎯 Misión` | el problema de AeroTaller y qué tiene que quedar andando |
| `## 📚 Repaso` | el concepto en criollo + "lo que vimos en clase" (link a la diapositiva) |
| `## 💻 Paso a paso` | pasos `### Paso N · Título`, que se revelan de a uno |
| `## 🔴 Breakpoints` | dónde frenar, qué mirar, qué preguntarse |
| `## 🔀 Alternativas` | al menos 2 formas, con tabla de pros y contras |
| `## ⚠️ Errores típicos` | lo que más se ve en los parciales |

Checkpoints: `## 🛬 ¿Cómo vamos?`, `## ✅ Lo que ya sabés hacer`, `## 🧭 Lo que viene`, `## 🔎 Repaso sugerido`.
Simulacro: `## ✈️ Consigna`, `## 📋 Requisitos`, `## ⏱️ Reglas del simulacro`, `## 🧪 Cómo se corrige`.

Lo que NO va en el `.md` porque lo arma el tutorial desde el JSON: checklist, cuestionario, lecturas ("Para investigar"),
commit y push, y verificación.

### Cajas

```
:::pista Título opcional
Se muestra plegada: el alumno la abre si la necesita.
:::

:::solucion Ver el código completo
Plegada también. Para el código "de referencia" de un paso.
:::

:::catedra
"Lo que vimos en clase": cómo lo muestra la diapositiva, con link.
:::

:::cuidado
Advertencia (errores que rompen todo).
:::

:::nota
Comentario al margen.
:::

:::breakpoint Archivo.cs · método
Un punto de interrupción sugerido: dónde, qué mirar.
:::
```

### Código

Bloques con lenguaje: ` ```csharp `, ` ```sql `, ` ```cshtml `, ` ```bash `, ` ```json `.

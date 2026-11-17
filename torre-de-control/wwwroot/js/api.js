// Se comunica con el backend (ASP.NET Web API). Si la respuesta es un error, lanza un Error con el ProblemDetails.
async function pedir(metodo, url, cuerpo) {
  const opciones = { method: metodo, headers: {} };
  if (cuerpo !== undefined) {
    opciones.headers['Content-Type'] = 'application/json';
    opciones.body = JSON.stringify(cuerpo);
  }
  let respuesta;
  try {
    respuesta = await fetch(url, opciones);
  } catch {
    throw Object.assign(new Error('No hay conexión con la Torre. Comprobar que dotnet run --project torre-de-control esté en ejecución.'), { faltantes: [] });
  }
  if (respuesta.status === 204) return null;
  const tipo = respuesta.headers.get('content-type') || '';
  const datos = tipo.includes('json') ? await respuesta.json() : await respuesta.text();
  if (!respuesta.ok) {
    const mensaje = (datos && (datos.title || datos.detail)) || `Error ${respuesta.status}`;
    throw Object.assign(new Error(mensaje), { estado: respuesta.status, faltantes: (datos && datos.faltantes) || [] });
  }
  return datos;
}

const base = id => `/api/alumnos/${id}`;
const desafio = (id, d) => `${base(id)}/desafios/${encodeURIComponent(d)}`;

export const api = {
  alumnos: () => pedir('GET', '/api/alumnos'),
  crearAlumno: datos => pedir('POST', '/api/alumnos', datos),
  actualizarAlumno: (id, datos) => pedir('PUT', base(id), datos),
  resumen: id => pedir('GET', base(id)),
  sugerencias: () => pedir('GET', '/api/sugerencias'),

  mapa: id => pedir('GET', `${base(id)}/mapa`),
  desafio: (id, d) => pedir('GET', desafio(id, d)),
  marcarTarea: (id, d, t, marcada) => pedir('PUT', `${desafio(id, d)}/tareas/${encodeURIComponent(t)}`, { marcada }),
  responder: (id, d, respuestas) => pedir('POST', `${desafio(id, d)}/quiz`, { respuestas }),
  completar: (id, d) => pedir('POST', `${desafio(id, d)}/completar`),
  checkpoint: (id, d, datos) => pedir('POST', `${desafio(id, d)}/checkpoint`, datos),
  abrirLectura: (id, d, l) => pedir('POST', `${desafio(id, d)}/lecturas/${encodeURIComponent(l)}`),
  verificar: (id, d) => pedir('POST', `${desafio(id, d)}/verificar`),
  repo: id => pedir('GET', `${base(id)}/repo`),
  base: id => pedir('GET', `${base(id)}/base`),
  crearBase: id => pedir('POST', `${base(id)}/base`),

  insignias: id => pedir('GET', `${base(id)}/insignias`),
  bitacora: id => pedir('GET', `${base(id)}/bitacora`),
  agregarNota: (id, nota) => pedir('POST', `${base(id)}/bitacora`, nota),
  resolverNota: (id, n, marcada) => pedir('PUT', `${base(id)}/bitacora/${n}/resuelta`, { marcada }),
  borrarNota: (id, n) => pedir('DELETE', `${base(id)}/bitacora/${n}`),

  pagina: nombre => pedir('GET', `/api/paginas/${nombre}`)
};

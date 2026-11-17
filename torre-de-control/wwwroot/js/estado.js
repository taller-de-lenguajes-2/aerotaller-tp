import { reactive } from 'vue';
import { api } from './api.js';

// Estado compartido por toda la aplicación. En localStorage solo se guarda qué tarjeta de embarque está activa
// (el progreso se almacena en progreso.db, en el servidor).
const CLAVE = 'torre.alumnoId';

export const estado = reactive({
  alumnoId: leerId(),
  resumen: null,          // { alumno, completados, total, insignias, insigniasTotal, actual }
  avisos: [],
  despegue: null          // { titulo, siguiente } mientras se muestra la animación
});

function leerId() {
  try { return Number(localStorage.getItem(CLAVE)) || null; } catch { return null; }
}

export function elegirAlumno(id) {
  estado.alumnoId = id;
  try { id ? localStorage.setItem(CLAVE, String(id)) : localStorage.removeItem(CLAVE); } catch { /* sin almacenamiento */ }
}

export async function refrescarResumen() {
  if (!estado.alumnoId) { estado.resumen = null; return; }
  try {
    estado.resumen = await api.resumen(estado.alumnoId);
  } catch (e) {
    if (e.estado === 404) elegirAlumno(null);
    estado.resumen = null;
  }
}

let siguienteAviso = 1;
export function avisar(texto, { icono = 'plane', tipo = '', titulo = '', duracion = 4500 } = {}) {
  const aviso = { id: siguienteAviso++, texto, icono, tipo, titulo };
  estado.avisos.push(aviso);
  setTimeout(() => {
    const i = estado.avisos.indexOf(aviso);
    if (i >= 0) estado.avisos.splice(i, 1);
  }, duracion);
}

export function festejarInsignias(insignias) {
  (insignias || []).forEach((ins, i) =>
    setTimeout(() => avisar(ins.descripcion, { icono: ins.icono, titulo: `Nueva insignia: ${ins.nombre}`, tipo: 'insignia-nueva', duracion: 6500 }), i * 700));
  if (insignias && insignias.length) refrescarResumen();
}

export function mostrarError(e) {
  avisar(e.message, { icono: 'triangle-alert', tipo: 'error', duracion: 6000 });
}

export function despegar(titulo, siguiente) {
  estado.despegue = { titulo, siguiente };
  setTimeout(() => { estado.despegue = null; }, 2200);
}

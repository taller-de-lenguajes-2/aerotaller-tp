import { ref } from 'vue';

// Combinaciones de colores de la Torre. La elección se recuerda en este navegador (localStorage);
// sin elección, la Torre sigue el modo claro u oscuro del sistema.
export const TEMAS = [
  { id: 'claro', nombre: 'Claro', muestra: ['#f4f6f9', '#0f2a47'], oscuro: false },
  { id: 'arena', nombre: 'Arena', muestra: ['#f5f1ea', '#2f3d38'], oscuro: false },
  { id: 'azul-noche', nombre: 'Azul noche', muestra: ['#141d2b', '#f2a516'], oscuro: true },
  { id: 'grafito', nombre: 'Grafito', muestra: ['#1b1c1f', '#f5b833'], oscuro: true }
];

const CLAVE = 'torre.tema';
const sistemaOscuro = () => window.matchMedia?.('(prefers-color-scheme: dark)').matches;

function leer() {
  try { return localStorage.getItem(CLAVE); } catch { return null; }
}

export const tema = ref(leer() || (sistemaOscuro() ? 'azul-noche' : 'claro'));

// Aplica el tema: atributo data-tema en <html> y la hoja de colores del código acorde (clara u oscura)
export function aplicarTema(id) {
  const elegido = TEMAS.find(t => t.id === id) || TEMAS[0];
  tema.value = elegido.id;
  document.documentElement.dataset.tema = elegido.id;
  document.querySelectorAll('link[data-hljs]').forEach(link => {
    link.media = (link.dataset.hljs === 'oscuro') === elegido.oscuro ? 'all' : 'not all';
  });
}

export function elegirTema(id) {
  aplicarTema(id);
  try { localStorage.setItem(CLAVE, id); } catch { /* sin almacenamiento: rige solo para esta visita */ }
}

aplicarTema(tema.value);

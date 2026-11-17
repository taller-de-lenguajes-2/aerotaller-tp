import { h } from 'vue';
import { ICONOS } from './iconos-lucide.js';

// Íconos de línea (Lucide) en lugar de emojis. Se usan de tres formas:
//   · en las plantillas Vue:     <Icono nombre="database" />
//   · como texto HTML:           svg('database')
//   · en el Markdown:            :database:  (en títulos ## :target: Misión, en tablas, en párrafos)

export const existe = nombre => Object.hasOwn(ICONOS, nombre);

export function svg(nombre, clase = '') {
  if (!existe(nombre)) return '';
  return `<svg class="icono-svg ${clase}" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor"`
    + ` stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${ICONOS[nombre]}</svg>`;
}

// Reemplaza :nombre: por el ícono, solo si existe (de este modo, "10:30:00" o "Data Source=a:b:" no se modifican)
export const conIconos = texto => texto.replace(/:([a-z0-9-]+):/g, (todo, nombre) => existe(nombre) ? svg(nombre) : todo);

// Separa el ícono inicial de un título: ":target: Misión" → { icono: 'target', texto: 'Misión' }
export function partirTitulo(titulo) {
  const m = /^:([a-z0-9-]+):\s*/.exec(titulo);
  return m && existe(m[1]) ? { icono: m[1], texto: titulo.slice(m[0].length) } : { icono: null, texto: titulo };
}

export const Icono = {
  props: { nombre: { type: String, default: '' }, tam: { type: [String, Number], default: null } },
  setup(props) {
    return () => existe(props.nombre)
      ? h('svg', {
          class: 'icono-svg', xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 24 24', fill: 'none',
          stroke: 'currentColor', 'stroke-width': 2, 'stroke-linecap': 'round', 'stroke-linejoin': 'round',
          'aria-hidden': 'true', style: props.tam ? { width: props.tam, height: props.tam } : null,
          innerHTML: ICONOS[props.nombre]
        })
      : null;
  }
};

import { marked } from '../lib/marked.esm.js';
import hljs from '../lib/highlight.min.js';
import { svg, conIconos, partirTitulo } from './iconos.js';

// Convierte el contenido.md de un desafío en secciones (## …) y pasos (### Paso …),
// con resaltado de código y las cajas :::pista, :::solucion, :::nota, :::cuidado, :::catedra, :::breakpoint, :::detalle.

const ALIAS = { cshtml: 'xml', razor: 'xml', html: 'xml', csharp: 'csharp', cs: 'csharp', sh: 'bash', shell: 'bash', jsonc: 'json' };
const CAJAS = {
  pista: { icono: 'lightbulb', titulo: 'Pista', plegada: true },
  solucion: { icono: 'puzzle', titulo: 'Ver la solución', plegada: true },
  nota: { icono: 'info', titulo: '' },
  cuidado: { icono: 'triangle-alert', titulo: 'Cuidado' },
  catedra: { icono: 'graduation-cap', titulo: 'Lo visto en clase' },
  breakpoint: { icono: 'circle-dot', titulo: 'Breakpoint' },
  detalle: { icono: 'table', titulo: 'Ver el detalle', plegada: true }
};

const escapar = t => t.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

marked.use({
  gfm: true,
  renderer: {
    code({ text, lang }) {
      const lenguaje = ALIAS[(lang || '').toLowerCase()] || (lang || '').toLowerCase();
      const html = lenguaje && hljs.getLanguage(lenguaje)
        ? hljs.highlight(text, { language: lenguaje, ignoreIllegals: true }).value
        : escapar(text);
      return `<pre><button class="copiar" type="button">copiar</button><code class="hljs">${html}</code></pre>`;
    },
    link({ href, title, tokens }) {
      const texto = this.parser.parseInline(tokens);
      const externo = /^https?:/.test(href);
      return `<a href="${href}"${title ? ` title="${escapar(title)}"` : ''}${externo ? ' target="_blank" rel="noopener"' : ''}>${texto}</a>`;
    }
  }
});

// Reemplaza las cajas :::tipo … ::: por HTML (el contenido interior también es Markdown)
function cajas(md) {
  return md.replace(/^:::(\w+)[ \t]*(.*)\n([\s\S]*?)^:::[ \t]*$/gm, (_, tipo, titulo, cuerpo) => {
    const caja = CAJAS[tipo] || { icono: '', titulo: '' };
    const texto = escapar(titulo.trim() || caja.titulo);
    const encabezado = texto ? `${svg(caja.icono)} ${texto}` : '';
    const interior = marked.parse(cajas(cuerpo));
    if (caja.plegada) return `\n<details class="caja ${tipo}"><summary>${encabezado}</summary>${interior}</details>\n`;
    return `\n<aside class="caja ${tipo}">${encabezado ? `<div class="caja-titulo">${encabezado}</div>` : ''}${interior}</aside>\n`;
  });
}

// Los :nombre: se convierten en íconos fuera de los bloques de código
export function renderizar(md) {
  // Cada tabla va dentro de un contenedor que se desplaza si la tabla no entra en el ancho disponible
  const html = marked.parse(cajas(md || '')).replace(/<table>/g, '<div class="tabla"><table>').replace(/<\/table>/g, '</table></div>');
  return html.split(/(<pre[\s\S]*?<\/pre>|<code[\s\S]*?<\/code>)/).map((parte, i) => i % 2 ? parte : conIconos(parte)).join('');
}

const slug = t => t.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/[^\w]+/g, '-').replace(/^-|-$/g, '');

// [{ id, titulo, icono, html, pasos: [{ titulo, html }] | null }]  ·  "## :target: Misión" → icono 'target', titulo 'Misión'
export function secciones(md) {
  const partes = ('\n' + (md || '')).split(/\n(?=## )/).filter(p => p.trim());
  return partes.map(parte => {
    const [primera, ...resto] = parte.trim().split('\n');
    const { icono, texto: titulo } = partirTitulo(primera.replace(/^##\s*/, '').trim());
    const cuerpo = resto.join('\n');
    const id = slug(titulo) || 'seccion';

    if (/paso a paso/i.test(titulo)) {
      const [intro, ...pasos] = ('\n' + cuerpo).split(/\n(?=### )/);
      return {
        id, titulo, icono, html: renderizar(intro),
        pasos: pasos.map(p => {
          const [t, ...c] = p.trim().split('\n');
          return { titulo: t.replace(/^###\s*/, ''), html: renderizar(c.join('\n')) };
        })
      };
    }
    return { id, titulo, icono, html: renderizar(cuerpo), pasos: null };
  });
}

// Botones "copiar" de los bloques de código (delegación de eventos)
document.addEventListener('click', async ev => {
  const boton = ev.target.closest('pre .copiar');
  if (!boton) return;
  const codigo = boton.parentElement.querySelector('code').innerText;
  try {
    await navigator.clipboard.writeText(codigo);
    boton.textContent = 'copiado';
  } catch {
    boton.textContent = 'error al copiar';
  }
  setTimeout(() => { boton.textContent = 'copiar'; }, 1500);
});

import { ref, onMounted } from 'vue';
import { api } from '../api.js';
import { mostrarError } from '../estado.js';
import { secciones as partir } from '../markdown.js';
import DiagramaEntidades from './diagrama-entidades.js';

// Introducción: una presentación breve del proyecto y cada entidad con sus propiedades (Contenido/introduccion.md)
export default {
  components: { DiagramaEntidades },
  setup() {
    const secciones = ref([]);
    onMounted(async () => {
      try { secciones.value = partir(await api.pagina('introduccion')); } catch (e) { mostrarError(e); }
    });
    // El diagrama va entre el texto de la sección y la caja plegada con el detalle de cada propiedad
    const corte = html => { const i = html.indexOf('<details'); return i < 0 ? html.length : i; };
    const intro = html => html.slice(0, corte(html));
    const resto = html => html.slice(corte(html));
    return { secciones, intro, resto };
  },
  template: `
    <div class="introduccion">
      <h1>Introducción</h1>
      <section class="seccion" v-for="s in secciones" :key="s.id" :id="s.id">
        <h2><Icono :nombre="s.icono" /> {{ s.titulo }}</h2>
        <div v-if="s.id === 'entidades-y-relaciones'">
          <div v-html="intro(s.html)"></div>
          <DiagramaEntidades />
          <div v-html="resto(s.html)"></div>
        </div>
        <div v-else v-html="s.html"></div>
      </section>
      <p class="no-imprimir"><a class="boton ambar" href="#/plan">Ir al plan de vuelo <Icono nombre="arrow-right" /></a></p>
    </div>
  `
};

import { ref, onMounted, watch } from 'vue';
import { api } from '../api.js';
import { mostrarError } from '../estado.js';
import { secciones as partir } from '../markdown.js';

// El "manual de vuelo": la receta de cada capa en una página para imprimir, y el glosario.
export default {
  setup() {
    const pagina = ref('manual');
    const secciones = ref([]);
    async function cargar() {
      try { secciones.value = partir(await api.pagina(pagina.value)); } catch (e) { mostrarError(e); }
    }
    onMounted(cargar);
    watch(pagina, cargar);
    const imprimir = () => window.print();
    return { pagina, secciones, imprimir };
  },
  template: `
    <div class="fila no-imprimir" style="justify-content:space-between;margin-bottom:1rem">
      <div class="fila">
        <button class="boton chico" type="button" :class="{ secundario: pagina !== 'manual' }" @click="pagina = 'manual'">📘 Manual de vuelo</button>
        <button class="boton chico" type="button" :class="{ secundario: pagina !== 'glosario' }" @click="pagina = 'glosario'">📖 Glosario</button>
      </div>
      <button class="boton ambar chico" type="button" @click="imprimir">🖨 Imprimir / PDF</button>
    </div>
    <section class="seccion" v-for="s in secciones" :key="s.id" :id="s.id">
      <h2>{{ s.titulo }}</h2>
      <div v-html="s.html"></div>
    </section>
  `
};

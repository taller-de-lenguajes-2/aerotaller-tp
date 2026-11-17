import { ref, onMounted, watch } from 'vue';
import { api } from '../api.js';
import { mostrarError } from '../estado.js';
import { secciones as partir } from '../markdown.js';
import BaseDatos from './base-datos.js';

// El "manual de vuelo": la guía de cada capa en una página imprimible, y el glosario.
export default {
  components: { BaseDatos },
  setup() {
    const pagina = ref('manual');
    const secciones = ref([]);
    async function cargar() {
      if (pagina.value === 'base') return;
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
        <button class="boton chico" type="button" :class="{ secundario: pagina !== 'manual' }" @click="pagina = 'manual'"><Icono nombre="book" /> Manual de vuelo</button>
        <button class="boton chico" type="button" :class="{ secundario: pagina !== 'glosario' }" @click="pagina = 'glosario'"><Icono nombre="book-open" /> Glosario</button>
        <button class="boton chico" type="button" :class="{ secundario: pagina !== 'base' }" @click="pagina = 'base'"><Icono nombre="database" /> Base de datos</button>
      </div>
      <button class="boton ambar chico" type="button" @click="imprimir"><Icono nombre="printer" /> Imprimir / PDF</button>
    </div>
    <BaseDatos v-if="pagina === 'base'" />
    <section v-else class="seccion" v-for="s in secciones" :key="s.id" :id="s.id">
      <h2><Icono :nombre="s.icono" /> {{ s.titulo }}</h2>
      <div v-html="s.html"></div>
    </section>
  `
};

import { reactive } from 'vue';
import { Icono } from './iconos.js';

// Secciones plegables de los desafíos. Lo que se pliega se recuerda por tipo de sección en este navegador:
// si se pliega "Repaso" en un desafío, aparece plegado también en los demás, para acceder directamente al trabajo.
const CLAVE = 'torre.plegadas';

function leer() {
  try { return JSON.parse(localStorage.getItem(CLAVE)) || {}; } catch { return {}; }
}

export const plegadas = reactive(leer());

function guardar() {
  try { localStorage.setItem(CLAVE, JSON.stringify(plegadas)); } catch { /* sin almacenamiento: rige solo para esta visita */ }
}

export function plegar(id, valor) {
  if (valor) plegadas[id] = true; else delete plegadas[id];
  guardar();
}

export function plegarVarias(ids, valor) {
  ids.forEach(id => { if (valor) plegadas[id] = true; else delete plegadas[id]; });
  guardar();
}

export const Seccion = {
  components: { Icono },
  props: { id: { type: String, required: true }, titulo: String, icono: String, clase: String },
  setup(props) {
    return { plegadas, alternar: () => plegar(props.id, !plegadas[props.id]) };
  },
  template: `
    <section class="seccion" :class="[clase, { plegada: plegadas[id] }]" :id="id">
      <h2 class="seccion-titulo">
        <button type="button" @click="alternar" :aria-expanded="!plegadas[id]" :title="plegadas[id] ? 'Desplegar la sección' : 'Plegar la sección'">
          <Icono :nombre="icono" />
          <span class="texto">{{ titulo }}</span>
          <span class="plegar-pista">{{ plegadas[id] ? 'desplegar' : 'plegar' }}<Icono :nombre="plegadas[id] ? 'chevron-right' : 'chevron-down'" /></span>
        </button>
      </h2>
      <div v-show="!plegadas[id]" class="seccion-cuerpo"><slot></slot></div>
    </section>
  `
};

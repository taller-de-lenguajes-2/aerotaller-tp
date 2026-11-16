import { createApp, ref, computed, onMounted, onBeforeUnmount, watch } from 'vue';
import { estado, elegirAlumno, refrescarResumen } from './estado.js';
import Embarque from './componentes/embarque.js';
import PlanDeVuelo from './componentes/plan.js';
import Desafio from './componentes/desafio.js';
import Insignias from './componentes/insignias.js';
import Bitacora from './componentes/bitacora.js';
import Manual from './componentes/manual.js';

// Ruteo mínimo con el "#" de la URL: #/plan, #/desafio/05-validacion, #/insignias…
function leerRuta() {
  const [, pagina = 'plan', parametro = null] = (location.hash.replace(/^#/, '') || '/plan').split('/');
  return { pagina, parametro: parametro && decodeURIComponent(parametro) };
}

const App = {
  components: { Embarque, PlanDeVuelo, Desafio, Insignias, Bitacora, Manual },
  setup() {
    const ruta = ref(leerRuta());
    const alCambiar = () => { ruta.value = leerRuta(); window.scrollTo({ top: 0 }); };
    onMounted(() => { window.addEventListener('hashchange', alCambiar); refrescarResumen(); });
    onBeforeUnmount(() => window.removeEventListener('hashchange', alCambiar));
    watch(() => estado.alumnoId, refrescarResumen);

    const pagina = computed(() => (!estado.alumnoId || ruta.value.pagina === 'embarque') ? 'embarque' : ruta.value.pagina);
    const cambiarPasajero = () => { elegirAlumno(null); location.hash = '#/embarque'; };

    return { estado, ruta, pagina, cambiarPasajero };
  },
  template: `
    <header class="barra">
      <a class="marca" href="#/plan"><span class="pantalla">TWR</span> Torre de Control · AeroTaller</a>
      <nav v-if="estado.alumnoId">
        <a href="#/plan" :class="{ activo: pagina === 'plan' || pagina === 'desafio' }">Plan de vuelo</a>
        <a href="#/insignias" :class="{ activo: pagina === 'insignias' }">Insignias</a>
        <a href="#/bitacora" :class="{ activo: pagina === 'bitacora' }">Bitácora</a>
        <a href="#/manual" :class="{ activo: pagina === 'manual' }">Manual de vuelo</a>
      </nav>
      <div class="pasajero" v-if="estado.resumen">
        <span>Pasajero <strong>{{ estado.resumen.alumno.nombre }}</strong></span>
        <span class="chip ambar">{{ estado.resumen.completados }}/{{ estado.resumen.total }}</span>
        <button type="button" @click="cambiarPasajero" title="Cambiar de tarjeta de embarque">cambiar</button>
      </div>
    </header>

    <main>
      <Embarque v-if="pagina === 'embarque'" />
      <PlanDeVuelo v-else-if="pagina === 'plan'" />
      <Desafio v-else-if="pagina === 'desafio'" :key="ruta.parametro" :id="ruta.parametro" />
      <Insignias v-else-if="pagina === 'insignias'" />
      <Bitacora v-else-if="pagina === 'bitacora'" />
      <Manual v-else-if="pagina === 'manual'" />
      <PlanDeVuelo v-else />
    </main>

    <div class="avisos" aria-live="polite">
      <div v-for="a in estado.avisos" :key="a.id" class="aviso" :class="a.tipo">
        <span class="icono">{{ a.icono }}</span>
        <div><strong v-if="a.titulo">{{ a.titulo }}<br></strong>{{ a.texto }}</div>
      </div>
    </div>

    <div class="despegue" v-if="estado.despegue">
      <span class="avion-grande">✈</span>
      <div class="texto"><small>DESPEGUE AUTORIZADO</small>{{ estado.despegue.titulo }}</div>
    </div>
  `
};

createApp(App).mount('#app');

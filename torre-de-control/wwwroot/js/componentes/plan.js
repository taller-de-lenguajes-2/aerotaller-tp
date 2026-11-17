import { ref, computed, onMounted, nextTick } from 'vue';
import { api } from '../api.js';
import { estado, mostrarError } from '../estado.js';

const CLAVE_AVION = 'torre.avion';

// El plan de vuelo: todas las escalas (desafíos y checkpoints), con el avión ubicado en la escala actual
export default {
  setup() {
    const etapas = ref([]);
    const insignias = ref([]);
    const repo = ref(null);
    const cargando = ref(true);
    const avionTop = ref(0);
    const lista = ref(null);

    const completados = computed(() => etapas.value.filter(e => e.estado === 'completado').length);
    const actual = computed(() => {
      const i = etapas.value.findIndex(e => e.estado === 'disponible');
      return i >= 0 ? i : etapas.value.length - 1;
    });
    const proxima = computed(() => etapas.value[actual.value]);
    const obtenidas = computed(() => insignias.value.filter(i => i.obtenida).length);

    function posicion(indice) {
      const elementos = lista.value ? lista.value.querySelectorAll('.escala') : [];
      const el = elementos[indice];
      return el ? el.offsetTop + el.offsetHeight / 2 - 18 : 0;
    }

    onMounted(async () => {
      try {
        const [m, ins] = await Promise.all([api.mapa(estado.alumnoId), api.insignias(estado.alumnoId)]);
        etapas.value = m;
        insignias.value = ins;
      } catch (e) { mostrarError(e); } finally { cargando.value = false; }

      // El avión parte de la última posición guardada y se desplaza con una animación hasta la escala actual
      await nextTick();
      let anterior = actual.value;
      try { anterior = Number(localStorage.getItem(CLAVE_AVION) ?? actual.value); } catch { /* nada */ }
      avionTop.value = posicion(Math.min(anterior, etapas.value.length - 1));
      requestAnimationFrame(() => setTimeout(() => { avionTop.value = posicion(actual.value); }, 150));
      try { localStorage.setItem(CLAVE_AVION, String(actual.value)); } catch { /* nada */ }

      api.repo(estado.alumnoId).then(r => { repo.value = r; }).catch(() => {});
    });

    const textoEstado = e => {
      if (e.estado === 'completado') return 'completado';
      if (e.estado === 'bloqueado') return 'bloqueado';
      if (e.tipo === 'checkpoint') return 'bitácora';
      return e.tareasTotal ? `${e.tareasHechas}/${e.tareasTotal} tareas` : 'disponible';
    };
    const enlace = e => e.estado === 'bloqueado' ? null : `#/desafio/${encodeURIComponent(e.id)}`;

    return { etapas, insignias, repo, cargando, avionTop, lista, completados, proxima, obtenidas, textoEstado, enlace, estado };
  },
  template: `
    <div v-if="cargando" class="cargando">Cargando el plan de vuelo…</div>
    <template v-else>
      <div class="tablero">
        <div class="dato">
          <div class="nombre">Escalas completadas</div>
          <div class="valor">{{ completados }}/{{ etapas.length }}</div>
          <div class="progreso"><span :style="{ width: (etapas.length ? completados * 100 / etapas.length : 0) + '%' }"></span></div>
        </div>
        <div class="dato">
          <div class="nombre">Insignias</div>
          <div class="valor">{{ obtenidas }}/{{ insignias.length }}</div>
        </div>
        <div class="dato">
          <div class="nombre">Próxima escala</div>
          <div class="valor" v-if="proxima"><a :href="'#/desafio/' + proxima.id" style="text-decoration:none">{{ proxima.codigo }}</a></div>
          <div class="suave" v-if="proxima" style="font-size:.85rem">{{ proxima.titulo }}</div>
        </div>
        <div class="dato">
          <div class="nombre">Repositorio</div>
          <div v-if="!repo" class="suave">revisando…</div>
          <div v-else-if="!repo.esRepo" class="suave" style="font-size:.85rem">{{ repo.mensaje }}</div>
          <template v-else>
            <div class="valor" :style="{ color: repo.sinCommitear || repo.sinPushear ? 'var(--ambar)' : 'var(--verde)' }">
              <Icono :nombre="repo.sinCommitear || repo.sinPushear ? 'triangle-alert' : 'circle-check'" /> <span class="mono" style="font-size:1rem">{{ repo.rama }}</span>
            </div>
            <div class="suave" style="font-size:.85rem">{{ repo.mensaje }}</div>
          </template>
        </div>
      </div>

      <div class="plan">
        <section>
          <h1 style="margin-top:0">Plan de vuelo</h1>
          <div class="tarjeta como-avanzar">
            <h3><Icono nombre="compass" /> Cómo avanzar en el TP</h3>
            <p>Cada escala es un desafío. Para completarla:</p>
            <ol>
              <li>Leer la <strong>misión</strong> y el <strong>repaso</strong>, con sus tips y las lecturas para investigar.</li>
              <li>Realizar el <strong>paso a paso</strong>, intentando cada paso antes de abrir la solución. Se recomienda colocar
                breakpoints para seguir el flujo del código.</li>
              <li>Marcar en el <strong>checklist</strong> cada tarea a medida que se cumple.</li>
              <li><strong>Verificar</strong>, hacer <strong>commit</strong> y <strong>push</strong>.</li>
              <li>Responder el <strong>cuestionario</strong>.</li>
              <li><strong>Completar la escala</strong>: se habilita con el checklist completo y el cuestionario aprobado, y
                desbloquea la siguiente.</li>
            </ol>
          </div>
          <div class="ruta-vuelo" ref="lista">
            <span class="avion" :style="{ top: avionTop + 'px' }" aria-hidden="true"><Icono nombre="plane" /></span>
            <component :is="enlace(e) ? 'a' : 'div'" v-for="e in etapas" :key="e.id" :href="enlace(e)"
                class="escala" :class="[e.estado, e.tipo]" :aria-disabled="e.estado === 'bloqueado'">
              <span class="codigo">{{ e.codigo }}</span>
              <div>
                <h3>{{ e.tipo === 'desafio' ? 'Desafío ' + e.numero + ' · ' : '' }}{{ e.titulo }}</h3>
                <p>{{ e.subtitulo }}</p>
                <div class="fila" style="gap:.3rem">
                  <span class="chip" v-for="t in e.temas" :key="t.numero">Tema {{ t.numero }}</span>
                  <span class="chip ambar" v-if="e.minutosEstimados">~{{ e.minutosEstimados }} min</span>
                  <span class="chip verde" v-if="e.verificado === true"><Icono nombre="check" /> verificador</span>
                  <span class="chip rojo" v-if="e.verificado === false"><Icono nombre="x" /> verificador</span>
                </div>
              </div>
              <span class="estado"><Icono v-if="e.estado === 'completado'" nombre="circle-check" /><Icono v-else-if="e.estado === 'bloqueado'" nombre="lock" /> {{ textoEstado(e) }}</span>
            </component>
          </div>
        </section>

        <aside class="lateral">
          <div class="tarjeta">
            <h3>Insignias</h3>
            <div class="mini-insignias">
              <span v-for="i in insignias" :key="i.id" :class="{ si: i.obtenida }" :title="i.nombre + ': ' + i.descripcion"><Icono :nombre="i.icono" /></span>
            </div>
            <p style="margin:.6rem 0 0"><a href="#/insignias">ver todas ▸</a></p>
          </div>
        </aside>
      </div>
    </template>
  `
};

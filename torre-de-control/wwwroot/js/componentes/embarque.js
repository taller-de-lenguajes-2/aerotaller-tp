import { ref, reactive, computed, onMounted } from 'vue';
import { api } from '../api.js';
import { estado, elegirAlumno, refrescarResumen, avisar, mostrarError } from '../estado.js';

// La "tarjeta de embarque": elegir o crear el perfil con el que se guarda el progreso en progreso.db
export default {
  setup() {
    const alumnos = ref([]);
    const editando = computed(() => !!estado.alumnoId && !!estado.resumen);
    const form = reactive({ nombre: '', rutaProyecto: '', modoLibre: false });
    const error = ref(null);
    const guardando = ref(false);

    onMounted(async () => {
      try {
        alumnos.value = await api.alumnos();
        if (editando.value) {
          const a = estado.resumen.alumno;
          Object.assign(form, { nombre: a.nombre, rutaProyecto: a.rutaProyecto || '', modoLibre: a.modoLibre });
        } else {
          form.rutaProyecto = (await api.sugerencias()).rutaProyecto;
        }
      } catch (e) { mostrarError(e); }
    });

    const iniciales = computed(() => (form.nombre || 'NOMBRE').trim().split(/\s+/).map(p => p[0]).join('').slice(0, 3).toUpperCase());
    const asiento = computed(() => {
      const n = [...(form.nombre || '')].reduce((s, c) => s + c.charCodeAt(0), 0);
      return form.nombre.trim() ? `${(n % 30) + 1}${'ABCDEF'[n % 6]}` : '—';
    });

    async function guardar() {
      error.value = null;
      if (!form.nombre.trim()) { error.value = 'El nombre es obligatorio.'; return; }
      guardando.value = true;
      try {
        if (editando.value) {
          await api.actualizarAlumno(estado.alumnoId, { ...form });
          avisar('Tarjeta de embarque actualizada.', { icono: '🎫' });
        } else {
          const nuevo = await api.crearAlumno({ ...form });
          elegirAlumno(nuevo.id);
          avisar(`Tarjeta de embarque creada: ${nuevo.nombre}.`, { icono: '🎫' });
        }
        await refrescarResumen();
        location.hash = '#/plan';
      } catch (e) { error.value = e.message; } finally { guardando.value = false; }
    }

    function elegir(a) {
      elegirAlumno(a.id);
      location.hash = '#/plan';
    }

    return { alumnos, form, guardar, elegir, error, guardando, editando, iniciales, asiento };
  },
  template: `
    <div class="embarque">
      <section>
        <h1>{{ editando ? 'Tarjeta de embarque' : 'Check-in' }}</h1>
        <p class="suave" v-if="!editando">
          El progreso (tareas, cuestionarios, bitácora e insignias) se guarda en <code>torre-de-control/progreso.db</code>,
          en esta computadora. Si la máquina es compartida, cada persona usa su propia tarjeta.
        </p>

        <div class="boarding">
          <div class="principal">
            <div class="cabecera"><strong>✈ AEROTALLER</strong><span class="mono">TARJETA DE EMBARQUE</span></div>
            <div class="ruta"><span>TL2</span><span class="linea"></span><span>{{ iniciales }}</span></div>
            <form @submit.prevent="guardar">
              <label class="campo" for="nombre">Pasajero (nombre y apellido)</label>
              <input id="nombre" type="text" v-model="form.nombre" autocomplete="name" placeholder="Ana Gómez">
              <label class="campo" for="ruta">Carpeta del repositorio (la que contiene AeroTaller/)</label>
              <input id="ruta" type="text" v-model="form.rutaProyecto" class="mono">
              <p class="suave" style="font-size:.85rem;margin:.3rem 0 0">La usan el botón Verificar y el estado de git. Si la Torre se ejecuta desde el repositorio clonado, no es necesario modificarla.</p>
              <label class="fila" style="margin-top:.9rem;font-size:.92rem">
                <input type="checkbox" v-model="form.modoLibre"> Modo libre: todos los desafíos desbloqueados (para repasar)
              </label>
              <div class="error-caja" v-if="error" style="margin-top:.8rem">{{ error }}</div>
              <p style="margin-top:1.2rem">
                <button class="boton ambar" type="submit" :disabled="guardando">{{ editando ? 'Guardar cambios' : 'Embarcar ✈' }}</button>
                <a v-if="editando" href="#/plan" style="margin-left:1rem">volver al plan</a>
              </p>
            </form>
          </div>
          <aside class="talon">
            <dl>
              <dt>Vuelo</dt><dd class="mono">AT-TP2</dd>
              <dt>Puerta</dt><dd class="mono">5200</dd>
              <dt>Asiento</dt><dd class="mono">{{ asiento }}</dd>
              <dt>Destino</dt><dd>Parcial ✓</dd>
            </dl>
          </aside>
        </div>
      </section>

      <section v-if="!editando">
        <h2>Tarjetas existentes</h2>
        <p class="suave" v-if="alumnos.length === 0">Todavía no hay tarjetas en esta computadora.</p>
        <div class="perfiles">
          <button type="button" class="perfil" v-for="a in alumnos" :key="a.id" @click="elegir(a)">
            <span><strong>{{ a.nombre }}</strong></span>
            <span>continuar ▸</span>
          </button>
        </div>
      </section>
    </div>
  `
};

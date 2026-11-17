import { ref, computed, onMounted } from 'vue';
import { api } from '../api.js';
import { estado, avisar, mostrarError } from '../estado.js';

// Panel "Base de datos": muestra el estado de AeroTaller/aerotaller.db en el repositorio del alumno y la crea
// (o la vuelve a crear) ejecutando AeroTaller/aerotaller.sql. Se usa en los desafíos que lo requieren y en el manual.
export default {
  setup() {
    const base = ref(null);
    const creando = ref(false);
    const resultado = ref(null);
    const confirmando = ref(false);

    async function cargar() {
      try { base.value = await api.base(estado.alumnoId); } catch (e) { mostrarError(e); }
    }
    onMounted(cargar);

    async function crear() {
      if (base.value?.existe && !confirmando.value) { confirmando.value = true; return; }
      confirmando.value = false;
      creando.value = true;
      resultado.value = null;
      try {
        const r = await api.crearBase(estado.alumnoId);
        resultado.value = r;
        base.value = r.estado;
        avisar(r.mensaje, { icono: r.ok ? 'database' : 'triangle-alert', tipo: r.ok ? '' : 'error' });
      } catch (e) { mostrarError(e); } finally { creando.value = false; }
    }

    const tamano = computed(() => {
      const b = base.value?.bytes || 0;
      return b < 1024 ? `${b} bytes` : `${(b / 1024).toFixed(0)} KB`;
    });
    const fecha = computed(() => base.value?.modificada ? new Date(base.value.modificada).toLocaleString('es-AR') : '');

    return { base, creando, resultado, confirmando, crear, cargar, tamano, fecha };
  },
  template: `
    <Seccion id="base-de-datos" titulo="Base de datos" icono="database">
      <p>La plantilla trae el script completo <code>AeroTaller/aerotaller.sql</code> (tablas, claves, índices y datos de ejemplo).
        Este botón lo ejecuta y genera <code>aerotaller.db</code> en la carpeta <code>AeroTaller/</code>, que es la ubicación desde la que la aplicación la abre.</p>

      <div v-if="base" class="caja" :class="base.existe && !base.problema ? 'nota' : 'cuidado'" style="margin:.8rem 0">
        <template v-if="!base.hayScript">
          No se encontró <code>aerotaller.sql</code> en <span class="mono">{{ base.carpeta }}</span>. Revisar la carpeta del repositorio en la tarjeta de embarque.
        </template>
        <template v-else-if="!base.existe">
          Todavía no existe <span class="mono">{{ base.archivo }}</span>.
        </template>
        <template v-else>
          <div><strong>{{ base.archivo }}</strong></div>
          <div class="suave">{{ tamano }} · modificada {{ fecha }}</div>
          <div v-if="base.problema" style="margin-top:.4rem">{{ base.problema }}</div>
          <div class="fila" style="flex-wrap:wrap;gap:.4rem;margin-top:.5rem" v-if="base.tablas.length">
            <span class="chip" v-for="t in base.tablas" :key="t.nombre">{{ t.nombre }} · {{ t.filas }}</span>
          </div>
        </template>
      </div>

      <div class="fila" style="flex-wrap:wrap">
        <button class="boton" type="button" @click="crear" :disabled="creando || (base && !base.hayScript)"
                :class="{ ambar: confirmando }">
          <span v-if="creando" class="spinner"></span>
          {{ creando ? 'Ejecutando el script…' : confirmando ? 'Confirmar: se perderán los cambios realizados en la base'
             : base && base.existe ? 'Recrear base de datos' : 'Crear base de datos' }}
        </button>
        <button v-if="confirmando" class="boton secundario" type="button" @click="confirmando = false">Cancelar</button>
        <button v-else class="boton secundario chico" type="button" @click="cargar">Actualizar</button>
      </div>
      <p class="suave" style="margin-bottom:0">Recrear restaura los datos del script, con las fechas de los vuelos calculadas a partir de hoy.
        No es necesario detener la aplicación, pero DB Browser no debe tener cambios sin guardar.</p>
      <div v-if="resultado && !resultado.ok" class="error-caja" style="margin-top:.8rem;white-space:pre-wrap">{{ resultado.mensaje }}</div>
    </Seccion>
  `
};

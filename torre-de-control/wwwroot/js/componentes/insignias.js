import { ref, computed, onMounted } from 'vue';
import { api } from '../api.js';
import { estado, mostrarError } from '../estado.js';

export default {
  setup() {
    const insignias = ref([]);
    onMounted(async () => {
      try { insignias.value = await api.insignias(estado.alumnoId); } catch (e) { mostrarError(e); }
    });
    const obtenidas = computed(() => insignias.value.filter(i => i.obtenida));
    const fecha = f => new Date(f).toLocaleDateString('es-AR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
    return { insignias, obtenidas, fecha };
  },
  template: `
    <h1>Insignias</h1>
    <p class="suave">{{ obtenidas.length }} de {{ insignias.length }}. Se ganan completando escalas, aprobando al primer intento,
      respondiendo correctamente las preguntas del depurador, abriendo lecturas, verificando y manteniendo el repositorio actualizado.</p>
    <div class="insignias">
      <div class="insignia" v-for="i in insignias" :key="i.id" :class="i.obtenida ? 'obtenida' : 'bloqueada'">
        <span class="icono"><Icono :nombre="i.icono" /></span>
        <h3>{{ i.nombre }}</h3>
        <p>{{ i.descripcion }}</p>
        <p v-if="i.obtenida" class="mono" style="margin-top:.5rem;font-size:.75rem">{{ fecha(i.fecha) }}</p>
      </div>
    </div>
  `
};

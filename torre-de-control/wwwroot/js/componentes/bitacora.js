import { ref, reactive, computed, onMounted } from 'vue';
import { api } from '../api.js';
import { estado, avisar, festejarInsignias, mostrarError } from '../estado.js';

const ICONOS = { Hice: 'circle-check', Falta: 'hourglass', Duda: 'message-circle-question', Nota: 'notebook-pen' };

// La bitácora: notas registradas en los checkpoints o en cualquier otro momento. Las dudas pueden copiarse para la consulta.
export default {
  setup() {
    const notas = ref([]);
    const filtro = ref('Todas');
    const nueva = reactive({ tipo: 'Duda', texto: '' });

    async function cargar() {
      try { notas.value = await api.bitacora(estado.alumnoId); } catch (e) { mostrarError(e); }
    }
    onMounted(cargar);

    const visibles = computed(() => filtro.value === 'Todas' ? notas.value : notas.value.filter(n => n.tipo === filtro.value));
    const dudasAbiertas = computed(() => notas.value.filter(n => n.tipo === 'Duda' && !n.resuelta));

    async function agregar() {
      if (!nueva.texto.trim()) return;
      try {
        const r = await api.agregarNota(estado.alumnoId, { tipo: nueva.tipo, texto: nueva.texto, desafioId: null });
        nueva.texto = '';
        festejarInsignias(r.nuevasInsignias);
        await cargar();
      } catch (e) { mostrarError(e); }
    }
    async function resolver(n) {
      try { await api.resolverNota(estado.alumnoId, n.id, !n.resuelta); n.resuelta = !n.resuelta; } catch (e) { mostrarError(e); }
    }
    async function borrar(n) {
      if (!confirm('¿Borrar esta nota?')) return;
      try { await api.borrarNota(estado.alumnoId, n.id); await cargar(); } catch (e) { mostrarError(e); }
    }
    async function copiarDudas() {
      const texto = dudasAbiertas.value.map((n, i) => `${i + 1}. ${n.texto}${n.desafioId ? ` (${n.desafioId})` : ''}`).join('\n');
      try { await navigator.clipboard.writeText(texto); avisar('Dudas copiadas al portapapeles para el canal de consultas.', { icono: 'clipboard' }); } catch { /* nada */ }
    }
    const fecha = f => new Date(f).toLocaleString('es-AR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });

    return { notas, filtro, nueva, visibles, dudasAbiertas, agregar, resolver, borrar, copiarDudas, fecha, ICONOS };
  },
  template: `
    <h1>Bitácora</h1>
    <p class="suave">Registro de lo realizado, lo pendiente y las dudas. Antes de la consulta, las dudas abiertas pueden copiarse con un botón.</p>

    <form class="tarjeta" @submit.prevent="agregar" style="margin-bottom:1.2rem">
      <div class="fila">
        <select v-model="nueva.tipo" style="padding:.5rem;border-radius:8px;border:1px solid var(--linea);background:var(--papel);color:var(--tinta)">
          <option>Duda</option><option>Hice</option><option>Falta</option><option>Nota</option>
        </select>
        <input type="text" v-model="nueva.texto" placeholder="Ej.: por qué las listas no se conservan después del POST" style="flex:1;min-width:240px">
        <button class="boton" type="submit">Anotar</button>
      </div>
    </form>

    <div class="fila" style="margin-bottom:1rem">
      <button v-for="f in ['Todas', 'Duda', 'Hice', 'Falta', 'Nota']" :key="f" type="button" class="boton chico"
              :class="{ secundario: filtro !== f }" @click="filtro = f">{{ f }}</button>
      <button class="boton ambar chico" type="button" style="margin-left:auto" @click="copiarDudas" :disabled="!dudasAbiertas.length">
        <Icono nombre="clipboard" /> Copiar {{ dudasAbiertas.length }} duda(s) abiertas</button>
    </div>

    <p v-if="!visibles.length" class="suave">No hay notas todavía.</p>
    <div class="notas">
      <div class="nota-item" v-for="n in visibles" :key="n.id" :class="{ resuelta: n.resuelta }">
        <span class="icono-nota" :title="n.tipo"><Icono :nombre="ICONOS[n.tipo]" /></span>
        <div>
          <div class="texto">{{ n.texto }}</div>
          <div class="suave mono" style="font-size:.75rem">{{ fecha(n.fecha) }}{{ n.desafioId ? ' · ' + n.desafioId : '' }}</div>
        </div>
        <div class="fila" style="gap:.4rem">
          <button v-if="n.tipo === 'Duda' || n.tipo === 'Falta'" class="boton secundario chico" type="button" @click="resolver(n)">
            <template v-if="n.resuelta">reabrir</template><template v-else><Icono nombre="check" /> resuelta</template></button>
          <button class="boton secundario chico" type="button" @click="borrar(n)" title="Borrar" aria-label="Borrar"><Icono nombre="trash-2" /></button>
        </div>
      </div>
    </div>
  `
};

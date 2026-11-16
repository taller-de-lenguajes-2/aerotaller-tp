import { ref, reactive, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { api } from '../api.js';
import { estado, avisar, festejarInsignias, mostrarError, despegar, refrescarResumen } from '../estado.js';
import { secciones as partirSecciones } from '../markdown.js';

// Un desafío (o checkpoint, o el simulacro): el contenido en Markdown + las partes interactivas
// (checklist, lecturas, verificación, commit, cuestionario y "despegar").
export default {
  props: { id: { type: String, required: true } },
  setup(props) {
    const d = ref(null);
    const error = ref(null);
    const secciones = ref([]);
    const revelados = ref(1);
    const respuestas = reactive({});
    const correccion = ref(null);         // resultado del último envío del cuestionario
    const enviandoQuiz = ref(false);
    const verificando = ref(false);
    const verificacion = ref(null);
    const repo = ref(null);
    const faltantes = ref(null);
    const completando = ref(false);
    const cp = reactive({ hice: '', falta: '', dudas: '' });
    const mapa = ref([]);
    const ahora = ref(Date.now());
    let reloj = null;

    const INTERVALO_COMMIT = 20 * 60;   // segundos
    const clavePasos = () => `torre.pasos.${estado.alumnoId}.${props.id}`;

    async function cargar() {
      try {
        d.value = await api.desafio(estado.alumnoId, props.id);
        secciones.value = partirSecciones(d.value.markdown);
        try { revelados.value = Number(localStorage.getItem(clavePasos())) || 1; } catch { revelados.value = 1; }
        if (d.value.progreso.estado === 'completado') revelados.value = 999;
        for (const r of d.value.progreso.ultimasRespuestas || []) respuestas[r.preguntaId] = r.opcionId;
        if (d.value.tipo !== 'desafio') mapa.value = await api.mapa(estado.alumnoId);
        api.repo(estado.alumnoId).then(r => { repo.value = r; }).catch(() => {});
      } catch (e) {
        error.value = e;
      }
    }

    onMounted(() => {
      cargar();
      reloj = setInterval(() => { ahora.value = Date.now(); }, 1000);
    });
    onBeforeUnmount(() => clearInterval(reloj));

    const completado = computed(() => d.value?.progreso.estado === 'completado');
    const pasoAPaso = computed(() => secciones.value.find(s => s.pasos));
    const totalPasos = computed(() => pasoAPaso.value?.pasos.length || 0);
    const tareasHechas = computed(() => d.value ? d.value.tareas.filter(t => d.value.progreso.tareasMarcadas.includes(t.id)).length : 0);
    const quizAprobado = computed(() => d.value?.progreso.quizAprobado);
    const puedeDespegar = computed(() => d.value && tareasHechas.value === d.value.tareas.length && (d.value.quiz.length === 0 || quizAprobado.value));

    const indice = computed(() => {
      if (!d.value) return [];
      const fijas = [];
      if (d.value.tipo === 'checkpoint') {
        fijas.push({ id: 'resumen', titulo: '📊 Tu progreso' });
        if (d.value.lecturas.length) fijas.push({ id: 'investigar', titulo: '🔎 Para investigar' });
        fijas.push({ id: 'bitacora', titulo: '📓 Bitácora' });
      } else {
        if (d.value.lecturas.length) fijas.push({ id: 'investigar', titulo: '🔎 Para investigar' });
        if (d.value.tareas.length) fijas.push({ id: 'checklist', titulo: '✅ Checklist' });
        if (d.value.numero !== null && d.value.numero !== undefined) fijas.push({ id: 'verificacion', titulo: '🛠️ Verificación' });
        if (d.value.commit) fijas.push({ id: 'commit', titulo: '📝 Commit y push' });
        if (d.value.quiz.length) fijas.push({ id: 'cuestionario', titulo: '❓ Cuestionario' });
        fijas.push({ id: 'despegar', titulo: '🛫 Despegar' });
      }
      return [...secciones.value.map(s => ({ id: s.id, titulo: s.titulo })), ...fijas];
    });

    // ── Paso a paso ──
    function revelar(todos = false) {
      revelados.value = todos ? totalPasos.value : Math.min(revelados.value + 1, totalPasos.value);
      try { localStorage.setItem(clavePasos(), String(revelados.value)); } catch { /* nada */ }
    }

    // ── Checklist ──
    async function marcar(tarea, marcada) {
      const lista = d.value.progreso.tareasMarcadas;
      const anterior = [...lista];
      if (marcada && !lista.includes(tarea.id)) lista.push(tarea.id);
      if (!marcada) lista.splice(lista.indexOf(tarea.id), 1);
      try {
        await api.marcarTarea(estado.alumnoId, props.id, tarea.id, marcada);
        if (marcada && tareasHechas.value === d.value.tareas.length) avisar('Checklist completa.', { icono: '✅' });
      } catch (e) {
        d.value.progreso.tareasMarcadas = anterior;
        mostrarError(e);
      }
    }

    // ── Lecturas ──
    async function abrirLectura(lectura) {
      if (d.value.progreso.lecturasAbiertas.includes(lectura.id)) return;
      d.value.progreso.lecturasAbiertas.push(lectura.id);
      try {
        const r = await api.abrirLectura(estado.alumnoId, props.id, lectura.id);
        festejarInsignias(r.nuevasInsignias);
      } catch (e) { mostrarError(e); }
    }

    // ── Cuestionario ──
    const respondidas = computed(() => d.value ? d.value.quiz.filter(p => respuestas[p.id]).length : 0);
    const devolucion = idPregunta => correccion.value?.detalle.find(x => x.preguntaId === idPregunta);
    async function enviarQuiz() {
      enviandoQuiz.value = true;
      try {
        const r = await api.responder(estado.alumnoId, props.id, { ...respuestas });
        correccion.value = r;
        d.value.progreso.intentosQuiz = r.intento;
        if (r.aprobado) {
          d.value.progreso.quizAprobado = true;
          avisar(`Cuestionario aprobado (${r.correctas}/${r.total}) en el intento ${r.intento}.`, { icono: '🎯' });
        } else {
          avisar(`${r.correctas}/${r.total}. Revisar las pistas y volver a intentar.`, { icono: '🔁' });
        }
        festejarInsignias(r.nuevasInsignias);
      } catch (e) { mostrarError(e); } finally { enviandoQuiz.value = false; }
    }

    // ── Verificación ──
    async function verificar() {
      verificando.value = true;
      verificacion.value = null;
      try {
        const r = await api.verificar(estado.alumnoId, props.id);
        verificacion.value = r;
        if (r.repo) repo.value = r.repo;
        if (r.ejecutado) {
          d.value.progreso.verificacion = { aprobado: r.aprobado, fecha: new Date().toISOString() };
          avisar(r.aprobado ? 'Todos los chequeos aprobados.' : 'Hay chequeos en rojo: ver el detalle.', { icono: r.aprobado ? '🟢' : '🔴' });
        }
        festejarInsignias(r.nuevasInsignias);
      } catch (e) { mostrarError(e); } finally { verificando.value = false; }
    }
    async function revisarRepo() {
      try { repo.value = await api.repo(estado.alumnoId); } catch (e) { mostrarError(e); }
    }

    // ── Completar ──
    async function completar() {
      faltantes.value = null;
      completando.value = true;
      try {
        const r = await api.completar(estado.alumnoId, props.id);
        terminar(r);
      } catch (e) {
        faltantes.value = { mensaje: e.message, lista: e.faltantes || [] };
      } finally { completando.value = false; }
    }

    async function enviarCheckpoint() {
      faltantes.value = null;
      completando.value = true;
      try {
        const r = await api.checkpoint(estado.alumnoId, props.id, { ...cp });
        terminar(r);
      } catch (e) {
        faltantes.value = { mensaje: e.message, lista: e.faltantes || [] };
      } finally { completando.value = false; }
    }

    function terminar(r) {
      d.value.progreso.estado = 'completado';
      despegar(d.value.titulo, d.value.siguiente);
      setTimeout(() => festejarInsignias(r.nuevasInsignias), 1800);
      refrescarResumen();
      setTimeout(() => { location.hash = d.value.siguiente ? `#/desafio/${d.value.siguiente}` : '#/plan'; }, 2300);
    }

    // ── Checkpoint: resumen automático ──
    const hechos = computed(() => mapa.value.filter(e => e.estado === 'completado' && e.tipo === 'desafio'));
    const pendientes = computed(() => mapa.value.filter(e => e.estado !== 'completado' && e.tipo !== 'checkpoint' && e.orden > (d.value?.orden || 0)));
    const sinVerificar = computed(() => hechos.value.filter(e => e.verificado !== true));

    // ── Simulacro: cronómetro ──
    const cronometro = computed(() => {
      if (!d.value || d.value.tipo !== 'simulacro' || !d.value.progreso.iniciado) return null;
      const inicio = new Date(d.value.progreso.iniciado).getTime();
      const fin = d.value.progreso.completado ? new Date(d.value.progreso.completado).getTime() : ahora.value;
      const usados = Math.max(0, Math.floor((fin - inicio) / 1000));
      const limite = (d.value.minutosLimite || 150) * 60;
      const resto = limite - usados;
      const fmt = s => `${String(Math.floor(Math.abs(s) / 3600)).padStart(2, '0')}:${String(Math.floor(Math.abs(s) % 3600 / 60)).padStart(2, '0')}:${String(Math.abs(s) % 60).padStart(2, '0')}`;
      // Regla del simulacro: commit y push cada 20 minutos
      const proximoCommit = INTERVALO_COMMIT - (usados % INTERVALO_COMMIT);
      return {
        usados: fmt(usados), resto: (resto < 0 ? '−' : '') + fmt(resto), porcentaje: Math.min(100, usados * 100 / limite), excedido: resto < 0,
        proximoCommit: fmt(proximoCommit).slice(3), tramo: Math.floor(usados / INTERVALO_COMMIT)
      };
    });

    // Aviso cada vez que se cumple un tramo de 20 minutos (mientras la página está abierta y el simulacro sigue en curso)
    watch(() => cronometro.value?.tramo, (tramo, anterior) => {
      if (anterior === undefined || tramo === undefined || tramo <= anterior || completado.value) return;
      avisar(`Se cumplieron ${tramo * 20} minutos: commit y push.`, { icono: '🌿', titulo: 'Commit del simulacro', duracion: 15000 });
    });

    const completitud = computed(() => d.value && d.value.tareas.length ? Math.round(tareasHechas.value * 100 / d.value.tareas.length) : 0);

    const comandos = computed(() => d.value?.commit
      ? `git status\ngit add -A\ngit commit -m "${d.value.commit.mensaje}"\ngit push`
      : '');

    const ir = idSeccion => document.getElementById(idSeccion)?.scrollIntoView({ behavior: 'smooth' });

    async function copiar(texto) {
      try { await navigator.clipboard.writeText(texto); avisar('Copiado al portapapeles.', { icono: '📋', duracion: 2000 }); } catch { /* nada */ }
    }

    return {
      d, error, secciones, revelados, totalPasos, indice, completado, tareasHechas, quizAprobado, puedeDespegar,
      revelar, marcar, abrirLectura, respuestas, respondidas, devolucion, enviarQuiz, enviandoQuiz, correccion,
      verificar, verificando, verificacion, repo, revisarRepo, completar, completando, faltantes,
      cp, enviarCheckpoint, hechos, pendientes, sinVerificar, cronometro, completitud, comandos, copiar, ir
    };
  },
  template: `
    <div v-if="error" class="tarjeta">
      <h1>🔒 {{ error.message }}</h1>
      <p><a href="#/plan">← Volver al plan de vuelo</a></p>
    </div>
    <div v-else-if="!d" class="cargando">Cargando la escala…</div>

    <div v-else class="desafio">
      <nav class="indice no-imprimir" aria-label="Secciones">
        <a href="#/plan">← Plan de vuelo</a>
        <a v-for="s in indice" :key="s.id" :href="'#/desafio/' + d.id" @click.prevent="ir(s.id)">{{ s.titulo }}</a>
        <div class="requisitos" v-if="d.tipo !== 'checkpoint'">
          <div><span>{{ tareasHechas === d.tareas.length ? '✅' : '⬜' }}</span> Tareas {{ tareasHechas }}/{{ d.tareas.length }}</div>
          <div v-if="d.quiz.length"><span>{{ quizAprobado ? '✅' : '⬜' }}</span> Cuestionario</div>
          <div v-if="d.numero !== null && d.numero !== undefined">
            <span>{{ d.progreso.verificacion ? (d.progreso.verificacion.aprobado ? '🟢' : '🔴') : '⚪' }}</span> Verificador
          </div>
          <div v-if="completado"><span>🛫</span> <strong>Completado</strong></div>
        </div>
      </nav>

      <article>
        <header class="encabezado">
          <span class="codigo-grande">{{ d.codigo }}</span>
          <div>
            <p class="mono" style="margin:0;font-size:.85rem">{{ d.tipo === 'desafio' ? 'DESAFÍO ' + d.numero : d.tipo === 'checkpoint' ? 'CHECKPOINT' : 'SIMULACRO DE PARCIAL' }} · ~{{ d.minutosEstimados }} min</p>
            <h1>{{ d.titulo }}</h1>
            <p>{{ d.subtitulo }}</p>
            <div class="fila" style="gap:.3rem">
              <a class="chip" v-for="t in d.temas" :key="t.numero" :href="t.url" target="_blank" rel="noopener">📖 Tema {{ t.numero }} · {{ t.titulo }}</a>
              <span class="chip verde" v-if="completado">✔ completado</span>
            </div>
          </div>
        </header>

        <section class="seccion" v-if="cronometro">
          <h2>⏱️ Cronómetro y completitud</h2>
          <div class="fila" style="justify-content:space-between">
            <div><div class="suave">Tiempo usado</div><div class="cronometro">{{ cronometro.usados }}</div></div>
            <div style="text-align:right"><div class="suave">{{ cronometro.excedido ? 'Tiempo excedido' : 'Tiempo restante' }}</div>
              <div class="cronometro" :style="{ color: cronometro.excedido ? 'var(--rojo)' : 'var(--verde)' }">{{ cronometro.resto }}</div></div>
          </div>
          <div class="progreso"><span :style="{ width: cronometro.porcentaje + '%', background: cronometro.excedido ? 'var(--rojo)' : 'var(--ambar)' }"></span></div>
          <div class="fila" style="justify-content:space-between;margin-top:1rem" v-if="!d.progreso.completado">
            <div><div class="suave">Próximo commit y push (cada 20 minutos)</div><div class="cronometro" style="color:var(--azul)">{{ cronometro.proximoCommit }}</div></div>
            <div class="suave" style="text-align:right">commits sugeridos hasta ahora: {{ cronometro.tramo }}</div>
          </div>
          <div class="fila" style="justify-content:space-between;margin-top:1rem">
            <div><div class="suave">Completitud (tareas cumplidas)</div><div class="cronometro">{{ completitud }}%</div></div>
            <div class="suave" style="text-align:right">{{ tareasHechas }} de {{ d.tareas.length }} tareas</div>
          </div>
          <div class="progreso"><span :style="{ width: completitud + '%', background: 'var(--verde)' }"></span></div>
          <p class="suave" style="margin-bottom:0">Se cuenta desde la primera apertura del simulacro. Como en el parcial, sin consultar la solución.</p>
        </section>

        <section class="seccion" v-for="s in secciones" :key="s.id" :id="s.id">
          <h2>{{ s.titulo }}</h2>
          <div v-html="s.html"></div>
          <template v-if="s.pasos">
            <div class="paso" v-for="(p, i) in s.pasos.slice(0, revelados)" :key="i">
              <h3>{{ p.titulo }}</h3>
              <div v-html="p.html"></div>
            </div>
            <div class="paso-acciones no-imprimir" v-if="revelados < s.pasos.length">
              <button class="boton" type="button" @click="revelar()">Siguiente paso ▸</button>
              <button class="boton secundario chico" type="button" @click="revelar(true)">mostrar todos</button>
              <span class="suave">{{ revelados }}/{{ s.pasos.length }}</span>
            </div>
          </template>
        </section>

        <!-- ───── Checkpoint ───── -->
        <template v-if="d.tipo === 'checkpoint'">
          <section class="seccion" id="resumen">
            <h2>📊 Progreso hasta este punto</h2>
            <div class="fila" style="align-items:flex-start;gap:2rem">
              <div style="flex:1;min-width:220px">
                <strong>Desafíos completados</strong>
                <ul><li v-for="e in hechos" :key="e.id">{{ e.codigo }} · {{ e.titulo }}
                  <span v-if="e.verificado === true" class="chip verde">verificador ✔</span>
                  <span v-else class="chip ambar">sin verificar</span></li></ul>
              </div>
              <div style="flex:1;min-width:220px">
                <strong>Próximos desafíos</strong>
                <ul><li v-for="e in pendientes" :key="e.id">{{ e.codigo }} · {{ e.titulo }}</li></ul>
              </div>
            </div>
            <p v-if="sinVerificar.length" class="caja cuidado" style="margin-bottom:0">
              Hay {{ sinVerificar.length }} desafío(s) completados sin el verificador aprobado. Antes de continuar, ejecutar
              <code>dotnet run --project verificador -- 0-{{ hechos.length ? hechos[hechos.length - 1].numero : 0 }}</code>
            </p>
            <p v-if="repo && repo.esRepo" style="margin-bottom:0">Repositorio: <strong>{{ repo.mensaje }}</strong></p>
          </section>
        </template>

        <!-- ───── Para investigar ───── -->
        <section class="seccion" id="investigar" v-if="d.lecturas.length">
          <h2>🔎 Para investigar</h2>
          <p class="suave">Lecturas breves para profundizar, cada una con una pregunta guía.</p>
          <div class="lecturas">
            <div class="lectura" v-for="l in d.lecturas" :key="l.id">
              <span class="icono">{{ d.progreso.lecturasAbiertas.includes(l.id) ? '✅' : '📖' }}</span>
              <div>
                <a :href="l.url" target="_blank" rel="noopener" @click="abrirLectura(l)"><strong>{{ l.titulo }}</strong></a>
                <span class="suave"> · {{ l.fuente }}</span>
                <p>🤔 {{ l.pregunta }}</p>
              </div>
            </div>
          </div>
        </section>

        <!-- ───── Checklist ───── -->
        <section class="seccion" id="checklist" v-if="d.tipo !== 'checkpoint' && d.tareas.length">
          <h2>✅ Checklist</h2>
          <ul class="tareas">
            <li v-for="t in d.tareas" :key="t.id" :class="{ hecha: d.progreso.tareasMarcadas.includes(t.id) }">
              <input type="checkbox" :id="'t-' + t.id" :checked="d.progreso.tareasMarcadas.includes(t.id)" @change="marcar(t, $event.target.checked)">
              <label :for="'t-' + t.id"><span>{{ t.texto }}</span><small v-if="t.ayuda">{{ t.ayuda }}</small></label>
            </li>
          </ul>
        </section>

        <!-- ───── Verificación ───── -->
        <section class="seccion" id="verificacion" v-if="d.tipo !== 'checkpoint' && d.numero !== null && d.numero !== undefined">
          <h2>🛠️ Verificación</h2>
          <p>El verificador compila el proyecto, lo ejecuta con una base <strong>temporal</strong> (el archivo <code>aerotaller.db</code> no se modifica)
            y lo prueba como lo haría una persona. Es el mismo verificador que ejecuta GitHub Classroom en cada push.</p>
          <div class="fila">
            <button class="boton" type="button" @click="verificar" :disabled="verificando">
              <span v-if="verificando" class="spinner"></span>{{ verificando ? 'Verificando…' : 'Verificar desafío ' + d.numero }}
            </button>
            <span class="suave mono" style="font-size:.85rem">dotnet run --project verificador -- {{ d.numero }}</span>
          </div>
          <p class="suave" v-if="verificando">Puede demorar un minuto: compila, inicia la aplicación y le envía pedidos de prueba.</p>
          <div v-if="verificacion">
            <div v-if="!verificacion.ejecutado" class="error-caja" style="margin-top:.8rem;white-space:pre-wrap">{{ verificacion.error }}</div>
            <template v-else>
              <p style="margin:.8rem 0 0"><strong :style="{ color: verificacion.aprobado ? 'var(--verde)' : 'var(--rojo)' }">
                {{ verificacion.aprobado ? '🟢 Todos los chequeos aprobados' : '🔴 ' + verificacion.chequeos.filter(c => !c.ok).length + ' chequeo(s) en rojo' }}</strong></p>
              <ul class="chequeos">
                <li v-for="(c, i) in verificacion.chequeos" :key="i">
                  {{ c.ok ? '✔' : '✘' }} {{ c.nombre }}
                  <div class="detalle" v-if="!c.ok && c.detalle">{{ c.detalle }}</div>
                </li>
              </ul>
            </template>
          </div>
        </section>

        <!-- ───── Commit y push ───── -->
        <section class="seccion" id="commit" v-if="d.commit">
          <h2>📝 Commit y push</h2>
          <p>Antes de completar la escala, registrar el trabajo <strong>y subirlo</strong>. Revisar el diff antes del commit: debe incluir todo lo necesario y nada más.</p>
          <pre><button class="copiar" type="button">copiar</button><code class="hljs">{{ comandos }}</code></pre>
          <p v-if="d.commit.detalle" class="suave">{{ d.commit.detalle }}</p>
          <div class="fila">
            <button class="boton secundario chico" type="button" @click="revisarRepo">Revisar mi repo</button>
            <span v-if="repo">{{ repo.esRepo ? (repo.sinCommitear || repo.sinPushear ? '⚠️ ' : '✅ ') : '' }}{{ repo.mensaje }}</span>
          </div>
          <p class="suave" style="margin-bottom:0">Luego, comprobar en GitHub que el commit esté publicado y que la corrección automática se haya ejecutado.</p>
        </section>

        <!-- ───── Cuestionario ───── -->
        <section class="seccion" id="cuestionario" v-if="d.quiz.length">
          <h2>❓ Cuestionario</h2>
          <p class="suave">Se aprueba con todas las respuestas correctas. Los intentos son ilimitados: ante una respuesta incorrecta se muestra una pista (no la respuesta).
            Las preguntas marcadas con 🔴 se responden observando el depurador.</p>
          <p v-if="quizAprobado" class="chip verde" style="font-size:.9rem">✔ Aprobado</p>
          <div class="pregunta" v-for="(p, n) in d.quiz" :key="p.id">
            <div class="enunciado">{{ n + 1 }}. {{ p.texto }}<span class="etiqueta-depurador" v-if="p.etiquetas.includes('depurador')">🔴 depurador</span></div>
            <pre v-if="p.codigo"><code class="hljs">{{ p.codigo }}</code></pre>
            <label class="opcion" v-for="o in p.opciones" :key="o.id" :class="{ elegida: respuestas[p.id] === o.id }">
              <input type="radio" :name="p.id" :value="o.id" v-model="respuestas[p.id]" :disabled="quizAprobado">
              <span>{{ o.texto }}</span>
            </label>
            <div v-if="devolucion(p.id)" class="devolucion" :class="devolucion(p.id).correcta ? 'bien' : 'mal'">
              {{ devolucion(p.id).correcta ? '✔' : '✘' }} {{ devolucion(p.id).mensaje }}
            </div>
          </div>
          <p v-if="!quizAprobado" style="margin-bottom:0">
            <button class="boton" type="button" @click="enviarQuiz" :disabled="enviandoQuiz || respondidas < d.quiz.length">
              {{ enviandoQuiz ? 'Corrigiendo…' : 'Enviar respuestas' }}</button>
            <span class="suave" style="margin-left:.8rem">{{ respondidas }}/{{ d.quiz.length }} respondidas · intento {{ d.progreso.intentosQuiz + 1 }}</span>
          </p>
        </section>

        <!-- ───── Bitácora del checkpoint ───── -->
        <section class="seccion" id="bitacora" v-if="d.tipo === 'checkpoint'">
          <h2>📓 Bitácora</h2>
          <p class="suave">Registro personal y para la consulta. Queda guardado en la bitácora.</p>
          <form @submit.prevent="enviarCheckpoint" v-if="!completado">
            <label class="campo" for="hice">Trabajo realizado hasta este punto y resultados obtenidos</label>
            <textarea id="hice" v-model="cp.hice"></textarea>
            <label class="campo" for="falta">Pendientes y temas para repasar antes del parcial</label>
            <textarea id="falta" v-model="cp.falta"></textarea>
            <label class="campo" for="dudas">Dudas para llevar a la consulta (opcional)</label>
            <textarea id="dudas" v-model="cp.dudas"></textarea>
            <div class="error-caja" v-if="faltantes" style="margin-top:.8rem">{{ faltantes.mensaje }}<ul><li v-for="f in faltantes.lista" :key="f">{{ f }}</li></ul></div>
            <p><button class="boton ambar" type="submit" :disabled="completando">Guardar y continuar ✈</button></p>
          </form>
          <p v-else>✔ Checkpoint completado. Las notas quedaron en la <a href="#/bitacora">bitácora</a>.</p>
        </section>

        <!-- ───── Despegar ───── -->
        <section class="seccion despegue-seccion" id="despegar" v-if="d.tipo !== 'checkpoint'">
          <h2>🛫 Despegar</h2>
          <template v-if="!completado">
            <p>
              <span class="requisito">{{ tareasHechas === d.tareas.length ? '✅' : '⬜' }} Checklist</span>
              <span class="requisito" v-if="d.quiz.length">{{ quizAprobado ? '✅' : '⬜' }} Cuestionario</span>
            </p>
            <button class="boton ambar" type="button" @click="completar" :disabled="completando">
              {{ puedeDespegar ? 'Marcar como completado y despegar ✈' : 'Marcar como completado' }}</button>
            <div class="error-caja" v-if="faltantes" style="margin-top:1rem;text-align:left">{{ faltantes.mensaje }}<ul><li v-for="f in faltantes.lista" :key="f">{{ f }}</li></ul></div>
          </template>
          <p v-else>✔ Escala completada. <a v-if="d.siguiente" :href="'#/desafio/' + d.siguiente">Siguiente escala ▸</a></p>
        </section>

        <p class="fila no-imprimir" style="justify-content:space-between">
          <a v-if="d.anterior" :href="'#/desafio/' + d.anterior">◂ Anterior</a><span v-else></span>
          <a href="#/plan">Plan de vuelo</a>
        </p>
      </article>
    </div>
  `
};

import { ref, reactive } from 'vue';

// Diagrama de AeroTaller: cada entidad es una caja que se despliega para mostrar sus propiedades,
// vistas como clases (C#) o como tablas (SQL). Las líneas muestran la cardinalidad de cada relación.
// Marcas: pk = clave primaria · fk = clave foránea · rel = propiedad cuyo tipo es otra entidad · unica · calc = calculada

const ENTIDADES = {
  aeropuerto: {
    nombre: 'Aeropuerto', icono: 'map-pin',
    clases: [['Id', 'int'], ['Codigo', 'string'], ['Ciudad', 'string'], ['Nombre', 'string']],
    tablas: [['Id', 'INTEGER', 'pk'], ['Codigo', 'TEXT', 'unica'], ['Ciudad', 'TEXT'], ['Nombre', 'TEXT']]
  },
  vuelo: {
    nombre: 'Vuelo', icono: 'plane',
    clases: [['Id', 'int'], ['Numero', 'string'], ['Origen', 'Aeropuerto', 'rel'], ['Destino', 'Aeropuerto', 'rel'], ['Salida', 'DateTime'],
      ['DuracionMinutos', 'int'], ['Filas', 'int'], ['Precio', 'decimal'], ['Capacidad', 'int', 'calc']],
    tablas: [['Id', 'INTEGER', 'pk'], ['Numero', 'TEXT', 'unica'], ['OrigenId', 'INTEGER', 'fk', 'Aeropuerto'], ['DestinoId', 'INTEGER', 'fk', 'Aeropuerto'],
      ['Salida', 'TEXT'], ['DuracionMinutos', 'INTEGER'], ['Filas', 'INTEGER'], ['Precio', 'REAL']]
  },
  reserva: {
    nombre: 'Reserva', icono: 'ticket',
    clases: [['Id', 'int'], ['Vuelo', 'Vuelo', 'rel'], ['Pasajero', 'Pasajero', 'rel'], ['Asiento', 'string'], ['Fecha', 'DateTime'], ['Estado', 'EstadoReserva']],
    tablas: [['Id', 'INTEGER', 'pk'], ['VueloId', 'INTEGER', 'fk', 'Vuelo'], ['PasajeroId', 'INTEGER', 'fk', 'Pasajero'], ['Asiento', 'TEXT'],
      ['Fecha', 'TEXT'], ['Estado', 'TEXT']]
  },
  pasajero: {
    nombre: 'Pasajero', icono: 'user-round',
    clases: [['Id', 'int'], ['Nombre', 'string'], ['Dni', 'string'], ['Email', 'string?']],
    tablas: [['Id', 'INTEGER', 'pk'], ['Nombre', 'TEXT'], ['Dni', 'TEXT', 'unica'], ['Email', 'TEXT (opcional)']]
  },
  equipaje: {
    nombre: 'Equipaje', icono: 'luggage', nota: 'simulacro',
    clases: [['Id', 'int'], ['ReservaId', 'int'], ['Tipo', 'TipoEquipaje'], ['PesoKg', 'decimal']],
    tablas: [['Id', 'INTEGER', 'pk'], ['ReservaId', 'INTEGER', 'fk', 'Reserva'], ['Tipo', 'TEXT'], ['PesoKg', 'REAL']]
  },
  usuario: {
    nombre: 'Usuario', icono: 'user-cog', nota: 'acceso al sistema',
    clases: [['Id', 'int'], ['NombreUsuario', 'string'], ['Nombre', 'string'], ['Clave', 'string'], ['Rol', 'string']],
    tablas: [['Id', 'INTEGER', 'pk'], ['NombreUsuario', 'TEXT', 'unica'], ['Nombre', 'TEXT'], ['Clave', 'TEXT'], ['Rol', 'TEXT']]
  }
};

// Cada relación: de qué lado está el "1" y de cuál el "N", y cómo se lee en cada vista
const RELACIONES = {
  c1: { izq: '1', der: 'N', clases: 'Origen y Destino', tablas: 'OrigenId, DestinoId', frase: 'Un aeropuerto es origen (y destino) de muchos vuelos.' },
  c2: { izq: '1', der: 'N', clases: 'Vuelo', tablas: 'VueloId', frase: 'Un vuelo tiene muchas reservas.' },
  c3: { izq: 'N', der: '1', clases: 'Pasajero', tablas: 'PasajeroId', frase: 'Un pasajero tiene muchas reservas; cada reserva es de un pasajero.' },
  c4: { izq: '1', der: 'N', clases: 'ReservaId', tablas: 'ReservaId', frase: 'Una reserva tiene varias piezas de equipaje.' }
};

// Una caja: cabecera con el nombre (clic = desplegar) y, abierta, la lista de propiedades de la vista elegida
const CajaEntidad = {
  props: { id: String, e: Object, vista: String, abierta: Boolean },
  emits: ['alternar'],
  template: `
    <div class="entidad" :class="['ent-' + id, { abierta, aparte: e.nota }]" :style="{ gridArea: id }">
      <button type="button" class="entidad-cabecera" @click="$emit('alternar', id)" :aria-expanded="abierta">
        <Icono :nombre="e.icono" />
        <strong>{{ e.nombre }}</strong>
        <Icono :nombre="abierta ? 'chevron-down' : 'chevron-right'" class="flecha" />
      </button>
      <small v-if="e.nota" class="entidad-nota">{{ e.nota }}</small>
      <ul v-if="abierta" class="entidad-propiedades">
        <li v-for="p in e[vista]" :key="p[0]" :class="p[2]">
          <span class="prop">
            <Icono v-if="p[2] === 'pk'" nombre="key-round" />
            <Icono v-else-if="p[2] === 'fk' || p[2] === 'rel'" nombre="link" />
            {{ p[0] }}
          </span>
          <span class="tipo">{{ p[1] }}<template v-if="p[2] === 'fk'"> → {{ p[3] }}</template><template v-if="p[2] === 'unica'"> · único</template><template v-if="p[2] === 'calc'"> · calculada</template></span>
        </li>
      </ul>
    </div>
  `
};

const Conector = {
  props: { id: String, r: Object, vista: String },
  template: `
    <div class="conector" :class="'con-' + id" :style="{ gridArea: id }" :title="r.frase">
      <span class="card">{{ r.izq }}</span>
      <span class="linea"><small>{{ r[vista] }}</small></span>
      <span class="card">{{ r.der }}</span>
    </div>
  `
};

export default {
  components: { CajaEntidad, Conector },
  setup() {
    const vista = ref('clases');
    const abiertas = reactive({});
    const alternar = id => { abiertas[id] = !abiertas[id]; };
    const todas = abrir => Object.keys(ENTIDADES).forEach(id => { abiertas[id] = abrir; });
    return { vista, abiertas, alternar, todas, E: ENTIDADES, R: RELACIONES };
  },
  template: `
    <div class="diagrama-entidades">
      <div class="fila diagrama-controles">
        <div class="segmentado" role="group" aria-label="Vista del diagrama">
          <button type="button" :class="{ activo: vista === 'clases' }" @click="vista = 'clases'"><Icono nombre="braces" /> Clases (C#)</button>
          <button type="button" :class="{ activo: vista === 'tablas' }" @click="vista = 'tablas'"><Icono nombre="table" /> Tablas (SQL)</button>
        </div>
        <button class="boton secundario chico" type="button" @click="todas(true)"><Icono nombre="maximize-2" /> Desplegar todo</button>
        <button class="boton secundario chico" type="button" @click="todas(false)"><Icono nombre="minimize-2" /> Plegar todo</button>
      </div>

      <div class="diagrama">
        <CajaEntidad id="aeropuerto" :e="E.aeropuerto" :vista="vista" :abierta="!!abiertas.aeropuerto" @alternar="alternar" />
        <Conector id="c1" :r="R.c1" :vista="vista" />
        <CajaEntidad id="vuelo" :e="E.vuelo" :vista="vista" :abierta="!!abiertas.vuelo" @alternar="alternar" />
        <Conector id="c2" :r="R.c2" :vista="vista" />
        <!-- Reserva con su equipaje debajo: de este modo la línea permanece unida aunque otra caja esté desplegada -->
        <div class="columna-reserva">
          <CajaEntidad id="reserva" :e="E.reserva" :vista="vista" :abierta="!!abiertas.reserva" @alternar="alternar" />
          <Conector id="c4" :r="R.c4" :vista="vista" />
          <CajaEntidad id="equipaje" :e="E.equipaje" :vista="vista" :abierta="!!abiertas.equipaje" @alternar="alternar" />
        </div>
        <Conector id="c3" :r="R.c3" :vista="vista" />
        <CajaEntidad id="pasajero" :e="E.pasajero" :vista="vista" :abierta="!!abiertas.pasajero" @alternar="alternar" />
        <CajaEntidad id="usuario" :e="E.usuario" :vista="vista" :abierta="!!abiertas.usuario" @alternar="alternar" />
      </div>

      <ul class="diagrama-lectura">
        <li v-for="(r, id) in R" :key="id">{{ r.frase }}</li>
        <li>Usuario no se relaciona con las demás entidades: representa a quien usa el sistema, no a quien viaja.</li>
      </ul>
    </div>
  `
};

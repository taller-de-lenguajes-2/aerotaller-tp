-- ============================================================
-- progreso.sql — la base del tutorial "Torre de Control"
-- Guarda el avance de cada alumno: tareas marcadas, cuestionarios,
-- lecturas, bitácora e insignias. El CONTENIDO de los desafíos no está aquí:
-- vive en la carpeta Contenido/ (Markdown + JSON) para poder editarlo sin tocar la base.
-- ============================================================

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Alumno (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre       TEXT NOT NULL,
    RutaProyecto TEXT,                       -- carpeta del proyecto AeroTaller del alumno (opcional)
    ModoLibre    INTEGER NOT NULL DEFAULT 0, -- 1 = sin bloqueos entre desafíos (para repasar o para el profe)
    Creado       TEXT NOT NULL
);

-- Cuándo empezó y cuándo terminó cada desafío
CREATE TABLE IF NOT EXISTS EstadoDesafio (
    AlumnoId   INTEGER NOT NULL,
    DesafioId  TEXT NOT NULL,
    Iniciado   TEXT NOT NULL,
    Completado TEXT,
    PRIMARY KEY (AlumnoId, DesafioId),
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS TareaMarcada (
    AlumnoId  INTEGER NOT NULL,
    DesafioId TEXT NOT NULL,
    TareaId   TEXT NOT NULL,
    Fecha     TEXT NOT NULL,
    PRIMARY KEY (AlumnoId, DesafioId, TareaId),
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

-- Cada intento de cuestionario, con las respuestas en JSON: [{"preguntaId":"p1","opcionId":"b","correcta":true}, …]
CREATE TABLE IF NOT EXISTS IntentoQuiz (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    AlumnoId   INTEGER NOT NULL,
    DesafioId  TEXT NOT NULL,
    Correctas  INTEGER NOT NULL,
    Total      INTEGER NOT NULL,
    Aprobado   INTEGER NOT NULL,
    Respuestas TEXT NOT NULL,
    Fecha      TEXT NOT NULL,
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS LecturaAbierta (
    AlumnoId  INTEGER NOT NULL,
    DesafioId TEXT NOT NULL,
    LecturaId TEXT NOT NULL,
    Fecha     TEXT NOT NULL,
    PRIMARY KEY (AlumnoId, DesafioId, LecturaId),
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

-- La bitácora: lo que hice, lo que me falta y las dudas para llevar a la consulta
CREATE TABLE IF NOT EXISTS NotaBitacora (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    AlumnoId  INTEGER NOT NULL,
    DesafioId TEXT,
    Tipo      TEXT NOT NULL CHECK (Tipo IN ('Hice', 'Falta', 'Duda', 'Nota')),
    Texto     TEXT NOT NULL,
    Resuelta  INTEGER NOT NULL DEFAULT 0,
    Fecha     TEXT NOT NULL,
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

-- Estado del repositorio git del alumno cada vez que verifica un desafío
CREATE TABLE IF NOT EXISTS EstadoRepo (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    AlumnoId     INTEGER NOT NULL,
    DesafioId    TEXT NOT NULL,
    SinCommitear INTEGER NOT NULL,
    SinPushear   INTEGER NOT NULL,
    Fecha        TEXT NOT NULL,
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

-- Cada vez que el alumno corre el verificador desde la Torre
CREATE TABLE IF NOT EXISTS Verificacion (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    AlumnoId  INTEGER NOT NULL,
    DesafioId TEXT NOT NULL,
    Aprobado  INTEGER NOT NULL,
    Chequeos  INTEGER NOT NULL,
    Fallidos  INTEGER NOT NULL,
    Fecha     TEXT NOT NULL,
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS InsigniaObtenida (
    AlumnoId   INTEGER NOT NULL,
    InsigniaId TEXT NOT NULL,
    Fecha      TEXT NOT NULL,
    PRIMARY KEY (AlumnoId, InsigniaId),
    FOREIGN KEY (AlumnoId) REFERENCES Alumno(Id) ON DELETE CASCADE
);

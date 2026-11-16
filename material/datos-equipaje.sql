-- ============================================================
-- datos-equipaje.sql — equipaje de ejemplo (simulacro de parcial)
-- Se agrega al final de aerotaller.sql, después del CREATE TABLE Equipaje.
-- ============================================================

INSERT INTO Equipaje (ReservaId, Tipo, PesoKg) VALUES
    (2, 'Bodega', 18.5),
    (2, 'Mano',   7),
    (3, 'Bodega', 22),
    (7, 'Bodega', 15.2);

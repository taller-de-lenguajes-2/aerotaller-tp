-- ============================================================
-- datos-usuarios.sql — usuarios de prueba (desafío 9)
-- Se agrega al final de aerotaller.sql, después del CREATE TABLE Usuario.
-- ⚠ Claves en texto plano SOLO para el TP (igual que en el tema 15).
-- ============================================================

INSERT INTO Usuario (NombreUsuario, Nombre, Clave, Rol) VALUES
    ('admin',     'Administración', 'aero2026',   'Admin'),
    ('mostrador', 'Marta',          'vuelos2026', 'Mostrador');

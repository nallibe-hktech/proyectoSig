-- Crear usuario sig_user con contraseña
CREATE USER sig_user WITH PASSWORD 'SigEs@2026';

-- Otorgar permisos sobre la base de datos sig_plataforma_dev
GRANT ALL PRIVILEGES ON DATABASE sig_plataforma_dev TO sig_user;

-- Conectar a la base de datos para otorgar permisos sobre schemas y objetos
\c sig_plataforma_dev postgres

-- Otorgar permisos sobre el schema public
GRANT ALL PRIVILEGES ON SCHEMA public TO sig_user;

-- Otorgar permisos sobre objetos futuros
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON TABLES TO sig_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON SEQUENCES TO sig_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE ON TYPES TO sig_user;

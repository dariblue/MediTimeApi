-- ================================================================
-- MediTime v2.1 — Migración: Perfil de Usuario
-- ================================================================
-- Ejecutar contra la base de datos MediTimeV2 ANTES de arrancar
-- la API actualizada.
--
-- Cambios:
--   1. ALTER USUARIOS: +Domicilio (VARCHAR 255), +AvatarBase64 (LONGTEXT)
--   2. CREATE TABLE PreferenciasUsuario   (FK → USUARIOS, ON DELETE CASCADE)
--   3. CREATE TABLE ConfiguracionNotificaciones (FK → USUARIOS, ON DELETE CASCADE)
--   4. CREATE TABLE SesionesUsuario       (FK → USUARIOS, ON DELETE CASCADE)
-- ================================================================

USE MediTimeV2;

-- ───────────────────────────────────────────────────
-- 1. Añadir columnas a USUARIOS (idempotente)
-- ───────────────────────────────────────────────────

DELIMITER //

CREATE PROCEDURE `Migrate_Perfil_Usuario`()
BEGIN
    -- Domicilio
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'MediTimeV2'
          AND TABLE_NAME   = 'USUARIOS'
          AND COLUMN_NAME  = 'Domicilio'
    ) THEN
        ALTER TABLE USUARIOS ADD COLUMN Domicilio VARCHAR(255) NULL;
    END IF;

    -- AvatarBase64
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'MediTimeV2'
          AND TABLE_NAME   = 'USUARIOS'
          AND COLUMN_NAME  = 'AvatarBase64'
    ) THEN
        ALTER TABLE USUARIOS ADD COLUMN AvatarBase64 LONGTEXT NULL;
    END IF;
END //

DELIMITER ;

CALL `Migrate_Perfil_Usuario`();
DROP PROCEDURE `Migrate_Perfil_Usuario`;

-- ───────────────────────────────────────────────────
-- 2. Tabla: PreferenciasUsuario
-- ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS PreferenciasUsuario (
    IDPreferencia       INT AUTO_INCREMENT PRIMARY KEY,
    IDUsuario           INT NOT NULL,
    Tema                VARCHAR(20)  NOT NULL DEFAULT 'light',
    TamanoTexto         VARCHAR(20)  NOT NULL DEFAULT 'medium',
    VistaCalendario     VARCHAR(20)  NOT NULL DEFAULT 'month',
    PrimerDiaSemana     INT          NOT NULL DEFAULT 0,
    Idioma              VARCHAR(10)  NOT NULL DEFAULT 'es',
    FormatoHora         VARCHAR(5)   NOT NULL DEFAULT '12',

    CONSTRAINT UQ_Preferencias_Usuario UNIQUE (IDUsuario),
    CONSTRAINT FK_Preferencias_Usuario
        FOREIGN KEY (IDUsuario) REFERENCES USUARIOS(IDUsuario)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ───────────────────────────────────────────────────
-- 3. Tabla: ConfiguracionNotificaciones
-- ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS ConfiguracionNotificaciones (
    IDConfiguracion         INT AUTO_INCREMENT PRIMARY KEY,
    IDUsuario               INT     NOT NULL,
    EmailMedicamentos       BOOLEAN NOT NULL DEFAULT TRUE,
    NavegadorMedicamentos   BOOLEAN NOT NULL DEFAULT TRUE,
    TiempoAnticipacion      INT     NOT NULL DEFAULT 5,
    NuevasCaracteristicas   BOOLEAN NOT NULL DEFAULT TRUE,
    Consejos                BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT UQ_ConfigNotif_Usuario UNIQUE (IDUsuario),
    CONSTRAINT FK_ConfigNotif_Usuario
        FOREIGN KEY (IDUsuario) REFERENCES USUARIOS(IDUsuario)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ───────────────────────────────────────────────────
-- 4. Tabla: SesionesUsuario
-- ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS SesionesUsuario (
    IDSesion        INT AUTO_INCREMENT PRIMARY KEY,
    IDUsuario       INT          NOT NULL,
    TokenSesion     VARCHAR(255) NOT NULL,
    FechaInicio     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DireccionIP     VARCHAR(45)  NULL,
    Dispositivo     VARCHAR(255) NULL,

    CONSTRAINT FK_Sesiones_Usuario
        FOREIGN KEY (IDUsuario) REFERENCES USUARIOS(IDUsuario)
        ON DELETE CASCADE,

    INDEX IX_Sesiones_Usuario (IDUsuario),
    INDEX IX_Sesiones_Token   (TokenSesion)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ───────────────────────────────────────────────────
-- Verificación rápida
-- ───────────────────────────────────────────────────

SELECT 'Columnas de USUARIOS después de la migración:' AS Info;
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'MediTimeV2' AND TABLE_NAME = 'USUARIOS'
ORDER BY ORDINAL_POSITION;

SELECT 'Tablas nuevas creadas:' AS Info;
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'MediTimeV2'
  AND TABLE_NAME IN ('PreferenciasUsuario', 'ConfiguracionNotificaciones', 'SesionesUsuario');

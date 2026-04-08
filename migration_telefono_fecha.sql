START TRANSACTION;

-- Intentar agregar las columnas si no existen de forma manual (ya que MariaDB requiere sintaxis específica en algunas versiones, pero usamos esta que es bastante universal en MariaDB > 10.x para evitar errores).
SET @existTelefono = (SELECT COUNT(*) FROM information_schema.columns 
                      WHERE table_schema = 'MediTimeV2' AND table_name = 'USUARIOS' AND column_name = 'Telefono');
SET @existFecha = (SELECT COUNT(*) FROM information_schema.columns 
                   WHERE table_schema = 'MediTimeV2' AND table_name = 'USUARIOS' AND column_name = 'FechaNacimiento');

-- Como IF en MariaDB SQL scripts necesita estar dentro de un bloque BEGIN...END y no podemos ejecutar sentencias ALTER dentro del IF de un bloque anónimo sin PROCEDURE,
-- vamos a crear un pequeño PROCEDURE temporal para garantizar la atomicidad en un solo script y luego lo eliminamos.

DELIMITER //

CREATE PROCEDURE `Migrate_Usuarios_ContactInfo`()
BEGIN
    DECLARE _rollback BOOL DEFAULT 0;
    DECLARE CONTINUE HANDLER FOR SQLEXCEPTION SET _rollback = 1;
    
    START TRANSACTION;
    
    -- Agregar columnas si no existen
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'MediTimeV2' AND TABLE_NAME = 'USUARIOS' AND COLUMN_NAME = 'Telefono') THEN
        ALTER TABLE USUARIOS ADD COLUMN Telefono VARCHAR(20) NULL;
    END IF;
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'MediTimeV2' AND TABLE_NAME = 'USUARIOS' AND COLUMN_NAME = 'FechaNacimiento') THEN
        ALTER TABLE USUARIOS ADD COLUMN FechaNacimiento DATE NULL;
    END IF;

    -- Realizar relleno de datos (Backfill)
    UPDATE USUARIOS 
    SET Telefono = '600000000'
    WHERE Telefono IS NULL OR Telefono = '';

    UPDATE USUARIOS 
    SET FechaNacimiento = '1970-01-01'
    WHERE FechaNacimiento IS NULL;
    
    IF _rollback THEN
        ROLLBACK;
    ELSE
        COMMIT;
    END IF;
END //

DELIMITER ;

-- Ejecutar el procedimiento
CALL `Migrate_Usuarios_ContactInfo`();

-- Limpiar el procedimiento
DROP PROCEDURE `Migrate_Usuarios_ContactInfo`;

-- ─────────────────────────────────────────────────────────────
-- Migración: Tabla PUSH_SUBSCRIPTIONS para notificaciones PWA
-- MediTime v2.2 — Fase 2: Suscripción Push (VAPID)
-- ─────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS PUSH_SUBSCRIPTIONS (
    IDSubscription   INT AUTO_INCREMENT PRIMARY KEY,
    IDUsuario        INT          NOT NULL,
    Endpoint         TEXT         NOT NULL,
    P256dh           VARCHAR(255) NOT NULL,
    Auth             VARCHAR(255) NOT NULL,
    FechaCreacion    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Clave foránea: eliminar suscripciones si se elimina el usuario
    CONSTRAINT FK_PushSub_Usuario
        FOREIGN KEY (IDUsuario) REFERENCES USUARIOS(IDUsuario)
        ON DELETE CASCADE,

    -- Índice para buscar suscripciones por usuario
    INDEX IX_PushSub_Usuario (IDUsuario),

    -- Evitar duplicados: mismo endpoint = mismo navegador/dispositivo
    UNIQUE INDEX UQ_PushSub_Endpoint (Endpoint(500))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

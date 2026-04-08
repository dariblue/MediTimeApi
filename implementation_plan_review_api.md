# Plan de Integración de API: Teléfono y Fecha de Nacimiento

Este plan está diseñado como una especificación de alto nivel para que un agente autónomo se encargue de actualizar el backend de la API (en .NET), añadiendo soporte para las columnas `Telefono` y `FechaNacimiento` en la gestión de usuarios, previniendo así errores de estructura y tipo 400 (Bad Request).

## Contexto de la Tarea

El frontend Web ha sido actualizado para capturar el *Teléfono* y la *Fecha de Nacimiento* en la vista de registro y enviar este payload correctamente en formato JSON hacia el endpoint `[POST] Usuarios/registro`. Como resultado, la API necesita reflejar estas dos nuevas propiedades a lo largo de todas sus capas lógicas. Además, es necesario normalizar los datos de los usuarios ficticios ya existentes mediante un script de migración estructurado.

---

## Tareas a ejecutar por el Agente

### Fase 1: Actualización de Modelos y DTOs
1. **Entidad Principal:** Localizar el modelo que representa al `Usuario` dentro del ecosistema (generalmente bajo el espacio de nombres `Models` o `Entities`) y agregar las propiedades correspondientes para `Telefono` (tipo cadena / string) y `FechaNacimiento` (tipo fecha / DateTime). Ambas deben ser tratadas preferiblemente como opcionales (`Nullable`) temporalmente o mapeadas correctamente para mantener compatibilidad con la base de datos durante la migración.
2. **Data Transfer Objects (DTOs):** Localizar y actualizar el objeto utilizado para recibir la petición de registro desde el Front-End (ej: `RegistroRequestDto` o similar) cerciorándose de que las propiedades entrantes mapean correctamente el nuevo payload del cliente (`telefono` y `fecha_Nacimiento` o `fechaNacimiento`).

### Fase 2: Actualización de la Capa de Acceso a Datos
1. **Manejo de Inserciones (Registro):** Entrar al Repositorio o Servicio encargado de registrar usuarios. Integrar `Telefono` y `FechaNacimiento` dentro de la lógica principal de creación (por ejemplo, actualizando la query `INSERT INTO USUARIOS...` y agregando los parámetros correspondientes, previniendo inyección SQL o mapeándolo correctamente si se usa Entity Framework).
2. **Extracción de Datos:** Revisar las consultas `SELECT` asociadas a este modelo (por ejemplo, operaciones de obtención de perfil) para asegurar que ahora los dos campos también se serializan en el envío cuando el Front-End solicita los datos del usuario.

### Fase 3: Script de Relleno de Datos (Backfill Migration)
1. **Creación del Script:** Dado que ya hay usuarios de prueba grabados en la base de datos, estructurar y escribir un script de bases de datos (o un paso de migración de Entity Framework) envolviendo una `TRANSACTION` que rellene de forma atómica información ficticia o por defecto tanto para `Telefono` como para `FechaNacimiento` en todas las filas existentes donde estos datos ahora son `NULL` o están vacíos.
2. **Restricción y Atomicidad:** El script debe incluir comprobaciones transaccionales (`COMMIT` si exitoso, `ROLLBACK` en caso de error) para asegurar que la actualización de la integridad del set de pruebas suceda de forma segura o no suceda en absoluto.

### Fase 4: Pruebas y Validación Final
1. **Compilación y Build:** Asegurarse de que el proyecto general compila sin problemas ni advertencias de variables no asignadas tras los cambios.
2. **Validación del Payload:** Verificar que el endpoint de Registro procese exitosamente un JSON entrante que incluya estas variables sin rechazar la solicitud HTTP por fallas de esquema (Error 400).

## Verification Plan

> [!CAUTION]
> Una vez que el agente implemente estos cambios y ejecute el script de migración, se debe invocar una comprobación manual desde el Front-End (ej: probando el registro de un nuevo usuario con *Teléfono* y *Fecha*) para confirmar el flujo exitoso a través de toda la pila tecnológica.

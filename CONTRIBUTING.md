# Guía de Contribución a NexusFM

¡Gracias por tu interés en mejorar **NexusFM**!

## Flujo de Trabajo
1. Haz un **Fork** del repositorio.
2. Crea una rama descriptiva: `git checkout -b feat/nueva-funcionalidad` o `fix/correccion-bug`.
3. Realiza tus cambios siguiendo nuestros estándares:
   - **Rust (`src-tauri/`)**: Ejecuta `cargo fmt` y `cargo clippy` antes de hacer commit.
   - **UI (`ui/`)**: Mantén soporte 100% para navegación por teclado y variables CSS de temas.
4. Usa **Conventional Commits** (`feat:`, `fix:`, `docs:`, `refactor:`, `perf:`).
5. Abre un **Pull Request** detallando los cambios y capturas de pantalla si modificaste la interfaz.

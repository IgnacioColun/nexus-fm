# ◈ NexusFM — Next-Gen Dual-Pane File Manager & Workspace Suite for Windows

![C# WPF Native](https://img.shields.io/badge/C%23-WPF_Native_Engine-512BD4?logo=dotnet)
![Zero Dependencies](https://img.shields.io/badge/Build-1--Click_Zero_Dependencies-2ECC71)
![Windows Explorer Replacement](https://img.shields.io/badge/Windows_10%2F11-Explorer_Replacement-0078D6?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)

**NexusFM** es un administrador de archivos nativo y suite de productividad para Windows, diseñado con una arquitectura **100% navegable por teclado y mouse**, estética minimalista profunda con pestañas conectadas al lienzo, **doble panel (`F3`)**, **terminal PowerShell integrada (`F4`)**, **analizador visual de espacio en disco (`F9`)**, **editor de código integrado (`Ctrl+E`)**, **verificador criptográfico SHA-256** e **interceptor nativo Win32 para reemplazar al Explorador de Windows en tiempo real**.

---

## ⚡ Inicio Instantáneo (Sin Instalar Dependencias Externas)

NexusFM se compila en **1 segundo** utilizando el motor nativo de C# / WPF incluido de fábrica en cualquier instalación de Windows 10 y Windows 11 (`csc.exe`), generando un ejecutable ligero y 100% portable (`~85 KB`).

1. **Ejecución directa**: Abre **`NexusFM.exe`**.
2. **Compilar desde el código fuente en 1 clic**: Haz doble clic en **`compilar-y-ejecutar.bat`**.

---

## 🌟 Características Únicas de la Suite

### 1. 🪟 Interceptor y Reemplazo Real del Explorador de Windows (Con Retorno en 1 Clic)
* **Interceptación automática Win32 (`CabinetWClass` + `Shell.Application`)**: Cuando el modo reemplazo está activo, al abrir cualquier carpeta desde tu **Escritorio de Windows**, presionar `Win + E` o hacer clic en el icono del Explorador de la barra de tareas, NexusFM captura la ruta solicitada, cierra la ventana clásica de Windows en milisegundos y abre la carpeta directamente en NexusFM.
* **Regreso instantáneo a Windows**: Desde el panel lateral **`⚙ Personalizar`**, puedes desactivar el reemplazo o hacer clic en **`Regresar a Windows Explorer y Salir`** para restaurar `explorer.exe` inmediatamente en cualquier momento.

### 2. 🎨 Panel Lateral de Personalización Integrado y Geometría Vectorial
* **Sin barras de título ni scrollbars clásicos de Windows**: Ventana `WindowStyle.None` con barra superior propia, iconos vectoriales SVG (`Path` geometry) y barras de desplazamiento oscuras ultradelgadas de `6px`.
* **4 Modelos Visuales de Carpetas**:
  * **Breeze Flat**: Carpetas geométricas planas de dos tonos con pestaña angulada y pestañas superiores conectadas directamente al marco del lienzo oscuro (`#0F1318`).
  * **Tarjetas Pro**: Tarjetas horizontales con metadatos en línea.
  * **Detalles**: Tabla oscura de alta densidad para desarrolladores.
  * **Cyber Neón**: Carpetas oscuras con trazos luminosos.
* **6 Temas de Profundidad + 10 Colores de Carpeta Personalizables**:
  * *Deep Breeze Blue*, *Nexus Emerald*, *Catppuccin Mocha*, *Cyberpunk Matrix*, *Crimson Obsidian* y *Royal Gold*, además de selector circular de color de carpetas en tiempo real.

### 3. 📊 Analizador Visual de Espacio en Disco (`F9`)
* Escanea al instante la carpeta activa y sus subdirectorios, generando un gráfico de barras proporcionales con el porcentaje y peso exacto de cada carpeta y archivo para detectar qué consume espacio en tu disco.

### 4. ✎ Nexus Studio Editor Integrado (`Ctrl + E`) y Verificador SHA-256
* **Editor de Código y Notas in-situ**: Selecciona cualquier archivo de código o texto (`.cs`, `.rs`, `.py`, `.js`, `.json`, `.md`, `.txt`, `.bat`, `.ini`) y presiona `Ctrl + E` (o clic derecho → *Editar en Nexus Studio Editor*) para modificarlo y guardarlo con `Ctrl + S` sin salir del explorador.
* **Verificador Criptográfico SHA-256**: En el panel inspector (`F11`) o mediante clic derecho puedes calcular y copiar al portapapeles el hash `SHA-256` de cualquier ejecutable, ISO o archivo en un clic.

### 5. 🔍 Filtrado Inteligente "Buscar por Tipo" y Ubicaciones Recientes
* En la barra lateral izquierda puedes filtrar instantáneamente cualquier directorio por categoría: **Documentos**, **Imágenes**, **Audio**, **Videos** o **Código fuente**, además de consultar tu historial de **Ubicaciones recientes**.

### 6. ◫ Doble Panel (`F3`), Pestañas Conectadas (`Ctrl+T`) y Terminal Sincronizada (`F4`)
* Divide la pantalla en dos paneles independientes con `F3`, alterna entre ellos con `Tab`, copia de un panel al otro con `F5` o mueve con `F6`.
* La terminal integrada (`F4`) ejecuta comandos reales de PowerShell en la carpeta activa; si ejecutas `cd <carpeta>`, el panel visual cambia a ese directorio automáticamente.

---

## ⌨️ Mapa Completo de Atajos de Teclado

| Atajo | Acción |
| :--- | :--- |
| `Flechas` o `h` `j` `k` `l` | Navegar en 2D por las carpetas y archivos del panel activo |
| `Enter` | Entrar a la carpeta o ejecutar el archivo seleccionado |
| `Backspace` | Subir al directorio padre |
| `Tab` | Alternar foco entre Panel Izquierdo y Panel Derecho |
| `Espacio` | Seleccionar / Deseleccionar elemento actual |
| `Ctrl + T` / `Ctrl + W` | Nueva pestaña / Cerrar pestaña activa |
| `Ctrl + Tab` | Cambiar a la siguiente pestaña |
| `Ctrl + L` | Editar manualmente la ruta en la barra superior |
| `Ctrl + F` | Buscar / Filtrar por nombre en tiempo real |
| `Ctrl + E` | Abrir archivo seleccionado en el **Editor de Código Integrado** |
| `Ctrl + N` | Crear nuevo archivo |
| `Ctrl + ,` | Abrir / Cerrar panel lateral de **Personalización y Sistema** |
| `Ctrl + C` / `X` / `V` / `D` | Copiar / Cortar / Pegar / Duplicar elemento aquí |
| `F2` | Cambiar nombre del archivo o carpeta |
| `F3` | Activar / Desactivar **Doble Panel (Split View)** |
| `F4` | Mostrar / Ocultar **Terminal PowerShell Integrada** |
| `F5` / `F6` | Copiar (`F5`) o Mover (`F6`) selección al panel opuesto |
| `F7` | Crear nueva carpeta |
| `F8` / `Supr` | Eliminar elementos seleccionados |
| `F9` | Abrir **Analizador Visual de Espacio en Disco** |
| `F11` | Mostrar / Ocultar **Inspector Lateral y Hash SHA-256** |

---

## 🏗️ Arquitectura del Repositorio

```text
nexus-fm/
├── NexusFM.exe                 # Ejecutable nativo portable para Windows
├── compilar-y-ejecutar.bat     # Compilador 1-clic sin dependencias externas
├── assets/
│   └── nexus-folder.ico        # Icono vectorial del ejecutable y carpetas
├── src-csharp/
│   ├── NexusFM.cs              # Motor completo C# WPF (UI vectorial, Interceptor Win32, Suite)
│   └── IconGen.cs              # Generador programático de iconos .ico
├── ui/                         # Demo interactiva Web (HTML5 / CSS / JS)
├── src-tauri/                  # Estructura opcional para backend Rust + Tauri v2
├── scripts/                    # Scripts .reg de respaldo para el Registro de Windows
└── .github/workflows/          # CI/CD con GitHub Actions (compila NexusFM.exe en cada push)
    └── ci-release.yml
```

---

## 📄 Licencia

Distribuido bajo la licencia **MIT**. Consulta el archivo `LICENSE` para más información.

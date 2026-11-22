/**
 * NexusFM — Explorador de Archivos de Doble Panel Estilo KDE Dolphin
 * Compatible con backend nativo Rust + Tauri v2 (`window.__TAURI__`)
 * y con Sistema de Archivos Virtual de respaldo para vista previa inmediata.
 */

const isTauri = Boolean(window.__TAURI__ && window.__TAURI__.core);
const tauriInvoke = isTauri ? window.__TAURI__.core.invoke : null;

// ============================================================================
// 1. SISTEMA DE ARCHIVOS VIRTUAL DE RESPALDO (PARA PREVIEW O MODO DEMO)
// ============================================================================
const VIRTUAL_FS = {
  "C:\\": [
    { name: "Users", is_dir: true, size: 0, modified: "2026-11-20 09:00", extension: "DIR" },
    { name: "Program Files", is_dir: true, size: 0, modified: "2026-11-18 14:22", extension: "DIR" },
    { name: "Windows", is_dir: true, size: 0, modified: "2026-10-05 08:00", extension: "DIR" }
  ],
  "C:\\Users": [
    { name: "ignac", is_dir: true, size: 0, modified: "2026-11-22 14:30", extension: "DIR" },
    { name: "Public", is_dir: true, size: 0, modified: "2026-09-12 10:15", extension: "DIR" }
  ],
  "C:\\Users\\ignac": [
    { name: "Proyectos", is_dir: true, size: 0, modified: "2026-11-22 14:50", extension: "DIR" },
    { name: "Documentos", is_dir: true, size: 0, modified: "2026-11-21 19:10", extension: "DIR" },
    { name: "Descargas", is_dir: true, size: 0, modified: "2026-11-22 11:40", extension: "DIR" },
    { name: "Escritorio", is_dir: true, size: 0, modified: "2026-11-22 14:00", extension: "DIR" },
    { name: ".gitconfig", is_dir: false, size: 428, modified: "2026-11-15 16:00", extension: "CONFIG", content: "[user]\n  name = Ignacio\n  email = ignacio@dev.local\n[core]\n  editor = code --wait" },
    { name: "notas-arquitectura.md", is_dir: false, size: 1840, modified: "2026-11-22 13:20", extension: "MD", content: "# NexusFM Architecture\n- Dual-Pane Dolphin workflow (F3)\n- 100% Keyboard Driven (Arrows / Vim hjkl / F-keys)\n- Integrated Terminal synced with active pane (F4)\n- Rust + Tauri v2 Native Backend" }
  ],
  "C:\\Users\\ignac\\Proyectos": [
    { name: "nexus-fm", is_dir: true, size: 0, modified: "2026-11-22 14:52", extension: "DIR" },
    { name: "rust-kernel-lab", is_dir: true, size: 0, modified: "2026-11-19 20:15", extension: "DIR" },
    { name: "README.md", is_dir: false, size: 2450, modified: "2026-11-22 14:45", extension: "MD", content: "# Portafolio de Proyectos\n\n1. **NexusFM**: Explorador de archivos estilo KDE Dolphin en Rust + Tauri.\n2. **Rust Kernel Lab**: Experimentos de bajo nivel." },
    { name: "roadmap-2027.json", is_dir: false, size: 912, modified: "2026-11-20 17:30", extension: "JSON", content: "{\n  \"project\": \"NexusFM\",\n  \"stack\": [\"Rust\", \"Tauri v2\", \"HTML5/CSS3/JS\"],\n  \"features\": [\"Dual Pane\", \"Tabs\", \"Integrated Terminal\", \"Keyboard First\"]\n}" }
  ],
  "C:\\Users\\ignac\\Proyectos\\nexus-fm": [
    { name: "src-tauri", is_dir: true, size: 0, modified: "2026-11-22 14:52", extension: "DIR" },
    { name: "ui", is_dir: true, size: 0, modified: "2026-11-22 14:52", extension: "DIR" },
    { name: "Cargo.toml", is_dir: false, size: 612, modified: "2026-11-22 14:52", extension: "TOML", content: "[package]\nname = \"nexus-fm\"\nversion = \"0.1.0\"\nedition = \"2021\"" },
    { name: "main.rs", is_dir: false, size: 4820, modified: "2026-11-22 14:52", extension: "RS", content: "fn main() {\n    tauri::Builder::default()\n        .run(tauri::generate_context!())\n        .expect(\"Error running NexusFM\");\n}" }
  ],
  "C:\\Users\\ignac\\Documentos": [
    { name: "CV-Ignacio-SoftwareEngineer.pdf", is_dir: false, size: 284512, modified: "2026-11-10 12:00", extension: "PDF", content: "[Documento PDF Binario - Vista previa de metadatos activada]" },
    { name: "presupuesto-hardware.csv", is_dir: false, size: 1240, modified: "2026-11-18 09:45", extension: "CSV", content: "Componente,Precio_USD\nRyzen 9,490\nRAM 64GB DDR5,210\nNVMe 2TB,145" }
  ],
  "C:\\Users\\ignac\\Descargas": [
    { name: "rustup-init.exe", is_dir: false, size: 10485760, modified: "2026-11-21 15:10", extension: "EXE", content: "[Ejecutable PE64 de Windows - Instalador oficial de Rust]" },
    { name: "kde-dolphin-shortcuts.txt", is_dir: false, size: 780, modified: "2026-11-14 22:00", extension: "TXT", content: "F3 = Split View\nF4 = Integrated Konsole\nF11 = Info Panel\nCtrl+T = New Tab\nTab = Switch Pane" }
  ],
  "C:\\Users\\ignac\\Escritorio": [
    { name: "Notas", is_dir: true, size: 0, modified: "2026-11-22 14:52", extension: "DIR" },
    { name: "atajos-rapidos.md", is_dir: false, size: 540, modified: "2026-11-22 10:00", extension: "MD", content: "# Atajos favoritos\nUsa `Tab` para alternar paneles y `F5` para copiar de un panel al otro instantáneamente." }
  ],
  "D:\\": [
    { name: "Backups", is_dir: true, size: 0, modified: "2026-11-01 08:30", extension: "DIR" },
    { name: "Juegos", is_dir: true, size: 0, modified: "2026-10-25 21:00", extension: "DIR" },
    { name: "imagen-sistema.iso", is_dir: false, size: 4724464025, modified: "2026-10-15 18:12", extension: "ISO", content: "[Imagen ISO 9660 - 4.4 GB]" }
  ]
};

// ============================================================================
// 2. ESTADO GLOBAL DEL EXPLORADOR (DOBLE PANEL + PESTAÑAS + PERSONALIZACIÓN)
// ============================================================================
const state = {
  activePane: "left",
  splitView: true,
  showTerminal: true,
  showInfoDock: true,
  syncTerminal: true,
  tagFilter: "ALL",
  settings: {
    theme: localStorage.getItem("nexus_theme") || "breeze-dark",
    accent: localStorage.getItem("nexus_accent") || "#3daee9",
    density: localStorage.getItem("nexus_density") || "normal",
    keymode: localStorage.getItem("nexus_keymode") || "dolphin-vim",
    fontSize: localStorage.getItem("nexus_fontsize") || "13"
  },
  panes: {
    left: {
      viewMode: "list",
      sortBy: "name",
      sortAsc: true,
      filter: "",
      activeTabIndex: 0,
      tabs: [
        {
          id: "tab-l-1",
          path: "C:\\Users\\ignac\\Proyectos",
          history: ["C:\\Users\\ignac\\Proyectos"],
          historyIndex: 0,
          cursor: 0,
          selected: new Set(),
          items: []
        },
        {
          id: "tab-l-2",
          path: "C:\\Users\\ignac",
          history: ["C:\\Users\\ignac"],
          historyIndex: 0,
          cursor: 0,
          selected: new Set(),
          items: []
        }
      ]
    },
    right: {
      viewMode: "list",
      sortBy: "name",
      sortAsc: true,
      filter: "",
      activeTabIndex: 0,
      tabs: [
        {
          id: "tab-r-1",
          path: "C:\\Users\\ignac\\Documentos",
          history: ["C:\\Users\\ignac\\Documentos"],
          historyIndex: 0,
          cursor: 0,
          selected: new Set(),
          items: []
        }
      ]
    }
  }
};

// ============================================================================
// 3. UTILIDADES Y ACCESO AL SISTEMA DE ARCHIVOS (TAURI O VIRTUAL)
// ============================================================================
function normalizePath(p) {
  let clean = p.replace(/\//g, "\\").trim();
  if (/^[A-Za-z]:$/.test(clean)) clean += "\\";
  if (clean.length > 3 && clean.endsWith("\\")) clean = clean.slice(0, -1);
  return clean;
}

function getParentPath(p) {
  const norm = normalizePath(p);
  if (/^[A-Za-z]:\\$/.test(norm)) return norm;
  const idx = norm.lastIndexOf("\\");
  if (idx <= 2) return norm.slice(0, 3);
  return norm.slice(0, idx);
}

function getBaseName(p) {
  const norm = normalizePath(p);
  if (/^[A-Za-z]:\\$/.test(norm)) return norm;
  const parts = norm.split("\\");
  return parts[parts.length - 1] || norm;
}

function formatBytes(bytes, isDir) {
  if (isDir) return "--";
  if (bytes === 0) return "0 B";
  const units = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / Math.pow(1024, i)).toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

function getFileIcon(entry) {
  if (entry.is_dir) return "📁";
  const ext = (entry.extension || "").toUpperCase();
  const map = {
    RS: "🦀",
    TOML: "⚙️",
    JSON: "🧩",
    MD: "📝",
    TXT: "📄",
    PDF: "📕",
    CSV: "📊",
    EXE: "⚡",
    ISO: "💿",
    CONFIG: "🔧"
  };
  return map[ext] || "📄";
}

function ensureVirtualDir(path) {
  const norm = normalizePath(path);
  if (!VIRTUAL_FS[norm]) VIRTUAL_FS[norm] = [];
  return norm;
}

async function fetchDirectory(path) {
  const norm = normalizePath(path);
  if (isTauri) {
    try {
      return await tauriInvoke("list_directory", { path: norm });
    } catch (err) {
      appendTerminalLine(`[Error Tauri] ${err}`, "term-err");
      return [];
    }
  }

  // Fallback Virtual FS
  const list = VIRTUAL_FS[norm];
  if (!list) throw new Error(`No existe la ruta: ${norm}`);
  return list.map((item) => ({
    ...item,
    path: norm.endsWith("\\") ? `${norm}${item.name}` : `${norm}\\${item.name}`
  }));
}

function getActivePaneObj(paneKey = state.activePane) {
  return state.panes[paneKey];
}

function getActiveTab(paneKey = state.activePane) {
  const pane = state.panes[paneKey];
  return pane.tabs[pane.activeTabIndex];
}

// ============================================================================
// 4. NAVEGACIÓN, RENDERIZADO DE PANELES, PESTAÑAS Y BREADCRUMBS
// ============================================================================
async function loadTabDirectory(paneKey, targetPath, pushHistory = true) {
  const pane = getActivePaneObj(paneKey);
  const tab = pane.tabs[pane.activeTabIndex];
  const norm = normalizePath(targetPath);

  try {
    const rawItems = await fetchDirectory(norm);
    tab.path = norm;
    tab.items = sortAndFilterItems(rawItems, pane);
    tab.cursor = Math.min(tab.cursor, Math.max(0, tab.items.length - 1));
    tab.selected.clear();

    if (pushHistory) {
      if (tab.history[tab.historyIndex] !== norm) {
        tab.history = tab.history.slice(0, tab.historyIndex + 1);
        tab.history.push(norm);
        tab.historyIndex = tab.history.length - 1;
      }
    }

    renderPane(paneKey);
    if (paneKey === state.activePane) {
      updatePreviewDock();
      syncTerminalWithActivePane();
      highlightSidebarPlace(norm);
    }
  } catch (err) {
    appendTerminalLine(`No se pudo abrir '${norm}': ${err.message || err}`, "term-err");
  }
}

function sortAndFilterItems(items, pane) {
  const query = (pane.filter || "").toLowerCase();
  const filtered = items.filter((item) => {
    const matchesText = !query || item.name.toLowerCase().includes(query);
    if (!matchesText) return false;
    if (state.tagFilter === "CODE") {
      return item.is_dir || ["RS", "TOML", "JSON", "JS", "CSS", "HTML", "PY"].includes(item.extension);
    }
    if (state.tagFilter === "DOC") {
      return item.is_dir || ["MD", "TXT", "PDF", "CSV"].includes(item.extension);
    }
    return true;
  });

  filtered.sort((a, b) => {
    if (a.is_dir !== b.is_dir) return a.is_dir ? -1 : 1;
    let cmp = 0;
    if (pane.sortBy === "name") cmp = a.name.localeCompare(b.name);
    else if (pane.sortBy === "ext") cmp = (a.extension || "").localeCompare(b.extension || "");
    else if (pane.sortBy === "size") cmp = (a.size || 0) - (b.size || 0);
    else if (pane.sortBy === "date") cmp = (a.modified || "").localeCompare(b.modified || "");
    return pane.sortAsc ? cmp : -cmp;
  });

  return filtered;
}

function renderPane(paneKey) {
  const pane = state.panes[paneKey];
  const tab = pane.tabs[pane.activeTabIndex];

  // 1. Renderizar Pestañas
  const tabsEl = document.getElementById(`tabs-${paneKey}`);
  tabsEl.innerHTML = "";
  pane.tabs.forEach((t, idx) => {
    const pill = document.createElement("div");
    pill.className = `tab-pill ${idx === pane.activeTabIndex ? "active" : ""}`;
    pill.innerHTML = `
      <span>📁 ${getBaseName(t.path)}</span>
      ${pane.tabs.length > 1 ? `<span class="tab-close" data-idx="${idx}">✕</span>` : ""}
    `;
    pill.addEventListener("click", (e) => {
      setActivePane(paneKey);
      if (e.target.classList.contains("tab-close")) {
        closeTab(paneKey, Number(e.target.dataset.idx));
      } else {
        pane.activeTabIndex = idx;
        loadTabDirectory(paneKey, pane.tabs[idx].path, false);
      }
    });
    tabsEl.appendChild(pill);
  });

  // 2. Renderizar Breadcrumbs estilo Dolphin
  const crumbsEl = document.getElementById(`breadcrumbs-${paneKey}`);
  crumbsEl.innerHTML = "";
  const parts = tab.path.split("\\").filter(Boolean);
  let acc = "";
  parts.forEach((part, idx) => {
    acc = idx === 0 ? `${part}\\` : `${acc.replace(/\\$/, "")}\\${part}`;
    const target = acc;
    const span = document.createElement("span");
    span.className = "crumb";
    span.textContent = part;
    span.addEventListener("click", (e) => {
      e.stopPropagation();
      setActivePane(paneKey);
      loadTabDirectory(paneKey, target, true);
    });
    crumbsEl.appendChild(span);
    if (idx < parts.length - 1) {
      const sep = document.createElement("span");
      sep.className = "crumb-sep";
      sep.textContent = "›";
      crumbsEl.appendChild(sep);
    }
  });

  // Clic en el fondo de breadcrumbs activa edición manual (Ctrl+L)
  document.getElementById(`breadcrumbs-wrap-${paneKey}`).onclick = () => {
    setActivePane(paneKey);
    activatePathInput(paneKey);
  };

  // 3. Renderizar Lista / Cuadrícula de Archivos
  const filesEl = document.getElementById(`files-${paneKey}`);
  filesEl.className = `pane-files ${pane.viewMode === "grid" ? "grid-mode" : ""}`;
  filesEl.innerHTML = "";

  tab.items.forEach((item, idx) => {
    const row = document.createElement("div");
    const isCursor = idx === tab.cursor;
    const isSelected = tab.selected.has(item.name);
    row.className = `file-row ${isCursor ? "cursor" : ""} ${isSelected ? "selected" : ""}`;
    row.dataset.index = idx;

    row.innerHTML = `
      <div class="file-name-cell">
        <span class="file-icon">${getFileIcon(item)}</span>
        <span class="file-label">${item.name}</span>
      </div>
      <span class="col-ext">${item.extension || "FILE"}</span>
      <span class="col-size">${formatBytes(item.size, item.is_dir)}</span>
      <span class="col-date">${item.modified || "-"}</span>
    `;

    row.addEventListener("click", (e) => {
      setActivePane(paneKey);
      tab.cursor = idx;
      if (e.ctrlKey) {
        toggleSelect(paneKey, item.name);
      } else if (!e.shiftKey) {
        tab.selected.clear();
      }
      renderPane(paneKey);
      updatePreviewDock();
    });

    row.addEventListener("dblclick", () => {
      setActivePane(paneKey);
      tab.cursor = idx;
      openCurrentItem(paneKey);
    });

    row.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      setActivePane(paneKey);
      tab.cursor = idx;
      renderPane(paneKey);
      updatePreviewDock();
      showContextMenu(e.clientX, e.clientY);
    });

    filesEl.appendChild(row);
  });

  // Mantener la fila del cursor visible al navegar con teclado
  const cursorRow = filesEl.querySelector(".file-row.cursor");
  if (cursorRow) {
    cursorRow.scrollIntoView({ block: "nearest" });
  }

  // 4. Barra de estado
  const statusEl = document.getElementById(`status-${paneKey}`);
  const dirCount = tab.items.filter((i) => i.is_dir).length;
  const fileCount = tab.items.length - dirCount;
  statusEl.querySelector(".status-count").textContent = `${dirCount} carpetas, ${fileCount} archivos`;
  statusEl.querySelector(".status-selected").textContent = `${tab.selected.size} seleccionados`;
}

function setActivePane(paneKey) {
  if (paneKey === "right" && !state.splitView) return;
  state.activePane = paneKey;
  document.getElementById("pane-left").classList.toggle("active", paneKey === "left");
  document.getElementById("pane-right").classList.toggle("active", paneKey === "right");
  updatePreviewDock();
  syncTerminalWithActivePane();
}

// ============================================================================
// 5. OPERACIONES DE ARCHIVOS (ABRIR, COPIAR F5, MOVER F6, CREAR F7, BORRAR F8)
// ============================================================================
async function openCurrentItem(paneKey = state.activePane) {
  const tab = getActiveTab(paneKey);
  const item = tab.items[tab.cursor];
  if (!item) return;

  if (item.is_dir) {
    tab.cursor = 0;
    await loadTabDirectory(paneKey, item.path, true);
  } else if (isTauri) {
    await tauriInvoke("open_with_system", { path: item.path });
  } else {
    appendTerminalLine(`Abriendo vista previa de '${item.name}' (${formatBytes(item.size, false)})`, "term-info");
    if (!state.showInfoDock) toggleInfoDock();
  }
}

async function goUpDirectory(paneKey = state.activePane) {
  const tab = getActiveTab(paneKey);
  const parent = getParentPath(tab.path);
  if (parent !== tab.path) {
    tab.cursor = 0;
    await loadTabDirectory(paneKey, parent, true);
  }
}

function toggleSelect(paneKey, itemName) {
  const tab = getActiveTab(paneKey);
  if (tab.selected.has(itemName)) tab.selected.delete(itemName);
  else tab.selected.add(itemName);
}

function getTargetItems(paneKey = state.activePane) {
  const tab = getActiveTab(paneKey);
  if (tab.selected.size > 0) {
    return tab.items.filter((i) => tab.selected.has(i.name));
  }
  const current = tab.items[tab.cursor];
  return current ? [current] : [];
}

async function createNewFolder() {
  const tab = getActiveTab();
  const name = prompt("Nombre de la nueva carpeta (F7):", "Nueva_Carpeta");
  if (!name) return;

  if (isTauri) {
    await tauriInvoke("create_directory", { parentPath: tab.path, name });
  } else {
    const dir = ensureVirtualDir(tab.path);
    VIRTUAL_FS[dir].push({
      name,
      is_dir: true,
      size: 0,
      modified: "2026-11-22 15:00",
      extension: "DIR"
    });
    ensureVirtualDir(`${dir}\\${name}`);
  }
  appendTerminalLine(`[F7] Carpeta creada: ${name}`, "term-info");
  await loadTabDirectory(state.activePane, tab.path, false);
}

async function createNewFile() {
  const tab = getActiveTab();
  const name = prompt("Nombre del nuevo archivo (Ctrl+N):", "notas.md");
  if (!name) return;
  const ext = name.includes(".") ? name.split(".").pop().toUpperCase() : "TXT";

  if (!isTauri) {
    const dir = ensureVirtualDir(tab.path);
    VIRTUAL_FS[dir].push({
      name,
      is_dir: false,
      size: 64,
      modified: "2026-11-22 15:00",
      extension: ext,
      content: `# ${name}\nCreado desde NexusFM.`
    });
  }
  appendTerminalLine(`[Ctrl+N] Archivo creado: ${name}`, "term-info");
  await loadTabDirectory(state.activePane, tab.path, false);
}

async function renameSelectedItem() {
  const tab = getActiveTab();
  const item = tab.items[tab.cursor];
  if (!item) return;
  const newName = prompt(`Renombrar '${item.name}' a:`, item.name);
  if (!newName || newName === item.name) return;

  if (isTauri) {
    await tauriInvoke("rename_item", { oldPath: item.path, newName });
  } else {
    const list = VIRTUAL_FS[tab.path] || [];
    const target = list.find((i) => i.name === item.name);
    if (target) {
      if (target.is_dir) {
        const oldSub = `${tab.path}\\${target.name}`;
        const newSub = `${tab.path}\\${newName}`;
        if (VIRTUAL_FS[oldSub]) {
          VIRTUAL_FS[newSub] = VIRTUAL_FS[oldSub];
          delete VIRTUAL_FS[oldSub];
        }
      }
      target.name = newName;
    }
  }
  appendTerminalLine(`[F2] Renombrado '${item.name}' → '${newName}'`, "term-info");
  await loadTabDirectory(state.activePane, tab.path, false);
}

async function copyOrMoveToOppositePane(mode = "copy") {
  const srcPaneKey = state.activePane;
  const dstPaneKey = srcPaneKey === "left" ? "right" : "left";
  const srcTab = getActiveTab(srcPaneKey);
  const dstTab = getActiveTab(dstPaneKey);
  const targets = getTargetItems(srcPaneKey);

  if (targets.length === 0) return;
  if (srcTab.path === dstTab.path) {
    appendTerminalLine("El panel origen y destino apuntan a la misma carpeta.", "term-err");
    return;
  }

  for (const item of targets) {
    if (isTauri) {
      const cmd = mode === "copy" ? "copy_item" : "move_item";
      await tauriInvoke(cmd, { srcPath: item.path, targetDir: dstTab.path });
    } else {
      const dstList = VIRTUAL_FS[ensureVirtualDir(dstTab.path)];
      const existingIdx = dstList.findIndex((x) => x.name === item.name);
      if (existingIdx >= 0) dstList.splice(existingIdx, 1);
      dstList.push({ ...item });

      if (mode === "move") {
        const srcList = VIRTUAL_FS[srcTab.path] || [];
        const idx = srcList.findIndex((x) => x.name === item.name);
        if (idx >= 0) srcList.splice(idx, 1);
      }
    }
  }

  const verb = mode === "copy" ? "Copiado(s)" : "Movido(s)";
  appendTerminalLine(`[${mode === "copy" ? "F5" : "F6"}] ${verb} ${targets.length} elemento(s) a ${dstTab.path}`, "term-info");
  await loadTabDirectory(srcPaneKey, srcTab.path, false);
  await loadTabDirectory(dstPaneKey, dstTab.path, false);
}

async function deleteSelectedItems() {
  const tab = getActiveTab();
  const targets = getTargetItems();
  if (targets.length === 0) return;

  const names = targets.map((t) => t.name).join(", ");
  if (!confirm(`¿Eliminar ${targets.length} elemento(s) (${names})?`)) return;

  for (const item of targets) {
    if (isTauri) {
      await tauriInvoke("delete_item", { path: item.path });
    } else {
      const list = VIRTUAL_FS[tab.path] || [];
      const idx = list.findIndex((x) => x.name === item.name);
      if (idx >= 0) list.splice(idx, 1);
    }
  }
  appendTerminalLine(`[F8] Eliminado(s): ${names}`, "term-err");
  await loadTabDirectory(state.activePane, tab.path, false);
}

// ============================================================================
// 6. GESTIÓN DE PESTAÑAS, VISTAS Y VISTA PREVIA (F11)
// ============================================================================
function createNewTab(paneKey = state.activePane, customPath = null) {
  const pane = state.panes[paneKey];
  const currentTab = pane.tabs[pane.activeTabIndex];
  const path = customPath || currentTab.path;
  pane.tabs.push({
    id: `tab-${Date.now()}`,
    path,
    history: [path],
    historyIndex: 0,
    cursor: 0,
    selected: new Set(),
    items: []
  });
  pane.activeTabIndex = pane.tabs.length - 1;
  loadTabDirectory(paneKey, path, false);
}

function closeTab(paneKey = state.activePane, idx = null) {
  const pane = state.panes[paneKey];
  if (pane.tabs.length <= 1) return;
  const targetIdx = idx !== null ? idx : pane.activeTabIndex;
  pane.tabs.splice(targetIdx, 1);
  pane.activeTabIndex = Math.max(0, Math.min(pane.activeTabIndex, pane.tabs.length - 1));
  loadTabDirectory(paneKey, pane.tabs[pane.activeTabIndex].path, false);
}

function cycleTab(paneKey = state.activePane) {
  const pane = state.panes[paneKey];
  pane.activeTabIndex = (pane.activeTabIndex + 1) % pane.tabs.length;
  loadTabDirectory(paneKey, pane.tabs[pane.activeTabIndex].path, false);
}

function toggleSplitView() {
  state.splitView = !state.splitView;
  document.getElementById("pane-right").classList.toggle("hidden", !state.splitView);
  document.getElementById("btn-toggle-split").classList.toggle("active", state.splitView);
  if (!state.splitView && state.activePane === "right") {
    setActivePane("left");
  }
}

function toggleTerminal() {
  state.showTerminal = !state.showTerminal;
  document.getElementById("terminal-dock").classList.toggle("hidden", !state.showTerminal);
  document.getElementById("btn-toggle-terminal").classList.toggle("active", state.showTerminal);
  if (state.showTerminal) {
    document.getElementById("terminal-input").focus();
  }
}

function toggleInfoDock() {
  state.showInfoDock = !state.showInfoDock;
  document.getElementById("info-dock").classList.toggle("hidden", !state.showInfoDock);
  document.getElementById("btn-toggle-info").classList.toggle("active", state.showInfoDock);
}

async function updatePreviewDock() {
  const tab = getActiveTab();
  const item = tab.items[tab.cursor];
  const iconEl = document.getElementById("preview-icon");
  const titleEl = document.getElementById("preview-title");
  const metaEl = document.getElementById("preview-meta");
  const codeEl = document.getElementById("preview-code");

  if (!item) {
    iconEl.textContent = "📂";
    titleEl.textContent = getBaseName(tab.path);
    metaEl.innerHTML = `<div class="meta-row"><span class="meta-label">Ruta:</span><span>${tab.path}</span></div>`;
    codeEl.textContent = "Carpeta vacía.";
    return;
  }

  iconEl.textContent = getFileIcon(item);
  titleEl.textContent = item.name;
  metaEl.innerHTML = `
    <div class="meta-row"><span class="meta-label">Tipo:</span><span>${item.is_dir ? "Carpeta de archivos" : item.extension}</span></div>
    <div class="meta-row"><span class="meta-label">Tamaño:</span><span>${formatBytes(item.size, item.is_dir)}</span></div>
    <div class="meta-row"><span class="meta-label">Modificado:</span><span>${item.modified}</span></div>
    <div class="meta-row"><span class="meta-label">Panel:</span><span>${state.activePane === "left" ? "Izquierdo" : "Derecho"}</span></div>
  `;

  if (item.is_dir) {
    if (!isTauri && VIRTUAL_FS[item.path]) {
      const children = VIRTUAL_FS[item.path];
      codeEl.textContent = `Contenido (${children.length} elementos):\n\n` +
        children.map((c) => `${getFileIcon(c)} ${c.name}`).join("\n");
    } else {
      codeEl.textContent = `Directorio: ${item.path}\nPresiona Enter o l para entrar.`;
    }
  } else if (isTauri) {
    try {
      const text = await tauriInvoke("read_text_preview", { path: item.path, maxBytes: 2048 });
      codeEl.textContent = text || "[Archivo vacío]";
    } catch {
      codeEl.textContent = "[Vista previa no disponible]";
    }
  } else {
    codeEl.textContent = item.content || `[Archivo ${item.extension} — ${formatBytes(item.size, false)}]`;
  }
}

function activatePathInput(paneKey = state.activePane) {
  const tab = getActiveTab(paneKey);
  const crumbs = document.getElementById(`breadcrumbs-${paneKey}`);
  const input = document.getElementById(`path-input-${paneKey}`);
  crumbs.classList.add("hidden");
  input.classList.remove("hidden");
  input.value = tab.path;
  input.focus();
  input.select();

  const finish = async (commit) => {
    input.classList.add("hidden");
    crumbs.classList.remove("hidden");
    if (commit && input.value.trim()) {
      await loadTabDirectory(paneKey, input.value.trim(), true);
    }
  };

  input.onkeydown = (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      finish(true);
    } else if (e.key === "Escape") {
      e.preventDefault();
      finish(false);
    }
  };
  input.onblur = () => finish(false);
}

// ============================================================================
// 7. TERMINAL INTEGRADA SINCRONIZADA (F4)
// ============================================================================
function syncTerminalWithActivePane() {
  if (!state.syncTerminal) return;
  const tab = getActiveTab();
  document.getElementById("terminal-cwd-badge").textContent = tab.path;
  document.getElementById("terminal-prompt").textContent = `PS ${tab.path}>`;
}

function appendTerminalLine(text, cls = "") {
  const out = document.getElementById("terminal-output");
  const div = document.createElement("div");
  div.className = `term-line ${cls}`;
  div.textContent = text;
  out.appendChild(div);
  out.scrollTop = out.scrollHeight;
}

async function runTerminalCommand(rawCmd) {
  const cmd = rawCmd.trim();
  if (!cmd) return;
  const tab = getActiveTab();
  appendTerminalLine(`PS ${tab.path}> ${cmd}`, "term-cmd");

  if (cmd === "clear" || cmd === "cls") {
    document.getElementById("terminal-output").innerHTML = "";
    return;
  }

  if (isTauri) {
    try {
      const res = await tauriInvoke("execute_terminal_command", { cwd: tab.path, command: cmd });
      if (res.stdout) appendTerminalLine(res.stdout.trim());
      if (res.stderr) appendTerminalLine(res.stderr.trim(), "term-err");
      if (res.new_cwd && res.new_cwd !== tab.path) {
        await loadTabDirectory(state.activePane, res.new_cwd, true);
      } else {
        await loadTabDirectory(state.activePane, tab.path, false);
      }
    } catch (err) {
      appendTerminalLine(`Error: ${err}`, "term-err");
    }
    return;
  }

  // Simulador interactivo de Terminal cuando se ejecuta directo en el navegador
  const [bin, ...rest] = cmd.split(" ");
  const arg = rest.join(" ").trim().replace(/^"|"$/g, "");

  switch (bin.toLowerCase()) {
    case "help":
      appendTerminalLine(
        "Comandos disponibles en NexusShell:\n" +
        "  ls / dir         Listar contenido del panel activo\n" +
        "  cd <ruta>        Cambiar directorio (sincroniza el panel activo en vivo)\n" +
        "  pwd              Mostrar ruta actual\n" +
        "  mkdir <nombre>   Crear carpeta en el directorio activo\n" +
        "  cat <archivo>    Mostrar contenido de un archivo\n" +
        "  git status       Estado del repositorio Git\n" +
        "  neofetch         Información de NexusFM y sistema\n" +
        "  clear / cls      Limpiar pantalla",
        "term-info"
      );
      break;
    case "pwd":
      appendTerminalLine(tab.path);
      break;
    case "ls":
    case "dir":
      appendTerminalLine(
        tab.items.map((i) => `${i.is_dir ? "<DIR> " : "      "} ${i.modified}  ${i.name}`).join("\n")
      );
      break;
    case "cd": {
      if (!arg || arg === "~") {
        await loadTabDirectory(state.activePane, "C:\\Users\\ignac", true);
      } else if (arg === "..") {
        await goUpDirectory(state.activePane);
      } else {
        const candidate = /^[A-Za-z]:/.test(arg)
          ? normalizePath(arg)
          : normalizePath(`${tab.path}\\${arg}`);
        if (VIRTUAL_FS[candidate]) {
          await loadTabDirectory(state.activePane, candidate, true);
        } else {
          appendTerminalLine(`cd: no se encontró la ruta '${candidate}'`, "term-err");
        }
      }
      break;
    }
    case "mkdir": {
      if (!arg) {
        appendTerminalLine("mkdir: falta el nombre de la carpeta", "term-err");
        break;
      }
      const dir = ensureVirtualDir(tab.path);
      VIRTUAL_FS[dir].push({ name: arg, is_dir: true, size: 0, modified: "2026-11-22 15:05", extension: "DIR" });
      ensureVirtualDir(`${dir}\\${arg}`);
      await loadTabDirectory(state.activePane, tab.path, false);
      appendTerminalLine(`Carpeta '${arg}' creada.`);
      break;
    }
    case "cat":
    case "type": {
      const found = tab.items.find((i) => i.name.toLowerCase() === arg.toLowerCase());
      if (!found) appendTerminalLine(`cat: no existe '${arg}'`, "term-err");
      else if (found.is_dir) appendTerminalLine(`cat: '${arg}' es un directorio`, "term-err");
      else appendTerminalLine(found.content || `[Archivo ${found.extension}]`);
      break;
    }
    case "git":
      appendTerminalLine(
        "On branch main\nYour branch is up to date with 'origin/main'.\nnothing to commit, working tree clean",
        "term-info"
      );
      break;
    case "neofetch":
      appendTerminalLine(
        "   ◈ NexusFM v0.1.0 (Dolphin-Inspired Dual-Pane Explorer)\n" +
        "   ─────────────────────────────────────────────────────\n" +
        "   Backend  : Rust + Tauri v2\n" +
        "   Panes    : Dual Split-View (F3) + Multi-Tabs (Ctrl+T)\n" +
        "   Keyboard : 100% Keyboard Driven (Arrows + Vim hjkl + F-Keys)\n" +
        "   Theme    : " + state.settings.theme,
        "term-info"
      );
      break;
    default:
      appendTerminalLine(`Comando ejecutado: '${cmd}' (Escribe 'help' para ver comandos integrados)`);
  }
}

// ============================================================================
// 8. NAVEGACIÓN 100% POR TECLADO (ATAJOS GLOBALES Y ESTILO DOLPHIN / VIM)
// ============================================================================
function setupKeyboardNavigation() {
  window.addEventListener("keydown", async (e) => {
    const tag = document.activeElement?.tagName;
    const isTyping = tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT";

    // Atajos globales que funcionan incluso con inputs abiertos
    if (e.key === "F3") {
      e.preventDefault();
      toggleSplitView();
      return;
    }
    if (e.key === "F4" || (e.ctrlKey && e.key === "`")) {
      e.preventDefault();
      toggleTerminal();
      return;
    }
    if (e.key === "F11") {
      e.preventDefault();
      toggleInfoDock();
      return;
    }
    if (e.ctrlKey && e.key.toLowerCase() === "k") {
      e.preventDefault();
      openCommandPalette();
      return;
    }
    if (e.ctrlKey && e.key === ",") {
      e.preventDefault();
      document.getElementById("settings-modal").classList.toggle("hidden");
      return;
    }
    if (e.key === "Escape") {
      document.querySelectorAll(".modal-backdrop, .context-menu").forEach((el) => el.classList.add("hidden"));
      if (isTyping) document.activeElement.blur();
      return;
    }

    if (isTyping) return;

    const paneKey = state.activePane;
    const tab = getActiveTab(paneKey);
    const maxIdx = Math.max(0, tab.items.length - 1);
    const vimEnabled = state.settings.keymode === "dolphin-vim";

    // Cambiar entre paneles con Tab ( o Ctrl+Tab para cambiar pestaña )
    if (e.key === "Tab") {
      e.preventDefault();
      if (e.ctrlKey) {
        cycleTab(paneKey);
      } else if (state.splitView) {
        setActivePane(paneKey === "left" ? "right" : "left");
      }
      return;
    }

    // Movimiento arriba / abajo
    if (e.key === "ArrowDown" || (vimEnabled && e.key === "j")) {
      e.preventDefault();
      tab.cursor = Math.min(maxIdx, tab.cursor + 1);
      renderPane(paneKey);
      updatePreviewDock();
      return;
    }
    if (e.key === "ArrowUp" || (vimEnabled && e.key === "k")) {
      e.preventDefault();
      tab.cursor = Math.max(0, tab.cursor - 1);
      renderPane(paneKey);
      updatePreviewDock();
      return;
    }
    if (e.key === "Home") {
      e.preventDefault();
      tab.cursor = 0;
      renderPane(paneKey);
      updatePreviewDock();
      return;
    }
    if (e.key === "End") {
      e.preventDefault();
      tab.cursor = maxIdx;
      renderPane(paneKey);
      updatePreviewDock();
      return;
    }

    // Abrir / Entrar (Enter o l)
    if (e.key === "Enter" || (vimEnabled && e.key === "l")) {
      e.preventDefault();
      await openCurrentItem(paneKey);
      return;
    }

    // Subir directorio (Backspace o h)
    if (e.key === "Backspace" || (vimEnabled && e.key === "h")) {
      e.preventDefault();
      await goUpDirectory(paneKey);
      return;
    }

    // Seleccionar con Espacio y avanzar cursor (estilo Commander / Dolphin)
    if (e.key === " ") {
      e.preventDefault();
      const item = tab.items[tab.cursor];
      if (item) {
        toggleSelect(paneKey, item.name);
        tab.cursor = Math.min(maxIdx, tab.cursor + 1);
        renderPane(paneKey);
      }
      return;
    }

    // Seleccionar todo (Ctrl+A)
    if (e.ctrlKey && e.key.toLowerCase() === "a") {
      e.preventDefault();
      tab.items.forEach((i) => tab.selected.add(i.name));
      renderPane(paneKey);
      return;
    }

    // Nueva pestaña (Ctrl+T) / Cerrar pestaña (Ctrl+W)
    if (e.ctrlKey && e.key.toLowerCase() === "t") {
      e.preventDefault();
      createNewTab(paneKey);
      return;
    }
    if (e.ctrlKey && e.key.toLowerCase() === "w") {
      e.preventDefault();
      closeTab(paneKey);
      return;
    }

    // Editar ruta (Ctrl+L)
    if (e.ctrlKey && e.key.toLowerCase() === "l") {
      e.preventDefault();
      activatePathInput(paneKey);
      return;
    }

    // Filtrar (Ctrl+F o /)
    if ((e.ctrlKey && e.key.toLowerCase() === "f") || e.key === "/") {
      e.preventDefault();
      document.getElementById(`filter-${paneKey}`).focus();
      return;
    }

    // Nuevo archivo (Ctrl+N)
    if (e.ctrlKey && e.key.toLowerCase() === "n") {
      e.preventDefault();
      await createNewFile();
      return;
    }

    // Teclas de Función F2, F5, F6, F7, F8/Delete
    if (e.key === "F2") {
      e.preventDefault();
      await renameSelectedItem();
    } else if (e.key === "F5") {
      e.preventDefault();
      await copyOrMoveToOppositePane("copy");
    } else if (e.key === "F6") {
      e.preventDefault();
      await copyOrMoveToOppositePane("move");
    } else if (e.key === "F7") {
      e.preventDefault();
      await createNewFolder();
    } else if (e.key === "F8" || e.key === "Delete") {
      e.preventDefault();
      await deleteSelectedItems();
    } else if (e.key === "?") {
      e.preventDefault();
      document.getElementById("help-modal").classList.toggle("hidden");
    }
  });
}

// ============================================================================
// 9. SIDEBAR, MENÚ CONTEXTUAL, PALETA DE COMANDOS Y PERSONALIZACIÓN
// ============================================================================
async function initSidebar() {
  const places = [
    { label: "Inicio (ignac)", icon: "🏠", path: "C:\\Users\\ignac" },
    { label: "Proyectos", icon: "🚀", path: "C:\\Users\\ignac\\Proyectos" },
    { label: "Escritorio", icon: "🖥️", path: "C:\\Users\\ignac\\Escritorio" },
    { label: "Documentos", icon: "📄", path: "C:\\Users\\ignac\\Documentos" },
    { label: "Descargas", icon: "⬇️", path: "C:\\Users\\ignac\\Descargas" }
  ];

  const placesEl = document.getElementById("places-list");
  placesEl.innerHTML = "";
  places.forEach((p) => {
    const li = document.createElement("li");
    li.className = "place-item";
    li.dataset.path = p.path;
    li.innerHTML = `<span>${p.icon}</span><span>${p.label}</span>`;
    li.onclick = () => loadTabDirectory(state.activePane, p.path, true);
    placesEl.appendChild(li);
  });

  const drivesEl = document.getElementById("drives-list");
  drivesEl.innerHTML = "";
  let drives = [
    { letter: "C:", path: "C:\\", label: "Sistema (C:)" },
    { letter: "D:", path: "D:\\", label: "Datos / Backups (D:)" }
  ];
  if (isTauri) {
    try {
      drives = await tauriInvoke("list_drives");
    } catch {}
  }
  drives.forEach((d) => {
    const li = document.createElement("li");
    li.className = "place-item";
    li.dataset.path = d.path;
    li.innerHTML = `<span>💽</span><span>${d.label}</span>`;
    li.onclick = () => loadTabDirectory(state.activePane, d.path, true);
    drivesEl.appendChild(li);
  });

  document.querySelectorAll("#tags-list .place-item").forEach((el) => {
    el.onclick = () => {
      state.tagFilter = el.dataset.filter;
      loadTabDirectory("left", getActiveTab("left").path, false);
      loadTabDirectory("right", getActiveTab("right").path, false);
    };
  });
}

function highlightSidebarPlace(currentPath) {
  document.querySelectorAll(".sidebar .place-item[data-path]").forEach((el) => {
    el.classList.toggle("active", normalizePath(el.dataset.path) === normalizePath(currentPath));
  });
}

function showContextMenu(x, y) {
  const menu = document.getElementById("context-menu");
  menu.classList.remove("hidden");
  menu.style.left = `${Math.min(x, window.innerWidth - 225)}px`;
  menu.style.top = `${Math.min(y, window.innerHeight - 270)}px`;
}

function applySettings() {
  const s = state.settings;
  document.body.dataset.theme = s.theme;
  document.body.dataset.density = s.density;
  document.documentElement.style.setProperty("--accent", s.accent);
  document.documentElement.style.setProperty("--font-size-base", `${s.fontSize}px`);
  document.getElementById("active-keymode-label").textContent =
    s.keymode === "dolphin-vim" ? "Dolphin + Vim (hjkl)" : "Estándar (Flechas)";

  localStorage.setItem("nexus_theme", s.theme);
  localStorage.setItem("nexus_accent", s.accent);
  localStorage.setItem("nexus_density", s.density);
  localStorage.setItem("nexus_keymode", s.keymode);
  localStorage.setItem("nexus_fontsize", s.fontSize);
}

const PALETTE_COMMANDS = [
  { label: "◫ Alternar Doble Panel (Split View)", shortcut: "F3", run: () => toggleSplitView() },
  { label: "❯_ Alternar Terminal Integrada", shortcut: "F4", run: () => toggleTerminal() },
  { label: "ℹ Alternar Panel de Vista Previa", shortcut: "F11", run: () => toggleInfoDock() },
  { label: "📑 Nueva Pestaña en Panel Activo", shortcut: "Ctrl+T", run: () => createNewTab() },
  { label: "📁 Crear Nueva Carpeta", shortcut: "F7", run: () => createNewFolder() },
  { label: "📄 Crear Nuevo Archivo", shortcut: "Ctrl+N", run: () => createNewFile() },
  { label: "⇄ Copiar Selección al Otro Panel", shortcut: "F5", run: () => copyOrMoveToOppositePane("copy") },
  { label: "➔ Mover Selección al Otro Panel", shortcut: "F6", run: () => copyOrMoveToOppositePane("move") },
  { label: "🎨 Cambiar Tema: KDE Breeze Dark", shortcut: "Tema", run: () => { state.settings.theme = "breeze-dark"; applySettings(); } },
  { label: "🎨 Cambiar Tema: Catppuccin Mocha", shortcut: "Tema", run: () => { state.settings.theme = "catppuccin"; applySettings(); } },
  { label: "🎨 Cambiar Tema: Nordic Frost", shortcut: "Tema", run: () => { state.settings.theme = "nord"; applySettings(); } },
  { label: "🎨 Cambiar Tema: Cyberpunk Neon", shortcut: "Tema", run: () => { state.settings.theme = "cyberpunk"; applySettings(); } }
];

function openCommandPalette() {
  const modal = document.getElementById("palette-modal");
  const input = document.getElementById("palette-input");
  const list = document.getElementById("palette-list");
  modal.classList.remove("hidden");
  input.value = "";
  input.focus();

  const renderList = (q) => {
    list.innerHTML = "";
    const filtered = PALETTE_COMMANDS.filter((c) => c.label.toLowerCase().includes(q.toLowerCase()));
    filtered.forEach((cmd) => {
      const li = document.createElement("li");
      li.className = "palette-item";
      li.innerHTML = `<span>${cmd.label}</span><kbd>${cmd.shortcut}</kbd>`;
      li.onclick = () => {
        modal.classList.add("hidden");
        cmd.run();
      };
      list.appendChild(li);
    });
  };

  renderList("");
  input.oninput = () => renderList(input.value);
}

// ============================================================================
// 10. INICIALIZACIÓN Y EVENT LISTENERS
// ============================================================================
window.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("runtime-badge").textContent = isTauri
    ? "Tauri Rust Nativo"
    : "Modo Interactivo UI";

  applySettings();
  await initSidebar();
  setupKeyboardNavigation();

  await loadTabDirectory("left", state.panes.left.tabs[0].path, false);
  await loadTabDirectory("right", state.panes.right.tabs[0].path, false);

  appendTerminalLine("Bienvenido a NexusFM — Terminal Integrada sincronizada con el panel activo.", "term-info");
  appendTerminalLine("Escribe 'help' o 'neofetch' para probar comandos, o usa F3/F4/Tab/Flechas.", "term-info");

  // Botones barra superior
  document.getElementById("btn-toggle-split").onclick = toggleSplitView;
  document.getElementById("btn-toggle-terminal").onclick = toggleTerminal;
  document.getElementById("btn-toggle-info").onclick = toggleInfoDock;
  document.getElementById("btn-cmd-palette").onclick = openCommandPalette;
  document.getElementById("btn-open-settings").onclick = () => document.getElementById("settings-modal").classList.remove("hidden");
  document.getElementById("btn-close-settings").onclick = () => document.getElementById("settings-modal").classList.add("hidden");
  document.getElementById("btn-save-settings").onclick = () => document.getElementById("settings-modal").classList.add("hidden");
  document.getElementById("btn-open-help").onclick = () => document.getElementById("help-modal").classList.remove("hidden");
  document.getElementById("btn-close-help").onclick = () => document.getElementById("help-modal").classList.add("hidden");
  document.getElementById("btn-close-info").onclick = toggleInfoDock;
  document.getElementById("btn-close-terminal").onclick = toggleTerminal;
  document.getElementById("btn-clear-terminal").onclick = () => { document.getElementById("terminal-output").innerHTML = ""; };

  // Controles de navegación superior
  document.getElementById("btn-up").onclick = () => goUpDirectory();
  document.getElementById("btn-refresh").onclick = () => loadTabDirectory(state.activePane, getActiveTab().path, false);
  document.getElementById("btn-back").onclick = () => {
    const tab = getActiveTab();
    if (tab.historyIndex > 0) {
      tab.historyIndex--;
      loadTabDirectory(state.activePane, tab.history[tab.historyIndex], false);
    }
  };
  document.getElementById("btn-forward").onclick = () => {
    const tab = getActiveTab();
    if (tab.historyIndex < tab.history.length - 1) {
      tab.historyIndex++;
      loadTabDirectory(state.activePane, tab.history[tab.historyIndex], false);
    }
  };

  // Botones de nuevas pestañas y vistas por panel
  document.querySelectorAll(".btn-new-tab").forEach((btn) => {
    btn.onclick = () => {
      setActivePane(btn.dataset.pane);
      createNewTab(btn.dataset.pane);
    };
  });

  document.querySelectorAll("[data-action^='view-']").forEach((btn) => {
    btn.onclick = () => {
      const pKey = btn.dataset.pane;
      state.panes[pKey].viewMode = btn.dataset.action === "view-grid" ? "grid" : "list";
      renderPane(pKey);
    };
  });

  // Filtros en vivo por panel
  ["left", "right"].forEach((pKey) => {
    const filterEl = document.getElementById(`filter-${pKey}`);
    filterEl.oninput = () => {
      state.panes[pKey].filter = filterEl.value;
      loadTabDirectory(pKey, getActiveTab(pKey).path, false);
    };
    document.getElementById(`pane-${pKey}`).addEventListener("focus", () => setActivePane(pKey));
  });

  // Ordenamiento por columnas
  document.querySelectorAll(".pane-table-header span[data-sort]").forEach((col) => {
    col.onclick = () => {
      const pKey = col.dataset.pane;
      const pane = state.panes[pKey];
      if (pane.sortBy === col.dataset.sort) pane.sortAsc = !pane.sortAsc;
      else {
        pane.sortBy = col.dataset.sort;
        pane.sortAsc = true;
      }
      loadTabDirectory(pKey, getActiveTab(pKey).path, false);
    };
  });

  // Barra inferior de teclas de función
  document.querySelectorAll(".fn-btn").forEach((btn) => {
    btn.onclick = () => {
      const fn = btn.dataset.fn;
      if (fn === "rename") renameSelectedItem();
      else if (fn === "split") toggleSplitView();
      else if (fn === "terminal") toggleTerminal();
      else if (fn === "copy") copyOrMoveToOppositePane("copy");
      else if (fn === "move") copyOrMoveToOppositePane("move");
      else if (fn === "mkdir") createNewFolder();
      else if (fn === "newfile") createNewFile();
      else if (fn === "delete") deleteSelectedItems();
    };
  });

  // Menú contextual
  window.addEventListener("click", () => document.getElementById("context-menu").classList.add("hidden"));
  document.querySelectorAll("#context-menu button").forEach((btn) => {
    btn.onclick = () => {
      const act = btn.dataset.ctx;
      if (act === "open") openCurrentItem();
      else if (act === "new-tab") {
        const item = getActiveTab().items[getActiveTab().cursor];
        createNewTab(state.activePane, item?.is_dir ? item.path : getActiveTab().path);
      } else if (act === "copy-other") copyOrMoveToOppositePane("copy");
      else if (act === "move-other") copyOrMoveToOppositePane("move");
      else if (act === "rename") renameSelectedItem();
      else if (act === "mkdir") createNewFolder();
      else if (act === "newfile") createNewFile();
      else if (act === "delete") deleteSelectedItems();
    };
  });

  // Terminal submit
  document.getElementById("terminal-form").onsubmit = async (e) => {
    e.preventDefault();
    const input = document.getElementById("terminal-input");
    const cmd = input.value;
    input.value = "";
    await runTerminalCommand(cmd);
  };

  document.getElementById("chk-sync-terminal").onchange = (e) => {
    state.syncTerminal = e.target.checked;
    if (state.syncTerminal) syncTerminalWithActivePane();
  };

  // Controles de personalización
  document.getElementById("select-theme").value = state.settings.theme;
  document.getElementById("input-accent").value = state.settings.accent;
  document.getElementById("select-density").value = state.settings.density;
  document.getElementById("select-keymode").value = state.settings.keymode;
  document.getElementById("input-fontsize").value = state.settings.fontSize;

  document.getElementById("select-theme").onchange = (e) => { state.settings.theme = e.target.value; applySettings(); };
  document.getElementById("input-accent").oninput = (e) => { state.settings.accent = e.target.value; applySettings(); };
  document.getElementById("select-density").onchange = (e) => { state.settings.density = e.target.value; applySettings(); };
  document.getElementById("select-keymode").onchange = (e) => { state.settings.keymode = e.target.value; applySettings(); };
  document.getElementById("input-fontsize").oninput = (e) => {
    state.settings.fontSize = e.target.value;
    document.getElementById("font-size-val").textContent = `${e.target.value}px`;
    applySettings();
  };
  document.getElementById("btn-reset-settings").onclick = () => {
    state.settings = { theme: "breeze-dark", accent: "#3daee9", density: "normal", keymode: "dolphin-vim", fontSize: "13" };
    applySettings();
  };
});

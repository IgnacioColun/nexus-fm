using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NexusFM
{
    // ========================================================================
    // 1. INTERCEPTOR NATIVO WIN32, SHELL ICONS Y REEMPLAZO DE OPEN FOLDER
    // ========================================================================
    public static class NativeWin32
    {
        public const int SHCNE_ATTRIBUTES = 0x00000800;
        public const int SHCNE_UPDATEDIR = 0x00001000;
        public const int SHCNE_UPDATEITEM = 0x00002000;
        public const int SHCNE_ASSOCCHANGED = 0x08000000;
        public const uint SHCNF_IDLIST = 0x0000;
        public const uint SHCNF_PATHW = 0x0005;
        public const uint SHCNF_FLUSH = 0x1000;
        public const uint SHCNF_FLUSHNOWAIT = 0x2000;

        public const uint WM_CLOSE = 0x0010;
        public const uint WM_SETTEXT = 0x000C;
        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_COMMAND = 0x0111;
        public const uint BM_CLICK = 0x00F5;
        public const int SW_RESTORE = 9;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const uint FCS_READ = 0x00000001;
        public const uint FCS_FORCEWRITE = 0x00000002;
        public const uint FCSM_INFOTIP = 0x00000004;
        public const uint FCSM_ICONFILE = 0x00000010;

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_LARGEICON = 0x000000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFOLDERCUSTOMSETTINGS
        {
            public uint dwSize;
            public uint dwMask;
            public IntPtr pvid;
            public string pszWebViewTemplate;
            public uint cchWebViewTemplate;
            public string pszWebViewTemplateVersion;
            public string pszInfoTip;
            public uint cchInfoTip;
            public IntPtr pclsid;
            public uint dwFlags;
            public string pszIconFile;
            public uint cchIconFile;
            public int iIconIndex;
            public string pszLogo;
            public uint cchLogo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHChangeNotify")]
        public static extern void SHChangeNotifyPath(int wEventId, uint uFlags, string dwItem1, IntPtr dwItem2);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHGetSetFolderCustomSettings(ref SHFOLDERCUSTOMSETTINGS pfcs, string pszPath, uint dwReadWrite);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ExtractIconExW")]
        public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PrivateExtractIconsW")]
        public static extern uint PrivateExtractIcons(string lpszFile, int nIconIndex, int cxIcon, int cyIcon, IntPtr[] phicon, uint[] piconid, uint nIcons, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr hWndCtl);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static string GetWindowClassName(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool IsCommonFileOrFolderDialog(IntPtr dlgHwnd, out bool isFolderBrowserControl)
        {
            isFolderBrowserControl = false;
            bool foundExplorerView = false;
            bool foundBrowseTree = false;

            EnumChildWindows(dlgHwnd, (child, lParam) =>
            {
                string cls = GetWindowClassName(child);
                if (cls == "DUIViewWndClassName" || cls == "SHELLDLL_DefView")
                {
                    foundExplorerView = true;
                    return false;
                }
                if (cls == "SHBrowseForFolder ShellNameSpace Control")
                {
                    foundBrowseTree = true;
                    foundExplorerView = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            isFolderBrowserControl = foundBrowseTree;
            return foundExplorerView;
        }

        public static IntPtr FindDialogPathEditControl(IntPtr dlgHwnd)
        {
            // 1. Control ID 1152 (0x0480) - Edit directo en diálogos Open Folder / File
            IntPtr edit1152 = GetDlgItem(dlgHwnd, 1152);
            if (edit1152 != IntPtr.Zero && GetWindowClassName(edit1152) == "Edit")
                return edit1152;

            // 2. Control ID 1148 (0x047C) - ComboBoxEx32 -> ComboBox -> Edit
            IntPtr ctl1148 = GetDlgItem(dlgHwnd, 1148);
            if (ctl1148 != IntPtr.Zero)
            {
                if (GetWindowClassName(ctl1148) == "Edit") return ctl1148;
                IntPtr foundInCombo = IntPtr.Zero;
                EnumChildWindows(ctl1148, (child, lParam) =>
                {
                    if (GetWindowClassName(child) == "Edit")
                    {
                        foundInCombo = child;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
                if (foundInCombo != IntPtr.Zero) return foundInCombo;
            }

            // 3. Fallback: Buscar cualquier Edit hijo de ComboBox o directo del diálogo (excluyendo la caja de búsqueda superior)
            IntPtr fallbackEdit = IntPtr.Zero;
            EnumChildWindows(dlgHwnd, (child, lParam) =>
            {
                if (GetWindowClassName(child) == "Edit")
                {
                    IntPtr parent = GetParent(child);
                    string pCls = parent != IntPtr.Zero ? GetWindowClassName(parent) : "";
                    if (parent == dlgHwnd || pCls == "ComboBox" || pCls == "ComboBoxEx32")
                    {
                        fallbackEdit = child;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);

            return fallbackEdit;
        }

        public static void NotifyFolderIconChanged(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            try
            {
                SHChangeNotifyPath(SHCNE_ATTRIBUTES, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, folderPath, IntPtr.Zero);
                SHChangeNotifyPath(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, folderPath, IntPtr.Zero);
                SHChangeNotifyPath(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, folderPath, IntPtr.Zero);
                var parent = Directory.GetParent(folderPath);
                if (parent != null && Directory.Exists(parent.FullName))
                {
                    SHChangeNotifyPath(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, parent.FullName, IntPtr.Zero);
                }
            }
            catch { }
        }

        private static DateTime _lastIe4uinitRun = DateTime.MinValue;

        public static void RefreshShell(bool triggerIconCacheReload = false)
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (Directory.Exists(desktop))
                {
                    SHChangeNotifyPath(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, desktop, IntPtr.Zero);
                    SHChangeNotifyPath(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, desktop, IntPtr.Zero);
                }

                if (triggerIconCacheReload && (DateTime.Now - _lastIe4uinitRun).TotalSeconds > 2.5)
                {
                    _lastIe4uinitRun = DateTime.Now;
                    Task.Run(() =>
                    {
                        try
                        {
                            string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                            string ie4Path = System.IO.Path.Combine(sysDir, "ie4uinit.exe");
                            if (File.Exists(ie4Path))
                            {
                                var psi = new ProcessStartInfo(ie4Path, "-show")
                                {
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };
                                using (var p = Process.Start(psi))
                                {
                                    if (p != null) p.WaitForExit(3000);
                                }
                            }
                        }
                        catch { }
                    });
                }
            }
            catch { }
        }
    }

    // ========================================================================
    // 1A. GESTOR DE DESCRIPCIONES DE CARPETAS Y RESUMEN AUTOMÁTICO DE CONTENIDO
    // ========================================================================
    public static class FolderDescriptionManager
    {
        private static readonly object _descLock = new object();
        private static readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool _loaded = false;

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            try
            {
                return System.IO.Path.GetFullPath(path).TrimEnd('\\');
            }
            catch
            {
                return path.Trim().TrimEnd('\\');
            }
        }

        private static string GetLocalAppDataDescriptionsPath()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusFM");
            Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "folder-descriptions.ini");
        }

        private static string GetRepoDescriptionsPath()
        {
            string exeDir = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            return System.IO.Path.Combine(exeDir, "nexus-descriptions.ini");
        }

        public static void EnsureLoaded()
        {
            lock (_descLock)
            {
                if (_loaded) return;
                _loaded = true;
                LoadFromFile(GetLocalAppDataDescriptionsPath());
                LoadFromFile(GetRepoDescriptionsPath());
            }
        }

        private static void LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;
                foreach (var line in File.ReadAllLines(filePath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith(";")) continue;
                    int sep = line.IndexOf('|');
                    if (sep <= 0) continue;
                    string path = NormalizePath(line.Substring(0, sep));
                    string desc = line.Substring(sep + 1).Replace("\\n", " ").Trim();
                    if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(desc))
                    {
                        _descriptions[path] = desc;
                    }
                }
            }
            catch { }
        }

        public static void SaveAll()
        {
            lock (_descLock)
            {
                try
                {
                    var sb = new StringBuilder();
                    foreach (var kv in _descriptions.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                        string cleanVal = kv.Value.Replace("\r", " ").Replace("\n", " ").Trim();
                        sb.AppendLine(kv.Key + "|" + cleanVal);
                    }
                    string content = sb.ToString();
                    try { File.WriteAllText(GetLocalAppDataDescriptionsPath(), content, Encoding.UTF8); } catch { }
                    try { File.WriteAllText(GetRepoDescriptionsPath(), content, Encoding.UTF8); } catch { }
                }
                catch { }
            }
        }

        public static string GetDescription(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return "";
            EnsureLoaded();
            string norm = NormalizePath(folderPath);

            lock (_descLock)
            {
                string cached;
                if (_descriptions.TryGetValue(norm, out cached))
                    return cached;
            }

            // Intentar leer desde desktop.ini dentro de la carpeta (por si la carpeta fue movida o renombrada)
            try
            {
                string iniPath = System.IO.Path.Combine(folderPath, "desktop.ini");
                if (File.Exists(iniPath))
                {
                    string foundDesc = null;
                    foreach (var rawLine in File.ReadAllLines(iniPath))
                    {
                        string line = rawLine.Trim();
                        if (line.StartsWith(";NexusDescription=", StringComparison.OrdinalIgnoreCase))
                        {
                            foundDesc = line.Substring(";NexusDescription=".Length).Trim();
                            break;
                        }
                        if (line.StartsWith("InfoTip=", StringComparison.OrdinalIgnoreCase))
                        {
                            string tip = line.Substring("InfoTip=".Length).Trim();
                            if (!tip.StartsWith("@", StringComparison.OrdinalIgnoreCase))
                                foundDesc = tip;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(foundDesc))
                    {
                        lock (_descLock)
                        {
                            _descriptions[norm] = foundDesc;
                        }
                        return foundDesc;
                    }
                }
            }
            catch { }

            return "";
        }

        public static void SetDescription(string folderPath, string description, string currentIcoPath, bool replaceExplorerActive)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;
            EnsureLoaded();
            string norm = NormalizePath(folderPath);
            string clean = (description ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

            lock (_descLock)
            {
                if (string.IsNullOrEmpty(clean))
                    _descriptions.Remove(norm);
                else
                    _descriptions[norm] = clean;
            }

            SaveAll();

            // Sincronizar también en el desktop.ini de la carpeta para que viaje con ella y se vea en Windows
            try
            {
                IconExtractor.ApplyFolderCustomIcon(folderPath, currentIcoPath, replaceExplorerActive, clean);
            }
            catch { }
        }

        public static void MigrateDescriptionPath(string oldPath, string newPath)
        {
            if (string.IsNullOrWhiteSpace(oldPath) || string.IsNullOrWhiteSpace(newPath)) return;
            EnsureLoaded();
            string oldNorm = NormalizePath(oldPath);
            string newNorm = NormalizePath(newPath);
            bool changed = false;

            lock (_descLock)
            {
                string desc;
                if (_descriptions.TryGetValue(oldNorm, out desc))
                {
                    _descriptions.Remove(oldNorm);
                    _descriptions[newNorm] = desc;
                    changed = true;
                }
            }
            if (changed) SaveAll();
        }

        public static string GetQuickContentSummary(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return "Carpeta";
            try
            {
                var di = new DirectoryInfo(folderPath);
                int subDirs = 0;
                int files = 0;
                foreach (var d in di.GetDirectories())
                {
                    if ((d.Attributes & FileAttributes.Hidden) == 0 && (d.Attributes & FileAttributes.System) == 0)
                        subDirs++;
                }
                foreach (var f in di.GetFiles())
                {
                    if ((f.Attributes & FileAttributes.Hidden) == 0 && (f.Attributes & FileAttributes.System) == 0)
                        files++;
                }

                if (subDirs == 0 && files == 0) return "Carpeta vacía";
                if (subDirs > 0 && files > 0)
                    return string.Format("{0} {1}, {2} {3}", subDirs, subDirs == 1 ? "carpeta" : "carpetas", files, files == 1 ? "archivo" : "archivos");
                if (subDirs > 0)
                    return string.Format("{0} {1} adentro", subDirs, subDirs == 1 ? "subcarpeta" : "subcarpetas");
                return string.Format("{0} {1} adentro", files, files == 1 ? "archivo" : "archivos");
            }
            catch
            {
                return "Carpeta";
            }
        }

        public static string GenerateSmartAutoDescription(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return "";

            try
            {
                var di = new DirectoryInfo(folderPath);
                var subDirs = di.GetDirectories()
                    .Where(d => (d.Attributes & FileAttributes.Hidden) == 0 && (d.Attributes & FileAttributes.System) == 0 && !d.Name.StartsWith("."))
                    .Select(d => d.Name)
                    .ToList();

                var files = di.GetFiles()
                    .Where(f => (f.Attributes & FileAttributes.Hidden) == 0 && (f.Attributes & FileAttributes.System) == 0 &&
                                !string.Equals(f.Name, "desktop.ini", StringComparison.OrdinalIgnoreCase) && !f.Name.StartsWith("."))
                    .ToList();

                if (subDirs.Count == 0 && files.Count == 0)
                    return "Carpeta vacía lista para organizar archivos.";

                var sampleNames = new List<string>();
                foreach (var sd in subDirs.Take(3))
                    sampleNames.Add(sd);
                foreach (var f in files.Take(4 - Math.Min(3, sampleNames.Count)))
                {
                    string cleanName = (f.Name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || f.Name.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                        ? f.Name.Substring(0, f.Name.Length - 4)
                        : f.Name;
                    sampleNames.Add(cleanName);
                }

                string examples = sampleNames.Count > 0 ? " (" + string.Join(", ", sampleNames) + (subDirs.Count + files.Count > sampleNames.Count ? "..." : "") + ")" : "";

                if (subDirs.Count > 0 && files.Count > 0)
                    return string.Format("Lleva {0} carpeta(s) y {1} archivo(s){2}", subDirs.Count, files.Count, examples);
                if (subDirs.Count > 0)
                    return string.Format("Lleva {0} subcarpeta(s){1}", subDirs.Count, examples);
                return string.Format("Lleva {0} archivo(s){1}", files.Count, examples);
            }
            catch
            {
                return "Carpeta de archivos y proyectos.";
            }
        }
    }

    // ========================================================================
    // 1B. EXTRACTOR Y COMPILADOR DE ICONOS DE JUEGOS, SOFTWARES Y ESCRITORIO
    // ========================================================================
    public static class IconExtractor
    {
        private static readonly object _cacheLock = new object();
        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _iconCache.Clear();
            }
        }

        // Directorio local inmutable en %LOCALAPPDATA%\NexusFM\assets para que los iconos NUNCA se rompan
        // al iniciar Windows (incluso antes de que cargue OneDrive) ni al mover la carpeta del proyecto
        public static string GetStableAssetsDir()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusFM", "assets");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetStableAppIconsDir()
        {
            string dir = System.IO.Path.Combine(GetStableAssetsDir(), "app-icons");
            Directory.CreateDirectory(dir);
            return dir;
        }

        // Escanea, repara y sincroniza todos los iconos de carpetas y accesos (.url / .lnk) en el Escritorio y ubicaciones del usuario
        public static void PrecompileAndRepairDesktopAppIcons(string activeFolderIcoPath = null, bool applyDesktopFolderIcons = true, IEnumerable<string> extraDirs = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(activeFolderIcoPath) && File.Exists(activeFolderIcoPath))
                {
                    ApplyNexusIconToDesktopFolders(activeFolderIcoPath, applyDesktopFolderIcons, extraDirs);
                }

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (Directory.Exists(desktop))
                {
                    ScanAndRepairShortcutsInDir(desktop, 4);
                }

                NativeWin32.RefreshShell(true);
            }
            catch { }
        }

        private static bool ShouldSkipDirectoryName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.StartsWith(".")) return true;
            return string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "target", StringComparison.OrdinalIgnoreCase);
        }

        private static void ScanAndRepairShortcutsInDir(string dirPath, int depth)
        {
            if (depth < 0 || !Directory.Exists(dirPath)) return;
            try
            {
                foreach (var f in Directory.GetFiles(dirPath))
                {
                    string ext = System.IO.Path.GetExtension(f).TrimStart('.').ToUpperInvariant();
                    if (ext == "URL" || ext == "LNK")
                    {
                        var dummy = new FileItem
                        {
                            Name = System.IO.Path.GetFileName(f),
                            FullPath = f,
                            IsDirectory = false,
                            Extension = ext
                        };
                        ImageSource extracted = (ext == "URL")
                            ? ResolveAndExtractUrlShortcutIcon(f, dummy.DisplayName)
                            : ResolveAndExtractLnkShortcutIcon(f, dummy.DisplayName);

                        if (extracted != null)
                        {
                            lock (_cacheLock)
                            {
                                _iconCache[f] = extracted;
                            }
                        }
                    }
                }
                if (depth > 0)
                {
                    foreach (var sub in Directory.GetDirectories(dirPath))
                    {
                        string name = System.IO.Path.GetFileName(sub);
                        if (ShouldSkipDirectoryName(name)) continue;
                        ScanAndRepairShortcutsInDir(sub, depth - 1);
                    }
                }
            }
            catch { }
        }

        // Genera un archivo .ico multi-resolución (256, 64, 48, 32, 16) tanto en %LOCALAPPDATA%\NexusFM\assets como en el repo
        public static string GenerateMultiSizeFolderIco(string repoAssetsDir, string accentHex)
        {
            string stableDir = GetStableAssetsDir();
            try
            {
                if (!string.IsNullOrEmpty(repoAssetsDir)) Directory.CreateDirectory(repoAssetsDir);
                if (string.IsNullOrWhiteSpace(accentHex)) accentHex = "#4DA2DB";
                string cleanHex = accentHex.TrimStart('#').ToUpperInvariant();

                string stableMainIco = System.IO.Path.Combine(stableDir, "nexus-folder.ico");
                string stableThemedIco = System.IO.Path.Combine(stableDir, "nexus-folder-" + cleanHex + ".ico");

                System.Drawing.Color mainColor = System.Drawing.ColorTranslator.FromHtml(accentHex);
                System.Drawing.Color darkColor = System.Drawing.Color.FromArgb(255,
                    (int)(mainColor.R * 0.58),
                    (int)(mainColor.G * 0.58),
                    (int)(mainColor.B * 0.58));
                System.Drawing.Color lightColor = System.Drawing.Color.FromArgb(255,
                    Math.Min(255, mainColor.R + 42),
                    Math.Min(255, mainColor.G + 42),
                    Math.Min(255, mainColor.B + 42));

                int[] sizes = new int[] { 256, 64, 48, 32, 16 };
                List<byte[]> pngFrames = new List<byte[]>();

                foreach (int sz in sizes)
                {
                    using (var bmp = RenderFolderBitmap(sz, mainColor, darkColor, lightColor))
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        pngFrames.Add(ms.ToArray());
                    }
                }

                SaveMultiFrameIco(sizes, pngFrames, stableMainIco);
                SaveMultiFrameIco(sizes, pngFrames, stableThemedIco);

                if (!string.IsNullOrEmpty(repoAssetsDir))
                {
                    try
                    {
                        SaveMultiFrameIco(sizes, pngFrames, System.IO.Path.Combine(repoAssetsDir, "nexus-folder.ico"));
                        SaveMultiFrameIco(sizes, pngFrames, System.IO.Path.Combine(repoAssetsDir, "nexus-folder-" + cleanHex + ".ico"));
                    }
                    catch { }
                }

                return stableThemedIco;
            }
            catch
            {
                return System.IO.Path.Combine(stableDir, "nexus-folder.ico");
            }
        }

        private static System.Drawing.Bitmap RenderFolderBitmap(int size, System.Drawing.Color mainColor, System.Drawing.Color darkColor, System.Drawing.Color lightColor)
        {
            var bmp = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.Clear(System.Drawing.Color.Transparent);

                float s = size / 64f;

                using (var backPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    System.Drawing.PointF[] pts = new System.Drawing.PointF[]
                    {
                        new System.Drawing.PointF(4 * s, 10 * s),
                        new System.Drawing.PointF(25 * s, 10 * s),
                        new System.Drawing.PointF(30 * s, 16 * s),
                        new System.Drawing.PointF(60 * s, 16 * s),
                        new System.Drawing.PointF(60 * s, 52 * s),
                        new System.Drawing.PointF(4 * s, 52 * s)
                    };
                    backPath.AddPolygon(pts);
                    using (var backBrush = new System.Drawing.SolidBrush(darkColor))
                        g.FillPath(backBrush, backPath);
                }

                using (var pocketShadow = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 0, 0, 0)))
                    g.FillRectangle(pocketShadow, 4 * s, 20 * s, 56 * s, Math.Max(1f, 2f * s));

                var frontRect = new System.Drawing.RectangleF(4 * s, 22 * s, 56 * s, 34 * s);
                using (var frontPath = RoundedRectGdi(frontRect, Math.Max(2f, 4f * s)))
                using (var frontBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    frontRect, lightColor, mainColor, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillPath(frontBrush, frontPath);
                }

                using (var topHighlight = new System.Drawing.Pen(System.Drawing.Color.FromArgb(155, 255, 255, 255), Math.Max(1f, 1.4f * s)))
                    g.DrawLine(topHighlight, 6 * s, 23 * s, 58 * s, 23 * s);

                using (var bottomLip = new System.Drawing.SolidBrush(darkColor))
                    g.FillRectangle(bottomLip, 5 * s, 54 * s, 54 * s, Math.Max(1f, 2f * s));

                if (size >= 32)
                {
                    float cx = 32 * s;
                    float cy = 39 * s;
                    float r = 6.5f * s;
                    System.Drawing.PointF[] diamond = new System.Drawing.PointF[]
                    {
                        new System.Drawing.PointF(cx, cy - r),
                        new System.Drawing.PointF(cx + r, cy),
                        new System.Drawing.PointF(cx, cy + r),
                        new System.Drawing.PointF(cx - r, cy)
                    };
                    using (var emblemFill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(45, 0, 0, 0)))
                    using (var emblemPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(170, 255, 255, 255), Math.Max(1f, 1.4f * s)))
                    {
                        g.FillPolygon(emblemFill, diamond);
                        g.DrawPolygon(emblemPen, diamond);
                    }
                }
            }
            return bmp;
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRectGdi(System.Drawing.RectangleF bounds, float radius)
        {
            float d = radius * 2f;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void SaveMultiFrameIco(int[] sizes, List<byte[]> frames, string icoPath)
        {
            using (var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var bw = new BinaryWriter(fs))
            {
                int count = sizes.Length;
                bw.Write((short)0);
                bw.Write((short)1);
                bw.Write((short)count);

                int offset = 6 + (16 * count);
                for (int i = 0; i < count; i++)
                {
                    int sz = sizes[i];
                    byte bSize = (sz >= 256) ? (byte)0 : (byte)sz;
                    bw.Write(bSize);
                    bw.Write(bSize);
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((short)1);
                    bw.Write((short)32);
                    bw.Write(frames[i].Length);
                    bw.Write(offset);
                    offset += frames[i].Length;
                }

                for (int i = 0; i < count; i++)
                    bw.Write(frames[i]);
            }
        }

        // Aplica o restaura el icono de carpeta de NexusFM a todas las carpetas del Escritorio de Windows (hasta 4 niveles)
        public static void ApplyNexusIconToDesktopFolders(string icoPath, bool enable, IEnumerable<string> extraFolders = null)
        {
            try
            {
                var dirsToUpdate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (Directory.Exists(desktop))
                {
                    CollectDirectoriesRecursive(desktop, 4, dirsToUpdate);
                }

                if (extraFolders != null)
                {
                    foreach (var ef in extraFolders)
                    {
                        if (!string.IsNullOrWhiteSpace(ef) && Directory.Exists(ef))
                        {
                            dirsToUpdate.Add(ef);
                            CollectDirectoriesRecursive(ef, 2, dirsToUpdate);
                        }
                    }
                }

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                foreach (var folderPath in dirsToUpdate)
                {
                    if (string.Equals(folderPath.TrimEnd('\\'), desktop.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(folderPath.TrimEnd('\\'), userProfile.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                        continue;

                    ApplyFolderCustomIcon(folderPath, icoPath, enable, null);
                }

                NativeWin32.RefreshShell(true);
            }
            catch { }
        }

        private static void CollectDirectoriesRecursive(string rootDir, int depth, HashSet<string> result)
        {
            if (depth <= 0 || string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return;
            try
            {
                foreach (var d in Directory.GetDirectories(rootDir))
                {
                    string name = System.IO.Path.GetFileName(d);
                    if (ShouldSkipDirectoryName(name)) continue;
                    result.Add(d);
                    if (depth > 1)
                    {
                        CollectDirectoriesRecursive(d, depth - 1, result);
                    }
                }
            }
            catch { }
        }

        public static void ApplyFolderCustomIcon(string folderPath, string icoPath, bool enable, string overrideDescription = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;
                string iniPath = System.IO.Path.Combine(folderPath, "desktop.ini");

                string backupIcon = null;
                string existingDesc = null;
                bool isNexusIni = false;

                if (File.Exists(iniPath))
                {
                    try
                    {
                        File.SetAttributes(iniPath, FileAttributes.Normal);
                        foreach (var rawLine in File.ReadAllLines(iniPath))
                        {
                            string line = rawLine.Trim();
                            if (line.IndexOf("nexus-folder", StringComparison.OrdinalIgnoreCase) >= 0)
                                isNexusIni = true;

                            if (line.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase) &&
                                line.IndexOf("nexus-folder", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                backupIcon = line.Substring("IconResource=".Length).Trim();
                            }
                            else if (line.StartsWith(";NexusBackupIcon=", StringComparison.OrdinalIgnoreCase))
                            {
                                backupIcon = line.Substring(";NexusBackupIcon=".Length).Trim();
                            }
                            else if (line.StartsWith(";NexusDescription=", StringComparison.OrdinalIgnoreCase))
                            {
                                existingDesc = line.Substring(";NexusDescription=".Length).Trim();
                            }
                            else if (line.StartsWith("InfoTip=", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(existingDesc))
                            {
                                string tip = line.Substring("InfoTip=".Length).Trim();
                                if (!tip.StartsWith("@", StringComparison.OrdinalIgnoreCase))
                                    existingDesc = tip;
                            }
                        }
                    }
                    catch { }
                }

                string folderDesc = (overrideDescription != null)
                    ? overrideDescription
                    : (!string.IsNullOrEmpty(existingDesc) ? existingDesc : FolderDescriptionManager.GetDescription(folderPath));

                if (enable && !string.IsNullOrEmpty(icoPath) && File.Exists(icoPath))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("[.ShellClassInfo]");
                    sb.AppendLine("IconResource=" + icoPath + ",0");
                    sb.AppendLine("IconFile=" + icoPath);
                    sb.AppendLine("IconIndex=0");
                    if (!string.IsNullOrWhiteSpace(folderDesc))
                    {
                        sb.AppendLine("InfoTip=" + folderDesc);
                        sb.AppendLine(";NexusDescription=" + folderDesc);
                    }
                    if (!string.IsNullOrWhiteSpace(backupIcon))
                    {
                        sb.AppendLine(";NexusBackupIcon=" + backupIcon);
                    }
                    sb.AppendLine("[ViewState]");
                    sb.AppendLine("Mode=");
                    sb.AppendLine("Vid=");
                    sb.AppendLine("FolderType=Generic");

                    try
                    {
                        if (File.Exists(iniPath))
                        {
                            File.SetAttributes(iniPath, FileAttributes.Normal);
                        }
                        File.WriteAllText(iniPath, sb.ToString(), Encoding.Unicode);
                        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                    }
                    catch { }

                    try
                    {
                        var dirInfo = new DirectoryInfo(folderPath);
                        if ((dirInfo.Attributes & FileAttributes.ReadOnly) == 0)
                            dirInfo.Attributes |= FileAttributes.ReadOnly;
                    }
                    catch { }

                    NativeWin32.NotifyFolderIconChanged(folderPath);
                }
                else
                {
                    if (!File.Exists(iniPath) && string.IsNullOrWhiteSpace(folderDesc)) return;

                    if (!isNexusIni && overrideDescription == null)
                    {
                        if (File.Exists(iniPath))
                            File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                        return;
                    }

                    if (!string.IsNullOrEmpty(backupIcon) || !string.IsNullOrWhiteSpace(folderDesc))
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("[.ShellClassInfo]");
                        if (!string.IsNullOrEmpty(backupIcon))
                            sb.AppendLine("IconResource=" + backupIcon);
                        if (!string.IsNullOrWhiteSpace(folderDesc))
                        {
                            sb.AppendLine("InfoTip=" + folderDesc);
                            sb.AppendLine(";NexusDescription=" + folderDesc);
                        }
                        File.SetAttributes(iniPath, FileAttributes.Normal);
                        File.WriteAllText(iniPath, sb.ToString(), Encoding.Unicode);
                        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                        var dirInfo = new DirectoryInfo(folderPath);
                        dirInfo.Attributes |= FileAttributes.ReadOnly;
                    }
                    else
                    {
                        if (File.Exists(iniPath))
                        {
                            File.SetAttributes(iniPath, FileAttributes.Normal);
                            File.Delete(iniPath);
                        }
                        var dirInfo = new DirectoryInfo(folderPath);
                        dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }
                    NativeWin32.NotifyFolderIconChanged(folderPath);
                }
            }
            catch { }
        }

        // Compila un icono .ico de alta resolución en %LOCALAPPDATA%\NexusFM\assets\app-icons (inmutable) y en el repo
        public static string CompileCustomAppIcon(string appTitle, bool isGame)
        {
            try
            {
                string stableOutDir = GetStableAppIconsDir();
                string exeDir = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string repoOutDir = System.IO.Path.Combine(exeDir, "assets", "app-icons");
                try { Directory.CreateDirectory(repoOutDir); } catch { }

                string safeName = new string(appTitle.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
                if (string.IsNullOrEmpty(safeName)) safeName = "app";
                string stableIcoPath = System.IO.Path.Combine(stableOutDir, safeName + ".ico");
                string repoIcoPath = System.IO.Path.Combine(repoOutDir, safeName + ".ico");

                if (File.Exists(stableIcoPath))
                {
                    if (!File.Exists(repoIcoPath))
                    {
                        try { File.Copy(stableIcoPath, repoIcoPath, true); } catch { }
                    }
                    return stableIcoPath;
                }

                // Seleccionar paleta vibrante según el nombre del juego/software
                string upper = appTitle.ToUpperInvariant();
                System.Drawing.Color cTop = System.Drawing.Color.FromArgb(255, 77, 162, 219);
                System.Drawing.Color cBot = System.Drawing.Color.FromArgb(255, 32, 85, 135);

                if (upper.Contains("FORTNITE")) { cTop = System.Drawing.Color.FromArgb(255, 138, 75, 255); cBot = System.Drawing.Color.FromArgb(255, 45, 145, 255); isGame = true; }
                else if (upper.Contains("RIOT") || upper.Contains("VALORANT") || upper.Contains("LEAGUE")) { cTop = System.Drawing.Color.FromArgb(255, 235, 59, 90); cBot = System.Drawing.Color.FromArgb(255, 145, 20, 42); isGame = true; }
                else if (upper.Contains("DAVINCI") || upper.Contains("RESOLVE")) { cTop = System.Drawing.Color.FromArgb(255, 255, 121, 63); cBot = System.Drawing.Color.FromArgb(255, 44, 62, 80); }
                else if (upper.Contains("WPS") || upper.Contains("PDF") || upper.Contains("OFFICE")) { cTop = System.Drawing.Color.FromArgb(255, 255, 82, 82); cBot = System.Drawing.Color.FromArgb(255, 179, 57, 57); }
                else if (isGame) { cTop = System.Drawing.Color.FromArgb(255, 26, 188, 156); cBot = System.Drawing.Color.FromArgb(255, 16, 105, 88); }

                string monogram = GetAppMonogram(appTitle);

                int[] sizes = new int[] { 256, 64, 48, 32, 16 };
                var frames = new List<byte[]>();

                foreach (int sz in sizes)
                {
                    using (var bmp = new System.Drawing.Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        g.Clear(System.Drawing.Color.Transparent);

                        float s = sz / 64f;
                        var rect = new System.Drawing.RectangleF(4 * s, 4 * s, 56 * s, 56 * s);
                        using (var path = RoundedRectGdi(rect, 13 * s))
                        using (var grad = new System.Drawing.Drawing2D.LinearGradientBrush(rect, cTop, cBot, System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
                        using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(190, 255, 255, 255), Math.Max(1f, 1.8f * s)))
                        {
                            g.FillPath(grad, path);
                            g.DrawPath(borderPen, path);
                        }

                        // Insignia interior
                        using (var font = new System.Drawing.Font("Segoe UI", Math.Max(7f, (monogram.Length > 1 ? 21f : 26f) * s), System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                        using (var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center })
                        using (var shadowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 0, 0, 0)))
                        using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        {
                            g.DrawString(monogram, font, shadowBrush, new System.Drawing.RectangleF(4 * s, 6 * s, 56 * s, 56 * s), sf);
                            g.DrawString(monogram, font, textBrush, rect, sf);
                        }

                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            frames.Add(ms.ToArray());
                        }
                    }
                }

                SaveMultiFrameIco(sizes, frames, stableIcoPath);
                try { SaveMultiFrameIco(sizes, frames, repoIcoPath); } catch { }
                return stableIcoPath;
            }
            catch
            {
                return null;
            }
        }

        private static string GetAppMonogram(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "◈";
            string clean = title.Replace("™", "").Replace("®", "").Replace(".lnk", "").Replace(".url", "").Trim();
            if (clean.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(4);
            string[] words = clean.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
                return (words[0][0].ToString() + words[1][0].ToString()).ToUpperInvariant();
            return clean.Substring(0, Math.Min(2, clean.Length)).ToUpperInvariant();
        }

        // Extrae o repara el icono real de cualquier juego (.url, .lnk, .exe) o software (100% Thread-Safe)
        public static ImageSource GetFileIcon(FileItem item)
        {
            if (item == null || item.IsDirectory || string.IsNullOrEmpty(item.FullPath)) return null;

            string ext = (item.Extension ?? "").ToUpperInvariant();
            if (ext != "EXE" && ext != "LNK" && ext != "URL" && ext != "MSI" &&
                ext != "ICO" && ext != "PNG" && ext != "JPG" && ext != "JPEG" && ext != "BMP")
            {
                return null;
            }

            lock (_cacheLock)
            {
                ImageSource cached;
                if (_iconCache.TryGetValue(item.FullPath, out cached))
                    return cached;
            }

            ImageSource result = null;
            try
            {
                if (ext == "PNG" || ext == "JPG" || ext == "JPEG" || ext == "BMP")
                {
                    result = LoadImageThumbnail(item.FullPath);
                }
                else if (ext == "ICO")
                {
                    result = LoadBestIcoFrame(item.FullPath) ?? ExtractHighResFromFile(item.FullPath, 0);
                }
                else if (ext == "URL")
                {
                    result = ResolveAndExtractUrlShortcutIcon(item.FullPath, item.DisplayName);
                }
                else if (ext == "LNK")
                {
                    result = ResolveAndExtractLnkShortcutIcon(item.FullPath, item.DisplayName);
                }
                else if (ext == "EXE" || ext == "MSI")
                {
                    result = ExtractHighResFromFile(item.FullPath, 0) ?? ExtractViaShellFileInfo(item.FullPath);
                }
            }
            catch { }

            if (result != null)
            {
                lock (_cacheLock)
                {
                    _iconCache[item.FullPath] = result;
                }
            }

            return result;
        }

        private static bool IsVolatileOneDriveAppIconPath(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath)) return false;
            string stableDir = GetStableAppIconsDir();
            if (iconPath.StartsWith(stableDir, StringComparison.OrdinalIgnoreCase)) return false;
            return iconPath.IndexOf(@"\assets\app-icons\", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static ImageSource ResolveAndExtractUrlShortcutIcon(string urlFilePath, string displayName)
        {
            try
            {
                string[] lines = File.ReadAllLines(urlFilePath);
                string iconFile = null;
                int iconIndex = 0;
                string urlTarget = "";

                foreach (var line in lines)
                {
                    string t = line.Trim();
                    if (t.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                        iconFile = Environment.ExpandEnvironmentVariables(t.Substring(9).Trim().Trim('"'));
                    else if (t.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(t.Substring(10).Trim(), out iconIndex);
                    else if (t.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                        urlTarget = t.Substring(4).Trim();
                }

                // Si el icono no es una ruta volátil de carpeta movida y existe en disco, cargarlo directamente
                if (!string.IsNullOrEmpty(iconFile) &&
                    !iconFile.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                    !IsVolatileOneDriveAppIconPath(iconFile) &&
                    File.Exists(iconFile))
                {
                    var img = LoadBestIcoFrame(iconFile) ?? ExtractHighResFromFile(iconFile, iconIndex);
                    if (img != null) return img;
                }

                // El icono del juego o software (.url) no existe o apunta a una carpeta movida -> Compilar en %LOCALAPPDATA%\NexusFM\assets\app-icons
                bool isGame = urlTarget.IndexOf("steam://", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              urlTarget.IndexOf("epicgames", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              urlFilePath.IndexOf("Games", StringComparison.OrdinalIgnoreCase) >= 0;

                string compiledIco = CompileCustomAppIcon(displayName, isGame);
                if (!string.IsNullOrEmpty(compiledIco) && File.Exists(compiledIco))
                {
                    if (!string.Equals(iconFile, compiledIco, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var outLines = new List<string>();
                            bool wroteIconFile = false;
                            bool wroteIconIndex = false;
                            foreach (var l in lines)
                            {
                                if (l.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                                {
                                    outLines.Add("IconFile=" + compiledIco);
                                    wroteIconFile = true;
                                }
                                else if (l.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                                {
                                    outLines.Add("IconIndex=0");
                                    wroteIconIndex = true;
                                }
                                else outLines.Add(l);
                            }
                            if (!wroteIconFile) outLines.Add("IconFile=" + compiledIco);
                            if (!wroteIconIndex) outLines.Add("IconIndex=0");
                            File.WriteAllLines(urlFilePath, outLines);
                            NativeWin32.SHChangeNotifyPath(NativeWin32.SHCNE_UPDATEITEM, NativeWin32.SHCNF_PATHW | NativeWin32.SHCNF_FLUSHNOWAIT, urlFilePath, IntPtr.Zero);
                        }
                        catch { }
                    }

                    return LoadBestIcoFrame(compiledIco) ?? ExtractHighResFromFile(compiledIco, 0);
                }
            }
            catch { }

            return null;
        }

        private static ImageSource ResolveAndExtractLnkShortcutIcon(string lnkFilePath, string displayName)
        {
            string targetPath = null;
            string iconLoc = null;
            int iconIndex = 0;

            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    object sc = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkFilePath });
                    if (sc != null)
                    {
                        targetPath = Convert.ToString(sc.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, sc, null));
                        string rawIcon = Convert.ToString(sc.GetType().InvokeMember("IconLocation", BindingFlags.GetProperty, null, sc, null));
                        if (!string.IsNullOrWhiteSpace(rawIcon))
                        {
                            int comma = rawIcon.LastIndexOf(',');
                            if (comma >= 0)
                            {
                                iconLoc = Environment.ExpandEnvironmentVariables(rawIcon.Substring(0, comma).Trim().Trim('"'));
                                int.TryParse(rawIcon.Substring(comma + 1).Trim(), out iconIndex);
                            }
                            else
                            {
                                iconLoc = Environment.ExpandEnvironmentVariables(rawIcon.Trim().Trim('"'));
                            }
                        }

                        bool isVolatileAppIcon = IsVolatileOneDriveAppIconPath(iconLoc);

                        // 1. Intentar extraer desde TargetPath real primero si existe en disco
                        if (!string.IsNullOrEmpty(targetPath))
                        {
                            targetPath = Environment.ExpandEnvironmentVariables(targetPath.Trim().Trim('"'));
                            if (File.Exists(targetPath))
                            {
                                // Si antes tenía una ruta volátil o vacía, apuntar al icono real o extraerlo
                                if (!string.IsNullOrEmpty(iconLoc) && !isVolatileAppIcon && File.Exists(iconLoc))
                                {
                                    var explicitImg = ExtractHighResFromFile(iconLoc, iconIndex) ?? LoadBestIcoFrame(iconLoc);
                                    if (explicitImg != null) return explicitImg;
                                }

                                var img = ExtractHighResFromFile(targetPath, 0) ?? LoadBestIcoFrame(targetPath) ?? ExtractViaShellFileInfo(targetPath);
                                if (img != null) return img;
                            }
                        }

                        // 2. Intentar extraer desde IconLocation explícito si existe y no es ruta volátil
                        if (!string.IsNullOrEmpty(iconLoc) && !isVolatileAppIcon && File.Exists(iconLoc))
                        {
                            var img = ExtractHighResFromFile(iconLoc, iconIndex) ?? LoadBestIcoFrame(iconLoc);
                            if (img != null) return img;
                        }

                        // 3. Si el icono falta o apuntaba a una carpeta movida -> Compilar en ruta estable %LOCALAPPDATA%\NexusFM\assets\app-icons
                        bool iconMissing = string.IsNullOrEmpty(iconLoc) || !File.Exists(iconLoc) || isVolatileAppIcon;
                        bool targetMissing = string.IsNullOrEmpty(targetPath) || (!File.Exists(targetPath) && !Directory.Exists(targetPath));

                        if (iconMissing && targetMissing)
                        {
                            bool isGame = lnkFilePath.IndexOf("Games", StringComparison.OrdinalIgnoreCase) >= 0;
                            string compiledIco = CompileCustomAppIcon(displayName, isGame);
                            if (!string.IsNullOrEmpty(compiledIco) && File.Exists(compiledIco))
                            {
                                if (!string.Equals(iconLoc, compiledIco, StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        sc.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { compiledIco + ",0" });
                                        sc.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
                                        NativeWin32.SHChangeNotifyPath(NativeWin32.SHCNE_UPDATEITEM, NativeWin32.SHCNF_PATHW | NativeWin32.SHCNF_FLUSHNOWAIT, lnkFilePath, IntPtr.Zero);
                                    }
                                    catch { }
                                }

                                return LoadBestIcoFrame(compiledIco) ?? ExtractHighResFromFile(compiledIco, 0);
                            }
                        }
                    }
                }
            }
            catch { }

            return ExtractViaShellFileInfo(lnkFilePath);
        }

        private static ImageSource LoadImageThumbnail(string path)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bi.DecodePixelWidth = 128;
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        private static ImageSource LoadBestIcoFrame(string path)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(path).ToUpperInvariant();
                if (ext == ".EXE" || ext == ".DLL") return null;

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                    if (decoder.Frames == null || decoder.Frames.Count == 0) return null;
                    BitmapFrame best = decoder.Frames
                        .OrderByDescending(f => (f.PixelWidth <= 256 ? f.PixelWidth : 0))
                        .FirstOrDefault() ?? decoder.Frames[0];
                    best.Freeze();
                    return best;
                }
            }
            catch { return null; }
        }

        private static ImageSource ExtractHighResFromFile(string filePath, int iconIndex)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            // 1. Intentar PrivateExtractIconsW a 128x128 para máxima nitidez
            try
            {
                IntPtr[] hIcons = new IntPtr[1];
                uint[] iconIds = new uint[1];
                uint count = NativeWin32.PrivateExtractIcons(filePath, iconIndex, 128, 128, hIcons, iconIds, 1, 0);
                if (count > 0 && hIcons[0] != IntPtr.Zero)
                {
                    try
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(hIcons[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        return src;
                    }
                    finally
                    {
                        NativeWin32.DestroyIcon(hIcons[0]);
                    }
                }
            }
            catch { }

            // 2. Fallback: ExtractIconExW
            try
            {
                IntPtr[] large = new IntPtr[1];
                IntPtr[] small = new IntPtr[1];
                uint count = NativeWin32.ExtractIconEx(filePath, iconIndex, large, small, 1);
                IntPtr hIcon = large[0] != IntPtr.Zero ? large[0] : small[0];
                if (count > 0 && hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        return src;
                    }
                    finally
                    {
                        if (large[0] != IntPtr.Zero) NativeWin32.DestroyIcon(large[0]);
                        if (small[0] != IntPtr.Zero) NativeWin32.DestroyIcon(small[0]);
                    }
                }
            }
            catch { }

            // 3. Fallback: System.Drawing.Icon.ExtractAssociatedIcon
            try
            {
                using (var ico = System.Drawing.Icon.ExtractAssociatedIcon(filePath))
                {
                    if (ico != null)
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        return src;
                    }
                }
            }
            catch { }

            return null;
        }

        private static ImageSource ExtractViaShellFileInfo(string filePath)
        {
            try
            {
                var shfi = new NativeWin32.SHFILEINFO();
                IntPtr res = NativeWin32.SHGetFileInfo(
                    filePath,
                    0,
                    ref shfi,
                    (uint)Marshal.SizeOf(shfi),
                    NativeWin32.SHGFI_ICON | NativeWin32.SHGFI_LARGEICON);

                if (res != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        return src;
                    }
                    finally
                    {
                        NativeWin32.DestroyIcon(shfi.hIcon);
                    }
                }
            }
            catch { }
            return null;
        }
    }

    // ========================================================================
    // 2. CONFIGURACIÓN PERSISTENTE DEL USUARIO
    // ========================================================================
    public class UserConfig
    {
        public bool ReplaceWindowsExplorer { get; set; }
        public bool AutoStartWithWindows { get; set; }
        public string ThemeName { get; set; }
        public string CustomAccentHex { get; set; }
        public string FolderModel { get; set; } // "Breeze", "Neon", "Cards", "Details"
        public double IconSize { get; set; }
        public bool ShowSidebar { get; set; }
        public bool ShowPreview { get; set; }
        public List<string> RecentLocations { get; set; }

        public UserConfig()
        {
            ReplaceWindowsExplorer = true;
            AutoStartWithWindows = true;
            ThemeName = "Deep Breeze Blue";
            CustomAccentHex = "";
            FolderModel = "Breeze";
            IconSize = 76;
            ShowSidebar = true;
            ShowPreview = false; // Limpio y profundo por defecto como en la imagen 2 (F11 abre inspector)
            RecentLocations = new List<string>();
        }

        private static string GetConfigPath()
        {
            string dir = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            return System.IO.Path.Combine(dir, "nexus-config.ini");
        }

        private static string GetLocalBackupConfigPath()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusFM");
            Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "nexus-config.ini");
        }

        public void AddRecentLocation(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            RecentLocations.RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
            RecentLocations.Insert(0, path);
            if (RecentLocations.Count > 6) RecentLocations.RemoveRange(6, RecentLocations.Count - 6);
        }

        public void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("ReplaceWindowsExplorer=" + ReplaceWindowsExplorer);
                sb.AppendLine("AutoStartWithWindows=" + AutoStartWithWindows);
                sb.AppendLine("ThemeName=" + ThemeName);
                sb.AppendLine("CustomAccentHex=" + CustomAccentHex);
                sb.AppendLine("FolderModel=" + FolderModel);
                sb.AppendLine("IconSize=" + (int)IconSize);
                sb.AppendLine("ShowSidebar=" + ShowSidebar);
                sb.AppendLine("ShowPreview=" + ShowPreview);
                sb.AppendLine("Recent=" + string.Join("|", RecentLocations));
                string txt = sb.ToString();
                try { File.WriteAllText(GetConfigPath(), txt, Encoding.UTF8); } catch { }
                try { File.WriteAllText(GetLocalBackupConfigPath(), txt, Encoding.UTF8); } catch { }
            }
            catch { }
        }

        public static UserConfig Load()
        {
            var cfg = new UserConfig();
            try
            {
                string p = GetConfigPath();
                if (!File.Exists(p)) p = GetLocalBackupConfigPath();
                if (!File.Exists(p)) return cfg;
                foreach (var line in File.ReadAllLines(p, Encoding.UTF8))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    if (k == "ReplaceWindowsExplorer") cfg.ReplaceWindowsExplorer = bool.Parse(v);
                    else if (k == "AutoStartWithWindows") cfg.AutoStartWithWindows = bool.Parse(v);
                    else if (k == "ThemeName") cfg.ThemeName = v;
                    else if (k == "CustomAccentHex") cfg.CustomAccentHex = v;
                    else if (k == "FolderModel") cfg.FolderModel = v;
                    else if (k == "IconSize") cfg.IconSize = double.Parse(v);
                    else if (k == "ShowSidebar") cfg.ShowSidebar = bool.Parse(v);
                    else if (k == "ShowPreview") cfg.ShowPreview = bool.Parse(v);
                    else if (k == "Recent" && !string.IsNullOrEmpty(v))
                        cfg.RecentLocations = v.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
            catch { }
            if (cfg.FolderModel == "Minimal") cfg.FolderModel = "Breeze";
            return cfg;
        }
    }

    public class FileItem
    {
        public string Name { get; set; }
        public string DisplayName
        {
            get
            {
                if (IsDirectory || string.IsNullOrEmpty(Name)) return Name;
                if (Name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                    Name.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                {
                    return Name.Substring(0, Name.Length - 4);
                }
                return Name;
            }
        }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public string Extension { get; set; }
        public long RawSize { get; set; }
        public string SizeText { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime AccessedDate { get; set; }
        public string ModifiedText { get; set; }
        public string Category { get; set; } // "DOC", "IMG", "AUD", "VID", "CODE", "OTHER"
        public string Description { get; set; } // Descripción personalizada de lo que lleva la carpeta adentro
        public string ContentSummary { get; set; } // Resumen automático del contenido (ej. "3 carpetas, 8 archivos")
    }

    public class TabSession
    {
        public string Title { get; set; }
        public string CurrentPath { get; set; }
        public List<string> History { get; set; }
        public int HistoryIndex { get; set; }
        public string FilterText { get; set; }
        public string CategoryFilter { get; set; } // "", "DOC", "IMG", "AUD", "VID", "CODE"

        public TabSession()
        {
            History = new List<string>();
            HistoryIndex = -1;
            FilterText = "";
            CategoryFilter = "";
        }
    }

    public class ThemePalette
    {
        public string Name { get; set; }
        public string BgCanvas { get; set; }
        public string BgChrome { get; set; }
        public string BgTitlebar { get; set; }
        public string BgPopup { get; set; }
        public string Accent { get; set; }
        public string AccentDark { get; set; }
        public string AccentLight { get; set; }
        public string CanvasFrame { get; set; }
        public string SelectBg { get; set; }
        public string HoverBg { get; set; }
        public string TextMain { get; set; }
        public string TextMuted { get; set; }
        public string Border { get; set; }

        public ThemePalette CloneWithAccent(string customAccentHex)
        {
            var copy = (ThemePalette)this.MemberwiseClone();
            if (string.IsNullOrEmpty(customAccentHex)) return copy;
            try
            {
                Color c = (Color)ColorConverter.ConvertFromString(customAccentHex);
                copy.Accent = customAccentHex;
                Color dark = Color.FromRgb((byte)(c.R * 0.62), (byte)(c.G * 0.62), (byte)(c.B * 0.62));
                Color light = Color.FromRgb(
                    (byte)Math.Min(255, c.R + 38),
                    (byte)Math.Min(255, c.G + 38),
                    (byte)Math.Min(255, c.B + 38));
                Color frame = Color.FromRgb((byte)(c.R * 0.55), (byte)(c.G * 0.55), (byte)(c.B * 0.55));
                Color sel = Color.FromArgb(65, c.R, c.G, c.B);
                Color hov = Color.FromArgb(32, c.R, c.G, c.B);

                copy.AccentDark = string.Format("#{0:X2}{1:X2}{2:X2}", dark.R, dark.G, dark.B);
                copy.AccentLight = string.Format("#{0:X2}{1:X2}{2:X2}", light.R, light.G, light.B);
                copy.CanvasFrame = string.Format("#{0:X2}{1:X2}{2:X2}", frame.R, frame.G, frame.B);
                copy.SelectBg = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", sel.A, sel.R, sel.G, sel.B);
                copy.HoverBg = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", hov.A, hov.R, hov.G, hov.B);
            }
            catch { }
            return copy;
        }
    }

    // ========================================================================
    // 3. MOTOR GRÁFICO VECTORIAL (GEOMETRÍA PLANA BREEZE + ICONOS REALES DE APPS)
    // ========================================================================
    public static class VectorStudio
    {
        public static SolidColorBrush Brush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        }

        // Crea un icono SVG Path de 16x16 nítido (nunca muestra símbolos rotos ▯)
        public static FrameworkElement IconPath(string svgPathData, string colorHex, double size = 14)
        {
            var path = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(svgPathData),
                Fill = Brush(colorHex),
                Stretch = Stretch.Uniform,
                Width = size,
                Height = size,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            return path;
        }

        // Geometrías SVG vectoriales limpias
        public const string SVG_FOLDER = "M 2,3 L 7,3 L 9,5 L 15,5 L 15,13 L 2,13 Z";
        public const string SVG_HOME = "M 8,2 L 1,8 L 3,8 L 3,14 L 7,14 L 7,10 L 9,10 L 9,14 L 13,14 L 13,8 L 15,8 Z";
        public const string SVG_DESKTOP = "M 1,2 L 15,2 L 15,11 L 9,11 L 9,13 L 12,13 L 12,14 L 4,14 L 4,13 L 7,13 L 7,11 L 1,11 Z M 2.5,3.5 L 2.5,9.5 L 13.5,9.5 L 13.5,3.5 Z";
        public const string SVG_DOC = "M 3,1 L 10,1 L 13,4 L 13,15 L 3,15 Z M 5,6 L 11,6 L 11,7.5 L 5,7.5 Z M 5,9 L 11,9 L 11,10.5 L 5,10.5 Z M 5,12 L 9,12 L 9,13.5 L 5,13.5 Z";
        public const string SVG_IMAGE = "M 1,2 L 15,2 L 15,14 L 1,14 Z M 2.5,3.5 L 2.5,12.5 L 13.5,12.5 L 13.5,3.5 Z M 5,5.5 A 1.2,1.2 0 1 1 5,8 A 1.2,1.2 0 1 1 5,5.5 Z M 3.5,11.5 L 6.5,8 L 9,10.5 L 11,8.5 L 12.5,11.5 Z";
        public const string SVG_AUDIO = "M 6,2 L 14,1 L 14,11 A 2.5,2.5 0 1 1 11.5,8.5 L 12.5,8.5 L 12.5,3.5 L 7.5,4.2 L 7.5,12.5 A 2.5,2.5 0 1 1 5,10 L 6,10 Z";
        public const string SVG_VIDEO = "M 1,3 L 11,3 L 11,7 L 15,4 L 15,12 L 11,9 L 11,13 L 1,13 Z";
        public const string SVG_CODE = "M 5,4 L 1,8 L 5,12 L 6,11 L 3,8 L 6,5 Z M 11,4 L 15,8 L 11,12 L 10,11 L 13,8 L 10,5 Z";
        public const string SVG_CLOCK = "M 8,1 A 7,7 0 1 1 7.99,1 Z M 8,2.5 A 5.5,5.5 0 1 0 8.01,2.5 Z M 7.3,4.5 L 8.7,4.5 L 8.7,8 L 11.2,9.8 L 10.4,10.9 L 7.3,8.7 Z";
        public const string SVG_DRIVE = "M 2,2 L 14,2 L 14,14 L 2,14 Z M 3.5,3.5 L 3.5,10 L 12.5,10 L 12.5,3.5 Z M 10.5,11.5 A 1,1 0 1 1 10.5,13 A 1,1 0 1 1 10.5,11.5 Z";
        public const string SVG_SEARCH = "M 6.5,1 A 5.5,5.5 0 1 1 2.6,10.4 L 6.5,1 Z M 6.5,2.8 A 3.7,3.7 0 1 0 6.51,2.8 Z M 10.5,9.5 L 15,14 L 13.8,15.2 L 9.3,10.7 Z";
        public const string SVG_SPLIT = "M 1,2 L 15,2 L 15,14 L 1,14 Z M 2.5,3.5 L 2.5,12.5 L 7.2,12.5 L 7.2,3.5 Z M 8.8,3.5 L 8.8,12.5 L 13.5,12.5 L 13.5,3.5 Z";
        public const string SVG_CHART = "M 1,13 L 15,13 L 15,14.5 L 1,14.5 Z M 2.5,7 L 5,7 L 5,12 L 2.5,12 Z M 6.8,3 L 9.3,3 L 9.3,12 L 6.8,12 Z M 11,5.5 L 13.5,5.5 L 13.5,12 L 11,12 Z";
        public const string SVG_EDIT = "M 11.5,1.5 L 14.5,4.5 L 5,14 L 2,14 L 2,11 Z";
        public const string SVG_GEAR = "M 7,1 L 9,1 L 9.4,3.1 L 11.2,3.9 L 13,2.7 L 14.4,4.1 L 13.2,5.9 L 14,7.7 L 16,8.1 L 16,10.1 L 13.9,10.5 L 13.1,12.3 L 14.3,14.1 L 12.9,15.5 L 11.1,14.3 L 9.3,15.1 L 8.9,17 L 6.9,17 L 6.5,14.9 L 4.7,14.1 L 2.9,15.3 L 1.5,13.9 L 2.7,12.1 L 1.9,10.3 L 0,9.9 L 0,7.9 L 2.1,7.5 L 2.9,5.7 L 1.7,3.9 L 3.1,2.5 L 4.9,3.7 L 6.7,2.9 Z M 8,6 A 3,3 0 1 0 8.01,6 Z";
        public const string SVG_TERM = "M 2,3 L 7,8 L 2,13 L 3.5,14.5 L 10,8 L 3.5,1.5 Z M 8,13 L 15,13 L 15,14.5 L 8,14.5 Z";
        public const string SVG_MOVE = "M 2,3 L 7,3 L 8.5,5 L 14,5 L 14,13 L 2,13 Z M 8,6.5 L 8,8.5 L 4.5,8.5 L 4.5,10.5 L 8,10.5 L 8,12.5 L 12,9.5 Z";
        public const string SVG_NOTE = "M 3,1 L 13,1 L 13,15 L 3,15 Z M 5,4 L 11,4 L 11,5.5 L 5,5.5 Z M 5,7.5 L 11,7.5 L 11,9 L 5,9 Z M 5,11 L 9,11 L 9,12.5 L 5,12.5 Z";

        // Dibuja la carpeta geométrica plana idéntica a la captura 2 (con pestaña angulada de dos tonos e indicador de descripción)
        public static FrameworkElement CreateFolderGraphic(double size, ThemePalette p, string model, bool hasDescription = false)
        {
            double w = size;
            double h = size * 0.78;
            var canvas = new Canvas { Width = w, Height = h };

            bool isNeon = string.Equals(model, "Neon", StringComparison.OrdinalIgnoreCase);

            // 1. Parte trasera con pestaña angulada
            string backPathStr = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "M {0:0.0},{1:0.0} L {2:0.0},{1:0.0} L {3:0.0},{4:0.0} L {5:0.0},{4:0.0} L {5:0.0},{6:0.0} L {0:0.0},{6:0.0} Z",
                w * 0.06, h * 0.08,
                w * 0.40,
                w * 0.47, h * 0.19,
                w * 0.94,
                h * 0.88);

            var backShape = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(backPathStr),
                Fill = Brush(isNeon ? "#101520" : p.AccentDark),
                Stroke = isNeon ? Brush(p.Accent) : null,
                StrokeThickness = isNeon ? 1.2 : 0
            };
            canvas.Children.Add(backShape);

            // 2. Hoja interior sutil cuando la carpeta tiene documentos/descripción
            if (hasDescription && size >= 28)
            {
                var paperSlip = new Rectangle
                {
                    Width = w * 0.36,
                    Height = h * 0.14,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Fill = new SolidColorBrush(Color.FromArgb(215, 240, 246, 252))
                };
                Canvas.SetLeft(paperSlip, w * 0.50);
                Canvas.SetTop(paperSlip, h * 0.15);
                canvas.Children.Add(paperSlip);
            }

            // 3. Línea de sombra interior sutil entre pestaña y bolsillo frontal
            var pocketLine = new Rectangle
            {
                Width = w * 0.88,
                Height = 1.6,
                Fill = new SolidColorBrush(Color.FromArgb(95, 0, 0, 0))
            };
            Canvas.SetLeft(pocketLine, w * 0.06);
            Canvas.SetTop(pocketLine, h * 0.27);
            canvas.Children.Add(pocketLine);

            // 4. Cuerpo frontal plano y limpio (Breeze Flat Geometry)
            var frontRect = new Rectangle
            {
                Width = w * 0.88,
                Height = h * 0.64,
                RadiusX = 2.5,
                RadiusY = 2.5,
                Fill = isNeon ? Brush("#171D2B") : Brush(p.Accent),
                Stroke = isNeon ? Brush(p.AccentLight) : new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                StrokeThickness = isNeon ? 1.5 : 0.8
            };
            Canvas.SetLeft(frontRect, w * 0.06);
            Canvas.SetTop(frontRect, h * 0.29);
            canvas.Children.Add(frontRect);

            // 5. Borde inferior sutil de profundidad
            var bottomLip = new Rectangle
            {
                Width = w * 0.88,
                Height = 2.0,
                Fill = Brush(p.AccentDark),
                Opacity = 0.78
            };
            Canvas.SetLeft(bottomLip, w * 0.06);
            Canvas.SetTop(bottomLip, h * 0.91);
            canvas.Children.Add(bottomLip);

            // 6. Líneas de etiqueta en la portada de la carpeta cuando tiene descripción personalizada
            if (hasDescription && size >= 36)
            {
                var line1 = new Rectangle
                {
                    Width = w * 0.34,
                    Height = Math.Max(1.5, h * 0.045),
                    RadiusX = 1,
                    RadiusY = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255))
                };
                Canvas.SetLeft(line1, w * 0.16);
                Canvas.SetTop(line1, h * 0.48);
                canvas.Children.Add(line1);

                var line2 = new Rectangle
                {
                    Width = w * 0.22,
                    Height = Math.Max(1.5, h * 0.045),
                    RadiusX = 1,
                    RadiusY = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(145, 255, 255, 255))
                };
                Canvas.SetLeft(line2, w * 0.16);
                Canvas.SetTop(line2, h * 0.59);
                canvas.Children.Add(line2);
            }

            return canvas;
        }

        public static FrameworkElement CreateFileGraphic(double size, ThemePalette p, string ext, FileItem item = null)
        {
            double boxH = size * 0.78;
            var grid = new Grid { Width = size, Height = boxH };

            ImageSource realIcon = (item != null) ? IconExtractor.GetFileIcon(item) : null;
            if (realIcon != null)
            {
                double imgSz = Math.Max(18, boxH * 0.88);
                var img = new Image
                {
                    Source = realIcon,
                    Width = imgSz,
                    Height = imgSz,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                grid.Children.Add(img);

                string upExt = (ext ?? "").ToUpperInvariant();
                if ((upExt == "LNK" || upExt == "URL") && size >= 36)
                {
                    double badgeSz = Math.Max(12, size * 0.20);
                    var badge = new Border
                    {
                        Width = badgeSz,
                        Height = badgeSz,
                        CornerRadius = new CornerRadius(3),
                        Background = Brush("#CC14181F"),
                        BorderBrush = Brush(p.Accent),
                        BorderThickness = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, size * 0.14, 2),
                        Child = new TextBlock
                        {
                            Text = "↗",
                            FontSize = Math.Max(8, badgeSz * 0.62),
                            FontWeight = FontWeights.Bold,
                            Foreground = Brush(p.AccentLight),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    grid.Children.Add(badge);
                }
                return grid;
            }

            double w = size * 0.68;
            double h = size * 0.78;
            string accentHex = GetExtColor(ext, p);
            var card = new Border
            {
                Width = w,
                Height = h,
                CornerRadius = new CornerRadius(4),
                Background = Brush("#1D222A"),
                BorderBrush = Brush(accentHex),
                BorderThickness = new Thickness(1.2, 1.2, 1.2, 3.0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = ext.Length > 4 ? ext.Substring(0, 4) : ext,
                FontSize = Math.Max(9, size * 0.17),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brush(accentHex)
            });

            card.Child = stack;
            grid.Children.Add(card);
            return grid;
        }

        public static string GetExtColor(string ext, ThemePalette p)
        {
            switch (ext)
            {
                case "CS":
                case "RS":
                case "CPP": return "#E67E22";
                case "JS":
                case "TS":
                case "JSON": return "#F1C40F";
                case "MD":
                case "TXT": return p.Accent;
                case "EXE":
                case "BAT":
                case "LNK":
                case "URL":
                case "MSI": return "#2ECC71";
                case "PNG":
                case "JPG":
                case "JPEG":
                case "ICO": return "#A29BFE";
                default: return "#7F8C8D";
            }
        }
    }

    // ========================================================================
    // 4. PANEL DE EXPLORACIÓN PROFUNDA (CON PESTAÑAS CONECTADAS AL MARCO)
    // ========================================================================
    public class NexusPaneControl : Grid
    {
        public string PaneId { get; private set; }
        public List<TabSession> Tabs { get; private set; }
        public int ActiveTabIndex { get; set; }
        public TabSession ActiveTab { get { return Tabs[ActiveTabIndex]; } }

        public List<FileItem> CurrentItems { get; private set; }
        public List<Border> RenderedTiles { get; private set; }
        public HashSet<string> SelectedPaths { get; private set; }
        public int CursorIndex { get; set; }
        public ScrollViewer CanvasScroll { get { return _canvasScroll; } }
        public Border CanvasFrameBorder { get { return _canvasFrameBorder; } }

        private MainWindow _owner;
        private StackPanel _tabsStack;
        private Border _canvasFrameBorder;
        private Border _currentFolderDescBar;
        private TextBlock _currentFolderDescText;
        private Border _detailsHeader;
        private ScrollViewer _canvasScroll;
        private Panel _itemsPanel;
        private Border _categoryBanner;
        private TextBlock _categoryBannerText;
        private FileSystemWatcher _watcher;

        public NexusPaneControl(string paneId, string initialPath, MainWindow owner)
        {
            PaneId = paneId;
            _owner = owner;
            Tabs = new List<TabSession>();
            ActiveTabIndex = 0;
            CurrentItems = new List<FileItem>();
            RenderedTiles = new List<Border>();
            SelectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CursorIndex = 0;

            Margin = new Thickness(6, 4, 6, 4);
            Focusable = true;

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Barra de pestañas siempre visible (como en la imagen 2)
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: Marco profundo del lienzo

            // 0. Barra de pestañas conectada al marco
            _tabsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, -1)
            };
            Panel.SetZIndex(_tabsStack, 5);
            Grid.SetRow(_tabsStack, 0);
            Children.Add(_tabsStack);

            // 1. Marco del lienzo profundo (#0F1318 con borde de 1px conectado a la pestaña activa)
            _canvasFrameBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 4, 4, 4)
            };

            var innerGrid = new Grid();
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Barra de descripción de la carpeta actual
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Banner de filtro por categoría
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Cabecera modo detalles
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: Archivos

            // 0. Barra superior de descripción de lo que lleva la carpeta actual adentro
            _currentFolderDescBar = new Border
            {
                Padding = new Thickness(14, 6, 14, 6),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var descDock = new DockPanel();
            var descBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var editCurrentDescBtn = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(9, 3, 9, 3),
                Margin = new Thickness(6, 0, 0, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = "Colocar o editar la descripción de lo que lleva esta carpeta adentro (F10)",
                Child = new TextBlock { Text = "✎ Colocar / Editar descripción (F10)", FontSize = 11.2, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White }
            };
            editCurrentDescBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                _owner.SetActivePane(this, false);
                _owner.OpenFolderDescriptionDialog(ActiveTab.CurrentPath);
            };
            descBtns.Children.Add(editCurrentDescBtn);
            DockPanel.SetDock(descBtns, Dock.Right);
            descDock.Children.Add(descBtns);

            _currentFolderDescText = new TextBlock
            {
                FontSize = 11.8,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            descDock.Children.Add(_currentFolderDescText);
            _currentFolderDescBar.Child = descDock;
            Grid.SetRow(_currentFolderDescBar, 0);
            innerGrid.Children.Add(_currentFolderDescBar);

            // Banner cuando se filtra por Documentos / Imágenes / Audio / Video / Código
            _categoryBanner = new Border
            {
                Padding = new Thickness(12, 6, 12, 6),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Visibility = Visibility.Collapsed
            };
            var bannerDock = new DockPanel();
            _categoryBannerText = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(_categoryBannerText, Dock.Left);
            bannerDock.Children.Add(_categoryBannerText);

            var clearFilterBtn = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock { Text = "Mostrar todos ✕", FontSize = 11.5, Foreground = Brushes.White }
            };
            clearFilterBtn.MouseLeftButtonDown += (s, e) =>
            {
                ActiveTab.CategoryFilter = "";
                _categoryBanner.Visibility = Visibility.Collapsed;
                ReadEntries();
                RenderItems();
            };
            DockPanel.SetDock(clearFilterBtn, Dock.Right);
            bannerDock.Children.Add(clearFilterBtn);
            _categoryBanner.Child = bannerDock;
            Grid.SetRow(_categoryBanner, 1);
            innerGrid.Children.Add(_categoryBanner);

            _detailsHeader = BuildDetailsHeader();
            Grid.SetRow(_detailsHeader, 2);
            innerGrid.Children.Add(_detailsHeader);

            _canvasScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(18, 14, 18, 14),
                AllowDrop = true
            };
            _canvasScroll.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _owner.SetActivePane(this);
                Focus();
            };
            _canvasScroll.MouseRightButtonUp += (s, e) =>
            {
                _owner.SetActivePane(this);
                _owner.ShowCustomContextMenu(null);
                e.Handled = true;
            };
            _canvasScroll.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                    e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
                }
                else e.Effects = DragDropEffects.None;
                e.Handled = true;
            };
            _canvasScroll.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                    bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                    if (paths != null && paths.Length > 0)
                        _owner.MoveOrCopyPathsToDestination(paths, ActiveTab.CurrentPath, !copy);
                    e.Handled = true;
                }
            };
            Grid.SetRow(_canvasScroll, 3);
            innerGrid.Children.Add(_canvasScroll);

            _canvasFrameBorder.Child = innerGrid;
            Grid.SetRow(_canvasFrameBorder, 1);
            Children.Add(_canvasFrameBorder);

            PreviewKeyDown += OnPanePreviewKeyDown;
            AddNewTab(initialPath);
        }

        private Border BuildDetailsHeader()
        {
            var border = new Border
            {
                Padding = new Thickness(18, 7, 18, 7),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Visibility = Visibility.Collapsed
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.0, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });

            g.Children.Add(CreateHeaderLabel("Nombre", 0));
            g.Children.Add(CreateHeaderLabel("Descripción / Lo que lleva adentro", 1));
            g.Children.Add(CreateHeaderLabel("Tipo", 2));
            g.Children.Add(CreateHeaderLabel("Tamaño", 3));
            g.Children.Add(CreateHeaderLabel("Modificado", 4));

            border.Child = g;
            return border;
        }

        private TextBlock CreateHeaderLabel(string text, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = VectorStudio.Brush("#7F8A96")
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        public void AddNewTab(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var tab = new TabSession
            {
                CurrentPath = path,
                Title = GetFolderTitle(path)
            };
            tab.History.Add(path);
            tab.HistoryIndex = 0;

            Tabs.Add(tab);
            ActiveTabIndex = Tabs.Count - 1;
            RenderTabsBar();
            LoadDirectory(path, false);
        }

        public void CloseCurrentTab()
        {
            if (Tabs.Count <= 1) return;
            Tabs.RemoveAt(ActiveTabIndex);
            ActiveTabIndex = Math.Max(0, Math.Min(ActiveTabIndex, Tabs.Count - 1));
            RenderTabsBar();
            LoadDirectory(ActiveTab.CurrentPath, false);
        }

        public void CycleTab()
        {
            if (Tabs.Count <= 1) return;
            ActiveTabIndex = (ActiveTabIndex + 1) % Tabs.Count;
            RenderTabsBar();
            LoadDirectory(ActiveTab.CurrentPath, false);
        }

        // Pestañas idénticas a la captura 2: siempre visibles y conectadas al lienzo oscuro
        public void RenderTabsBar()
        {
            var p = _owner.ActivePalette;
            _tabsStack.Children.Clear();

            for (int i = 0; i < Tabs.Count; i++)
            {
                int idx = i;
                bool active = (i == ActiveTabIndex);

                var tabBorder = new Border
                {
                    Background = VectorStudio.Brush(active ? p.BgCanvas : p.BgChrome),
                    BorderBrush = VectorStudio.Brush(active ? p.CanvasFrame : p.Border),
                    BorderThickness = new Thickness(1, 1, 1, active ? 0 : 1),
                    CornerRadius = new CornerRadius(4, 4, 0, 0),
                    Padding = new Thickness(12, 6, 10, 6),
                    Margin = new Thickness(0, 0, 3, 0),
                    Cursor = Cursors.Hand
                };

                var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var fIcon = VectorStudio.IconPath(VectorStudio.SVG_FOLDER, active ? p.Accent : p.TextMuted, 13);
                fIcon.Margin = new Thickness(0, 0, 7, 0);
                row.Children.Add(fIcon);

                row.Children.Add(new TextBlock
                {
                    Text = Tabs[i].Title,
                    Foreground = VectorStudio.Brush(active ? p.TextMain : p.TextMuted),
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var closeBtn = new TextBlock
                {
                    Text = "×",
                    FontSize = 13,
                    Foreground = VectorStudio.Brush(p.TextMuted),
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(3, 0, 3, 0)
                };
                closeBtn.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    if (Tabs.Count > 1)
                    {
                        ActiveTabIndex = idx;
                        CloseCurrentTab();
                    }
                };
                row.Children.Add(closeBtn);

                tabBorder.Child = row;
                string tabTargetDir = Tabs[i].CurrentPath;
                _owner.AttachFolderDropTarget(tabBorder, () => tabTargetDir, active ? p.BgCanvas : p.BgChrome, active ? p.CanvasFrame : p.Border);
                tabBorder.MouseLeftButtonDown += (s, e) =>
                {
                    _owner.SetActivePane(this);
                    ActiveTabIndex = idx;
                    RenderTabsBar();
                    LoadDirectory(ActiveTab.CurrentPath, false);
                };
                _tabsStack.Children.Add(tabBorder);
            }

            var addTabBtn = new Border
            {
                Padding = new Thickness(10, 4, 10, 4),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                ToolTip = "Nueva pestaña (Ctrl+T)",
                Child = new TextBlock
                {
                    Text = "+",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = VectorStudio.Brush(p.TextMuted),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            addTabBtn.MouseEnter += (s, e) => addTabBtn.Background = VectorStudio.Brush(p.HoverBg);
            addTabBtn.MouseLeave += (s, e) => addTabBtn.Background = Brushes.Transparent;
            addTabBtn.MouseLeftButtonDown += (s, e) => AddNewTab(ActiveTab.CurrentPath);
            _tabsStack.Children.Add(addTabBtn);
        }

        public static string GetFolderTitle(string path)
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.Equals(path.TrimEnd('\\'), user.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                return "Carpeta personal";

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.Equals(path.TrimEnd('\\'), desktop.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                return "Escritorio";

            string downloads = System.IO.Path.Combine(user, "Downloads");
            if (string.Equals(path.TrimEnd('\\'), downloads.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                return "Descargas";

            string trimmed = path.TrimEnd('\\');
            if (trimmed.EndsWith(":")) return "Disco (" + trimmed + ")";
            string name = System.IO.Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(name) ? path : name;
        }

        public static bool IsDownloadsDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloads = System.IO.Path.Combine(user, "Downloads");
            return string.Equals(path.TrimEnd('\\'), downloads.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        public void SetCategoryFilter(string catCode, string catTitle)
        {
            ActiveTab.CategoryFilter = catCode;
            if (string.IsNullOrEmpty(catCode))
            {
                _categoryBanner.Visibility = Visibility.Collapsed;
            }
            else
            {
                _categoryBanner.Background = VectorStudio.Brush(_owner.ActivePalette.SelectBg);
                _categoryBanner.BorderBrush = VectorStudio.Brush(_owner.ActivePalette.Accent);
                _categoryBannerText.Text = "Filtrando por tipo: " + catTitle + " en " + ActiveTab.Title;
                _categoryBannerText.Foreground = VectorStudio.Brush(_owner.ActivePalette.TextMain);
                _categoryBanner.Visibility = Visibility.Visible;
            }
            ReadEntries();
            RenderItems();
            _owner.UpdateBottomStatusBar();
        }

        public void LoadDirectory(string path, bool pushHistory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                string full = System.IO.Path.GetFullPath(path);
                if (!Directory.Exists(full)) return;

                ActiveTab.CurrentPath = full;
                ActiveTab.Title = GetFolderTitle(full);
                _owner.Config.AddRecentLocation(full);
                _owner.Config.Save();

                if (pushHistory)
                {
                    if (ActiveTab.HistoryIndex < ActiveTab.History.Count - 1)
                        ActiveTab.History.RemoveRange(ActiveTab.HistoryIndex + 1, ActiveTab.History.Count - ActiveTab.HistoryIndex - 1);
                    ActiveTab.History.Add(full);
                    ActiveTab.HistoryIndex = ActiveTab.History.Count - 1;
                }

                SetupWatcher(full);
                ReadEntries();
                RenderTabsBar();
                RenderItems();

                if (_owner.ActivePane == this)
                {
                    _owner.RefreshBreadcrumbs();
                    _owner.UpdateBottomStatusBar();
                    _owner.UpdateInfoDock(GetCurrentCursorItem());
                    _owner.SyncTerminalPrompt();
                }
            }
            catch { }
        }

        private void SetupWatcher(string path)
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                }
                _watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };
                FileSystemEventHandler handler = (s, e) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ReadEntries();
                        RenderItems();
                        _owner.UpdateBottomStatusBar();
                    }));
                };
                RenamedEventHandler renHandler = (s, e) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ReadEntries();
                        RenderItems();
                        _owner.UpdateBottomStatusBar();
                    }));
                };
                _watcher.Created += handler;
                _watcher.Deleted += handler;
                _watcher.Renamed += renHandler;
                _watcher.EnableRaisingEvents = true;
            }
            catch { }
        }

        public void PauseWatcher(bool pause)
        {
            try
            {
                if (_watcher != null)
                    _watcher.EnableRaisingEvents = !pause;
            }
            catch { }
        }

        public static string ClassifyExtension(string ext)
        {
            switch (ext)
            {
                case "MD":
                case "TXT":
                case "PDF":
                case "DOC":
                case "DOCX":
                case "XLS":
                case "XLSX":
                case "CSV":
                case "PPT":
                case "PPTX": return "DOC";
                case "PNG":
                case "JPG":
                case "JPEG":
                case "GIF":
                case "SVG":
                case "WEBP":
                case "BMP":
                case "ICO": return "IMG";
                case "MP3":
                case "WAV":
                case "FLAC":
                case "OGG":
                case "M4A": return "AUD";
                case "MP4":
                case "MKV":
                case "AVI":
                case "MOV":
                case "WEBM": return "VID";
                case "CS":
                case "RS":
                case "PY":
                case "JS":
                case "TS":
                case "HTML":
                case "CSS":
                case "JSON":
                case "TOML":
                case "YAML":
                case "XML":
                case "BAT":
                case "SH":
                case "CPP":
                case "H": return "CODE";
                default: return "OTHER";
            }
        }

        public void ReadEntries()
        {
            CurrentItems.Clear();
            string filter = (ActiveTab.FilterText ?? "").Trim().ToLowerInvariant();
            string catFilter = ActiveTab.CategoryFilter ?? "";

            try
            {
                var dir = new DirectoryInfo(ActiveTab.CurrentPath);

                if (string.IsNullOrEmpty(catFilter))
                {
                    foreach (var d in dir.GetDirectories())
                    {
                        try
                        {
                            if ((d.Attributes & FileAttributes.Hidden) != 0 || (d.Attributes & FileAttributes.System) != 0)
                                continue;
                            string folderDesc = FolderDescriptionManager.GetDescription(d.FullName);
                            string folderSummary = FolderDescriptionManager.GetQuickContentSummary(d.FullName);
                            if (!string.IsNullOrEmpty(filter) &&
                                !d.Name.ToLowerInvariant().Contains(filter) &&
                                (string.IsNullOrEmpty(folderDesc) || !folderDesc.ToLowerInvariant().Contains(filter)))
                                continue;

                            CurrentItems.Add(new FileItem
                            {
                                Name = d.Name,
                                FullPath = d.FullName,
                                IsDirectory = true,
                                Extension = "Carpeta",
                                RawSize = 0,
                                SizeText = folderSummary,
                                ModifiedDate = d.LastWriteTime,
                                CreatedDate = d.CreationTime,
                                AccessedDate = d.LastAccessTime,
                                ModifiedText = FormatRelativeTime(d.LastWriteTime),
                                Category = "DIR",
                                Description = folderDesc,
                                ContentSummary = folderSummary
                            });
                        }
                        catch { }
                    }
                }

                foreach (var f in dir.GetFiles())
                {
                    try
                    {
                        if ((f.Attributes & FileAttributes.Hidden) != 0 || (f.Attributes & FileAttributes.System) != 0)
                            continue;
                        if (!string.IsNullOrEmpty(filter) && !f.Name.ToLowerInvariant().Contains(filter))
                            continue;

                        string ext = string.IsNullOrEmpty(f.Extension) ? "FILE" : f.Extension.TrimStart('.').ToUpperInvariant();
                        string cat = ClassifyExtension(ext);
                        if (!string.IsNullOrEmpty(catFilter) && cat != catFilter)
                            continue;

                        bool isRecentDownload = (DateTime.Now - f.LastWriteTime).TotalHours <= 24;
                        string modText = (isRecentDownload && IsDownloadsDirectory(ActiveTab.CurrentPath))
                            ? "⬇ Reciente • " + FormatRelativeTime(f.LastWriteTime)
                            : FormatRelativeTime(f.LastWriteTime);

                        CurrentItems.Add(new FileItem
                        {
                            Name = f.Name,
                            FullPath = f.FullName,
                            IsDirectory = false,
                            Extension = ext,
                            RawSize = f.Length,
                            SizeText = FormatBytes(f.Length),
                            ModifiedDate = f.LastWriteTime,
                            CreatedDate = f.CreationTime,
                            AccessedDate = f.LastAccessTime,
                            ModifiedText = modText,
                            Category = cat
                        });
                    }
                    catch { }
                }

                // En la carpeta Descargas (Downloads), mostrar primero los archivos recién descargados arriba del todo
                if (IsDownloadsDirectory(ActiveTab.CurrentPath))
                {
                    CurrentItems = CurrentItems
                        .OrderByDescending(x => !x.IsDirectory && (DateTime.Now - x.ModifiedDate).TotalDays <= 7)
                        .ThenByDescending(x => x.ModifiedDate)
                        .ToList();
                }
            }
            catch { }

            if (CurrentItems.Count > 0)
            {
                CursorIndex = Math.Max(0, Math.Min(CursorIndex, CurrentItems.Count - 1));
                SelectedPaths.Clear();
                SelectedPaths.Add(CurrentItems[CursorIndex].FullPath);
            }
            else
            {
                CursorIndex = 0;
                SelectedPaths.Clear();
            }
        }

        private ToolTip BuildRichFolderToolTip(FileItem item, ThemePalette p)
        {
            var tipBorder = new Border
            {
                Background = VectorStudio.Brush(p.BgPopup),
                BorderBrush = VectorStudio.Brush(p.Accent),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 9, 12, 9),
                MaxWidth = 340
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = "📁 " + item.DisplayName,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = VectorStudio.Brush(p.TextMain),
                Margin = new Thickness(0, 0, 0, 4)
            });

            bool hasCustomDesc = !string.IsNullOrWhiteSpace(item.Description);
            stack.Children.Add(new TextBlock
            {
                Text = hasCustomDesc
                    ? "📝 Descripción: " + item.Description
                    : "📝 Sin descripción personalizada aún.",
                FontSize = 12,
                FontWeight = hasCustomDesc ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = VectorStudio.Brush(hasCustomDesc ? p.AccentLight : p.TextMuted),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "📦 Contenido adentro: " + (string.IsNullOrEmpty(item.ContentSummary) ? "Carpeta" : item.ContentSummary) + "  •  " + item.ModifiedText,
                FontSize = 11.2,
                Foreground = VectorStudio.Brush(p.TextMuted),
                TextWrapping = TextWrapping.Wrap
            });

            stack.Children.Add(new TextBlock
            {
                Text = "💡 Haz clic en [✎] o presiona F10 para colocar o editar su descripción",
                FontSize = 10.5,
                Foreground = VectorStudio.Brush(p.Accent),
                Margin = new Thickness(0, 5, 0, 0)
            });

            tipBorder.Child = stack;
            return new ToolTip
            {
                Content = tipBorder,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };
        }

        private Border CreateEditDescriptionMiniButton(FileItem item, ThemePalette p)
        {
            bool hasDesc = !string.IsNullOrWhiteSpace(item.Description);
            var btn = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = Cursors.Hand,
                Background = VectorStudio.Brush(hasDesc ? p.SelectBg : p.BgCanvas),
                BorderBrush = VectorStudio.Brush(hasDesc ? p.Accent : p.Border),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Colocar o editar descripción de lo que lleva '" + item.DisplayName + "' adentro (F10)",
                Child = new TextBlock
                {
                    Text = hasDesc ? "✎ Nota" : "＋ Nota",
                    FontSize = 10.2,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = VectorStudio.Brush(hasDesc ? p.AccentLight : p.TextMuted)
                }
            };
            btn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                _owner.SetActivePane(this, false);
                _owner.OpenFolderDescriptionDialog(item.FullPath);
            };
            return btn;
        }

        private void RefreshCurrentFolderDescriptionBanner()
        {
            if (_currentFolderDescBar == null || ActiveTab == null) return;
            var p = _owner.ActivePalette;
            string curPath = ActiveTab.CurrentPath;
            string curDesc = FolderDescriptionManager.GetDescription(curPath);
            bool hasDesc = !string.IsNullOrWhiteSpace(curDesc);

            _currentFolderDescBar.Background = VectorStudio.Brush(hasDesc ? p.SelectBg : p.BgChrome);
            _currentFolderDescBar.BorderBrush = VectorStudio.Brush(p.Border);

            var dock = _currentFolderDescBar.Child as DockPanel;
            if (dock != null && dock.Children.Count > 0)
            {
                var btnsStack = dock.Children[0] as StackPanel;
                if (btnsStack != null && btnsStack.Children.Count > 0)
                {
                    var editBtn = btnsStack.Children[0] as Border;
                    if (editBtn != null)
                    {
                        editBtn.Background = VectorStudio.Brush(p.BgCanvas);
                        editBtn.BorderBrush = VectorStudio.Brush(p.Accent);
                    }
                }
            }

            if (hasDesc)
            {
                _currentFolderDescText.Text = "📝 " + ActiveTab.Title + ": \"" + curDesc + "\"";
                _currentFolderDescText.Foreground = VectorStudio.Brush(p.AccentLight);
                _currentFolderDescText.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                string quick = FolderDescriptionManager.GetQuickContentSummary(curPath);
                _currentFolderDescText.Text = "📁 " + ActiveTab.Title + " (" + quick + ") — Sin descripción personalizada aún. Haz clic en [✎ Colocar / Editar descripción] para anotar qué lleva adentro.";
                _currentFolderDescText.Foreground = VectorStudio.Brush(p.TextMuted);
                _currentFolderDescText.FontWeight = FontWeights.Normal;
            }
        }

        public void RenderItems()
        {
            var p = _owner.ActivePalette;
            string model = _owner.Config.FolderModel;
            double zoom = _owner.Config.IconSize;

            RenderedTiles.Clear();
            _canvasFrameBorder.Background = VectorStudio.Brush(p.BgCanvas);
            _canvasFrameBorder.BorderBrush = VectorStudio.Brush(_owner.ActivePane == this ? p.CanvasFrame : p.Border);
            _detailsHeader.Background = VectorStudio.Brush(p.BgChrome);
            _detailsHeader.BorderBrush = VectorStudio.Brush(p.Border);
            RefreshCurrentFolderDescriptionBanner();

            bool isDetails = string.Equals(model, "Details", StringComparison.OrdinalIgnoreCase);
            bool isCards = string.Equals(model, "Cards", StringComparison.OrdinalIgnoreCase);
            _detailsHeader.Visibility = isDetails ? Visibility.Visible : Visibility.Collapsed;

            if (isDetails)
            {
                var listStack = new StackPanel { Orientation = Orientation.Vertical };
                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    int idx = i;
                    var item = CurrentItems[i];
                    bool isSelected = SelectedPaths.Contains(item.FullPath);
                    bool isCursor = (i == CursorIndex);
                    bool hasDesc = item.IsDirectory && !string.IsNullOrWhiteSpace(item.Description);

                    var rowBorder = new Border
                    {
                        Padding = new Thickness(12, 6, 12, 6),
                        Margin = new Thickness(0, 1, 0, 1),
                        CornerRadius = new CornerRadius(4),
                        Cursor = Cursors.Hand,
                        Background = isSelected ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent,
                        BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : Brushes.Transparent,
                        BorderThickness = new Thickness(isSelected || isCursor ? 1 : 0)
                    };
                    if (item.IsDirectory) rowBorder.ToolTip = BuildRichFolderToolTip(item, p);

                    var g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.0, GridUnitType.Star) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });

                    var nameStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    var miniIcon = item.IsDirectory
                        ? VectorStudio.CreateFolderGraphic(24, p, "Breeze", hasDesc)
                        : VectorStudio.CreateFileGraphic(22, p, item.Extension, item);
                    miniIcon.Margin = new Thickness(0, 0, 10, 0);
                    nameStack.Children.Add(miniIcon);
                    nameStack.Children.Add(new TextBlock
                    {
                        Text = item.DisplayName,
                        Foreground = VectorStudio.Brush(p.TextMain),
                        FontWeight = item.IsDirectory ? FontWeights.SemiBold : FontWeights.Normal,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        FontSize = 12.5
                    });
                    Grid.SetColumn(nameStack, 0);
                    g.Children.Add(nameStack);

                    // Columna 1: Descripción de la carpeta o resumen de lo que lleva adentro
                    var descDock = new DockPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 8, 0) };
                    if (item.IsDirectory)
                    {
                        var editBtn = CreateEditDescriptionMiniButton(item, p);
                        DockPanel.SetDock(editBtn, Dock.Right);
                        descDock.Children.Add(editBtn);
                    }
                    descDock.Children.Add(new TextBlock
                    {
                        Text = item.IsDirectory
                            ? (hasDesc ? "📝 " + item.Description : "📦 " + (item.ContentSummary ?? "Carpeta"))
                            : "",
                        Foreground = VectorStudio.Brush(hasDesc ? p.AccentLight : p.TextMuted),
                        FontStyle = hasDesc ? FontStyles.Normal : FontStyles.Italic,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        FontSize = 11.8
                    });
                    Grid.SetColumn(descDock, 1);
                    g.Children.Add(descDock);

                    var typeTb = new TextBlock { Text = item.Extension, Foreground = VectorStudio.Brush(p.TextMuted), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
                    Grid.SetColumn(typeTb, 2);
                    g.Children.Add(typeTb);

                    var sizeTb = new TextBlock { Text = item.SizeText, Foreground = VectorStudio.Brush(p.TextMuted), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
                    Grid.SetColumn(sizeTb, 3);
                    g.Children.Add(sizeTb);

                    var modTb = new TextBlock { Text = item.ModifiedText, Foreground = VectorStudio.Brush(p.TextMuted), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
                    Grid.SetColumn(modTb, 4);
                    g.Children.Add(modTb);

                    rowBorder.Child = g;
                    RenderedTiles.Add(rowBorder);
                    AttachTileEvents(rowBorder, idx, item);
                    listStack.Children.Add(rowBorder);
                }
                _itemsPanel = listStack;
                _canvasScroll.Content = listStack;
            }
            else if (isCards)
            {
                var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    int idx = i;
                    var item = CurrentItems[i];
                    bool isSelected = SelectedPaths.Contains(item.FullPath);
                    bool isCursor = (i == CursorIndex);
                    bool hasDesc = item.IsDirectory && !string.IsNullOrWhiteSpace(item.Description);

                    var card = new Border
                    {
                        Width = 264,
                        Height = 82,
                        Margin = new Thickness(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        CornerRadius = new CornerRadius(6),
                        Cursor = Cursors.Hand,
                        Background = isSelected ? VectorStudio.Brush(p.SelectBg) : VectorStudio.Brush(p.BgChrome),
                        BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : VectorStudio.Brush(p.Border),
                        BorderThickness = new Thickness(isSelected || isCursor ? 1.4 : 1)
                    };
                    if (item.IsDirectory) card.ToolTip = BuildRichFolderToolTip(item, p);

                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var icon = item.IsDirectory
                        ? VectorStudio.CreateFolderGraphic(44, p, "Breeze", hasDesc)
                        : VectorStudio.CreateFileGraphic(42, p, item.Extension, item);
                    icon.Margin = new Thickness(0, 0, 12, 0);
                    icon.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(icon, 0);
                    row.Children.Add(icon);

                    var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    var topTitleDock = new DockPanel();
                    if (item.IsDirectory)
                    {
                        var editBtn = CreateEditDescriptionMiniButton(item, p);
                        DockPanel.SetDock(editBtn, Dock.Right);
                        topTitleDock.Children.Add(editBtn);
                    }
                    topTitleDock.Children.Add(new TextBlock
                    {
                        Text = item.DisplayName,
                        Foreground = VectorStudio.Brush(p.TextMain),
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12.6,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    info.Children.Add(topTitleDock);

                    if (item.IsDirectory)
                    {
                        info.Children.Add(new TextBlock
                        {
                            Text = hasDesc ? item.Description : ("📦 " + (item.ContentSummary ?? "Carpeta")),
                            Foreground = VectorStudio.Brush(hasDesc ? p.AccentLight : p.TextMuted),
                            FontSize = 11.2,
                            FontWeight = hasDesc ? FontWeights.Medium : FontWeights.Normal,
                            TextWrapping = TextWrapping.Wrap,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            MaxHeight = 30,
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                        info.Children.Add(new TextBlock
                        {
                            Text = hasDesc ? ((item.ContentSummary ?? "Carpeta") + " • " + item.ModifiedText) : ("Clic en +Nota o F10 para describir • " + item.ModifiedText),
                            Foreground = VectorStudio.Brush(p.TextMuted),
                            FontSize = 10.2,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }
                    else
                    {
                        info.Children.Add(new TextBlock
                        {
                            Text = item.SizeText + " • " + item.Extension,
                            Foreground = VectorStudio.Brush(p.TextMuted),
                            FontSize = 11.2,
                            Margin = new Thickness(0, 3, 0, 0)
                        });
                        info.Children.Add(new TextBlock
                        {
                            Text = item.ModifiedText,
                            Foreground = VectorStudio.Brush(p.TextMuted),
                            FontSize = 10.2,
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }

                    Grid.SetColumn(info, 1);
                    row.Children.Add(info);

                    card.Child = row;
                    RenderedTiles.Add(card);
                    AttachTileEvents(card, idx, item);
                    wrap.Children.Add(card);
                }
                _itemsPanel = wrap;
                _canvasScroll.Content = wrap;
            }
            else
            {
                // Vista de carpetas geométricas planas Breeze / Cyber Neón con descripción debajo del título
                var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
                double tileW = zoom + 54;
                double tileH = zoom + 62;

                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    int idx = i;
                    var item = CurrentItems[i];
                    bool isSelected = SelectedPaths.Contains(item.FullPath);
                    bool isCursor = (i == CursorIndex);
                    bool hasDesc = item.IsDirectory && !string.IsNullOrWhiteSpace(item.Description);

                    var tile = new Border
                    {
                        Width = tileW,
                        Height = tileH,
                        Margin = new Thickness(8, 6, 8, 6),
                        Padding = new Thickness(6, 6, 6, 6),
                        CornerRadius = new CornerRadius(5),
                        Cursor = Cursors.Hand,
                        Background = isSelected ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent,
                        BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : Brushes.Transparent,
                        BorderThickness = new Thickness(isSelected || isCursor ? 1.3 : 0)
                    };
                    if (item.IsDirectory) tile.ToolTip = BuildRichFolderToolTip(item, p);

                    var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                    var art = item.IsDirectory
                        ? VectorStudio.CreateFolderGraphic(zoom, p, model, hasDesc)
                        : VectorStudio.CreateFileGraphic(zoom, p, item.Extension, item);
                    stack.Children.Add(art);

                    var lbl = new TextBlock
                    {
                        Text = item.DisplayName,
                        Foreground = VectorStudio.Brush(p.TextMain),
                        FontWeight = item.IsDirectory ? FontWeights.SemiBold : FontWeights.Normal,
                        FontSize = 12.2,
                        TextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = tileW - 10,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    stack.Children.Add(lbl);

                    if (item.IsDirectory)
                    {
                        var descLbl = new TextBlock
                        {
                            Text = hasDesc ? item.Description : (item.ContentSummary ?? ""),
                            Foreground = VectorStudio.Brush(hasDesc ? p.AccentLight : p.TextMuted),
                            FontSize = 10.4,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            MaxWidth = tileW - 8,
                            MaxHeight = 28,
                            Margin = new Thickness(0, 1, 0, 0)
                        };
                        stack.Children.Add(descLbl);
                    }

                    tile.Child = stack;
                    RenderedTiles.Add(tile);
                    AttachTileEvents(tile, idx, item);
                    wrap.Children.Add(tile);
                }

                _itemsPanel = wrap;
                _canvasScroll.Content = wrap;
            }

            UpdateSelectionVisuals();
        }

        // Actualiza colores de selección y resaltado sin destruir los elementos visuales (preserva el arrastre con mouse)
        public void UpdateSelectionVisuals()
        {
            var p = _owner.ActivePalette;
            string model = _owner.Config.FolderModel;
            bool isDetails = string.Equals(model, "Details", StringComparison.OrdinalIgnoreCase);
            bool isCards = string.Equals(model, "Cards", StringComparison.OrdinalIgnoreCase);

            _canvasFrameBorder.BorderBrush = VectorStudio.Brush(_owner.ActivePane == this ? p.CanvasFrame : p.Border);

            int count = Math.Min(CurrentItems.Count, RenderedTiles.Count);
            for (int i = 0; i < count; i++)
            {
                var item = CurrentItems[i];
                var tile = RenderedTiles[i];
                bool isSelected = SelectedPaths.Contains(item.FullPath);
                bool isCursor = (i == CursorIndex);
                bool isBeingDragged = _owner.IsPathBeingDragged(item.FullPath);
                bool isKbDropTarget = _owner.IsKeyboardDragActive && _owner.ActivePane == this && isCursor && item.IsDirectory && !isBeingDragged;

                tile.Opacity = isBeingDragged ? 0.45 : 1.0;

                if (isKbDropTarget)
                {
                    tile.Background = VectorStudio.Brush("#262ECC71");
                    tile.BorderBrush = VectorStudio.Brush("#2ECC71");
                    tile.BorderThickness = new Thickness(2.2);
                }
                else if (isCards)
                {
                    tile.Background = isSelected ? VectorStudio.Brush(p.SelectBg) : VectorStudio.Brush(p.BgChrome);
                    tile.BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : VectorStudio.Brush(p.Border);
                    tile.BorderThickness = new Thickness(isSelected || isCursor ? 1.4 : 1);
                }
                else if (isDetails)
                {
                    tile.Background = isSelected ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent;
                    tile.BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : Brushes.Transparent;
                    tile.BorderThickness = new Thickness(isSelected || isCursor ? 1 : 0);
                }
                else
                {
                    tile.Background = isSelected ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent;
                    tile.BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : Brushes.Transparent;
                    tile.BorderThickness = new Thickness(isSelected || isCursor ? 1.3 : 0);
                }
            }

            if (_owner.IsKeyboardDragActive && _owner.ActivePane == this)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    _owner.UpdateKeyboardDragGhostPosition();
                }));
            }
        }

        private Point _dragStartWindowPoint;
        private bool _isMouseDownOnTile;
        private bool _isVisualDragging;

        private void AttachTileEvents(Border tile, int idx, FileItem item)
        {
            var p = _owner.ActivePalette;
            bool isCards = string.Equals(_owner.Config.FolderModel, "Cards", StringComparison.OrdinalIgnoreCase);

            tile.MouseEnter += (s, e) =>
            {
                if (!_isVisualDragging && !SelectedPaths.Contains(item.FullPath))
                    tile.Background = VectorStudio.Brush(p.HoverBg);
            };

            tile.MouseLeave += (s, e) =>
            {
                if (!_isVisualDragging && !SelectedPaths.Contains(item.FullPath))
                    tile.Background = isCards ? VectorStudio.Brush(p.BgChrome) : Brushes.Transparent;
            };

            tile.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                _owner.SetActivePane(this, false);
                Focus();

                // Si el usuario estaba arrastrando con el teclado y hace clic en una carpeta destino, soltarla ahí
                if (_owner.IsKeyboardDragActive && item.IsDirectory && !_owner.IsPathBeingDragged(item.FullPath))
                {
                    _owner.CompleteKeyboardDragInto(item.FullPath);
                    return;
                }

                CursorIndex = idx;

                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (SelectedPaths.Contains(item.FullPath)) SelectedPaths.Remove(item.FullPath);
                    else SelectedPaths.Add(item.FullPath);
                }
                else
                {
                    if (!SelectedPaths.Contains(item.FullPath) || e.ClickCount == 1)
                    {
                        SelectedPaths.Clear();
                        SelectedPaths.Add(item.FullPath);
                    }
                }

                UpdateSelectionVisuals();
                _owner.UpdateInfoDock(item);
                _owner.UpdateBottomStatusBar();

                if (e.ClickCount == 2)
                {
                    _isMouseDownOnTile = false;
                    _isVisualDragging = false;
                    if (tile.IsMouseCaptured) tile.ReleaseMouseCapture();
                    OpenCursorItem();
                    return;
                }

                _dragStartWindowPoint = e.GetPosition(_owner);
                _isMouseDownOnTile = true;
                _isVisualDragging = false;
                tile.CaptureMouse();
            };

            tile.MouseMove += (s, e) =>
            {
                if (!_isMouseDownOnTile || e.LeftButton != MouseButtonState.Pressed) return;
                Point winPos = e.GetPosition(_owner);
                if (!_isVisualDragging)
                {
                    if (Math.Abs(winPos.X - _dragStartWindowPoint.X) > 5 || Math.Abs(winPos.Y - _dragStartWindowPoint.Y) > 5)
                    {
                        _isVisualDragging = true;
                        if (!SelectedPaths.Contains(item.FullPath))
                        {
                            SelectedPaths.Clear();
                            SelectedPaths.Add(item.FullPath);
                        }
                        _owner.StartVisualMouseDrag(this, item, GetSelectedItems(), winPos);
                    }
                }

                if (_isVisualDragging)
                {
                    _owner.UpdateVisualMouseDrag(winPos);
                }
            };

            tile.MouseLeftButtonUp += (s, e) =>
            {
                if (tile.IsMouseCaptured) tile.ReleaseMouseCapture();
                _isMouseDownOnTile = false;
                if (_isVisualDragging)
                {
                    _isVisualDragging = false;
                    Point winPos = e.GetPosition(_owner);
                    bool isCopy = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    _owner.CompleteVisualMouseDrag(winPos, isCopy);
                }
            };

            // También mantener soporte para soltar archivos arrastrados desde fuera de NexusFM
            if (item.IsDirectory)
            {
                tile.AllowDrop = true;
                tile.DragEnter += (s, e) =>
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        tile.Background = VectorStudio.Brush("#262ECC71");
                        tile.BorderBrush = VectorStudio.Brush("#2ECC71");
                        tile.BorderThickness = new Thickness(2);
                        bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                        e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
                    }
                    else e.Effects = DragDropEffects.None;
                    e.Handled = true;
                };
                tile.DragOver += (s, e) =>
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                        e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
                    }
                    else e.Effects = DragDropEffects.None;
                    e.Handled = true;
                };
                tile.DragLeave += (s, e) =>
                {
                    UpdateSelectionVisuals();
                };
                tile.Drop += (s, e) =>
                {
                    e.Handled = true;
                    UpdateSelectionVisuals();
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
                        bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                        if (dropped != null && dropped.Length > 0)
                        {
                            _owner.MoveOrCopyPathsToDestination(dropped, item.FullPath, !copy);
                        }
                    }
                };
            }

            tile.MouseRightButtonUp += (s, e) =>
            {
                e.Handled = true;
                _owner.SetActivePane(this, false);
                Focus();
                CursorIndex = idx;
                if (!SelectedPaths.Contains(item.FullPath))
                {
                    SelectedPaths.Clear();
                    SelectedPaths.Add(item.FullPath);
                }
                UpdateSelectionVisuals();
                _owner.UpdateInfoDock(item);
                _owner.UpdateBottomStatusBar();
                _owner.ShowCustomContextMenu(item);
            };
        }

        public FileItem GetCurrentCursorItem()
        {
            if (CurrentItems.Count == 0 || CursorIndex < 0 || CursorIndex >= CurrentItems.Count)
                return null;
            return CurrentItems[CursorIndex];
        }

        public List<FileItem> GetSelectedItems()
        {
            var list = CurrentItems.Where(x => SelectedPaths.Contains(x.FullPath)).ToList();
            if (list.Count == 0 && GetCurrentCursorItem() != null)
                list.Add(GetCurrentCursorItem());
            return list;
        }

        public void OpenCursorItem()
        {
            var item = GetCurrentCursorItem();
            if (item == null) return;

            if (item.IsDirectory)
            {
                CursorIndex = 0;
                LoadDirectory(item.FullPath, true);
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
                }
                catch { }
            }
        }

        public void GoUp()
        {
            try
            {
                var parent = Directory.GetParent(ActiveTab.CurrentPath);
                if (parent != null)
                {
                    CursorIndex = 0;
                    LoadDirectory(parent.FullName, true);
                }
            }
            catch { }
        }

        public void GoBack()
        {
            if (ActiveTab.HistoryIndex > 0)
            {
                ActiveTab.HistoryIndex--;
                LoadDirectory(ActiveTab.History[ActiveTab.HistoryIndex], false);
            }
        }

        public void GoForward()
        {
            if (ActiveTab.HistoryIndex < ActiveTab.History.Count - 1)
            {
                ActiveTab.HistoryIndex++;
                LoadDirectory(ActiveTab.History[ActiveTab.HistoryIndex], false);
            }
        }

        private void OnPanePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox) return;

            int count = CurrentItems.Count;
            bool isDetails = string.Equals(_owner.Config.FolderModel, "Details", StringComparison.OrdinalIgnoreCase);
            bool isCards = string.Equals(_owner.Config.FolderModel, "Cards", StringComparison.OrdinalIgnoreCase);

            int cols = 1;
            if (!isDetails && _canvasScroll.ActualWidth > 0)
            {
                double tileW = isCards ? 276 : (_owner.Config.IconSize + 70);
                cols = Math.Max(1, (int)((_canvasScroll.ActualWidth - 36) / tileW));
            }

            bool isShiftHeld = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (e.Key == Key.Right || (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.None && !isDetails))
            {
                if (isShiftHeld && !_owner.IsKeyboardDragActive) _owner.StartKeyboardFolderDrag(GetSelectedItems());
                MoveCursorBy(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Left || (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.None && !isDetails))
            {
                if (isShiftHeld && !_owner.IsKeyboardDragActive) _owner.StartKeyboardFolderDrag(GetSelectedItems());
                MoveCursorBy(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Down || (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.None))
            {
                if (isShiftHeld && !_owner.IsKeyboardDragActive) _owner.StartKeyboardFolderDrag(GetSelectedItems());
                MoveCursorBy(cols);
                e.Handled = true;
            }
            else if (e.Key == Key.Up || (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.None))
            {
                if (isShiftHeld && !_owner.IsKeyboardDragActive) _owner.StartKeyboardFolderDrag(GetSelectedItems());
                MoveCursorBy(-cols);
                e.Handled = true;
            }
            else if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (_owner.IsKeyboardDragActive) _owner.ConfirmKeyboardFolderDragDrop();
                else _owner.StartKeyboardFolderDrag(GetSelectedItems());
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                MoveCursorBy(-count);
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                MoveCursorBy(count);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && _owner.HasActiveExternalFileDialog)
                {
                    _owner.ConfirmExternalFileDialogSelection(null);
                    e.Handled = true;
                }
                else if (_owner.IsKeyboardDragActive)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        OpenCursorItem();
                    else
                        _owner.ConfirmKeyboardFolderDragDrop();
                    e.Handled = true;
                }
                else
                {
                    OpenCursorItem();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.None)
            {
                _owner.OpenMoveToFolderOverlay();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                GoUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (_owner.IsKeyboardDragActive)
                {
                    _owner.ConfirmKeyboardFolderDragDrop();
                    e.Handled = true;
                    return;
                }
                var item = GetCurrentCursorItem();
                if (item != null)
                {
                    if (SelectedPaths.Contains(item.FullPath)) SelectedPaths.Remove(item.FullPath);
                    else SelectedPaths.Add(item.FullPath);
                    UpdateSelectionVisuals();
                    _owner.UpdateBottomStatusBar();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    CycleTab();
                else
                    _owner.SwitchActivePane();
                e.Handled = true;
            }
        }

        private void MoveCursorBy(int delta)
        {
            if (CurrentItems.Count == 0) return;
            CursorIndex = Math.Max(0, Math.Min(CurrentItems.Count - 1, CursorIndex + delta));
            SelectedPaths.Clear();
            SelectedPaths.Add(CurrentItems[CursorIndex].FullPath);
            UpdateSelectionVisuals();

            if (_itemsPanel != null && CursorIndex < _itemsPanel.Children.Count)
            {
                var el = _itemsPanel.Children[CursorIndex] as FrameworkElement;
                if (el != null) el.BringIntoView();
            }

            _owner.UpdateInfoDock(GetCurrentCursorItem());
            _owner.UpdateBottomStatusBar();
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            string[] units = { "KiB", "MiB", "GiB", "TiB" };
            double val = bytes;
            int idx = -1;
            while (val >= 1024 && idx < units.Length - 1)
            {
                val /= 1024;
                idx++;
            }
            return string.Format("{0:0.0} {1}", val, units[idx]);
        }

        public static string FormatRelativeTime(DateTime dt)
        {
            TimeSpan diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "Hace un momento";
            if (diff.TotalMinutes < 60) return string.Format("Hace {0} min", (int)diff.TotalMinutes);
            if (diff.TotalHours < 24) return string.Format("Hace {0} h", (int)diff.TotalHours);
            return dt.ToString("yyyy-MM-dd HH:mm");
        }
    }

    // ========================================================================
    // 5. VENTANA PRINCIPAL DE NEXUSFM (SUITE COMPLETA Y PERSONALIZACIÓN INTEGRADA)
    // ========================================================================
    public class MainWindow : Window
    {
        public UserConfig Config { get; private set; }
        public NexusPaneControl LeftPane { get; private set; }
        public NexusPaneControl RightPane { get; private set; }
        public NexusPaneControl ActivePane { get; private set; }

        public bool IsSplitView { get; private set; }
        public bool IsTerminalOpen { get; private set; }

        public List<ThemePalette> Themes { get; private set; }
        public ThemePalette BasePalette { get; private set; }
        public ThemePalette ActivePalette { get; private set; }

        private Border _outerWindowBorder;
        private Border _customTitleBar;
        private TextBlock _titleBarFolderLabel;
        private Border _toolbarBorder;
        private Border _sidebarBorder;
        private Border _infoBorder;
        private Border _statusBorder;
        private Border _terminalBorder;
        private Grid _panesGrid;

        // Paneles deslizables integrados (Sin ventanas naranjas externas)
        private Border _settingsDrawer;
        private Border _analyzerOverlay;
        private Border _editorOverlay;
        private Border _moveToFolderOverlay;
        private TextBox _moveSearchBox;
        private StackPanel _moveResultsStack;
        private TextBlock _moveOverlayHeaderTitle;
        private TextBlock _moveOverlaySubtitle;
        private List<Tuple<string, string>> _moveCandidateFolders;
        private List<Tuple<string, string>> _moveFilteredFolders;
        private int _moveSelectedIndex;

        // Interceptor de diálogo "Open Folder" (#32770) de cualquier software externo
        private Border _openFolderPickerBanner;
        private TextBlock _openFolderPickerBannerText;
        private IntPtr _activeExternalDialogHwnd = IntPtr.Zero;
        private NativeWin32.RECT _activeExternalDialogOrigRect;
        private string _activeExternalDialogTitle = "";
        private readonly HashSet<IntPtr> _ignoredExternalDialogs = new HashSet<IntPtr>();
        public bool HasActiveExternalFileDialog { get { return _activeExternalDialogHwnd != IntPtr.Zero && NativeWin32.IsWindow(_activeExternalDialogHwnd); } }

        // Motor Visual de Arrastre de Iconos (Mouse Drag & Keyboard Drag)
        private Canvas _dragOverlayCanvas;
        private Border _dragGhostBorder;
        private Border _dragGhostIconHolder;
        private TextBlock _dragGhostTitle;
        private TextBlock _dragGhostSubtitle;
        private Border _keyboardDragBanner;
        private TextBlock _keyboardDragBannerText;
        private List<string> _activeDragPaths = new List<string>();
        private FileItem _activeDragLeadItem;
        private bool _isMouseDragActive;
        private bool _isKeyboardDragActive;
        private Border _hoveredDropTargetBorder;
        private Brush _hoveredDropTargetOrigBg;
        private Brush _hoveredDropTargetOrigBorder;
        private Thickness _hoveredDropTargetOrigThickness;
        private string _hoveredDropTargetPath;
        private readonly List<Tuple<Border, Func<string>>> _registeredDropTargets = new List<Tuple<Border, Func<string>>>();

        public bool IsKeyboardDragActive { get { return _isKeyboardDragActive && _activeDragPaths != null && _activeDragPaths.Count > 0; } }

        public bool IsPathBeingDragged(string path)
        {
            if ((!_isMouseDragActive && !_isKeyboardDragActive) || _activeDragPaths == null || string.IsNullOrEmpty(path))
                return false;
            return _activeDragPaths.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
        }

        private TextBox _codeEditorBox;
        private TextBlock _codeEditorTitle;
        private string _editingFilePath;

        private Border _btnModelBreeze;
        private Border _btnModelNeon;
        private Border _btnModelCards;
        private Border _btnModelDetails;

        private Border _breadcrumbsBox;
        private StackPanel _breadcrumbsPanel;
        private TextBox _pathInputBox;
        private TextBox _searchBox;
        private Border _btnDescribeFolderToggle;
        private Border _btnMoveToFolderToggle;
        private Border _btnSplitToggle;
        private Border _btnAnalyzerToggle;
        private Border _btnSettingsToggle;

        private Border _infoArtworkHolder;
        private TextBlock _infoTitle;
        private StackPanel _infoMetaTable;
        private TextBox _infoTextPreview;
        private TextBlock _hashResultText;

        private TextBlock _statusLeftLabel;
        private Slider _zoomSlider;
        private ProgressBar _diskSpaceBar;
        private TextBlock _diskSpaceLabel;

        private TextBox _termOutput;
        private TextBlock _termPrompt;
        private TextBox _termInput;

        private List<string> _clipboardPaths;
        private bool _clipboardIsCut;
        private Popup _contextPopup;
        private DispatcherTimer _explorerInterceptorTimer;
        private bool _isSilentStartupBoot;
        public string ActiveStableFolderIcoPath { get; private set; }

        public MainWindow(string startupPath, bool silentStartupBoot = false)
        {
            _isSilentStartupBoot = silentStartupBoot;
            InjectModernDarkScrollBarStyle();
            FolderDescriptionManager.EnsureLoaded();

            Config = UserConfig.Load();
            _clipboardPaths = new List<string>();
            IsSplitView = false;
            IsTerminalOpen = false;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Title = "NexusFM";
            Width = 1300;
            Height = 810;
            MinWidth = 940;
            MinHeight = 560;
            MaxHeight = SystemParameters.WorkArea.Height + 10;
            MaxWidth = SystemParameters.WorkArea.Width + 10;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            InitThemes();
            BasePalette = Themes.FirstOrDefault(t => t.Name == Config.ThemeName) ?? Themes[0];
            ActivePalette = BasePalette.CloneWithAccent(Config.CustomAccentHex);

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Barra título propia
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Barra navegación
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Banner Open Folder para Software Externo
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Banner Arrastre por Teclado
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 4: Centro + Drawers integrados
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5: Terminal (F4)
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 6: Barra estado inferior

            _customTitleBar = BuildCustomTitleBar();
            Grid.SetRow(_customTitleBar, 0);
            rootGrid.Children.Add(_customTitleBar);

            _toolbarBorder = BuildToolbar();
            Grid.SetRow(_toolbarBorder, 1);
            rootGrid.Children.Add(_toolbarBorder);

            _openFolderPickerBanner = BuildOpenFolderPickerBanner();
            Grid.SetRow(_openFolderPickerBanner, 2);
            rootGrid.Children.Add(_openFolderPickerBanner);

            _keyboardDragBanner = BuildKeyboardDragBanner();
            Grid.SetRow(_keyboardDragBanner, 3);
            rootGrid.Children.Add(_keyboardDragBanner);

            var centerContainer = new Grid();

            var workspaceGrid = new Grid();
            workspaceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Sidebar
            workspaceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Panes
            workspaceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Info Panel / Settings Drawer

            _sidebarBorder = BuildSidebar();
            Grid.SetColumn(_sidebarBorder, 0);
            workspaceGrid.Children.Add(_sidebarBorder);

            _panesGrid = new Grid();
            _panesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _panesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });

            string interceptedPath = TryInterceptOpenWindowsExplorer();
            string initialLeft = !string.IsNullOrEmpty(startupPath) && Directory.Exists(startupPath)
                ? startupPath
                : (!string.IsNullOrEmpty(interceptedPath) && Directory.Exists(interceptedPath)
                    ? interceptedPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            string initialRight = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            LeftPane = new NexusPaneControl("left", initialLeft, this);
            RightPane = new NexusPaneControl("right", initialRight, this) { Visibility = Visibility.Collapsed };

            Grid.SetColumn(LeftPane, 0);
            Grid.SetColumn(RightPane, 1);
            _panesGrid.Children.Add(LeftPane);
            _panesGrid.Children.Add(RightPane);

            // Overlays integrados dentro del área central (Analizador, Editor y Buscador para Mover Carpeta)
            _analyzerOverlay = BuildDiskSpaceAnalyzerOverlay();
            _editorOverlay = BuildIntegratedCodeEditorOverlay();
            _moveToFolderOverlay = BuildMoveToFolderOverlay();
            _panesGrid.Children.Add(_analyzerOverlay);
            _panesGrid.Children.Add(_editorOverlay);
            _panesGrid.Children.Add(_moveToFolderOverlay);
            Grid.SetColumnSpan(_analyzerOverlay, 2);
            Grid.SetColumnSpan(_editorOverlay, 2);
            Grid.SetColumnSpan(_moveToFolderOverlay, 2);

            Grid.SetColumn(_panesGrid, 1);
            workspaceGrid.Children.Add(_panesGrid);

            var rightDocksHolder = new StackPanel { Orientation = Orientation.Horizontal };
            _infoBorder = BuildInfoDock();
            _settingsDrawer = BuildIntegratedSettingsDrawer();
            rightDocksHolder.Children.Add(_infoBorder);
            rightDocksHolder.Children.Add(_settingsDrawer);

            Grid.SetColumn(rightDocksHolder, 2);
            workspaceGrid.Children.Add(rightDocksHolder);

            centerContainer.Children.Add(workspaceGrid);
            Grid.SetRow(centerContainer, 4);
            rootGrid.Children.Add(centerContainer);

            _terminalBorder = BuildTerminalDock();
            Grid.SetRow(_terminalBorder, 5);
            rootGrid.Children.Add(_terminalBorder);

            _statusBorder = BuildStatusBar();
            Grid.SetRow(_statusBorder, 6);
            rootGrid.Children.Add(_statusBorder);

            _dragOverlayCanvas = BuildDragOverlayCanvas();
            var windowOverlayGrid = new Grid();
            windowOverlayGrid.Children.Add(rootGrid);
            windowOverlayGrid.Children.Add(_dragOverlayCanvas);

            _outerWindowBorder = new Border
            {
                BorderThickness = new Thickness(1),
                Child = windowOverlayGrid
            };
            Content = _outerWindowBorder;

            PreviewKeyDown += OnGlobalPreviewKeyDown;
            ApplyCurrentThemeAndConfig();
            SetActivePane(LeftPane);

            StartExplorerInterceptorTimer();
            ApplyWindowsStartupRegistry(Config.AutoStartWithWindows);
            if (Config.ReplaceWindowsExplorer)
            {
                ApplyWindowsExplorerRegistry(true);
            }

            Loaded += (s, e) =>
            {
                if (_isSilentStartupBoot)
                {
                    Hide();
                }
                else
                {
                    LeftPane.Focus();
                }

                ScheduleStartupIconSyncWaves();
            };
        }

        private void ScheduleStartupIconSyncWaves()
        {
            // Oleada 1: Inmediata al abrir o iniciar Windows
            Task.Run(() =>
            {
                IconExtractor.PrecompileAndRepairDesktopAppIcons(ActiveStableFolderIcoPath, Config.ReplaceWindowsExplorer, Config.RecentLocations);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (LeftPane != null) LeftPane.RenderItems();
                    if (IsSplitView && RightPane != null) RightPane.RenderItems();
                }));
            });

            // Oleada 2: A los 4.5 segundos (cuando explorer.exe termina de inicializar el Escritorio tras el arranque de Windows)
            var wave2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
            wave2.Tick += (s, e) =>
            {
                wave2.Stop();
                Task.Run(() =>
                {
                    if (Config.ReplaceWindowsExplorer)
                        IconExtractor.ApplyNexusIconToDesktopFolders(ActiveStableFolderIcoPath, true, Config.RecentLocations);
                    NativeWin32.RefreshShell(true);
                });
            };
            wave2.Start();

            // Oleada 3: A los 12 segundos (cuando OneDrive termina de montar carpetas del Escritorio al iniciar Windows)
            if (_isSilentStartupBoot)
            {
                var wave3 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12.0) };
                wave3.Tick += (s, e) =>
                {
                    wave3.Stop();
                    Task.Run(() =>
                    {
                        IconExtractor.PrecompileAndRepairDesktopAppIcons(ActiveStableFolderIcoPath, Config.ReplaceWindowsExplorer, Config.RecentLocations);
                    });
                };
                wave3.Start();
            }
        }

        // Reemplaza la barra de desplazamiento blanca clásica de Windows por una píldora oscura de 6px (como en la imagen 2)
        private void InjectModernDarkScrollBarStyle()
        {
            string xaml = @"
            <ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                <Style TargetType='{x:Type Thumb}'>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='{x:Type Thumb}'>
                                <Border Background='#3D4652' CornerRadius='3' Margin='1'/>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
                <Style TargetType='{x:Type ScrollBar}'>
                    <Setter Property='Width' Value='7'/>
                    <Setter Property='MinWidth' Value='7'/>
                    <Setter Property='Background' Value='Transparent'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='{x:Type ScrollBar}'>
                                <Grid Background='Transparent' Width='7'>
                                    <Track Name='PART_Track' IsDirectionReversed='true'>
                                        <Track.Thumb>
                                            <Thumb/>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                var dict = (ResourceDictionary)XamlReader.Parse(xaml);
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch { }
        }

        private void InitThemes()
        {
            Themes = new List<ThemePalette>
            {
                new ThemePalette
                {
                    Name = "Deep Breeze Blue",
                    BgCanvas = "#0F1318",
                    BgChrome = "#1D2126",
                    BgTitlebar = "#181B20",
                    BgPopup = "#22272E",
                    Accent = "#4DA2DB",
                    AccentDark = "#2C6B97",
                    AccentLight = "#6EC1F8",
                    CanvasFrame = "#235478",
                    SelectBg = "#1C3B52",
                    HoverBg = "#172736",
                    TextMain = "#EFF0F1",
                    TextMuted = "#8A949E",
                    Border = "#2A3038"
                },
                new ThemePalette
                {
                    Name = "Nexus Emerald",
                    BgCanvas = "#0E1214",
                    BgChrome = "#1A1E22",
                    BgTitlebar = "#15181C",
                    BgPopup = "#21262C",
                    Accent = "#1ABC9C",
                    AccentDark = "#117A65",
                    AccentLight = "#48C9B0",
                    CanvasFrame = "#166958",
                    SelectBg = "#193D37",
                    HoverBg = "#152927",
                    TextMain = "#EFF0F1",
                    TextMuted = "#8A949E",
                    Border = "#282E34"
                },
                new ThemePalette
                {
                    Name = "Catppuccin Mocha",
                    BgCanvas = "#11111B",
                    BgChrome = "#181825",
                    BgTitlebar = "#14141F",
                    BgPopup = "#1E1E2E",
                    Accent = "#CBA6F7",
                    AccentDark = "#8963BA",
                    AccentLight = "#DEC2FF",
                    CanvasFrame = "#5D467E",
                    SelectBg = "#2D2342",
                    HoverBg = "#1F1A2E",
                    TextMain = "#CDD6F4",
                    TextMuted = "#9399B2",
                    Border = "#2B2C3F"
                },
                new ThemePalette
                {
                    Name = "Cyberpunk Matrix",
                    BgCanvas = "#07090E",
                    BgChrome = "#0E121B",
                    BgTitlebar = "#0A0D14",
                    BgPopup = "#141A29",
                    Accent = "#00F0FF",
                    AccentDark = "#008C9E",
                    AccentLight = "#75F8FF",
                    CanvasFrame = "#006C7A",
                    SelectBg = "#0E3340",
                    HoverBg = "#0B202A",
                    TextMain = "#E6F9FF",
                    TextMuted = "#6C82A6",
                    Border = "#1B2438"
                },
                new ThemePalette
                {
                    Name = "Crimson Obsidian",
                    BgCanvas = "#0F0C0E",
                    BgChrome = "#1A1518",
                    BgTitlebar = "#141013",
                    BgPopup = "#241D21",
                    Accent = "#FF4757",
                    AccentDark = "#A82431",
                    AccentLight = "#FF6B81",
                    CanvasFrame = "#7A1F28",
                    SelectBg = "#3B1A20",
                    HoverBg = "#261317",
                    TextMain = "#F5EFF1",
                    TextMuted = "#9E8D93",
                    Border = "#2E2328"
                },
                new ThemePalette
                {
                    Name = "Royal Gold",
                    BgCanvas = "#100F0C",
                    BgChrome = "#1B1915",
                    BgTitlebar = "#151310",
                    BgPopup = "#24211B",
                    Accent = "#F59E0B",
                    AccentDark = "#A36602",
                    AccentLight = "#FBBF24",
                    CanvasFrame = "#784C04",
                    SelectBg = "#3B2B10",
                    HoverBg = "#261D0E",
                    TextMain = "#F7F4EE",
                    TextMuted = "#9E9587",
                    Border = "#302B22"
                }
            };
        }

        // ====================================================================
        // BARRA DE TÍTULO PROPIA DE NEXUSFM
        // ====================================================================
        private Border BuildCustomTitleBar()
        {
            var bar = new Border
            {
                Height = 34,
                Padding = new Thickness(12, 0, 6, 0),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var miniFolder = VectorStudio.IconPath(VectorStudio.SVG_FOLDER, "#4DA2DB", 15);
            miniFolder.Margin = new Thickness(0, 0, 8, 0);
            brandStack.Children.Add(miniFolder);
            brandStack.Children.Add(new TextBlock
            {
                Text = "NexusFM",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(brandStack, 0);
            grid.Children.Add(brandStack);

            _titleBarFolderLabel = new TextBlock
            {
                Text = "Carpeta personal — NexusFM",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_titleBarFolderLabel, 1);
            grid.Children.Add(_titleBarFolderLabel);

            var winControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            winControls.Children.Add(CreateWindowControlButton("─", "Minimizar", false, () => WindowState = WindowState.Minimized));
            winControls.Children.Add(CreateWindowControlButton("□", "Maximizar / Restaurar", false, () => ToggleMaximize()));
            winControls.Children.Add(CreateWindowControlButton("✕", "Cerrar", true, () => HandleWindowCloseRequest()));
            Grid.SetColumn(winControls, 2);
            grid.Children.Add(winControls);

            bar.Child = grid;
            bar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2) ToggleMaximize();
                else if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };
            return bar;
        }

        private Border CreateWindowControlButton(string symbol, string tooltip, bool isClose, Action onClick)
        {
            var tb = new TextBlock
            {
                Text = symbol,
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var b = new Border
            {
                Width = 34,
                Height = 26,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(1, 0, 1, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Background = Brushes.Transparent,
                Child = tb
            };
            b.MouseEnter += (s, e) => b.Background = VectorStudio.Brush(isClose ? "#E81123" : ActivePalette.HoverBg);
            b.MouseLeave += (s, e) => b.Background = Brushes.Transparent;
            b.MouseLeftButtonDown += (s, e) => { e.Handled = true; onClick(); };
            return b;
        }

        private void ToggleMaximize()
        {
            WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void HandleWindowCloseRequest()
        {
            if (Config.ReplaceWindowsExplorer)
                Hide();
            else
                Application.Current.Shutdown();
        }

        // ====================================================================
        // INTERCEPTOR EN TIEMPO REAL DE WINDOWS EXPLORER Y DIÁLOGOS "OPEN FOLDER" (#32770)
        // ====================================================================
        private Border BuildOpenFolderPickerBanner()
        {
            var banner = new Border
            {
                Background = VectorStudio.Brush("#14332C"),
                BorderBrush = VectorStudio.Brush("#2ECC71"),
                BorderThickness = new Thickness(0, 0, 0, 1.5),
                Padding = new Thickness(14, 7, 14, 7),
                Visibility = Visibility.Collapsed
            };

            var dock = new DockPanel();
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnConfirm = CreatePillButton("✔ Abrir Carpeta en el Software (Ctrl+Enter)", "Enviar la carpeta seleccionada o actual al software externo", () =>
            {
                ConfirmExternalFileDialogSelection(null);
            });
            btnConfirm.Background = VectorStudio.Brush("#1E5646");
            btnConfirm.BorderBrush = VectorStudio.Brush("#2ECC71");
            ((TextBlock)btnConfirm.Child).FontWeight = FontWeights.Bold;
            ((TextBlock)btnConfirm.Child).Foreground = Brushes.White;

            var btnShowOrig = CreatePillButton("👁 Usar diálogo clásico", "Mostrar la ventana clásica de Windows para esta acción", () =>
            {
                RestoreCloakedExternalFileDialog(true);
            });
            btnShowOrig.Background = VectorStudio.Brush("#1F252D");
            btnShowOrig.BorderBrush = VectorStudio.Brush("#3C4654");

            var btnCancel = CreatePillButton("✕ Cancelar", "Cancelar y cerrar el diálogo Open Folder del software", () =>
            {
                CancelExternalFileDialog();
            });
            btnCancel.Background = VectorStudio.Brush("#381C21");
            btnCancel.BorderBrush = VectorStudio.Brush("#E74C3C");

            btns.Children.Add(btnConfirm);
            btns.Children.Add(btnShowOrig);
            btns.Children.Add(btnCancel);
            DockPanel.SetDock(btns, Dock.Right);
            dock.Children.Add(btns);

            _openFolderPickerBannerText = new TextBlock
            {
                Text = "📂 MODO OPEN FOLDER ACTIVO — Selecciona la carpeta destino y presiona Ctrl+Enter",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = VectorStudio.Brush("#A3F7BF"),
                VerticalAlignment = VerticalAlignment.Center
            };
            dock.Children.Add(_openFolderPickerBannerText);

            banner.Child = dock;
            return banner;
        }

        private void StartExplorerInterceptorTimer()
        {
            _explorerInterceptorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(130)
            };
            uint myPid = (uint)Process.GetCurrentProcess().Id;

            _explorerInterceptorTimer.Tick += (s, e) =>
            {
                // 0. Revisar si otra instancia de NexusFM envió una ruta vía IPC
                CheckSingleInstanceIpcRequest();

                if (!Config.ReplaceWindowsExplorer) return;

                // 1. Asegurar que ningún diálogo "Guardar como" / "Abrir" (#32770) de navegadores o programas
                // sea ocultado ni desviado por NexusFM: dejar que Windows guarde directamente en Descargas o donde elijas
                if (_activeExternalDialogHwnd != IntPtr.Zero)
                {
                    RestoreCloakedExternalFileDialog(true);
                }

                IntPtr dlg = IntPtr.Zero;
                while ((dlg = NativeWin32.FindWindowEx(IntPtr.Zero, dlg, "#32770", null)) != IntPtr.Zero)
                {
                    if (!NativeWin32.IsWindowVisible(dlg)) continue;
                    NativeWin32.RECT r;
                    if (NativeWin32.GetWindowRect(dlg, out r) && (r.Left < -10000 || r.Top < -10000))
                    {
                        NativeWin32.SetWindowPos(dlg, IntPtr.Zero, 180, 120, 0, 0,
                            NativeWin32.SWP_NOSIZE | NativeWin32.SWP_NOZORDER | NativeWin32.SWP_SHOWWINDOW);
                    }
                }

                // 2. Interceptar únicamente ventanas abiertas de Windows Explorer (CabinetWClass y ExploreWClass)
                IntPtr explorerHwnd = NativeWin32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "CabinetWClass", null);
                if (explorerHwnd == IntPtr.Zero)
                    explorerHwnd = NativeWin32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "ExploreWClass", null);

                if (explorerHwnd != IntPtr.Zero)
                {
                    string targetPath = TryInterceptOpenWindowsExplorer();

                    IntPtr rem1 = NativeWin32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "CabinetWClass", null);
                    if (rem1 != IntPtr.Zero) NativeWin32.PostMessage(rem1, NativeWin32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                    IntPtr rem2 = NativeWin32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "ExploreWClass", null);
                    if (rem2 != IntPtr.Zero) NativeWin32.PostMessage(rem2, NativeWin32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                    BringNexusToFront();

                    if (!string.IsNullOrEmpty(targetPath) && Directory.Exists(targetPath) && ActivePane != null)
                    {
                        ActivePane.LoadDirectory(targetPath, true);
                    }
                    return;
                }
            };
            _explorerInterceptorTimer.Start();
        }

        private void CheckSingleInstanceIpcRequest()
        {
            try
            {
                string exeDir = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string ipcFile = System.IO.Path.Combine(exeDir, "nexus-ipc-open.txt");
                if (!File.Exists(ipcFile)) return;

                string requestedPath = File.ReadAllText(ipcFile, Encoding.UTF8).Trim();
                File.Delete(ipcFile);

                BringNexusToFront();
                if (!string.IsNullOrEmpty(requestedPath) && Directory.Exists(requestedPath) && ActivePane != null)
                {
                    ActivePane.LoadDirectory(requestedPath, true);
                }
            }
            catch { }
        }

        public void BringNexusToFront()
        {
            try
            {
                ShowInTaskbar = true;
                if (!IsVisible) Show();
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                Activate();
                IntPtr myHwnd = new WindowInteropHelper(this).Handle;
                if (myHwnd != IntPtr.Zero)
                {
                    NativeWin32.ShowWindow(myHwnd, NativeWin32.SW_RESTORE);
                    NativeWin32.SetForegroundWindow(myHwnd);
                }
            }
            catch { }
        }

        public void ConfirmExternalFileDialogSelection(string explicitPath)
        {
            if (!HasActiveExternalFileDialog) return;
            IntPtr dlg = _activeExternalDialogHwnd;

            string chosenPath = explicitPath;
            if (string.IsNullOrEmpty(chosenPath) && ActivePane != null)
            {
                var cur = ActivePane.GetCurrentCursorItem();
                if (cur != null && cur.IsDirectory)
                    chosenPath = cur.FullPath;
                else
                    chosenPath = ActivePane.ActiveTab.CurrentPath;
            }

            if (string.IsNullOrEmpty(chosenPath)) return;

            try
            {
                IntPtr editHwnd = NativeWin32.FindDialogPathEditControl(dlg);
                IntPtr okBtn = NativeWin32.GetDlgItem(dlg, 1);

                if (editHwnd != IntPtr.Zero)
                {
                    NativeWin32.SendMessage(editHwnd, NativeWin32.WM_SETTEXT, IntPtr.Zero, chosenPath);
                    int ctrlId = NativeWin32.GetDlgCtrlID(editHwnd);
                    IntPtr parent = NativeWin32.GetParent(editHwnd);
                    IntPtr wParamChange = (IntPtr)((0x0300 << 16) | (ctrlId & 0xFFFF)); // EN_CHANGE
                    NativeWin32.SendMessage(parent != IntPtr.Zero ? parent : dlg, NativeWin32.WM_COMMAND, wParamChange, editHwnd);
                }

                // Enviar IDOK (1) para confirmar la selección en el software externo
                NativeWin32.PostMessage(dlg, NativeWin32.WM_COMMAND, (IntPtr)1, okBtn);
                if (okBtn != IntPtr.Zero)
                    NativeWin32.PostMessage(okBtn, NativeWin32.BM_CLICK, IntPtr.Zero, IntPtr.Zero);

                // Segundo pulso automático a los 140ms por si el diálogo primero navegó al directorio antes de aceptar
                var followUp = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
                followUp.Tick += (s, e) =>
                {
                    followUp.Stop();
                    if (NativeWin32.IsWindow(dlg))
                    {
                        NativeWin32.PostMessage(dlg, NativeWin32.WM_COMMAND, (IntPtr)1, okBtn);
                        if (okBtn != IntPtr.Zero)
                            NativeWin32.PostMessage(okBtn, NativeWin32.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    }
                    _activeExternalDialogHwnd = IntPtr.Zero;
                    _openFolderPickerBanner.Visibility = Visibility.Collapsed;
                };
                followUp.Start();
            }
            catch
            {
                _activeExternalDialogHwnd = IntPtr.Zero;
                _openFolderPickerBanner.Visibility = Visibility.Collapsed;
            }
        }

        public void RestoreCloakedExternalFileDialog(bool ignoreThisDialog)
        {
            if (_activeExternalDialogHwnd != IntPtr.Zero && NativeWin32.IsWindow(_activeExternalDialogHwnd))
            {
                if (ignoreThisDialog) _ignoredExternalDialogs.Add(_activeExternalDialogHwnd);
                int x = _activeExternalDialogOrigRect.Left < -10000 ? 180 : _activeExternalDialogOrigRect.Left;
                int y = _activeExternalDialogOrigRect.Top < -10000 ? 120 : _activeExternalDialogOrigRect.Top;
                NativeWin32.SetWindowPos(_activeExternalDialogHwnd, IntPtr.Zero, x, y, 0, 0,
                    NativeWin32.SWP_NOSIZE | NativeWin32.SWP_NOZORDER | NativeWin32.SWP_SHOWWINDOW);
                NativeWin32.SetForegroundWindow(_activeExternalDialogHwnd);
            }
            _activeExternalDialogHwnd = IntPtr.Zero;
            _openFolderPickerBanner.Visibility = Visibility.Collapsed;
        }

        public void CancelExternalFileDialog()
        {
            if (_activeExternalDialogHwnd != IntPtr.Zero && NativeWin32.IsWindow(_activeExternalDialogHwnd))
            {
                NativeWin32.PostMessage(_activeExternalDialogHwnd, NativeWin32.WM_COMMAND, (IntPtr)2, IntPtr.Zero);
                NativeWin32.PostMessage(_activeExternalDialogHwnd, NativeWin32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            _activeExternalDialogHwnd = IntPtr.Zero;
            _openFolderPickerBanner.Visibility = Visibility.Collapsed;
        }

        private string TryInterceptOpenWindowsExplorer()
        {
            if (!Config.ReplaceWindowsExplorer) return null;
            string capturedPath = null;

            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return null;
                object shell = Activator.CreateInstance(shellType);
                object windows = shellType.InvokeMember("Windows", BindingFlags.InvokeMethod, null, shell, null);
                if (windows == null) return null;

                int count = Convert.ToInt32(windows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, windows, null));
                for (int i = count - 1; i >= 0; i--)
                {
                    object win = windows.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    if (win == null) continue;

                    try
                    {
                        string locUrl = Convert.ToString(win.GetType().InvokeMember("LocationURL", BindingFlags.GetProperty, null, win, null));
                        if (!string.IsNullOrEmpty(locUrl) && locUrl.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                        {
                            string local = Uri.UnescapeDataString(new Uri(locUrl).LocalPath);
                            if (Directory.Exists(local))
                                capturedPath = local;
                        }
                    }
                    catch { }

                    try
                    {
                        win.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, win, null);
                    }
                    catch { }
                }
            }
            catch { }

            return capturedPath;
        }

        public void ApplyWindowsStartupRegistry(bool enable)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        if (enable)
                            runKey.SetValue("NexusFM", "\"" + exePath + "\" --startup");
                        else
                            runKey.DeleteValue("NexusFM", false);
                    }
                }
            }
            catch { }
        }

        public void RepairAndRefreshAllIconsNow(bool showStatusMessage)
        {
            try
            {
                IconExtractor.ClearCache();
                ApplyWindowsStartupRegistry(Config.AutoStartWithWindows);
                ApplyWindowsExplorerRegistry(Config.ReplaceWindowsExplorer);

                Task.Run(() =>
                {
                    IconExtractor.PrecompileAndRepairDesktopAppIcons(ActiveStableFolderIcoPath, Config.ReplaceWindowsExplorer, Config.RecentLocations);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (LeftPane != null) { LeftPane.ReadEntries(); LeftPane.RenderItems(); }
                        if (IsSplitView && RightPane != null) { RightPane.ReadEntries(); RightPane.RenderItems(); }
                        if (showStatusMessage && _statusLeftLabel != null)
                        {
                            _statusLeftLabel.Text = "✔ Iconos de carpetas, juegos, softwares y arranque de Windows reparados y actualizados.";
                        }
                    }));
                });
            }
            catch { }
        }

        public void ApplyWindowsExplorerRegistry(bool enable)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                string assetsDir = System.IO.Path.Combine(exeDir, "assets");
                string accentHex = ActivePalette != null ? ActivePalette.Accent : "#4DA2DB";

                // 1. Generar el icono multi-resolución (256/64/48/32/16px) en ruta local inmutable (%LOCALAPPDATA%\NexusFM\assets) y en el repo
                string icoPath = IconExtractor.GenerateMultiSizeFolderIco(assetsDir, accentHex);
                if (!File.Exists(icoPath)) icoPath = exePath;
                ActiveStableFolderIcoPath = icoPath;

                ApplyWindowsStartupRegistry(Config.AutoStartWithWindows);

                if (enable)
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell"))
                        key.SetValue("", "openin_nexusfm");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\openin_nexusfm"))
                    {
                        key.SetValue("", "Abrir en NexusFM");
                        key.SetValue("Icon", icoPath + ",0");
                    }

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\openin_nexusfm\command"))
                        key.SetValue("", "\"" + exePath + "\" \"%1\"");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\shell\openin_nexusfm"))
                    {
                        key.SetValue("", "Abrir aquí NexusFM");
                        key.SetValue("Icon", icoPath + ",0");
                    }

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\shell\openin_nexusfm\command"))
                        key.SetValue("", "\"" + exePath + "\" \"%V\"");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell"))
                        key.SetValue("", "openin_nexusfm");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell\openin_nexusfm\command"))
                        key.SetValue("", "\"" + exePath + "\" \"%1\"");

                    // 2. Registrar el icono de carpeta de NexusFM en Directory, Folder y Shell Icons
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\DefaultIcon"))
                        key.SetValue("", icoPath + ",0");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Folder\DefaultIcon"))
                        key.SetValue("", icoPath + ",0");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
                    {
                        key.SetValue("3", icoPath + ",0");
                        key.SetValue("4", icoPath + ",0");
                    }
                }
                else
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell"))
                        key.SetValue("", "none");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell"))
                        key.SetValue("", "none");

                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\openin_nexusfm", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\openin_nexusfm", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Drive\shell\openin_nexusfm", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\DefaultIcon", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Folder\DefaultIcon", false);

                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("3", false);
                            key.DeleteValue("4", false);
                        }
                    }
                }

                // 3. Aplicar (o restaurar) el icono de NexusFM a todas las carpetas en el Escritorio de Windows (hasta 4 niveles)
                var recentSnap = Config.RecentLocations != null ? Config.RecentLocations.ToList() : new List<string>();
                Task.Run(() => IconExtractor.ApplyNexusIconToDesktopFolders(icoPath, enable, recentSnap));
            }
            catch { }
        }

        public void RestoreOriginalWindowsExplorerAndOpenIt(bool exitNexusAfter)
        {
            Config.ReplaceWindowsExplorer = false;
            Config.Save();
            ApplyWindowsExplorerRegistry(false);

            string currentFolder = ActivePane != null
                ? ActivePane.ActiveTab.CurrentPath
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            try
            {
                Process.Start("explorer.exe", "\"" + currentFolder + "\"");
            }
            catch { }

            if (exitNexusAfter)
            {
                Application.Current.Shutdown();
            }
        }

        // ====================================================================
        // BARRA DE HERRAMIENTAS DE NEXUSFM
        // ====================================================================
        private Border BuildToolbar()
        {
            var border = new Border
            {
                Padding = new Thickness(10, 5, 10, 5),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Izquierda: < > y 4 modos de visualización
            var leftBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            leftBar.Children.Add(CreateFlatIconButton("‹", "Atrás (Alt+Izquierda)", () => ActivePane.GoBack(), 18));
            leftBar.Children.Add(CreateFlatIconButton("›", "Adelante (Alt+Derecha)", () => ActivePane.GoForward(), 18));

            var sep = new Rectangle { Width = 1, Height = 18, Margin = new Thickness(8, 0, 8, 0), Fill = VectorStudio.Brush("#2E343D") };
            leftBar.Children.Add(sep);

            _btnModelBreeze = CreateFlatIconButton("⊞", "Vista Iconos Breeze (Por defecto)", () => SetFolderModel("Breeze"), 15);
            _btnModelCards = CreateFlatIconButton("▤", "Vista Tarjetas Modernas", () => SetFolderModel("Cards"), 14);
            _btnModelDetails = CreateFlatIconButton("☰", "Vista Lista de Detalles", () => SetFolderModel("Details"), 14);
            _btnModelNeon = CreateFlatIconButton("◈", "Vista Cyberpunk Neón", () => SetFolderModel("Neon"), 14);

            leftBar.Children.Add(_btnModelBreeze);
            leftBar.Children.Add(_btnModelCards);
            leftBar.Children.Add(_btnModelDetails);
            leftBar.Children.Add(_btnModelNeon);

            Grid.SetColumn(leftBar, 0);
            grid.Children.Add(leftBar);

            // Centro: Breadcrumbs limpios estilo captura 2 (> Carpeta personal > ...)
            _breadcrumbsBox = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(12, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var centerHolder = new Grid();
            _breadcrumbsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                Cursor = Cursors.IBeam
            };
            _breadcrumbsPanel.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource == _breadcrumbsPanel)
                    ActivateManualPathInput();
            };

            _pathInputBox = new TextBox
            {
                Visibility = Visibility.Collapsed,
                Padding = new Thickness(6, 3, 6, 3),
                FontSize = 12.5,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas")
            };
            _pathInputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    string target = _pathInputBox.Text.Trim();
                    _pathInputBox.Visibility = Visibility.Collapsed;
                    _breadcrumbsPanel.Visibility = Visibility.Visible;
                    if (Directory.Exists(target)) ActivePane.LoadDirectory(target, true);
                    ActivePane.Focus();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    _pathInputBox.Visibility = Visibility.Collapsed;
                    _breadcrumbsPanel.Visibility = Visibility.Visible;
                    ActivePane.Focus();
                    e.Handled = true;
                }
            };
            _pathInputBox.LostFocus += (s, e) =>
            {
                _pathInputBox.Visibility = Visibility.Collapsed;
                _breadcrumbsPanel.Visibility = Visibility.Visible;
            };

            centerHolder.Children.Add(_breadcrumbsPanel);
            centerHolder.Children.Add(_pathInputBox);
            _breadcrumbsBox.Child = centerHolder;
            Grid.SetColumn(_breadcrumbsBox, 1);
            grid.Children.Add(_breadcrumbsBox);

            // Derecha: Buscador + Analizador de Espacio + Dividir (F3) + Terminal + Personalizar (Drawer limpio)
            var rightBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _searchBox = new TextBox
            {
                Width = 155,
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12,
                Visibility = Visibility.Collapsed,
                ToolTip = "Filtrar por nombre (Esc para cerrar)"
            };
            _searchBox.TextChanged += (s, e) =>
            {
                if (ActivePane != null)
                {
                    ActivePane.ActiveTab.FilterText = _searchBox.Text;
                    ActivePane.ReadEntries();
                    ActivePane.RenderItems();
                }
            };
            _searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape || e.Key == Key.Enter)
                {
                    if (e.Key == Key.Escape)
                    {
                        _searchBox.Text = "";
                        _searchBox.Visibility = Visibility.Collapsed;
                    }
                    ActivePane.Focus();
                    e.Handled = true;
                }
            };
            rightBar.Children.Add(_searchBox);

            _btnDescribeFolderToggle = CreateIconTextPill(VectorStudio.SVG_NOTE, "Descripción", "Colocar o editar la descripción de lo que lleva la carpeta adentro (F10)", () => OpenFolderDescriptionDialog(null));
            rightBar.Children.Add(_btnDescribeFolderToggle);

            _btnMoveToFolderToggle = CreateIconTextPill(VectorStudio.SVG_MOVE, "Mover a...", "Mover carpeta o archivo seleccionado buscando la carpeta destino (Ctrl+M o tecla M)", () => OpenMoveToFolderOverlay());
            rightBar.Children.Add(_btnMoveToFolderToggle);

            _btnAnalyzerToggle = CreateIconTextPill(VectorStudio.SVG_CHART, "Espacio", "Analizador Visual de Espacio en Disco (F9)", () => ToggleDiskAnalyzer());
            rightBar.Children.Add(_btnAnalyzerToggle);

            _btnSplitToggle = CreateIconTextPill(VectorStudio.SVG_SPLIT, "Dividir", "Dividir en Doble Panel (F3)", () => ToggleSplitView());
            rightBar.Children.Add(_btnSplitToggle);

            rightBar.Children.Add(CreateVectorIconButton(VectorStudio.SVG_SEARCH, "Buscar (Ctrl+F)", () => ToggleSearchBox()));
            rightBar.Children.Add(CreateVectorIconButton(VectorStudio.SVG_TERM, "Terminal PowerShell (F4)", () => ToggleTerminalDock()));

            _btnSettingsToggle = CreateIconTextPill(VectorStudio.SVG_GEAR, "Personalizar", "Abrir panel lateral de Apariencia y Configuración de Windows", () => ToggleSettingsDrawer());
            rightBar.Children.Add(_btnSettingsToggle);

            Grid.SetColumn(rightBar, 2);
            grid.Children.Add(rightBar);

            border.Child = grid;
            return border;
        }

        private Border CreateVectorIconButton(string svg, string tooltip, Action onClick)
        {
            var b = new Border
            {
                Width = 30,
                Height = 28,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Background = Brushes.Transparent,
                Child = VectorStudio.IconPath(svg, "#EFF0F1", 13)
            };
            b.MouseEnter += (s, e) => b.Background = VectorStudio.Brush(ActivePalette.HoverBg);
            b.MouseLeave += (s, e) => b.Background = Brushes.Transparent;
            b.MouseLeftButtonDown += (s, e) => { e.Handled = true; onClick(); };
            return b;
        }

        private Border CreateIconTextPill(string svg, string label, string tooltip, Action onClick)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var ic = VectorStudio.IconPath(svg, "#EFF0F1", 12.5);
            ic.Margin = new Thickness(0, 0, 6, 0);
            row.Children.Add(ic);
            row.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = VectorStudio.Brush("#EFF0F1"),
                VerticalAlignment = VerticalAlignment.Center
            });

            var b = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                BorderThickness = new Thickness(1),
                Child = row
            };
            b.MouseLeftButtonDown += (s, e) => { e.Handled = true; onClick(); };
            return b;
        }

        private void SetFolderModel(string modelName)
        {
            Config.FolderModel = modelName;
            Config.Save();
            UpdateModelButtonsHighlight();
            if (LeftPane != null) LeftPane.RenderItems();
            if (RightPane != null) RightPane.RenderItems();
        }

        private void UpdateModelButtonsHighlight()
        {
            if (_btnModelBreeze == null) return;
            var p = ActivePalette;
            string m = Config.FolderModel;
            _btnModelBreeze.Background = (m == "Breeze") ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent;
            _btnModelNeon.Background = (m == "Neon") ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent;
            _btnModelCards.Background = (m == "Cards") ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent;
            _btnModelDetails.Background = (m == "Details") ? VectorStudio.Brush(p.SelectBg) : Brushes.Transparent;
        }

        private Border CreateFlatIconButton(string icon, string tooltip, Action onClick, double fontSize)
        {
            var tb = new TextBlock
            {
                Text = icon,
                FontSize = fontSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var b = new Border
            {
                Width = 30,
                Height = 28,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(1, 0, 1, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Background = Brushes.Transparent,
                Child = tb
            };
            b.MouseEnter += (s, e) => b.Background = VectorStudio.Brush(ActivePalette.HoverBg);
            b.MouseLeave += (s, e) => UpdateModelButtonsHighlight();
            b.MouseLeftButtonDown += (s, e) => { e.Handled = true; onClick(); };
            return b;
        }

        private Border CreatePillButton(string text, string tooltip, Action onClick)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            var b = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                BorderThickness = new Thickness(1),
                Child = tb
            };
            b.MouseLeftButtonDown += (s, e) => { e.Handled = true; onClick(); };
            return b;
        }

        // ====================================================================
        // 6. PANEL LATERAL DE PERSONALIZACIÓN INTEGRADO (SIN VENTANA NARANJA!)
        // ====================================================================
        private Border BuildIntegratedSettingsDrawer()
        {
            var drawer = new Border
            {
                Width = 310,
                Padding = new Thickness(16, 14, 16, 14),
                BorderThickness = new Thickness(1, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();

            // Cabecera del Drawer con botón cerrar
            var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
            headerDock.Children.Add(new TextBlock
            {
                Text = "Personalización y Sistema",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            var closeBtn = CreateFlatIconButton("✕", "Cerrar panel", () => ToggleSettingsDrawer(), 12);
            HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(closeBtn, Dock.Right);
            headerDock.Children.Add(closeBtn);
            stack.Children.Add(headerDock);

            // TARJETA 1: REEMPLAZO DE WINDOWS EXPLORER (Sencillo, visual y sin símbolos rotos)
            var expCard = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 16),
                BorderThickness = new Thickness(1),
                Background = VectorStudio.Brush("#161A20"),
                BorderBrush = VectorStudio.Brush("#2C3440")
            };
            var expStack = new StackPanel();

            var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            var statusDot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = VectorStudio.Brush(Config.ReplaceWindowsExplorer ? "#2ECC71" : "#7F8C8D"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var statusTitle = new TextBlock
            {
                Text = Config.ReplaceWindowsExplorer ? "Reemplazo de Windows Activo" : "Explorador de Windows Original",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusRow.Children.Add(statusDot);
            statusRow.Children.Add(statusTitle);
            expStack.Children.Add(statusRow);

            expStack.Children.Add(new TextBlock
            {
                Text = "Abre todas tus carpetas del Escritorio y accesos de Windows directamente en NexusFM.",
                FontSize = 11.2,
                Foreground = VectorStudio.Brush("#8A949E"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var btnToggleReplace = CreatePillButton(
                Config.ReplaceWindowsExplorer ? "Desactivar Reemplazo" : "Activar Reemplazo ahora",
                "", () =>
                {
                    Config.ReplaceWindowsExplorer = !Config.ReplaceWindowsExplorer;
                    Config.Save();
                    ApplyWindowsExplorerRegistry(Config.ReplaceWindowsExplorer);
                    statusDot.Fill = VectorStudio.Brush(Config.ReplaceWindowsExplorer ? "#2ECC71" : "#7F8C8D");
                    statusTitle.Text = Config.ReplaceWindowsExplorer ? "Reemplazo de Windows Activo" : "Explorador de Windows Original";
                });
            btnToggleReplace.Background = VectorStudio.Brush("#1F3347");
            btnToggleReplace.BorderBrush = VectorStudio.Brush("#4DA2DB");
            btnToggleReplace.Margin = new Thickness(0, 0, 0, 6);
            expStack.Children.Add(btnToggleReplace);

            Border btnToggleAutoStart = null;
            btnToggleAutoStart = CreatePillButton(
                Config.AutoStartWithWindows ? "✔ Iniciar y Actualizar Iconos con Windows: Activo" : "Iniciar y Actualizar Iconos con Windows: Inactivo",
                "Sincroniza y repara todos los iconos automáticamente cada vez que inicia Windows", () =>
                {
                    Config.AutoStartWithWindows = !Config.AutoStartWithWindows;
                    Config.Save();
                    ApplyWindowsStartupRegistry(Config.AutoStartWithWindows);
                    ((TextBlock)btnToggleAutoStart.Child).Text = Config.AutoStartWithWindows
                        ? "✔ Iniciar y Actualizar Iconos con Windows: Activo"
                        : "Iniciar y Actualizar Iconos con Windows: Inactivo";
                });
            btnToggleAutoStart.Background = VectorStudio.Brush("#183028");
            btnToggleAutoStart.BorderBrush = VectorStudio.Brush("#2ECC71");
            btnToggleAutoStart.Margin = new Thickness(0, 0, 0, 6);
            expStack.Children.Add(btnToggleAutoStart);

            var btnRepairIconsNow = CreatePillButton("🔄 Reparar y Actualizar Iconos Ahora", "Regenera y repara todos los iconos de carpetas, juegos y softwares en Windows y NexusFM", () =>
            {
                RepairAndRefreshAllIconsNow(true);
            });
            btnRepairIconsNow.Background = VectorStudio.Brush("#1C2838");
            btnRepairIconsNow.BorderBrush = VectorStudio.Brush("#00F0FF");
            btnRepairIconsNow.Margin = new Thickness(0, 0, 0, 6);
            expStack.Children.Add(btnRepairIconsNow);

            var btnBackToWin = CreatePillButton("Regresar a Windows Explorer y Salir", "Restaura explorer.exe y cierra NexusFM", () =>
            {
                RestoreOriginalWindowsExplorerAndOpenIt(true);
            });
            btnBackToWin.Background = VectorStudio.Brush("#361B20");
            btnBackToWin.BorderBrush = VectorStudio.Brush("#E74C3C");
            ((TextBlock)btnBackToWin.Child).Foreground = VectorStudio.Brush("#FF7675");
            btnBackToWin.Margin = new Thickness(0, 0, 0, 0);
            expStack.Children.Add(btnBackToWin);

            expCard.Child = expStack;
            stack.Children.Add(expCard);

            // SECCIÓN 2: ESTILO DE CARPETAS (Tarjetas visuales limpias)
            stack.Children.Add(new TextBlock
            {
                Text = "Estilo Visual de Carpetas",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var modelsGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 16) };
            modelsGrid.Children.Add(CreateModelPreviewCard("Breeze", "Breeze Flat"));
            modelsGrid.Children.Add(CreateModelPreviewCard("Neon", "Cyber Neón"));
            modelsGrid.Children.Add(CreateModelPreviewCard("Cards", "Tarjetas Pro"));
            modelsGrid.Children.Add(CreateModelPreviewCard("Details", "Detalles"));
            stack.Children.Add(modelsGrid);

            // SECCIÓN 3: TEMAS DE PROFUNDIDAD
            stack.Children.Add(new TextBlock
            {
                Text = "Temas de Apariencia",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            foreach (var t in Themes)
            {
                var themeRef = t;
                var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = VectorStudio.Brush(t.Accent),
                    Margin = new Thickness(0, 0, 10, 0)
                });
                row.Children.Add(new TextBlock
                {
                    Text = t.Name,
                    FontSize = 12,
                    Foreground = VectorStudio.Brush(t.TextMain)
                });

                var themeCard = new Border
                {
                    Background = VectorStudio.Brush(t.BgCanvas),
                    BorderBrush = VectorStudio.Brush(t.Accent),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10, 7, 10, 7),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = Cursors.Hand,
                    Child = row
                };
                themeCard.MouseLeftButtonDown += (s, e) =>
                {
                    Config.ThemeName = themeRef.Name;
                    Config.CustomAccentHex = "";
                    Config.Save();
                    BasePalette = themeRef;
                    ApplyCurrentThemeAndConfig();
                };
                stack.Children.Add(themeCard);
            }

            // SECCIÓN 4: CÍRCULOS DE COLOR DE CARPETA A TU GUSTO
            stack.Children.Add(new TextBlock
            {
                Text = "Color de Carpetas Personalizado",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 8)
            });

            var swatchesWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
            string[] swatchColors = { "#4DA2DB", "#1ABC9C", "#CBA6F7", "#00F0FF", "#FF4757", "#F59E0B", "#2ECC71", "#FD79A8", "#6C5CE7", "#E17055" };
            foreach (var hex in swatchColors)
            {
                string cHex = hex;
                var circle = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = VectorStudio.Brush(cHex),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 8, 8),
                    Cursor = Cursors.Hand,
                    ToolTip = "Pintar carpetas con " + cHex
                };
                circle.MouseLeftButtonDown += (s, e) =>
                {
                    Config.CustomAccentHex = cHex;
                    Config.Save();
                    ApplyCurrentThemeAndConfig();
                };
                swatchesWrap.Children.Add(circle);
            }
            stack.Children.Add(swatchesWrap);

            // SECCIÓN 5: PANELES ADICIONALES
            var btnToggleInspector = CreatePillButton("Mostrar / Ocultar Inspector de Archivos (F11)", "", () => ToggleInfoDock());
            btnToggleInspector.Background = VectorStudio.Brush("#161A20");
            btnToggleInspector.BorderBrush = VectorStudio.Brush("#2C3440");
            btnToggleInspector.Margin = new Thickness(0, 0, 0, 6);
            stack.Children.Add(btnToggleInspector);

            scroll.Content = stack;
            drawer.Child = scroll;
            return drawer;
        }

        private Border CreateModelPreviewCard(string modelKey, string title)
        {
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(VectorStudio.CreateFolderGraphic(36, ActivePalette, modelKey == "Neon" ? "Neon" : "Breeze"));
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11.2,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });

            var card = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                Background = VectorStudio.Brush("#14181E"),
                BorderBrush = VectorStudio.Brush("#2C3440"),
                BorderThickness = new Thickness(1),
                Child = stack
            };
            card.MouseLeftButtonDown += (s, e) => SetFolderModel(modelKey);
            return card;
        }

        private void ToggleSettingsDrawer()
        {
            bool show = (_settingsDrawer.Visibility != Visibility.Visible);
            _settingsDrawer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        // ====================================================================
        // 7. HERRAMIENTA ÚNICA 1: ANALIZADOR VISUAL DE ESPACIO EN DISCO (F9)
        // ====================================================================
        private Border BuildDiskSpaceAnalyzerOverlay()
        {
            var overlay = new Border
            {
                Margin = new Thickness(6, 28, 6, 4),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(22),
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(overlay, 20);
            return overlay;
        }

        public void ToggleDiskAnalyzer()
        {
            if (_analyzerOverlay.Visibility == Visibility.Visible)
            {
                _analyzerOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            _editorOverlay.Visibility = Visibility.Collapsed;
            var p = ActivePalette;
            _analyzerOverlay.Background = VectorStudio.Brush(p.BgCanvas);
            _analyzerOverlay.BorderBrush = VectorStudio.Brush(p.Accent);

            var rootStack = new StackPanel();
            var topDock = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            topDock.Children.Add(new TextBlock
            {
                Text = "Analizador Visual de Espacio y Distribución — " + ActivePane.ActiveTab.Title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = VectorStudio.Brush(p.AccentLight)
            });
            var closeBtn = CreatePillButton("Cerrar Analizador (Esc)", "", () => _analyzerOverlay.Visibility = Visibility.Collapsed);
            closeBtn.Background = VectorStudio.Brush(p.BgChrome);
            closeBtn.BorderBrush = VectorStudio.Brush(p.Border);
            DockPanel.SetDock(closeBtn, Dock.Right);
            topDock.Children.Add(closeBtn);
            rootStack.Children.Add(topDock);

            // Calcular estadísticas reales del directorio actual y sus subcarpetas de primer nivel
            var entries = new List<Tuple<string, long, bool, string>>();
            long totalBytes = 0;

            try
            {
                var dir = new DirectoryInfo(ActivePane.ActiveTab.CurrentPath);
                foreach (var f in dir.GetFiles())
                {
                    totalBytes += f.Length;
                    entries.Add(Tuple.Create(f.Name, f.Length, false, f.FullName));
                }
                foreach (var sub in dir.GetDirectories())
                {
                    long subSize = 0;
                    try
                    {
                        foreach (var sf in sub.GetFiles("*", SearchOption.TopDirectoryOnly))
                            subSize += sf.Length;
                    }
                    catch { }
                    totalBytes += subSize;
                    entries.Add(Tuple.Create("📁 " + sub.Name, subSize, true, sub.FullName));
                }
            }
            catch { }

            entries = entries.OrderByDescending(x => x.Item2).Take(14).ToList();

            rootStack.Children.Add(new TextBlock
            {
                Text = "Peso total analizado en este nivel: " + NexusPaneControl.FormatBytes(totalBytes) + " (" + ActivePane.CurrentItems.Count + " elementos)",
                FontSize = 12.5,
                Foreground = VectorStudio.Brush(p.TextMuted),
                Margin = new Thickness(0, 0, 0, 14)
            });

            var listScroll = new ScrollViewer { Height = 440, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var barsStack = new StackPanel();

            foreach (var item in entries)
            {
                double pct = totalBytes > 0 ? (item.Item2 / (double)totalBytes) : 0;
                var itemRow = new Border
                {
                    Background = VectorStudio.Brush(p.BgChrome),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = item.Item3 ? Cursors.Hand : Cursors.Arrow
                };

                var rowStack = new StackPanel();
                var lblDock = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
                lblDock.Children.Add(new TextBlock
                {
                    Text = item.Item1,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12.5,
                    Foreground = VectorStudio.Brush(p.TextMain)
                });
                var sizeLbl = new TextBlock
                {
                    Text = string.Format("{0}  ({1:0.0}%)", NexusPaneControl.FormatBytes(item.Item2), pct * 100),
                    FontSize = 12,
                    Foreground = VectorStudio.Brush(p.AccentLight),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(sizeLbl, Dock.Right);
                lblDock.Children.Add(sizeLbl);
                rowStack.Children.Add(lblDock);

                var progressTrack = new Border
                {
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    Background = VectorStudio.Brush(p.BgCanvas)
                };
                var progressFill = new Border
                {
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(6, 560 * pct),
                    Background = VectorStudio.Brush(item.Item3 ? p.Accent : "#E67E22")
                };
                progressTrack.Child = progressFill;
                rowStack.Children.Add(progressTrack);

                itemRow.Child = rowStack;
                if (item.Item3)
                {
                    string targetDir = item.Item4;
                    itemRow.MouseLeftButtonDown += (s, e) =>
                    {
                        _analyzerOverlay.Visibility = Visibility.Collapsed;
                        ActivePane.LoadDirectory(targetDir, true);
                    };
                }
                barsStack.Children.Add(itemRow);
            }

            listScroll.Content = barsStack;
            rootStack.Children.Add(listScroll);
            _analyzerOverlay.Child = rootStack;
            _analyzerOverlay.Visibility = Visibility.Visible;
        }

        // ====================================================================
        // 8. HERRAMIENTA ÚNICA 2: EDITOR DE CÓDIGO Y NOTAS INTEGRADO (Ctrl+E)
        // ====================================================================
        private Border BuildIntegratedCodeEditorOverlay()
        {
            var overlay = new Border
            {
                Margin = new Thickness(6, 28, 6, 4),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(overlay, 25);

            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            _codeEditorTitle = new TextBlock
            {
                Text = "Nexus Studio Editor",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(_codeEditorTitle);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var saveBtn = CreatePillButton("💾 Guardar Cambios (Ctrl+S)", "", () => SaveEditedFile());
            saveBtn.Background = VectorStudio.Brush("#1A3B34");
            saveBtn.BorderBrush = VectorStudio.Brush("#2ECC71");
            var closeBtn = CreatePillButton("Cerrar Editor (Esc)", "", () => _editorOverlay.Visibility = Visibility.Collapsed);
            closeBtn.Background = VectorStudio.Brush("#242930");
            closeBtn.BorderBrush = VectorStudio.Brush("#3A404A");
            btns.Children.Add(saveBtn);
            btns.Children.Add(closeBtn);
            DockPanel.SetDock(btns, Dock.Right);
            header.Children.Add(btns);

            Grid.SetRow(header, 0);
            g.Children.Add(header);

            _codeEditorBox = new TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Padding = new Thickness(12),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(1)
            };
            _codeEditorBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    SaveEditedFile();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    _editorOverlay.Visibility = Visibility.Collapsed;
                    ActivePane.Focus();
                    e.Handled = true;
                }
            };
            Grid.SetRow(_codeEditorBox, 1);
            g.Children.Add(_codeEditorBox);

            overlay.Child = g;
            return overlay;
        }

        public void OpenFileInIntegratedEditor(FileItem item)
        {
            if (item == null || item.IsDirectory) return;
            try
            {
                var p = ActivePalette;
                _editingFilePath = item.FullPath;
                _codeEditorTitle.Text = "✎ Editando: " + item.Name + " (" + item.SizeText + ")";
                _codeEditorTitle.Foreground = VectorStudio.Brush(p.AccentLight);
                _editorOverlay.Background = VectorStudio.Brush(p.BgCanvas);
                _editorOverlay.BorderBrush = VectorStudio.Brush(p.Accent);
                _codeEditorBox.Background = VectorStudio.Brush("#0B0E12");
                _codeEditorBox.Foreground = VectorStudio.Brush(p.TextMain);
                _codeEditorBox.BorderBrush = VectorStudio.Brush(p.Border);
                _codeEditorBox.Text = File.ReadAllText(item.FullPath, Encoding.UTF8);
                _editorOverlay.Visibility = Visibility.Visible;
                _codeEditorBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir en el editor: " + ex.Message);
            }
        }

        private void SaveEditedFile()
        {
            if (string.IsNullOrEmpty(_editingFilePath)) return;
            try
            {
                File.WriteAllText(_editingFilePath, _codeEditorBox.Text, Encoding.UTF8);
                _codeEditorTitle.Text = "✔ Guardado exitosamente: " + System.IO.Path.GetFileName(_editingFilePath);
                ActivePane.ReadEntries();
                ActivePane.RenderItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        // ====================================================================
        // 8B. BUSCADOR INTERACTIVO POR TECLADO/MOUSE PARA MOVER CARPETA (Ctrl+M / Tecla M)
        // ====================================================================
        private Border BuildMoveToFolderOverlay()
        {
            var overlay = new Border
            {
                Margin = new Thickness(6, 28, 6, 4),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(20),
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(overlay, 30);

            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Subtitle
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Search Box
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: Results

            var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            _moveOverlayHeaderTitle = new TextBlock
            {
                Text = "📦 Mover / Enviar selección a carpeta...",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerDock.Children.Add(_moveOverlayHeaderTitle);

            var closeBtn = CreatePillButton("Cerrar (Esc)", "", () =>
            {
                _moveToFolderOverlay.Visibility = Visibility.Collapsed;
                if (ActivePane != null) ActivePane.Focus();
            });
            closeBtn.Background = VectorStudio.Brush("#242930");
            closeBtn.BorderBrush = VectorStudio.Brush("#3A404A");
            DockPanel.SetDock(closeBtn, Dock.Right);
            headerDock.Children.Add(closeBtn);
            Grid.SetRow(headerDock, 0);
            g.Children.Add(headerDock);

            _moveOverlaySubtitle = new TextBlock
            {
                Text = "Escribe el nombre de la carpeta que buscas, navega con ↑/↓ y presiona Enter para mover (o Ctrl+Enter para copiar):",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(_moveOverlaySubtitle, 1);
            g.Children.Add(_moveOverlaySubtitle);

            _moveSearchBox = new TextBox
            {
                FontSize = 13.5,
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1.5)
            };
            _moveSearchBox.TextChanged += (s, e) => FilterMoveDestinationFolders(_moveSearchBox.Text);
            _moveSearchBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Down)
                {
                    if (_moveFilteredFolders != null && _moveFilteredFolders.Count > 0)
                    {
                        _moveSelectedIndex = Math.Min(_moveFilteredFolders.Count - 1, _moveSelectedIndex + 1);
                        RenderMoveFolderCandidates();
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    if (_moveFilteredFolders != null && _moveFilteredFolders.Count > 0)
                    {
                        _moveSelectedIndex = Math.Max(0, _moveSelectedIndex - 1);
                        RenderMoveFolderCandidates();
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    bool isCopy = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    ExecuteSelectedFolderMove(!isCopy);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    _moveToFolderOverlay.Visibility = Visibility.Collapsed;
                    if (ActivePane != null) ActivePane.Focus();
                    e.Handled = true;
                }
            };
            Grid.SetRow(_moveSearchBox, 2);
            g.Children.Add(_moveSearchBox);

            _moveResultsStack = new StackPanel();
            var scroll = new ScrollViewer
            {
                Content = _moveResultsStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroll, 3);
            g.Children.Add(scroll);

            overlay.Child = g;
            return overlay;
        }

        public void OpenMoveToFolderOverlay()
        {
            if (ActivePane == null) return;
            var selected = ActivePane.GetSelectedItems();
            if (selected.Count == 0) return;

            _analyzerOverlay.Visibility = Visibility.Collapsed;
            _editorOverlay.Visibility = Visibility.Collapsed;

            var p = ActivePalette;
            _moveToFolderOverlay.Background = VectorStudio.Brush(p.BgCanvas);
            _moveToFolderOverlay.BorderBrush = VectorStudio.Brush(p.Accent);
            _moveOverlayHeaderTitle.Foreground = VectorStudio.Brush(p.AccentLight);
            _moveOverlaySubtitle.Foreground = VectorStudio.Brush(p.TextMuted);
            _moveSearchBox.Background = VectorStudio.Brush("#0B0E12");
            _moveSearchBox.Foreground = VectorStudio.Brush(p.TextMain);
            _moveSearchBox.BorderBrush = VectorStudio.Brush(p.Accent);

            string itemDesc = selected.Count == 1
                ? "'" + selected[0].DisplayName + "'"
                : selected.Count + " elementos seleccionados";
            _moveOverlayHeaderTitle.Text = "📦 Mover / Enviar " + itemDesc + " a otra carpeta (Ctrl+M)";

            BuildMoveFolderCandidateIndex(selected.Select(x => x.FullPath).ToList());
            _moveSearchBox.Text = "";
            FilterMoveDestinationFolders("");

            _moveToFolderOverlay.Visibility = Visibility.Visible;
            _moveSearchBox.Focus();
        }

        private void BuildMoveFolderCandidateIndex(List<string> excludedPaths)
        {
            var excluded = new HashSet<string>(excludedPaths, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _moveCandidateFolders = new List<Tuple<string, string>>();

            Action<string, string> addDir = (label, dirPath) =>
            {
                if (string.IsNullOrWhiteSpace(dirPath)) return;
                try
                {
                    string full = System.IO.Path.GetFullPath(dirPath);
                    if (!Directory.Exists(full)) return;
                    if (excluded.Contains(full)) return;
                    if (ActivePane != null && string.Equals(full, ActivePane.ActiveTab.CurrentPath, StringComparison.OrdinalIgnoreCase)) return;
                    foreach (var ex in excluded)
                    {
                        if (full.StartsWith(ex.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                    if (seen.Add(full))
                    {
                        _moveCandidateFolders.Add(Tuple.Create(label, full));
                    }
                }
                catch { }
            };

            string current = ActivePane.ActiveTab.CurrentPath;
            var parent = Directory.GetParent(current);
            if (parent != null) addDir("⬆ Carpeta padre (" + parent.Name + ")", parent.FullName);

            if (IsSplitView)
            {
                var other = (ActivePane == LeftPane) ? RightPane : LeftPane;
                addDir("◫ Panel opuesto (" + other.ActiveTab.Title + ")", other.ActiveTab.CurrentPath);
            }

            // Subcarpetas del directorio actual y del directorio padre
            try
            {
                foreach (var d in Directory.GetDirectories(current))
                    addDir(System.IO.Path.GetFileName(d), d);
                if (parent != null)
                {
                    foreach (var d in Directory.GetDirectories(parent.FullName))
                        addDir(parent.Name + " › " + System.IO.Path.GetFileName(d), d);
                }
            }
            catch { }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string downloads = System.IO.Path.Combine(home, "Downloads");

            addDir("Escritorio", desktop);
            addDir("Carpeta personal", home);
            addDir("Documentos", docs);
            addDir("Descargas", downloads);

            foreach (var rec in Config.RecentLocations)
                addDir(NexusPaneControl.GetFolderTitle(rec), rec);

            // Indexar carpetas del Escritorio (incluyendo Home, Games, Notas, etc.) hasta 3 niveles
            ScanSubdirectoriesForMoveIndex(desktop, "Escritorio", 3, addDir);
            ScanSubdirectoriesForMoveIndex(docs, "Documentos", 2, addDir);
            ScanSubdirectoriesForMoveIndex(downloads, "Descargas", 2, addDir);
        }

        private void ScanSubdirectoriesForMoveIndex(string rootDir, string prefix, int maxDepth, Action<string, string> addDir)
        {
            if (maxDepth <= 0 || string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return;
            try
            {
                foreach (var sub in Directory.GetDirectories(rootDir))
                {
                    string name = System.IO.Path.GetFileName(sub);
                    if (name.StartsWith(".")) continue;
                    string label = prefix + " › " + name;
                    addDir(label, sub);
                    if (maxDepth > 1)
                        ScanSubdirectoriesForMoveIndex(sub, label, maxDepth - 1, addDir);
                }
            }
            catch { }
        }

        private void FilterMoveDestinationFolders(string query)
        {
            string q = (query ?? "").Trim();
            _moveFilteredFolders = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(q) && Directory.Exists(q))
            {
                _moveFilteredFolders.Add(Tuple.Create("Ruta directa: " + System.IO.Path.GetFileName(q.TrimEnd('\\')), System.IO.Path.GetFullPath(q)));
            }

            if (_moveCandidateFolders != null)
            {
                var matches = _moveCandidateFolders.Where(x =>
                    string.IsNullOrEmpty(q) ||
                    x.Item1.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.Item2.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(30)
                    .ToList();
                _moveFilteredFolders.AddRange(matches);
            }

            _moveSelectedIndex = 0;
            RenderMoveFolderCandidates();
        }

        private void RenderMoveFolderCandidates()
        {
            if (_moveResultsStack == null) return;
            _moveResultsStack.Children.Clear();
            var p = ActivePalette;

            if (_moveFilteredFolders == null || _moveFilteredFolders.Count == 0)
            {
                _moveResultsStack.Children.Add(new TextBlock
                {
                    Text = "No se encontraron carpetas que coincidan. Puedes escribir una ruta completa directamente.",
                    Foreground = VectorStudio.Brush(p.TextMuted),
                    FontSize = 12.5,
                    Margin = new Thickness(4, 12, 4, 4)
                });
                return;
            }

            for (int i = 0; i < _moveFilteredFolders.Count; i++)
            {
                int idx = i;
                var entry = _moveFilteredFolders[i];
                bool isSel = (i == _moveSelectedIndex);

                var rowBorder = new Border
                {
                    Background = VectorStudio.Brush(isSel ? p.SelectBg : p.BgChrome),
                    BorderBrush = VectorStudio.Brush(isSel ? p.Accent : p.Border),
                    BorderThickness = new Thickness(isSel ? 1.5 : 1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };

                var dock = new DockPanel();
                var actionBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

                var moveBtn = CreatePillButton("Mover aquí ↵", "Mover selección a esta carpeta", () =>
                {
                    _moveSelectedIndex = idx;
                    ExecuteSelectedFolderMove(true);
                });
                moveBtn.Background = VectorStudio.Brush("#1A3B34");
                moveBtn.BorderBrush = VectorStudio.Brush("#2ECC71");

                var copyBtn = CreatePillButton("Copiar", "Copiar selección a esta carpeta (Ctrl+Enter)", () =>
                {
                    _moveSelectedIndex = idx;
                    ExecuteSelectedFolderMove(false);
                });
                copyBtn.Background = VectorStudio.Brush(p.BgCanvas);
                copyBtn.BorderBrush = VectorStudio.Brush(p.Border);

                actionBtns.Children.Add(moveBtn);
                actionBtns.Children.Add(copyBtn);
                DockPanel.SetDock(actionBtns, Dock.Right);
                dock.Children.Add(actionBtns);

                var leftInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
                var fIcon = VectorStudio.IconPath(VectorStudio.SVG_FOLDER, isSel ? p.AccentLight : p.Accent, 14);
                fIcon.Margin = new Thickness(0, 0, 8, 0);
                titleRow.Children.Add(fIcon);
                titleRow.Children.Add(new TextBlock
                {
                    Text = entry.Item1,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = VectorStudio.Brush(p.TextMain)
                });
                leftInfo.Children.Add(titleRow);
                leftInfo.Children.Add(new TextBlock
                {
                    Text = entry.Item2,
                    FontSize = 11.2,
                    Foreground = VectorStudio.Brush(p.TextMuted),
                    Margin = new Thickness(22, 2, 0, 0)
                });
                dock.Children.Add(leftInfo);

                rowBorder.Child = dock;
                rowBorder.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    _moveSelectedIndex = idx;
                    ExecuteSelectedFolderMove(true);
                };

                _moveResultsStack.Children.Add(rowBorder);
                if (isSel) rowBorder.BringIntoView();
            }
        }

        private void ExecuteSelectedFolderMove(bool isMove)
        {
            if (_moveFilteredFolders == null || _moveFilteredFolders.Count == 0) return;
            int idx = Math.Max(0, Math.Min(_moveSelectedIndex, _moveFilteredFolders.Count - 1));
            string targetFolder = _moveFilteredFolders[idx].Item2;

            var paths = ActivePane.GetSelectedItems().Select(x => x.FullPath).ToArray();
            _moveToFolderOverlay.Visibility = Visibility.Collapsed;
            MoveOrCopyPathsToDestination(paths, targetFolder, isMove);
            ActivePane.Focus();
        }

        // ====================================================================
        // 8C. MOTOR VISUAL DE ARRASTRE DE ICONO (POR MOUSE Y POR TECLADO)
        // ====================================================================
        private Canvas BuildDragOverlayCanvas()
        {
            var canvas = new Canvas
            {
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(canvas, 999);

            _dragGhostBorder = new Border
            {
                Background = VectorStudio.Brush("#EE151A22"),
                BorderBrush = VectorStudio.Brush("#2ECC71"),
                BorderThickness = new Thickness(1.6),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 14, 8),
                Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 5, Opacity = 0.75, Color = Colors.Black }
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _dragGhostIconHolder = new Border
            {
                Width = 42,
                Height = 36,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(_dragGhostIconHolder);

            var textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _dragGhostTitle = new TextBlock
            {
                Text = "Moviendo carpeta...",
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            _dragGhostSubtitle = new TextBlock
            {
                Text = "Arrastra hacia otra carpeta para soltar",
                FontSize = 11.2,
                Foreground = VectorStudio.Brush("#A3F7BF"),
                Margin = new Thickness(0, 2, 0, 0)
            };
            textCol.Children.Add(_dragGhostTitle);
            textCol.Children.Add(_dragGhostSubtitle);
            row.Children.Add(textCol);

            _dragGhostBorder.Child = row;
            canvas.Children.Add(_dragGhostBorder);
            return canvas;
        }

        private Border BuildKeyboardDragBanner()
        {
            var banner = new Border
            {
                Background = VectorStudio.Brush("#17362B"),
                BorderBrush = VectorStudio.Brush("#2ECC71"),
                BorderThickness = new Thickness(0, 0, 0, 1.5),
                Padding = new Thickness(14, 6, 14, 6),
                Visibility = Visibility.Collapsed
            };

            var dock = new DockPanel();
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnDropHere = CreatePillButton("✔ Soltar en Carpeta Seleccionada (Enter)", "Mover dentro de la carpeta donde está el cursor", () =>
            {
                ConfirmKeyboardFolderDragDrop();
            });
            btnDropHere.Background = VectorStudio.Brush("#1E5646");
            btnDropHere.BorderBrush = VectorStudio.Brush("#2ECC71");
            ((TextBlock)btnDropHere.Child).FontWeight = FontWeights.Bold;
            ((TextBlock)btnDropHere.Child).Foreground = Brushes.White;

            var btnCancel = CreatePillButton("✕ Cancelar Arrastre (Esc)", "Cancelar movimiento", () =>
            {
                CancelKeyboardFolderDrag();
            });
            btnCancel.Background = VectorStudio.Brush("#381C21");
            btnCancel.BorderBrush = VectorStudio.Brush("#E74C3C");

            btns.Children.Add(btnDropHere);
            btns.Children.Add(btnCancel);
            DockPanel.SetDock(btns, Dock.Right);
            dock.Children.Add(btns);

            _keyboardDragBannerText = new TextBlock
            {
                Text = "🖐 ARRASTRANDO CON TECLADO — Usa las Flechas para ir a otra carpeta y presiona Enter para moverla ahí",
                FontSize = 12.2,
                FontWeight = FontWeights.SemiBold,
                Foreground = VectorStudio.Brush("#A3F7BF"),
                VerticalAlignment = VerticalAlignment.Center
            };
            dock.Children.Add(_keyboardDragBannerText);

            banner.Child = dock;
            return banner;
        }

        private void PopulateDragGhostVisual(FileItem leadItem, int totalCount)
        {
            var p = ActivePalette;
            if (leadItem != null)
            {
                _dragGhostIconHolder.Child = leadItem.IsDirectory
                    ? VectorStudio.CreateFolderGraphic(40, p, Config.FolderModel)
                    : VectorStudio.CreateFileGraphic(38, p, leadItem.Extension, leadItem);
                _dragGhostTitle.Text = totalCount > 1
                    ? string.Format("📦 Moviendo: {0} (+{1} más)", leadItem.DisplayName, totalCount - 1)
                    : string.Format("📦 Moviendo: {0}", leadItem.DisplayName);
            }
            else
            {
                _dragGhostIconHolder.Child = VectorStudio.CreateFolderGraphic(40, p, "Breeze");
                _dragGhostTitle.Text = "📦 Moviendo elementos...";
            }
        }

        public void StartVisualMouseDrag(NexusPaneControl sourcePane, FileItem leadItem, List<FileItem> selectedItems, Point windowPos)
        {
            CancelKeyboardFolderDrag();
            _activeDragLeadItem = leadItem;
            _activeDragPaths = selectedItems.Select(x => x.FullPath).ToList();
            _isMouseDragActive = true;
            _hoveredDropTargetPath = null;
            ClearHoveredDropTargetHighlight();

            PopulateDragGhostVisual(leadItem, _activeDragPaths.Count);
            _dragGhostSubtitle.Text = "Arrastra el icono hacia otra carpeta para mover...";
            _dragOverlayCanvas.Visibility = Visibility.Visible;

            if (LeftPane != null) LeftPane.UpdateSelectionVisuals();
            if (IsSplitView && RightPane != null) RightPane.UpdateSelectionVisuals();

            UpdateVisualMouseDrag(windowPos);
        }

        public void UpdateVisualMouseDrag(Point windowPos)
        {
            if (!_isMouseDragActive || _dragOverlayCanvas == null) return;

            double x = Math.Max(8, Math.Min(ActualWidth - 260, windowPos.X + 16));
            double y = Math.Max(8, Math.Min(ActualHeight - 75, windowPos.Y + 12));
            Canvas.SetLeft(_dragGhostBorder, x);
            Canvas.SetTop(_dragGhostBorder, y);

            Border targetBorder;
            string targetFolder = FindDropTargetAtWindowPoint(windowPos, out targetBorder);

            if (targetBorder != _hoveredDropTargetBorder)
            {
                ClearHoveredDropTargetHighlight();
                if (targetBorder != null)
                {
                    _hoveredDropTargetBorder = targetBorder;
                    _hoveredDropTargetOrigBg = targetBorder.Background;
                    _hoveredDropTargetOrigBorder = targetBorder.BorderBrush;
                    _hoveredDropTargetOrigThickness = targetBorder.BorderThickness;

                    targetBorder.Background = VectorStudio.Brush("#332ECC71");
                    targetBorder.BorderBrush = VectorStudio.Brush("#2ECC71");
                    targetBorder.BorderThickness = new Thickness(2.2);
                }
            }

            _hoveredDropTargetPath = targetFolder;
            if (!string.IsNullOrEmpty(targetFolder))
            {
                _dragGhostBorder.BorderBrush = VectorStudio.Brush("#2ECC71");
                _dragGhostSubtitle.Text = "➜ Soltar para mover dentro de: " + NexusPaneControl.GetFolderTitle(targetFolder);
                _dragGhostSubtitle.Foreground = VectorStudio.Brush("#2ECC71");
            }
            else
            {
                _dragGhostBorder.BorderBrush = VectorStudio.Brush(ActivePalette.Accent);
                _dragGhostSubtitle.Text = "Arrastra el icono sobre cualquier carpeta destino...";
                _dragGhostSubtitle.Foreground = VectorStudio.Brush(ActivePalette.TextMuted);
            }
        }

        public void CompleteVisualMouseDrag(Point windowPos, bool isCopy)
        {
            if (!_isMouseDragActive) return;

            Border targetBorder;
            string targetFolder = FindDropTargetAtWindowPoint(windowPos, out targetBorder);
            if (string.IsNullOrEmpty(targetFolder))
                targetFolder = _hoveredDropTargetPath;

            var pathsToMove = _activeDragPaths != null ? _activeDragPaths.ToList() : new List<string>();

            _isMouseDragActive = false;
            _activeDragPaths.Clear();
            _hoveredDropTargetPath = null;
            ClearHoveredDropTargetHighlight();
            _dragOverlayCanvas.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(targetFolder) && Directory.Exists(targetFolder) && pathsToMove.Count > 0)
            {
                MoveOrCopyPathsToDestination(pathsToMove, targetFolder, !isCopy);
            }
            else
            {
                if (LeftPane != null) LeftPane.UpdateSelectionVisuals();
                if (IsSplitView && RightPane != null) RightPane.UpdateSelectionVisuals();
            }
        }

        private void ClearHoveredDropTargetHighlight()
        {
            if (_hoveredDropTargetBorder != null)
            {
                try
                {
                    _hoveredDropTargetBorder.Background = _hoveredDropTargetOrigBg;
                    _hoveredDropTargetBorder.BorderBrush = _hoveredDropTargetOrigBorder;
                    _hoveredDropTargetBorder.BorderThickness = _hoveredDropTargetOrigThickness;
                }
                catch { }
                _hoveredDropTargetBorder = null;
            }
        }

        private string FindDropTargetAtWindowPoint(Point winPos, out Border matchedBorder)
        {
            matchedBorder = null;
            var draggedSet = new HashSet<string>(_activeDragPaths ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            // 1. Verificar todas las carpetas visibles en el Panel Izquierdo y Panel Derecho
            NexusPaneControl[] panes = IsSplitView ? new[] { LeftPane, RightPane } : new[] { LeftPane };
            foreach (var pane in panes)
            {
                if (pane == null || !pane.IsVisible) continue;

                // Verificar que el punto esté dentro del área visible del ScrollViewer de ese panel
                if (IsPointInsideElement(winPos, pane.CanvasScroll))
                {
                    int count = Math.Min(pane.CurrentItems.Count, pane.RenderedTiles.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var item = pane.CurrentItems[i];
                        if (!item.IsDirectory) continue;
                        if (draggedSet.Contains(item.FullPath)) continue;

                        var tile = pane.RenderedTiles[i];
                        if (IsPointInsideElement(winPos, tile))
                        {
                            matchedBorder = tile;
                            return item.FullPath;
                        }
                    }
                }
            }

            // 2. Verificar Pestañas, Breadcrumbs y Accesos de la Barra Lateral registrados
            for (int i = _registeredDropTargets.Count - 1; i >= 0; i--)
            {
                var entry = _registeredDropTargets[i];
                var b = entry.Item1;
                if (b == null || !b.IsVisible || b.ActualWidth <= 0 || b.ActualHeight <= 0) continue;
                if (IsPointInsideElement(winPos, b))
                {
                    string dir = entry.Item2();
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && !draggedSet.Contains(dir))
                    {
                        matchedBorder = b;
                        return dir;
                    }
                }
            }

            // 3. Si está activo el Doble Panel (F3), permitir soltar en el fondo del panel opuesto
            if (IsSplitView)
            {
                foreach (var pane in panes)
                {
                    if (pane != null && IsPointInsideElement(winPos, pane.CanvasFrameBorder))
                    {
                        string paneDir = pane.ActiveTab.CurrentPath;
                        bool anyFromDifferentDir = _activeDragPaths.Any(p =>
                            !string.Equals(System.IO.Path.GetDirectoryName(p.TrimEnd('\\')), paneDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
                        if (anyFromDifferentDir)
                        {
                            matchedBorder = pane.CanvasFrameBorder;
                            return paneDir;
                        }
                    }
                }
            }

            return null;
        }

        private bool IsPointInsideElement(Point winPos, FrameworkElement el)
        {
            if (el == null || !el.IsVisible || el.ActualWidth <= 0 || el.ActualHeight <= 0) return false;
            try
            {
                Point topLeft = el.TranslatePoint(new Point(0, 0), this);
                var rect = new Rect(topLeft.X, topLeft.Y, el.ActualWidth, el.ActualHeight);
                return rect.Contains(winPos);
            }
            catch
            {
                return false;
            }
        }

        // Arrastre de carpeta con el teclado (Shift + Flechas o tecla G)
        public void StartKeyboardFolderDrag(List<FileItem> itemsToDrag)
        {
            if (itemsToDrag == null || itemsToDrag.Count == 0) return;
            _activeDragLeadItem = itemsToDrag[0];
            _activeDragPaths = itemsToDrag.Select(x => x.FullPath).ToList();
            _isKeyboardDragActive = true;
            _isMouseDragActive = false;

            PopulateDragGhostVisual(_activeDragLeadItem, _activeDragPaths.Count);
            _dragOverlayCanvas.Visibility = Visibility.Visible;

            string desc = _activeDragPaths.Count == 1 ? "'" + _activeDragLeadItem.DisplayName + "'" : _activeDragPaths.Count + " elementos";
            _keyboardDragBannerText.Text = string.Format(
                "🖐 MOVIENDO {0} CON TECLADO: Usa las Flechas (← ↑ → ↓) para colocarte sobre la carpeta destino y presiona [Enter] para soltarla ahí (o [Esc] para cancelar)",
                desc);
            _keyboardDragBanner.Visibility = Visibility.Visible;

            if (LeftPane != null) LeftPane.UpdateSelectionVisuals();
            if (IsSplitView && RightPane != null) RightPane.UpdateSelectionVisuals();
            UpdateKeyboardDragGhostPosition();
        }

        public void UpdateKeyboardDragGhostPosition()
        {
            if (!IsKeyboardDragActive || ActivePane == null || _dragOverlayCanvas == null) return;

            var curItem = ActivePane.GetCurrentCursorItem();
            bool validTargetFolder = curItem != null && curItem.IsDirectory && !IsPathBeingDragged(curItem.FullPath);

            if (validTargetFolder)
            {
                _dragGhostBorder.BorderBrush = VectorStudio.Brush("#2ECC71");
                _dragGhostSubtitle.Text = "➜ Presiona [Enter] para mover dentro de: " + curItem.DisplayName;
                _dragGhostSubtitle.Foreground = VectorStudio.Brush("#2ECC71");
            }
            else
            {
                _dragGhostBorder.BorderBrush = VectorStudio.Brush(ActivePalette.Accent);
                _dragGhostSubtitle.Text = "Muévete con las flechas hacia otra carpeta y presiona [Enter]";
                _dragGhostSubtitle.Foreground = VectorStudio.Brush(ActivePalette.TextMuted);
            }

            if (ActivePane.CursorIndex >= 0 && ActivePane.CursorIndex < ActivePane.RenderedTiles.Count)
            {
                var tile = ActivePane.RenderedTiles[ActivePane.CursorIndex];
                try
                {
                    Point pt = tile.TranslatePoint(new Point(tile.ActualWidth * 0.55, Math.Max(4, tile.ActualHeight * 0.15)), this);
                    double x = Math.Max(10, Math.Min(ActualWidth - 310, pt.X));
                    double y = Math.Max(70, Math.Min(ActualHeight - 85, pt.Y));
                    Canvas.SetLeft(_dragGhostBorder, x);
                    Canvas.SetTop(_dragGhostBorder, y);
                    return;
                }
                catch { }
            }

            Canvas.SetLeft(_dragGhostBorder, 260);
            Canvas.SetTop(_dragGhostBorder, 110);
        }

        public void ConfirmKeyboardFolderDragDrop()
        {
            if (!IsKeyboardDragActive || ActivePane == null) return;
            var cur = ActivePane.GetCurrentCursorItem();
            string targetDir = null;

            if (cur != null && cur.IsDirectory && !IsPathBeingDragged(cur.FullPath))
            {
                targetDir = cur.FullPath;
            }
            else
            {
                // Si el usuario navegó dentro de otro directorio distinto al origen, soltar en el directorio actual
                string activeDir = ActivePane.ActiveTab.CurrentPath;
                bool fromOtherDir = _activeDragPaths.Any(p =>
                    !string.Equals(System.IO.Path.GetDirectoryName(p.TrimEnd('\\')), activeDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
                if (fromOtherDir)
                    targetDir = activeDir;
            }

            if (string.IsNullOrEmpty(targetDir))
            {
                if (_statusLeftLabel != null)
                    _statusLeftLabel.Text = "⚠ Coloca el cursor sobre otra carpeta destino antes de presionar Enter.";
                return;
            }

            CompleteKeyboardDragInto(targetDir);
        }

        public void CompleteKeyboardDragInto(string targetDir)
        {
            if (!IsKeyboardDragActive || string.IsNullOrEmpty(targetDir)) return;
            var paths = _activeDragPaths.ToList();
            CancelKeyboardFolderDrag();
            MoveOrCopyPathsToDestination(paths, targetDir, true);
        }

        public void CancelKeyboardFolderDrag()
        {
            _isKeyboardDragActive = false;
            if (!_isMouseDragActive)
            {
                _activeDragPaths.Clear();
                if (_dragOverlayCanvas != null) _dragOverlayCanvas.Visibility = Visibility.Collapsed;
            }
            if (_keyboardDragBanner != null) _keyboardDragBanner.Visibility = Visibility.Collapsed;
            if (LeftPane != null) LeftPane.UpdateSelectionVisuals();
            if (IsSplitView && RightPane != null) RightPane.UpdateSelectionVisuals();
        }

        public void MoveOrCopyPathsToDestination(IEnumerable<string> sourcePaths, string destinationFolder, bool isMove)
        {
            if (sourcePaths == null || string.IsNullOrWhiteSpace(destinationFolder) || !Directory.Exists(destinationFolder))
                return;

            string fullDstDir = System.IO.Path.GetFullPath(destinationFolder);
            int processed = 0;
            string lastError = null;

            if (LeftPane != null) LeftPane.PauseWatcher(true);
            if (RightPane != null) RightPane.PauseWatcher(true);

            try
            {
                foreach (var src in sourcePaths)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(src)) continue;
                        string fullSrc = System.IO.Path.GetFullPath(src);
                        string name = System.IO.Path.GetFileName(fullSrc.TrimEnd('\\'));
                        if (string.IsNullOrEmpty(name)) continue;

                        string dst = System.IO.Path.Combine(fullDstDir, name);
                        if (string.Equals(fullSrc, dst, StringComparison.OrdinalIgnoreCase)) continue;

                        // Evitar mover una carpeta dentro de sí misma
                        if (Directory.Exists(fullSrc) &&
                            fullDstDir.StartsWith(fullSrc.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                        {
                            lastError = "No se puede mover una carpeta dentro de sí misma.";
                            continue;
                        }

                        if (Directory.Exists(fullSrc))
                        {
                            if (isMove)
                            {
                                string runningExe = Process.GetCurrentProcess().MainModule.FileName;
                                bool containsRunningSelf = runningExe.StartsWith(fullSrc.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);

                                bool movedFast = false;
                                if (!containsRunningSelf &&
                                    !Directory.Exists(dst) &&
                                    string.Equals(System.IO.Path.GetPathRoot(fullSrc), System.IO.Path.GetPathRoot(dst), StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        var di = new DirectoryInfo(fullSrc);
                                        if ((di.Attributes & FileAttributes.ReadOnly) != 0)
                                            di.Attributes &= ~FileAttributes.ReadOnly;
                                        Directory.Move(fullSrc, dst);
                                        movedFast = true;
                                    }
                                    catch
                                    {
                                        movedFast = false;
                                    }
                                }

                                if (!movedFast)
                                {
                                    RobustCopyDirectory(fullSrc, dst);
                                    if (!containsRunningSelf)
                                    {
                                        RobustDeleteDirectory(fullSrc);
                                    }
                                    else
                                    {
                                        lastError = "La carpeta contiene el ejecutable activo de NexusFM; se realizó una copia segura sin borrar el origen.";
                                    }
                                }
                                FolderDescriptionManager.MigrateDescriptionPath(fullSrc, dst);
                            }
                            else
                            {
                                RobustCopyDirectory(fullSrc, dst);
                            }
                            processed++;
                        }
                        else if (File.Exists(fullSrc))
                        {
                            if (isMove)
                            {
                                if (File.Exists(dst))
                                {
                                    File.SetAttributes(dst, FileAttributes.Normal);
                                    File.Delete(dst);
                                }
                                File.Move(fullSrc, dst);
                            }
                            else
                            {
                                if (File.Exists(dst))
                                    File.SetAttributes(dst, FileAttributes.Normal);
                                File.Copy(fullSrc, dst, true);
                            }
                            processed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex.Message;
                    }
                }
            }
            finally
            {
                if (LeftPane != null) LeftPane.PauseWatcher(false);
                if (RightPane != null) RightPane.PauseWatcher(false);
            }

            LeftPane.ReadEntries();
            LeftPane.RenderItems();
            if (IsSplitView)
            {
                RightPane.ReadEntries();
                RightPane.RenderItems();
            }
            UpdateBottomStatusBar();

            if (processed > 0 && _statusLeftLabel != null)
            {
                _statusLeftLabel.Text = string.Format("✔ Se {0} {1} elemento(s) dentro de: {2}",
                    isMove ? "movió" : "copió", processed, NexusPaneControl.GetFolderTitle(fullDstDir));
            }
            else if (processed == 0 && !string.IsNullOrEmpty(lastError) && _statusLeftLabel != null)
            {
                _statusLeftLabel.Text = "⚠ No se pudo mover: " + lastError;
            }
        }

        private void RobustCopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                string dstFile = System.IO.Path.Combine(dst, System.IO.Path.GetFileName(f));
                if (File.Exists(dstFile))
                {
                    try { File.SetAttributes(dstFile, FileAttributes.Normal); } catch { }
                }
                File.Copy(f, dstFile, true);
            }
            foreach (var d in Directory.GetDirectories(src))
            {
                RobustCopyDirectory(d, System.IO.Path.Combine(dst, System.IO.Path.GetFileName(d)));
            }
        }

        private void RobustDeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return;
            foreach (var f in Directory.GetFiles(targetDir))
            {
                try { File.SetAttributes(f, FileAttributes.Normal); } catch { }
            }
            foreach (var d in Directory.GetDirectories(targetDir))
            {
                RobustDeleteDirectory(d);
            }
            try
            {
                var di = new DirectoryInfo(targetDir);
                di.Attributes = FileAttributes.Normal;
            }
            catch { }
            Directory.Delete(targetDir, true);
        }

        public void AttachFolderDropTarget(Border targetBorder, Func<string> getTargetDir, string normalBgHex, string normalBorderHex)
        {
            _registeredDropTargets.Add(Tuple.Create(targetBorder, getTargetDir));
            targetBorder.AllowDrop = true;
            targetBorder.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    targetBorder.Background = VectorStudio.Brush(ActivePalette.SelectBg);
                    targetBorder.BorderBrush = VectorStudio.Brush("#2ECC71");
                    bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                    e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
                }
                else e.Effects = DragDropEffects.None;
                e.Handled = true;
            };
            targetBorder.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                    e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
                }
                else e.Effects = DragDropEffects.None;
                e.Handled = true;
            };
            targetBorder.DragLeave += (s, e) =>
            {
                targetBorder.Background = string.IsNullOrEmpty(normalBgHex) ? Brushes.Transparent : VectorStudio.Brush(normalBgHex);
                targetBorder.BorderBrush = string.IsNullOrEmpty(normalBorderHex) ? Brushes.Transparent : VectorStudio.Brush(normalBorderHex);
            };
            targetBorder.Drop += (s, e) =>
            {
                e.Handled = true;
                targetBorder.Background = string.IsNullOrEmpty(normalBgHex) ? Brushes.Transparent : VectorStudio.Brush(normalBgHex);
                targetBorder.BorderBrush = string.IsNullOrEmpty(normalBorderHex) ? Brushes.Transparent : VectorStudio.Brush(normalBorderHex);
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                    string dir = getTargetDir();
                    bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                    if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(dir))
                        MoveOrCopyPathsToDestination(paths, dir, !copy);
                }
            };
        }

        // ====================================================================
        // 9. BARRA LATERAL COMPLETA (LUGARES + RECIENTES + BUSCAR POR + DISPOSITIVOS)
        // ====================================================================
        private Border BuildSidebar()
        {
            var border = new Border
            {
                Width = 205,
                Padding = new Thickness(8, 10, 8, 10),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            var stack = new StackPanel();

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string downloads = System.IO.Path.Combine(home, "Downloads");

            stack.Children.Add(CreateSectionHeader("Lugares"));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_HOME, "Carpeta personal", () => ActivePane.LoadDirectory(home, true), home));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_DESKTOP, "Escritorio", () => ActivePane.LoadDirectory(desktop, true), desktop));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_FOLDER, "Descargas", () => ActivePane.LoadDirectory(downloads, true), downloads));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_DOC, "Documentos", () => ActivePane.LoadDirectory(docs, true), docs));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_CODE, "Proyecto NexusFM", () => ActivePane.LoadDirectory(Environment.CurrentDirectory, true), Environment.CurrentDirectory));

            // Sección "Recientes" (como en la imagen 2)
            stack.Children.Add(CreateSectionHeader("Recientes"));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_CLOCK, "Ubicaciones recientes", () => ShowRecentLocationsMenu()));

            // Sección "Buscar por" (idéntica a "Search For: Documents, Images, Audio, Videos" de la imagen 2)
            stack.Children.Add(CreateSectionHeader("Buscar por tipo"));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_DOC, "Documentos", () => ActivePane.SetCategoryFilter("DOC", "Documentos")));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_IMAGE, "Imágenes", () => ActivePane.SetCategoryFilter("IMG", "Imágenes")));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_AUDIO, "Audio", () => ActivePane.SetCategoryFilter("AUD", "Audio")));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_VIDEO, "Videos", () => ActivePane.SetCategoryFilter("VID", "Videos")));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_CODE, "Código fuente", () => ActivePane.SetCategoryFilter("CODE", "Código fuente")));

            // Sección "Dispositivos" con barra fina debajo de cada unidad (como en la imagen 2)
            stack.Children.Add(CreateSectionHeader("Dispositivos"));
            foreach (var d in DriveInfo.GetDrives().Where(x => x.IsReady))
            {
                stack.Children.Add(CreateDriveItem(d));
            }

            border.Child = new ScrollViewer
            {
                Content = stack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            return border;
        }

        private TextBlock CreateSectionHeader(string title)
        {
            return new TextBlock
            {
                Text = title,
                FontSize = 11.2,
                Foreground = VectorStudio.Brush("#7F8A96"),
                Margin = new Thickness(8, 12, 4, 4)
            };
        }

        private Border CreateSidebarVectorRow(string svgPath, string label, Action onClick, string dropTargetFolder = null)
        {
            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconEl = VectorStudio.IconPath(svgPath, "#9AA4AF", 13.5);
            var lblTb = new TextBlock
            {
                Text = label,
                FontSize = 12.3,
                Foreground = VectorStudio.Brush("#EFF0F1"),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(iconEl, 0);
            Grid.SetColumn(lblTb, 1);
            rowGrid.Children.Add(iconEl);
            rowGrid.Children.Add(lblTb);

            var itemBorder = new Border
            {
                Padding = new Thickness(6, 5, 8, 5),
                Margin = new Thickness(0, 1, 0, 1),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Child = rowGrid
            };

            if (!string.IsNullOrEmpty(dropTargetFolder))
            {
                AttachFolderDropTarget(itemBorder, () => dropTargetFolder, null, null);
            }

            itemBorder.MouseEnter += (s, e) => itemBorder.Background = VectorStudio.Brush(ActivePalette.HoverBg);
            itemBorder.MouseLeave += (s, e) => itemBorder.Background = Brushes.Transparent;
            itemBorder.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                ActivePane.SetCategoryFilter("", "");
                onClick();
            };

            return itemBorder;
        }

        private void ShowRecentLocationsMenu()
        {
            if (_contextPopup != null) _contextPopup.IsOpen = false;
            var p = ActivePalette;
            var outer = new Border
            {
                Background = VectorStudio.Brush(p.BgPopup),
                BorderBrush = VectorStudio.Brush(p.Accent),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4, 6, 4, 6),
                Width = 320
            };
            var stack = new StackPanel();
            foreach (var loc in Config.RecentLocations)
            {
                string path = loc;
                stack.Children.Add(CreateMenuRow("📁", path, "", false, () => ActivePane.LoadDirectory(path, true)));
            }
            outer.Child = stack;
            _contextPopup = new Popup
            {
                Child = outer,
                Placement = PlacementMode.MousePoint,
                StaysOpen = false,
                AllowsTransparency = true
            };
            _contextPopup.IsOpen = true;
        }

        private Border CreateDriveItem(DriveInfo d)
        {
            double total = Math.Max(1, d.TotalSize);
            double used = total - d.AvailableFreeSpace;
            double ratio = Math.Max(0.06, Math.Min(1.0, used / total));
            string name = string.IsNullOrEmpty(d.VolumeLabel) ? "Disco local (" + d.Name.TrimEnd('\\') + ")" : d.VolumeLabel + " (" + d.Name.TrimEnd('\\') + ")";

            var stack = new StackPanel();
            var topRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var dIcon = VectorStudio.IconPath(VectorStudio.SVG_DRIVE, "#8D96A0", 13);
            dIcon.Margin = new Thickness(4, 0, 8, 0);
            topRow.Children.Add(dIcon);
            topRow.Children.Add(new TextBlock { Text = name, Foreground = VectorStudio.Brush("#EFF0F1"), FontSize = 12.2 });
            stack.Children.Add(topRow);

            var barBg = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(1.5),
                Background = VectorStudio.Brush("#101317"),
                Margin = new Thickness(24, 4, 6, 0)
            };
            var barFill = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(1.5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 145 * ratio,
                Background = VectorStudio.Brush("#4DA2DB")
            };
            barBg.Child = barFill;
            stack.Children.Add(barBg);

            var container = new Border
            {
                Padding = new Thickness(4, 5, 6, 6),
                Margin = new Thickness(0, 1, 0, 2),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Child = stack
            };
            container.MouseEnter += (s, e) => container.Background = VectorStudio.Brush(ActivePalette.HoverBg);
            container.MouseLeave += (s, e) => container.Background = Brushes.Transparent;
            container.MouseLeftButtonDown += (s, e) => ActivePane.LoadDirectory(d.RootDirectory.FullName, true);

            return container;
        }

        // ====================================================================
        // 10. INSPECTOR DERECHO CON VERIFICADOR SHA-256 Y EDICIÓN RÁPIDA (F11)
        // ====================================================================
        private Border BuildInfoDock()
        {
            var border = new Border
            {
                Width = 245,
                Padding = new Thickness(16, 18, 16, 14),
                BorderThickness = new Thickness(1, 0, 0, 0)
            };

            var stack = new StackPanel();
            _infoArtworkHolder = new Border
            {
                Height = 110,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 10)
            };
            stack.Children.Add(_infoArtworkHolder);

            stack.Children.Add(new Border
            {
                Height = 1,
                Background = VectorStudio.Brush("#2A3038"),
                Margin = new Thickness(0, 4, 0, 10)
            });

            _infoTitle = new TextBlock
            {
                Text = "Carpeta personal",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(_infoTitle);

            _infoMetaTable = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(_infoMetaTable);

            var btnDescribeDock = CreateIconTextPill(VectorStudio.SVG_NOTE, "Descripción de Carpeta (F10)", "", () =>
            {
                OpenFolderDescriptionDialog(null);
            });
            btnDescribeDock.Margin = new Thickness(0, 0, 0, 6);
            stack.Children.Add(btnDescribeDock);

            var btnEditInline = CreateIconTextPill(VectorStudio.SVG_EDIT, "Editar Archivo (Ctrl+E)", "", () =>
            {
                var cur = ActivePane.GetCurrentCursorItem();
                if (cur != null && !cur.IsDirectory) OpenFileInIntegratedEditor(cur);
            });
            btnEditInline.Margin = new Thickness(0, 0, 0, 6);
            stack.Children.Add(btnEditInline);

            var btnHash = CreatePillButton("🔐 Calcular Hash SHA-256", "Verificar integridad del archivo", () => ComputeSelectedFileHash());
            btnHash.Background = VectorStudio.Brush("#161A20");
            btnHash.BorderBrush = VectorStudio.Brush("#2C3440");
            btnHash.Margin = new Thickness(0, 0, 0, 6);
            stack.Children.Add(btnHash);

            _hashResultText = new TextBlock
            {
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };
            stack.Children.Add(_hashResultText);

            _infoTextPreview = new TextBox
            {
                IsReadOnly = true,
                Visibility = Visibility.Collapsed,
                Height = 170,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1)
            };
            stack.Children.Add(_infoTextPreview);

            border.Child = new ScrollViewer
            {
                Content = stack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            return border;
        }

        private void ComputeSelectedFileHash()
        {
            var item = ActivePane.GetCurrentCursorItem();
            if (item == null || item.IsDirectory) return;
            try
            {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(item.FullPath))
                {
                    byte[] hash = sha.ComputeHash(stream);
                    string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    _hashResultText.Text = "SHA-256:\n" + hex;
                    _hashResultText.Foreground = VectorStudio.Brush(ActivePalette.AccentLight);
                    _hashResultText.Visibility = Visibility.Visible;
                    Clipboard.SetText(hex);
                }
            }
            catch (Exception ex)
            {
                _hashResultText.Text = "Error: " + ex.Message;
                _hashResultText.Visibility = Visibility.Visible;
            }
        }

        public void UpdateInfoDock(FileItem item)
        {
            if (_infoArtworkHolder == null) return;
            var p = ActivePalette;
            _infoMetaTable.Children.Clear();
            _hashResultText.Visibility = Visibility.Collapsed;

            if (item == null)
            {
                string curDir = ActivePane != null ? ActivePane.ActiveTab.CurrentPath : "";
                string curDesc = FolderDescriptionManager.GetDescription(curDir);
                _infoArtworkHolder.Child = VectorStudio.CreateFolderGraphic(96, p, Config.FolderModel, !string.IsNullOrEmpty(curDesc));
                _infoTitle.Text = ActivePane != null ? ActivePane.ActiveTab.Title : "Carpeta";
                AddMetaRow("Tipo:", "Carpeta actual");
                AddMetaRow("Elementos:", (ActivePane != null ? ActivePane.CurrentItems.Count : 0).ToString());
                AddMetaRow("Descripción:", !string.IsNullOrEmpty(curDesc) ? curDesc : "Sin descripción (F10 para agregar)");
                _infoTextPreview.Visibility = Visibility.Collapsed;
                return;
            }

            _infoArtworkHolder.Child = item.IsDirectory
                ? VectorStudio.CreateFolderGraphic(96, p, Config.FolderModel, !string.IsNullOrEmpty(item.Description))
                : VectorStudio.CreateFileGraphic(92, p, item.Extension, item);

            _infoTitle.Text = item.DisplayName;
            AddMetaRow("Tipo:", item.IsDirectory ? "Carpeta" : "Archivo " + item.Extension);
            if (item.IsDirectory)
            {
                AddMetaRow("Adentro:", string.IsNullOrEmpty(item.ContentSummary) ? "Carpeta" : item.ContentSummary);
                AddMetaRow("Descripción:", !string.IsNullOrEmpty(item.Description) ? item.Description : "Sin descripción (F10 para colocar)");
            }
            else
            {
                AddMetaRow("Tamaño:", item.SizeText);
            }
            AddMetaRow("Modificado:", item.ModifiedText);
            AddMetaRow("Creado:", NexusPaneControl.FormatRelativeTime(item.CreatedDate));

            if (!item.IsDirectory && item.RawSize < 64 * 1024)
            {
                try
                {
                    using (var sr = new StreamReader(item.FullPath, Encoding.UTF8, true))
                    {
                        char[] buf = new char[1200];
                        int n = sr.Read(buf, 0, buf.Length);
                        string txt = new string(buf, 0, n);
                        if (!txt.Contains('\0'))
                        {
                            _infoTextPreview.Text = txt;
                            _infoTextPreview.Visibility = Visibility.Visible;
                            return;
                        }
                    }
                }
                catch { }
            }
            _infoTextPreview.Visibility = Visibility.Collapsed;
        }

        private void AddMetaRow(string label, string val)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = label,
                FontSize = 11.5,
                Foreground = VectorStudio.Brush(ActivePalette.TextMuted),
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var v = new TextBlock
            {
                Text = val,
                FontSize = 11.5,
                Foreground = VectorStudio.Brush(ActivePalette.TextMain),
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(v, 1);
            g.Children.Add(lbl);
            g.Children.Add(v);
            _infoMetaTable.Children.Add(g);
        }

        // ====================================================================
        // 11. BARRA DE ESTADO INFERIOR CON ZOOM Y DISCO (COMO EN LA IMAGEN 2)
        // ====================================================================
        private Border BuildStatusBar()
        {
            var border = new Border
            {
                Padding = new Thickness(12, 4, 18, 4),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            var dock = new DockPanel();

            _statusLeftLabel = new TextBlock
            {
                Text = "Listo",
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_statusLeftLabel, Dock.Left);
            dock.Children.Add(_statusLeftLabel);

            var rightStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            rightStack.Children.Add(new TextBlock
            {
                Text = "Zoom:",
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            _zoomSlider = new Slider
            {
                Minimum = 52,
                Maximum = 128,
                Value = Config.IconSize,
                Width = 95,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };
            _zoomSlider.ValueChanged += (s, e) =>
            {
                Config.IconSize = _zoomSlider.Value;
                Config.Save();
                if (LeftPane != null) LeftPane.RenderItems();
                if (RightPane != null) RightPane.RenderItems();
            };
            rightStack.Children.Add(_zoomSlider);

            _diskSpaceBar = new ProgressBar
            {
                Width = 75,
                Height = 4,
                Minimum = 0,
                Maximum = 100,
                Value = 35,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Background = VectorStudio.Brush("#0E1115"),
                BorderThickness = new Thickness(0)
            };
            rightStack.Children.Add(_diskSpaceBar);

            _diskSpaceLabel = new TextBlock
            {
                Text = "-- GiB libre",
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            rightStack.Children.Add(_diskSpaceLabel);

            DockPanel.SetDock(rightStack, Dock.Right);
            dock.Children.Add(rightStack);

            border.Child = dock;
            return border;
        }

        public void UpdateBottomStatusBar()
        {
            if (ActivePane == null || _statusLeftLabel == null) return;
            int dirs = ActivePane.CurrentItems.Count(x => x.IsDirectory);
            int files = ActivePane.CurrentItems.Count - dirs;
            var item = ActivePane.GetCurrentCursorItem();

            if (item != null)
            {
                _statusLeftLabel.Text = string.Format("{0} carpetas, {1} archivos  •  Seleccionado: {2}", dirs, files, item.Name);
            }
            else
            {
                _statusLeftLabel.Text = string.Format("{0} carpetas, {1} archivos", dirs, files);
            }

            try
            {
                string root = System.IO.Path.GetPathRoot(ActivePane.ActiveTab.CurrentPath);
                var d = new DriveInfo(root);
                if (d.IsReady)
                {
                    double freeGib = d.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    double usedPct = ((d.TotalSize - d.AvailableFreeSpace) / (double)d.TotalSize) * 100.0;
                    _diskSpaceBar.Value = usedPct;
                    _diskSpaceLabel.Text = string.Format("{0:0.0} GiB libre", freeGib);
                }
            }
            catch { }
        }

        // ====================================================================
        // 12. MENÚ CONTEXTUAL OSCURO CON TODAS LAS HERRAMIENTAS DE LA SUITE
        // ====================================================================
        public void ShowCustomContextMenu(FileItem targetItem)
        {
            if (_contextPopup != null) _contextPopup.IsOpen = false;

            var p = ActivePalette;
            var outerBorder = new Border
            {
                Background = VectorStudio.Brush(p.BgPopup),
                BorderBrush = VectorStudio.Brush(p.CanvasFrame),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4, 6, 4, 6),
                Width = 315,
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 4, Opacity = 0.65, Color = Colors.Black }
            };

            var stack = new StackPanel();

            if (HasActiveExternalFileDialog)
            {
                string confirmPath = (targetItem != null && targetItem.IsDirectory) ? targetItem.FullPath : ActivePane.ActiveTab.CurrentPath;
                stack.Children.Add(CreateMenuRow("✔", "Abrir esta carpeta en el Software Externo", "Ctrl+Enter", false, () =>
                {
                    ConfirmExternalFileDialogSelection(confirmPath);
                }));
                stack.Children.Add(CreateMenuSeparator());
            }

            if (targetItem != null && targetItem.IsDirectory)
            {
                stack.Children.Add(CreateMenuRow("📝", "Colocar / Editar descripción de lo que lleva adentro...", "F10", false, () =>
                {
                    OpenFolderDescriptionDialog(targetItem.FullPath);
                }));
                stack.Children.Add(CreateMenuRow("✨", "Auto-describir carpeta según su contenido", "", false, () =>
                {
                    string autoDesc = FolderDescriptionManager.GenerateSmartAutoDescription(targetItem.FullPath);
                    FolderDescriptionManager.SetDescription(targetItem.FullPath, autoDesc, ActiveStableFolderIcoPath, Config.ReplaceWindowsExplorer);
                    ActivePane.ReadEntries();
                    ActivePane.RenderItems();
                    UpdateInfoDock(ActivePane.GetCurrentCursorItem());
                    if (_statusLeftLabel != null)
                        _statusLeftLabel.Text = "✔ Descripción actualizada para '" + targetItem.DisplayName + "': " + autoDesc;
                }));
                stack.Children.Add(CreateMenuSeparator());
                stack.Children.Add(CreateMenuRow("📑", "Abrir en una pestaña nueva", "Control+T", false, () =>
                {
                    ActivePane.AddNewTab(targetItem.FullPath);
                }));
                stack.Children.Add(CreateMenuRow("◫", "Abrir en panel dividido", "F3", false, () =>
                {
                    if (!IsSplitView) ToggleSplitView();
                    RightPane.LoadDirectory(targetItem.FullPath, true);
                }));
                stack.Children.Add(CreateMenuSeparator());
            }
            else if (targetItem == null && ActivePane != null)
            {
                stack.Children.Add(CreateMenuRow("📝", "Colocar / Editar descripción de esta carpeta actual...", "F10", false, () =>
                {
                    OpenFolderDescriptionDialog(ActivePane.ActiveTab.CurrentPath);
                }));
                stack.Children.Add(CreateMenuSeparator());
            }
            else if (targetItem != null && !targetItem.IsDirectory)
            {
                stack.Children.Add(CreateMenuRow("✎", "Editar en Nexus Studio Editor", "Control+E", false, () =>
                {
                    OpenFileInIntegratedEditor(targetItem);
                }));
                stack.Children.Add(CreateMenuRow("🔐", "Calcular Checksum SHA-256", "", false, () =>
                {
                    if (!Config.ShowPreview) ToggleInfoDock();
                    ComputeSelectedFileHash();
                }));
                stack.Children.Add(CreateMenuSeparator());
            }

            stack.Children.Add(CreateMenuRow("＋", "Crear nueva carpeta...", "F7", false, () => CreateNewFolderDialog()));
            stack.Children.Add(CreateMenuRow("📄", "Crear nuevo archivo...", "Control+N", false, () => CreateNewFileDialog()));
            stack.Children.Add(CreateMenuSeparator());

            if (targetItem != null)
            {
                if (IsKeyboardDragActive && targetItem.IsDirectory && !IsPathBeingDragged(targetItem.FullPath))
                {
                    stack.Children.Add(CreateMenuRow("✔", "Soltar carpeta arrastrada aquí dentro", "Enter", false, () =>
                    {
                        CompleteKeyboardDragInto(targetItem.FullPath);
                    }));
                }
                stack.Children.Add(CreateMenuRow("🖐", "Agarrar icono para arrastrar con Teclado", "Tecla G / Shift+Flechas", false, () =>
                {
                    StartKeyboardFolderDrag(ActivePane.GetSelectedItems());
                }));
                stack.Children.Add(CreateMenuRow("📦", "Mover / Enviar a carpeta que busques...", "Ctrl+M / M", false, () => OpenMoveToFolderOverlay()));
                stack.Children.Add(CreateMenuRow("✂", "Cortar", "Control+X", false, () => CopySelectionToClipboard(true)));
                stack.Children.Add(CreateMenuRow("📋", "Copiar", "Control+C", false, () => CopySelectionToClipboard(false)));
                stack.Children.Add(CreateMenuRow("📍", "Copiar ruta completa", "Control+Alt+C", false, () =>
                {
                    Clipboard.SetText(targetItem.FullPath);
                }));
            }

            stack.Children.Add(CreateMenuRow("📥", "Pegar del portapapeles", "Control+V", false, () => PasteFromClipboard()));

            if (targetItem != null)
            {
                stack.Children.Add(CreateMenuRow("📑", "Duplicar aquí", "Control+D", false, () => DuplicateItem(targetItem)));
                stack.Children.Add(CreateMenuRow("✏", "Cambiar nombre...", "F2", false, () => RenameItemDialog(targetItem)));
                stack.Children.Add(CreateMenuRow("🖥", "Enviar al Escritorio de Windows", "", false, () => SendFolderToDesktop(targetItem)));
                stack.Children.Add(CreateMenuSeparator());
                stack.Children.Add(CreateMenuRow("🗑", "Eliminar permanentemente", "Suprimir", true, () => DeleteSelectedItems()));
            }

            stack.Children.Add(CreateMenuSeparator());
            stack.Children.Add(CreateMenuRow("📊", "Analizar espacio de esta carpeta", "F9", false, () => ToggleDiskAnalyzer()));
            stack.Children.Add(CreateMenuRow("❯_", "Abrir terminal aquí", "F4", false, () =>
            {
                if (!IsTerminalOpen) ToggleTerminalDock();
            }));
            stack.Children.Add(CreateMenuRow("⚙", "Personalizar Apariencia y Sistema", "", false, () => ToggleSettingsDrawer()));

            outerBorder.Child = stack;

            _contextPopup = new Popup
            {
                Child = outerBorder,
                Placement = PlacementMode.MousePoint,
                StaysOpen = false,
                AllowsTransparency = true
            };
            _contextPopup.IsOpen = true;
        }

        private FrameworkElement CreateMenuRow(string icon, string text, string shortcut, bool isDanger, Action onClick)
        {
            var p = ActivePalette;
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var ic = new TextBlock
            {
                Text = icon,
                FontSize = 12,
                Foreground = VectorStudio.Brush(isDanger ? "#E74C3C" : p.AccentLight),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var lbl = new TextBlock
            {
                Text = text,
                FontSize = 12.3,
                Foreground = VectorStudio.Brush(isDanger ? "#FF7675" : p.TextMain),
                VerticalAlignment = VerticalAlignment.Center
            };
            var sc = new TextBlock
            {
                Text = shortcut,
                FontSize = 11.2,
                Foreground = VectorStudio.Brush(p.TextMuted),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0)
            };

            Grid.SetColumn(ic, 0);
            Grid.SetColumn(lbl, 1);
            Grid.SetColumn(sc, 2);
            g.Children.Add(ic);
            g.Children.Add(lbl);
            g.Children.Add(sc);

            var rowBorder = new Border
            {
                Padding = new Thickness(6, 5, 8, 5),
                Margin = new Thickness(2, 1, 2, 1),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Child = g
            };

            rowBorder.MouseEnter += (s, e) => rowBorder.Background = VectorStudio.Brush(p.SelectBg);
            rowBorder.MouseLeave += (s, e) => rowBorder.Background = Brushes.Transparent;
            rowBorder.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (_contextPopup != null) _contextPopup.IsOpen = false;
                onClick();
            };

            return rowBorder;
        }

        private FrameworkElement CreateMenuSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = VectorStudio.Brush("#2E343D"),
                Margin = new Thickness(8, 4, 8, 4)
            };
        }

        // ====================================================================
        // 13. TERMINAL POWERSHELL ACOPLADA (F4)
        // ====================================================================
        private Border BuildTerminalDock()
        {
            var border = new Border
            {
                Height = 165,
                Padding = new Thickness(10, 6, 10, 6),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _termOutput = new TextBox
            {
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(_termOutput, 0);
            g.Children.Add(_termOutput);

            var inputRow = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
            _termPrompt = new TextBlock
            {
                Text = "PS> ",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            DockPanel.SetDock(_termPrompt, Dock.Left);
            inputRow.Children.Add(_termPrompt);

            _termInput = new TextBox
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12.5,
                Padding = new Thickness(6, 3, 6, 3),
                BorderThickness = new Thickness(1)
            };
            _termInput.KeyDown += async (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    string cmd = _termInput.Text.Trim();
                    _termInput.Clear();
                    e.Handled = true;
                    await RunTerminalCommandAsync(cmd);
                }
                else if (e.Key == Key.Escape || e.Key == Key.F4)
                {
                    ToggleTerminalDock();
                    e.Handled = true;
                }
            };
            inputRow.Children.Add(_termInput);

            Grid.SetRow(inputRow, 1);
            g.Children.Add(inputRow);

            border.Child = g;
            return border;
        }

        public void SyncTerminalPrompt()
        {
            if (ActivePane != null && _termPrompt != null)
                _termPrompt.Text = "PS " + ActivePane.ActiveTab.CurrentPath + "> ";
        }

        private async Task RunTerminalCommandAsync(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            string cwd = ActivePane.ActiveTab.CurrentPath;
            _termOutput.AppendText("PS " + cwd + "> " + cmd + Environment.NewLine);

            if (cmd.Equals("cls", StringComparison.OrdinalIgnoreCase) || cmd.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                _termOutput.Clear();
                return;
            }

            if (cmd.StartsWith("cd ", StringComparison.OrdinalIgnoreCase) || cmd.Equals("cd", StringComparison.OrdinalIgnoreCase))
            {
                string arg = cmd.Length > 2 ? cmd.Substring(2).Trim().Trim('"') : "";
                string next = string.IsNullOrEmpty(arg) || arg == "~"
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : System.IO.Path.GetFullPath(System.IO.Path.Combine(cwd, arg));

                if (Directory.Exists(next)) ActivePane.LoadDirectory(next, true);
                else _termOutput.AppendText("cd: no existe la ruta '" + next + "'" + Environment.NewLine);
                _termOutput.ScrollToEnd();
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"" + cmd.Replace("\"", "\\\"") + "\"")
                    {
                        WorkingDirectory = cwd,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                    using (var p = Process.Start(psi))
                    {
                        string so = p.StandardOutput.ReadToEnd();
                        string se = p.StandardError.ReadToEnd();
                        p.WaitForExit(10000);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(so)) _termOutput.AppendText(so.TrimEnd() + Environment.NewLine);
                            if (!string.IsNullOrWhiteSpace(se)) _termOutput.AppendText("[Error] " + se.TrimEnd() + Environment.NewLine);
                            _termOutput.ScrollToEnd();
                            ActivePane.ReadEntries();
                            ActivePane.RenderItems();
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(() => _termOutput.AppendText(ex.Message + Environment.NewLine)));
                }
            });
        }

        // ====================================================================
        // 14. BREADCRUMBS, VISTAS Y OPERACIONES DE ARCHIVOS
        // ====================================================================
        public void RefreshBreadcrumbs()
        {
            if (_breadcrumbsPanel == null || ActivePane == null) return;
            _breadcrumbsPanel.Children.Clear();

            string title = ActivePane.ActiveTab.Title + " — NexusFM";
            Title = title;
            if (_titleBarFolderLabel != null) _titleBarFolderLabel.Text = title;

            string full = ActivePane.ActiveTab.CurrentPath;
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var parts = new List<Tuple<string, string>>();
            if (full.StartsWith(user, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(Tuple.Create("Carpeta personal", user));
                string rel = full.Substring(user.Length).Trim('\\');
                if (!string.IsNullOrEmpty(rel))
                {
                    string acc = user;
                    foreach (var seg in rel.Split('\\'))
                    {
                        acc = System.IO.Path.Combine(acc, seg);
                        parts.Add(Tuple.Create(seg, acc));
                    }
                }
            }
            else
            {
                string[] raw = full.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string acc = "";
                for (int i = 0; i < raw.Length; i++)
                {
                    acc = (i == 0) ? raw[i] + "\\" : System.IO.Path.Combine(acc, raw[i]);
                    parts.Add(Tuple.Create(raw[i], acc));
                }
            }

            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                string target = part.Item2;

                _breadcrumbsPanel.Children.Add(new TextBlock
                {
                    Text = "›",
                    Foreground = VectorStudio.Brush(ActivePalette.TextMuted),
                    FontSize = 13,
                    Margin = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var crumbLbl = new TextBlock
                {
                    Text = part.Item1,
                    Foreground = VectorStudio.Brush(ActivePalette.TextMain),
                    FontWeight = (i == parts.Count - 1) ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = 12.5,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var crumbBorder = new Border
                {
                    Padding = new Thickness(5, 2, 5, 2),
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Transparent,
                    Cursor = Cursors.Hand,
                    Child = crumbLbl
                };
                AttachFolderDropTarget(crumbBorder, () => target, null, null);
                crumbBorder.MouseEnter += (s, e) => crumbBorder.Background = VectorStudio.Brush(ActivePalette.HoverBg);
                crumbBorder.MouseLeave += (s, e) => crumbBorder.Background = Brushes.Transparent;
                crumbBorder.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ActivePane.LoadDirectory(target, true);
                };
                _breadcrumbsPanel.Children.Add(crumbBorder);
            }
        }

        private void ActivateManualPathInput()
        {
            _breadcrumbsPanel.Visibility = Visibility.Collapsed;
            _pathInputBox.Visibility = Visibility.Visible;
            _pathInputBox.Text = ActivePane.ActiveTab.CurrentPath;
            _pathInputBox.Focus();
            _pathInputBox.SelectAll();
        }

        public void SetActivePane(NexusPaneControl pane, bool fullRebuild = true)
        {
            if (pane == RightPane && !IsSplitView) return;
            ActivePane = pane;

            if (fullRebuild)
            {
                LeftPane.RenderItems();
                if (IsSplitView) RightPane.RenderItems();
            }
            else
            {
                LeftPane.UpdateSelectionVisuals();
                if (IsSplitView) RightPane.UpdateSelectionVisuals();
            }

            RefreshBreadcrumbs();
            UpdateModelButtonsHighlight();
            UpdateInfoDock(ActivePane.GetCurrentCursorItem());
            UpdateBottomStatusBar();
            SyncTerminalPrompt();
        }

        public void SwitchActivePane()
        {
            if (!IsSplitView) return;
            var next = (ActivePane == LeftPane) ? RightPane : LeftPane;
            SetActivePane(next);
            next.Focus();
        }

        public void ToggleSplitView()
        {
            IsSplitView = !IsSplitView;
            if (IsSplitView)
            {
                _panesGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                RightPane.Visibility = Visibility.Visible;
                SetActivePane(RightPane);
                RightPane.Focus();
            }
            else
            {
                _panesGrid.ColumnDefinitions[1].Width = new GridLength(0);
                RightPane.Visibility = Visibility.Collapsed;
                SetActivePane(LeftPane);
                LeftPane.Focus();
            }

            _btnSplitToggle.Background = VectorStudio.Brush(IsSplitView ? ActivePalette.SelectBg : ActivePalette.BgChrome);
            _btnSplitToggle.BorderBrush = VectorStudio.Brush(IsSplitView ? ActivePalette.Accent : ActivePalette.Border);
        }

        public void ToggleTerminalDock()
        {
            IsTerminalOpen = !IsTerminalOpen;
            _terminalBorder.Visibility = IsTerminalOpen ? Visibility.Visible : Visibility.Collapsed;
            if (IsTerminalOpen) _termInput.Focus();
            else ActivePane.Focus();
        }

        public void ToggleInfoDock()
        {
            Config.ShowPreview = !Config.ShowPreview;
            Config.Save();
            _infoBorder.Visibility = Config.ShowPreview ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ToggleSearchBox()
        {
            if (_searchBox.Visibility == Visibility.Visible)
            {
                _searchBox.Text = "";
                _searchBox.Visibility = Visibility.Collapsed;
                ActivePane.Focus();
            }
            else
            {
                _searchBox.Visibility = Visibility.Visible;
                _searchBox.Focus();
            }
        }

        private void CopySelectionToClipboard(bool isCut)
        {
            _clipboardPaths = ActivePane.GetSelectedItems().Select(x => x.FullPath).ToList();
            _clipboardIsCut = isCut;
            if (_statusLeftLabel != null && _clipboardPaths.Count > 0)
            {
                _statusLeftLabel.Text = string.Format("{0} {1} elemento(s) — Selecciona otra carpeta y presiona Ctrl+V para {2} dentro",
                    isCut ? "✂ Cortado:" : "📋 Copiado:",
                    _clipboardPaths.Count,
                    isCut ? "mover" : "pegar");
            }
        }

        private void PasteFromClipboard()
        {
            if (_clipboardPaths == null || _clipboardPaths.Count == 0) return;
            string dstDir = ActivePane.ActiveTab.CurrentPath;

            // Si el usuario seleccionó otra carpeta en la vista actual y presiona Ctrl+V, pegar/mover DENTRO de esa carpeta seleccionada
            var cur = ActivePane.GetCurrentCursorItem();
            if (cur != null && cur.IsDirectory && !_clipboardPaths.Any(x => string.Equals(x, cur.FullPath, StringComparison.OrdinalIgnoreCase)))
            {
                bool anyFromSameDir = _clipboardPaths.Any(x =>
                    string.Equals(System.IO.Path.GetDirectoryName(x.TrimEnd('\\')), dstDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
                if (anyFromSameDir)
                {
                    dstDir = cur.FullPath;
                }
            }

            var toProcess = _clipboardPaths.ToList();
            bool isMove = _clipboardIsCut;
            if (_clipboardIsCut) _clipboardPaths.Clear();

            MoveOrCopyPathsToDestination(toProcess, dstDir, isMove);
        }

        private void DuplicateItem(FileItem item)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(item.FullPath);
                string ext = System.IO.Path.GetExtension(item.Name);
                string baseName = System.IO.Path.GetFileNameWithoutExtension(item.Name);
                string dst = System.IO.Path.Combine(dir, baseName + " (copia)" + ext);

                if (item.IsDirectory) RobustCopyDirectory(item.FullPath, dst);
                else File.Copy(item.FullPath, dst, true);

                ActivePane.ReadEntries();
                ActivePane.RenderItems();
            }
            catch { }
        }

        private void SendFolderToDesktop(FileItem item)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string dst = System.IO.Path.Combine(desktop, item.Name);
                if (item.IsDirectory) RobustCopyDirectory(item.FullPath, dst);
                else File.Copy(item.FullPath, dst, true);
                MessageBox.Show("Se envió '" + item.Name + "' a tu Escritorio de Windows.", "NexusFM", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public void CopyOrMoveBetweenPanes(bool isMove)
        {
            if (!IsSplitView)
            {
                if (isMove)
                {
                    OpenMoveToFolderOverlay();
                    return;
                }
                ToggleSplitView();
            }
            var items = ActivePane.GetSelectedItems().Select(x => x.FullPath).ToList();
            var otherPane = (ActivePane == LeftPane) ? RightPane : LeftPane;
            string dstDir = otherPane.ActiveTab.CurrentPath;
            MoveOrCopyPathsToDestination(items, dstDir, isMove);
        }

        private void CopyDirRecursive(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
                File.Copy(f, System.IO.Path.Combine(dst, System.IO.Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src))
                CopyDirRecursive(d, System.IO.Path.Combine(dst, System.IO.Path.GetFileName(d)));
        }

        public void CreateNewFolderDialog()
        {
            string name = ShowMinimalPrompt("Crear nueva carpeta (F7)", "Nombre de la carpeta:", "Nueva carpeta");
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                Directory.CreateDirectory(System.IO.Path.Combine(ActivePane.ActiveTab.CurrentPath, name));
                ActivePane.ReadEntries();
                ActivePane.RenderItems();
            }
            catch { }
        }

        public void CreateNewFileDialog()
        {
            string name = ShowMinimalPrompt("Crear nuevo archivo (Ctrl+N)", "Nombre del archivo:", "documento.md");
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                string target = System.IO.Path.Combine(ActivePane.ActiveTab.CurrentPath, name);
                if (!File.Exists(target)) File.WriteAllText(target, "", Encoding.UTF8);
                ActivePane.ReadEntries();
                ActivePane.RenderItems();
            }
            catch { }
        }

        public void RenameItemDialog(FileItem item)
        {
            if (item == null) item = ActivePane.GetCurrentCursorItem();
            if (item == null) return;
            string next = ShowMinimalPrompt("Cambiar nombre (F2)", "Nuevo nombre:", item.Name);
            if (string.IsNullOrWhiteSpace(next) || next == item.Name) return;
            try
            {
                string dst = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(item.FullPath), next);
                if (item.IsDirectory)
                {
                    var di = new DirectoryInfo(item.FullPath);
                    if ((di.Attributes & FileAttributes.ReadOnly) != 0)
                        di.Attributes &= ~FileAttributes.ReadOnly;
                    Directory.Move(item.FullPath, dst);
                    if (Config.ReplaceWindowsExplorer)
                        di.Attributes |= FileAttributes.ReadOnly;
                    FolderDescriptionManager.MigrateDescriptionPath(item.FullPath, dst);
                }
                else
                {
                    File.Move(item.FullPath, dst);
                }
                ActivePane.ReadEntries();
                ActivePane.RenderItems();
            }
            catch { }
        }

        public void OpenFolderDescriptionDialog(string explicitFolderPath)
        {
            string targetFolder = explicitFolderPath;
            if (string.IsNullOrWhiteSpace(targetFolder) && ActivePane != null)
            {
                var cur = ActivePane.GetCurrentCursorItem();
                if (cur != null && cur.IsDirectory)
                    targetFolder = cur.FullPath;
                else
                    targetFolder = ActivePane.ActiveTab.CurrentPath;
            }

            if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder)) return;

            var p = ActivePalette;
            string folderTitle = NexusPaneControl.GetFolderTitle(targetFolder);
            string currentDesc = FolderDescriptionManager.GetDescription(targetFolder);
            string autoContent = FolderDescriptionManager.GetQuickContentSummary(targetFolder);

            var win = new Window
            {
                Title = "Descripción de carpeta — " + folderTitle,
                Width = 490,
                Height = 290,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                Background = VectorStudio.Brush(p.BgPopup),
                Foreground = VectorStudio.Brush(p.TextMain),
                ResizeMode = ResizeMode.NoResize
            };

            var outer = new Border
            {
                BorderBrush = VectorStudio.Brush(p.Accent),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(20)
            };

            var stack = new StackPanel();

            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var folderArt = VectorStudio.CreateFolderGraphic(38, p, Config.FolderModel, true);
            folderArt.Margin = new Thickness(0, 0, 12, 0);
            headerRow.Children.Add(folderArt);

            var titleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleCol.Children.Add(new TextBlock
            {
                Text = "📝 Descripción de la carpeta: " + folderTitle,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = VectorStudio.Brush(p.AccentLight)
            });
            titleCol.Children.Add(new TextBlock
            {
                Text = "Contenido detectado adentro: " + autoContent,
                FontSize = 11.3,
                Foreground = VectorStudio.Brush(p.TextMuted),
                Margin = new Thickness(0, 2, 0, 0)
            });
            headerRow.Children.Add(titleCol);
            stack.Children.Add(headerRow);

            stack.Children.Add(new TextBlock
            {
                Text = "Escribe qué lleva esta carpeta adentro (se mostrará en las tarjetas, lista, barra superior y al pasar el mouse):",
                FontSize = 11.8,
                Foreground = VectorStudio.Brush(p.TextMuted),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var tb = new TextBox
            {
                Text = currentDesc,
                Height = 74,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = false,
                Padding = new Thickness(9, 7, 9, 7),
                FontSize = 12.8,
                Background = VectorStudio.Brush(p.BgCanvas),
                Foreground = VectorStudio.Brush(p.TextMain),
                BorderBrush = VectorStudio.Brush(p.Accent),
                BorderThickness = new Thickness(1.2)
            };
            stack.Children.Add(tb);

            var toolsRow = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };

            var leftBtns = new StackPanel { Orientation = Orientation.Horizontal };
            var btnAuto = CreatePillButton("✨ Auto-describir contenido", "Generar automáticamente una descripción con lo que lleva la carpeta adentro", () =>
            {
                tb.Text = FolderDescriptionManager.GenerateSmartAutoDescription(targetFolder);
                tb.Focus();
                tb.SelectAll();
            });
            btnAuto.Background = VectorStudio.Brush("#183028");
            btnAuto.BorderBrush = VectorStudio.Brush("#2ECC71");
            ((TextBlock)btnAuto.Child).Foreground = VectorStudio.Brush("#A3F7BF");

            var btnClear = CreatePillButton("🗑 Borrar", "Quitar la descripción personalizada", () =>
            {
                tb.Text = "";
                tb.Focus();
            });
            btnClear.Background = VectorStudio.Brush("#321B20");
            btnClear.BorderBrush = VectorStudio.Brush("#E74C3C");
            ((TextBlock)btnClear.Child).Foreground = VectorStudio.Brush("#FF7675");

            leftBtns.Children.Add(btnAuto);
            leftBtns.Children.Add(btnClear);
            DockPanel.SetDock(leftBtns, Dock.Left);
            toolsRow.Children.Add(leftBtns);

            bool confirmed = false;
            string finalVal = currentDesc;

            var rightBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = CreatePillButton("💾 Guardar (Enter)", "Guardar la descripción de la carpeta", () =>
            {
                confirmed = true;
                finalVal = tb.Text;
                win.Close();
            });
            ok.Background = VectorStudio.Brush(p.SelectBg);
            ok.BorderBrush = VectorStudio.Brush(p.Accent);
            ((TextBlock)ok.Child).FontWeight = FontWeights.Bold;
            ((TextBlock)ok.Child).Foreground = VectorStudio.Brush(p.TextMain);

            var cancel = CreatePillButton("Cancelar", "Cerrar sin cambios", () => win.Close());
            cancel.Background = VectorStudio.Brush(p.BgChrome);
            cancel.BorderBrush = VectorStudio.Brush(p.Border);
            ((TextBlock)cancel.Child).Foreground = VectorStudio.Brush(p.TextMuted);

            rightBtns.Children.Add(ok);
            rightBtns.Children.Add(cancel);
            DockPanel.SetDock(rightBtns, Dock.Right);
            toolsRow.Children.Add(rightBtns);

            stack.Children.Add(toolsRow);

            tb.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    confirmed = true;
                    finalVal = tb.Text;
                    win.Close();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    win.Close();
                    e.Handled = true;
                }
            };

            outer.Child = stack;
            win.Content = outer;
            win.Loaded += (s, e) => { tb.Focus(); tb.SelectAll(); };
            win.ShowDialog();

            if (confirmed)
            {
                FolderDescriptionManager.SetDescription(targetFolder, finalVal, ActiveStableFolderIcoPath, Config.ReplaceWindowsExplorer);
                if (LeftPane != null) { LeftPane.ReadEntries(); LeftPane.RenderItems(); }
                if (IsSplitView && RightPane != null) { RightPane.ReadEntries(); RightPane.RenderItems(); }
                if (ActivePane != null) UpdateInfoDock(ActivePane.GetCurrentCursorItem());
                if (_statusLeftLabel != null)
                {
                    _statusLeftLabel.Text = string.IsNullOrWhiteSpace(finalVal)
                        ? "✔ Se quitó la descripción personalizada de '" + folderTitle + "'."
                        : "✔ Descripción guardada en '" + folderTitle + "': " + finalVal.Trim();
                }
            }
        }

        public void DeleteSelectedItems()
        {
            var items = ActivePane.GetSelectedItems();
            if (items.Count == 0) return;

            var res = MessageBox.Show(
                string.Format("¿Eliminar {0} elemento(s)?\n\n{1}", items.Count, string.Join("\n", items.Take(6).Select(x => "• " + x.Name))),
                "Confirmar borrado — NexusFM",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) return;

            foreach (var item in items)
            {
                try
                {
                    if (item.IsDirectory) Directory.Delete(item.FullPath, true);
                    else File.Delete(item.FullPath);
                }
                catch { }
            }
            ActivePane.ReadEntries();
            ActivePane.RenderItems();
            UpdateBottomStatusBar();
        }

        private void OnGlobalPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (IsKeyboardDragActive) { CancelKeyboardFolderDrag(); e.Handled = true; return; }
                if (_moveToFolderOverlay.Visibility == Visibility.Visible) { _moveToFolderOverlay.Visibility = Visibility.Collapsed; e.Handled = true; return; }
                if (_analyzerOverlay.Visibility == Visibility.Visible) { _analyzerOverlay.Visibility = Visibility.Collapsed; e.Handled = true; return; }
                if (_editorOverlay.Visibility == Visibility.Visible) { _editorOverlay.Visibility = Visibility.Collapsed; e.Handled = true; return; }
                if (_settingsDrawer.Visibility == Visibility.Visible) { _settingsDrawer.Visibility = Visibility.Collapsed; e.Handled = true; return; }
            }

            if (e.Key == Key.F2) { RenameItemDialog(null); e.Handled = true; }
            else if (e.Key == Key.F3) { ToggleSplitView(); e.Handled = true; }
            else if (e.Key == Key.F4) { ToggleTerminalDock(); e.Handled = true; }
            else if (e.Key == Key.F5) { CopyOrMoveBetweenPanes(false); e.Handled = true; }
            else if (e.Key == Key.F6) { CopyOrMoveBetweenPanes(true); e.Handled = true; }
            else if (e.Key == Key.F7) { CreateNewFolderDialog(); e.Handled = true; }
            else if (e.Key == Key.F8 || (e.Key == Key.Delete && !(Keyboard.FocusedElement is TextBox))) { DeleteSelectedItems(); e.Handled = true; }
            else if (e.Key == Key.F9) { ToggleDiskAnalyzer(); e.Handled = true; }
            else if (e.Key == Key.F10 || e.SystemKey == Key.F10) { OpenFolderDescriptionDialog(null); e.Handled = true; }
            else if (e.Key == Key.F11) { ToggleInfoDock(); e.Handled = true; }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.T) { ActivePane.AddNewTab(ActivePane.ActiveTab.CurrentPath); e.Handled = true; }
                else if (e.Key == Key.W) { ActivePane.CloseCurrentTab(); e.Handled = true; }
                else if (e.Key == Key.L) { ActivateManualPathInput(); e.Handled = true; }
                else if (e.Key == Key.F) { ToggleSearchBox(); e.Handled = true; }
                else if (e.Key == Key.M) { OpenMoveToFolderOverlay(); e.Handled = true; }
                else if (e.Key == Key.Enter && HasActiveExternalFileDialog) { ConfirmExternalFileDialogSelection(null); e.Handled = true; }
                else if (e.Key == Key.N) { CreateNewFileDialog(); e.Handled = true; }
                else if (e.Key == Key.E)
                {
                    var cur = ActivePane.GetCurrentCursorItem();
                    if (cur != null && !cur.IsDirectory) OpenFileInIntegratedEditor(cur);
                    e.Handled = true;
                }
                else if (e.Key == Key.OemComma) { ToggleSettingsDrawer(); e.Handled = true; }
                else if (e.Key == Key.C && !(Keyboard.FocusedElement is TextBox)) { CopySelectionToClipboard(false); e.Handled = true; }
                else if (e.Key == Key.X && !(Keyboard.FocusedElement is TextBox)) { CopySelectionToClipboard(true); e.Handled = true; }
                else if (e.Key == Key.V && !(Keyboard.FocusedElement is TextBox)) { PasteFromClipboard(); e.Handled = true; }
                else if (e.Key == Key.D && !(Keyboard.FocusedElement is TextBox))
                {
                    var cur = ActivePane.GetCurrentCursorItem();
                    if (cur != null) DuplicateItem(cur);
                    e.Handled = true;
                }
            }
        }

        public void ApplyCurrentThemeAndConfig()
        {
            BasePalette = Themes.FirstOrDefault(t => t.Name == Config.ThemeName) ?? Themes[0];
            ActivePalette = BasePalette.CloneWithAccent(Config.CustomAccentHex);
            var p = ActivePalette;

            Background = VectorStudio.Brush(p.BgChrome);
            Foreground = VectorStudio.Brush(p.TextMain);

            _outerWindowBorder.BorderBrush = VectorStudio.Brush(p.Border);
            _customTitleBar.Background = VectorStudio.Brush(p.BgTitlebar);
            _customTitleBar.BorderBrush = VectorStudio.Brush(p.Border);
            _titleBarFolderLabel.Foreground = VectorStudio.Brush(p.TextMuted);

            _toolbarBorder.Background = VectorStudio.Brush(p.BgChrome);
            _toolbarBorder.BorderBrush = VectorStudio.Brush(p.Border);

            _pathInputBox.Background = VectorStudio.Brush(p.BgCanvas);
            _pathInputBox.Foreground = VectorStudio.Brush(p.TextMain);
            _pathInputBox.BorderBrush = VectorStudio.Brush(p.Accent);
            _searchBox.Background = VectorStudio.Brush(p.BgCanvas);
            _searchBox.Foreground = VectorStudio.Brush(p.TextMain);
            _searchBox.BorderBrush = VectorStudio.Brush(p.Accent);

            if (_btnDescribeFolderToggle != null)
            {
                _btnDescribeFolderToggle.Background = VectorStudio.Brush(p.BgChrome);
                _btnDescribeFolderToggle.BorderBrush = VectorStudio.Brush(p.Border);
            }

            if (_btnMoveToFolderToggle != null)
            {
                _btnMoveToFolderToggle.Background = VectorStudio.Brush(p.BgChrome);
                _btnMoveToFolderToggle.BorderBrush = VectorStudio.Brush(p.Border);
            }

            _btnAnalyzerToggle.Background = VectorStudio.Brush(p.BgChrome);
            _btnAnalyzerToggle.BorderBrush = VectorStudio.Brush(p.Border);

            _btnSplitToggle.Background = VectorStudio.Brush(IsSplitView ? p.SelectBg : p.BgChrome);
            _btnSplitToggle.BorderBrush = VectorStudio.Brush(IsSplitView ? p.Accent : p.Border);

            _btnSettingsToggle.Background = VectorStudio.Brush(p.SelectBg);
            _btnSettingsToggle.BorderBrush = VectorStudio.Brush(p.Accent);

            _sidebarBorder.Background = VectorStudio.Brush(p.BgChrome);
            _sidebarBorder.BorderBrush = VectorStudio.Brush(p.Border);
            _sidebarBorder.Visibility = Config.ShowSidebar ? Visibility.Visible : Visibility.Collapsed;

            _infoBorder.Background = VectorStudio.Brush(p.BgChrome);
            _infoBorder.BorderBrush = VectorStudio.Brush(p.Border);
            _infoBorder.Visibility = Config.ShowPreview ? Visibility.Visible : Visibility.Collapsed;
            _infoTitle.Foreground = VectorStudio.Brush(p.TextMain);
            _infoTextPreview.Background = VectorStudio.Brush(p.BgCanvas);
            _infoTextPreview.Foreground = VectorStudio.Brush(p.TextMain);
            _infoTextPreview.BorderBrush = VectorStudio.Brush(p.Border);

            _settingsDrawer.Background = VectorStudio.Brush(p.BgChrome);
            _settingsDrawer.BorderBrush = VectorStudio.Brush(p.Border);

            _terminalBorder.Background = VectorStudio.Brush(p.BgChrome);
            _terminalBorder.BorderBrush = VectorStudio.Brush(p.Accent);
            _termOutput.Background = VectorStudio.Brush(p.BgCanvas);
            _termOutput.Foreground = VectorStudio.Brush(p.TextMain);
            _termPrompt.Foreground = VectorStudio.Brush(p.AccentLight);
            _termInput.Background = VectorStudio.Brush(p.BgCanvas);
            _termInput.Foreground = VectorStudio.Brush(p.TextMain);
            _termInput.BorderBrush = VectorStudio.Brush(p.Border);

            _statusBorder.Background = VectorStudio.Brush(p.BgChrome);
            _statusBorder.BorderBrush = VectorStudio.Brush(p.Border);
            _statusLeftLabel.Foreground = VectorStudio.Brush(p.TextMuted);
            _diskSpaceLabel.Foreground = VectorStudio.Brush(p.TextMuted);
            _diskSpaceBar.Foreground = VectorStudio.Brush(p.Accent);

            UpdateModelButtonsHighlight();

            if (Config.ReplaceWindowsExplorer)
            {
                ApplyWindowsExplorerRegistry(true);
            }

            if (LeftPane != null) { LeftPane.RenderTabsBar(); LeftPane.RenderItems(); }
            if (RightPane != null) { RightPane.RenderTabsBar(); RightPane.RenderItems(); }
            if (ActivePane != null)
            {
                RefreshBreadcrumbs();
                UpdateInfoDock(ActivePane.GetCurrentCursorItem());
            }
        }

        private string ShowMinimalPrompt(string title, string label, string defaultVal)
        {
            var p = ActivePalette;
            var win = new Window
            {
                Title = title,
                Width = 380,
                Height = 165,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                Background = VectorStudio.Brush(p.BgPopup),
                Foreground = VectorStudio.Brush(p.TextMain),
                ResizeMode = ResizeMode.NoResize
            };

            var outer = new Border
            {
                BorderBrush = VectorStudio.Brush(p.Accent),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, FontSize = 13.5, Foreground = VectorStudio.Brush(p.AccentLight), Margin = new Thickness(0, 0, 0, 8) });
            stack.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = VectorStudio.Brush(p.TextMuted), Margin = new Thickness(0, 0, 0, 8) });

            var tb = new TextBox
            {
                Text = defaultVal,
                Padding = new Thickness(7, 5, 7, 5),
                Background = VectorStudio.Brush(p.BgCanvas),
                Foreground = VectorStudio.Brush(p.TextMain),
                BorderBrush = VectorStudio.Brush(p.Accent)
            };
            stack.Children.Add(tb);

            string res = null;
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var ok = CreatePillButton("Aceptar", "Confirmar", () => { res = tb.Text; win.Close(); });
            ok.Background = VectorStudio.Brush(p.SelectBg);
            ok.BorderBrush = VectorStudio.Brush(p.Accent);
            ((TextBlock)ok.Child).Foreground = VectorStudio.Brush(p.TextMain);

            var cancel = CreatePillButton("Cancelar", "Cerrar", () => win.Close());
            cancel.Background = VectorStudio.Brush(p.BgChrome);
            cancel.BorderBrush = VectorStudio.Brush(p.Border);
            ((TextBlock)cancel.Child).Foreground = VectorStudio.Brush(p.TextMuted);

            btnRow.Children.Add(ok);
            btnRow.Children.Add(cancel);
            stack.Children.Add(btnRow);

            tb.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) { res = tb.Text; win.Close(); }
                else if (e.Key == Key.Escape) win.Close();
            };

            outer.Child = stack;
            win.Content = outer;
            win.Loaded += (s, e) => { tb.Focus(); tb.SelectAll(); };
            win.ShowDialog();
            return res;
        }
    }

    public static class Program
    {
        private static string ParseStartupArgument(string[] args)
        {
            if (args == null || args.Length == 0) return null;
            string joined = string.Join(" ", args).Trim();
            foreach (var raw in args)
            {
                string a = raw.Trim().Trim('"');
                if (a.StartsWith("/select,", StringComparison.OrdinalIgnoreCase) ||
                    a.StartsWith("-select,", StringComparison.OrdinalIgnoreCase))
                {
                    a = a.Substring(8).Trim().Trim('"');
                }
                if (File.Exists(a)) return System.IO.Path.GetDirectoryName(a);
                if (Directory.Exists(a)) return a;
            }
            if (joined.StartsWith("/select,", StringComparison.OrdinalIgnoreCase))
                joined = joined.Substring(8).Trim().Trim('"');
            if (File.Exists(joined)) return System.IO.Path.GetDirectoryName(joined);
            if (Directory.Exists(joined)) return joined;
            return null;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            bool isSilentStartup = args != null && args.Any(a =>
                string.Equals(a, "--startup", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a, "/startup", StringComparison.OrdinalIgnoreCase));

            bool isRefreshIconsOnly = args != null && args.Any(a =>
                string.Equals(a, "--refresh-icons", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a, "/refresh-icons", StringComparison.OrdinalIgnoreCase));

            if (isRefreshIconsOnly)
            {
                try
                {
                    var cfg = UserConfig.Load();
                    string exeDir = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    string assetsDir = System.IO.Path.Combine(exeDir, "assets");
                    string accent = !string.IsNullOrEmpty(cfg.CustomAccentHex) ? cfg.CustomAccentHex : "#00F0FF";
                    string icoPath = IconExtractor.GenerateMultiSizeFolderIco(assetsDir, accent);
                    IconExtractor.PrecompileAndRepairDesktopAppIcons(icoPath, cfg.ReplaceWindowsExplorer, cfg.RecentLocations);
                }
                catch { }
                return;
            }

            string startupPath = ParseStartupArgument(args);

            // Si ya hay una instancia de NexusFM abierta, enviarle la ruta para no duplicar ventanas
            try
            {
                var current = Process.GetCurrentProcess();
                var existing = Process.GetProcessesByName(current.ProcessName).FirstOrDefault(p => p.Id != current.Id);
                if (existing != null)
                {
                    if (!isSilentStartup)
                    {
                        string exeDir = System.IO.Path.GetDirectoryName(current.MainModule.FileName);
                        string ipcFile = System.IO.Path.Combine(exeDir, "nexus-ipc-open.txt");
                        File.WriteAllText(ipcFile, startupPath ?? "", Encoding.UTF8);
                    }
                    return;
                }
            }
            catch { }

            var app = new Application();
            app.Run(new MainWindow(startupPath, isSilentStartup));
        }
    }
}

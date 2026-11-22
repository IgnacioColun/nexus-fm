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
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NexusFM
{
    // ========================================================================
    // 1. INTERCEPTOR NATIVO WIN32 Y REEMPLAZO REAL DE WINDOWS EXPLORER
    // ========================================================================
    public static class NativeWin32
    {
        public const int SHCNE_ASSOCCHANGED = 0x08000000;
        public const uint SHCNF_IDLIST = 0x0000;
        public const uint WM_CLOSE = 0x0010;
        public const int SW_RESTORE = 9;

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void RefreshShell()
        {
            try { SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero); }
            catch { }
        }
    }

    // ========================================================================
    // 2. CONFIGURACIÓN PERSISTENTE DEL USUARIO
    // ========================================================================
    public class UserConfig
    {
        public bool ReplaceWindowsExplorer { get; set; }
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
                sb.AppendLine("ThemeName=" + ThemeName);
                sb.AppendLine("CustomAccentHex=" + CustomAccentHex);
                sb.AppendLine("FolderModel=" + FolderModel);
                sb.AppendLine("IconSize=" + (int)IconSize);
                sb.AppendLine("ShowSidebar=" + ShowSidebar);
                sb.AppendLine("ShowPreview=" + ShowPreview);
                sb.AppendLine("Recent=" + string.Join("|", RecentLocations));
                File.WriteAllText(GetConfigPath(), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static UserConfig Load()
        {
            var cfg = new UserConfig();
            try
            {
                string p = GetConfigPath();
                if (!File.Exists(p)) return cfg;
                foreach (var line in File.ReadAllLines(p, Encoding.UTF8))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    if (k == "ReplaceWindowsExplorer") cfg.ReplaceWindowsExplorer = bool.Parse(v);
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
    // 3. MOTOR GRÁFICO VECTORIAL (GEOMETRÍA PLANA BREEZE + ICONOS SVG PUROS)
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

        // Dibuja la carpeta geométrica plana idéntica a la captura 2 (con pestaña angulada de dos tonos)
        public static FrameworkElement CreateFolderGraphic(double size, ThemePalette p, string model)
        {
            double w = size;
            double h = size * 0.78;
            var canvas = new Canvas { Width = w, Height = h };

            bool isNeon = string.Equals(model, "Neon", StringComparison.OrdinalIgnoreCase);

            // 1. Parte trasera con pestaña angulada (exacta a la carpeta azul de la imagen 2)
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

            // 2. Línea de sombra interior sutil entre pestaña y bolsillo frontal
            var pocketLine = new Rectangle
            {
                Width = w * 0.88,
                Height = 1.6,
                Fill = new SolidColorBrush(Color.FromArgb(95, 0, 0, 0))
            };
            Canvas.SetLeft(pocketLine, w * 0.06);
            Canvas.SetTop(pocketLine, h * 0.27);
            canvas.Children.Add(pocketLine);

            // 3. Cuerpo frontal plano y limpio (Breeze Flat Geometry)
            var frontRect = new Rectangle
            {
                Width = w * 0.88,
                Height = h * 0.64,
                RadiusX = 2.5,
                RadiusY = 2.5,
                Fill = isNeon ? Brush("#171D2B") : Brush(p.Accent),
                Stroke = isNeon ? Brush(p.AccentLight) : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                StrokeThickness = isNeon ? 1.5 : 0.7
            };
            Canvas.SetLeft(frontRect, w * 0.06);
            Canvas.SetTop(frontRect, h * 0.29);
            canvas.Children.Add(frontRect);

            // 4. Borde inferior sutil de profundidad
            var bottomLip = new Rectangle
            {
                Width = w * 0.88,
                Height = 2.0,
                Fill = Brush(p.AccentDark),
                Opacity = 0.75
            };
            Canvas.SetLeft(bottomLip, w * 0.06);
            Canvas.SetTop(bottomLip, h * 0.91);
            canvas.Children.Add(bottomLip);

            return canvas;
        }

        public static FrameworkElement CreateFileGraphic(double size, ThemePalette p, string ext)
        {
            double w = size * 0.68;
            double h = size * 0.78;
            var grid = new Grid { Width = size, Height = size * 0.78 };

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
        public HashSet<string> SelectedPaths { get; private set; }
        public int CursorIndex { get; set; }

        private MainWindow _owner;
        private StackPanel _tabsStack;
        private Border _canvasFrameBorder;
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
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Banner de filtro por categoría
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Cabecera modo detalles
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Archivos

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
            Grid.SetRow(_categoryBanner, 0);
            innerGrid.Children.Add(_categoryBanner);

            _detailsHeader = BuildDetailsHeader();
            Grid.SetRow(_detailsHeader, 1);
            innerGrid.Children.Add(_detailsHeader);

            _canvasScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(18, 14, 18, 14)
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
            Grid.SetRow(_canvasScroll, 2);
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
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });

            g.Children.Add(CreateHeaderLabel("Nombre", 0));
            g.Children.Add(CreateHeaderLabel("Tipo", 1));
            g.Children.Add(CreateHeaderLabel("Tamaño", 2));
            g.Children.Add(CreateHeaderLabel("Modificado", 3));

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

            string trimmed = path.TrimEnd('\\');
            if (trimmed.EndsWith(":")) return "Disco (" + trimmed + ")";
            string name = System.IO.Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(name) ? path : name;
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
                            if (!string.IsNullOrEmpty(filter) && !d.Name.ToLowerInvariant().Contains(filter))
                                continue;

                            CurrentItems.Add(new FileItem
                            {
                                Name = d.Name,
                                FullPath = d.FullName,
                                IsDirectory = true,
                                Extension = "Carpeta",
                                RawSize = 0,
                                SizeText = "Carpeta",
                                ModifiedDate = d.LastWriteTime,
                                CreatedDate = d.CreationTime,
                                AccessedDate = d.LastAccessTime,
                                ModifiedText = FormatRelativeTime(d.LastWriteTime),
                                Category = "DIR"
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
                            ModifiedText = FormatRelativeTime(f.LastWriteTime),
                            Category = cat
                        });
                    }
                    catch { }
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

        public void RenderItems()
        {
            var p = _owner.ActivePalette;
            string model = _owner.Config.FolderModel;
            double zoom = _owner.Config.IconSize;

            _canvasFrameBorder.Background = VectorStudio.Brush(p.BgCanvas);
            _canvasFrameBorder.BorderBrush = VectorStudio.Brush(_owner.ActivePane == this ? p.CanvasFrame : p.Border);
            _detailsHeader.Background = VectorStudio.Brush(p.BgChrome);
            _detailsHeader.BorderBrush = VectorStudio.Brush(p.Border);

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

                    var g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });

                    var nameStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    var miniIcon = item.IsDirectory
                        ? VectorStudio.CreateFolderGraphic(22, p, "Breeze")
                        : VectorStudio.CreateFileGraphic(22, p, item.Extension);
                    miniIcon.Margin = new Thickness(0, 0, 10, 0);
                    nameStack.Children.Add(miniIcon);
                    nameStack.Children.Add(new TextBlock
                    {
                        Text = item.Name,
                        Foreground = VectorStudio.Brush(p.TextMain),
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12.5
                    });
                    Grid.SetColumn(nameStack, 0);
                    g.Children.Add(nameStack);

                    var typeTb = new TextBlock { Text = item.Extension, Foreground = VectorStudio.Brush(p.TextMuted), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
                    Grid.SetColumn(typeTb, 1);
                    g.Children.Add(typeTb);

                    var sizeTb = new TextBlock { Text = item.SizeText, Foreground = VectorStudio.Brush(p.TextMuted), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
                    Grid.SetColumn(sizeTb, 2);
                    g.Children.Add(sizeTb);

                    var modTb = new TextBlock { Text = item.ModifiedText, Foreground = VectorStudio.Brush(p.TextMuted), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
                    Grid.SetColumn(modTb, 3);
                    g.Children.Add(modTb);

                    rowBorder.Child = g;
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

                    var card = new Border
                    {
                        Width = 230,
                        Height = 64,
                        Margin = new Thickness(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        CornerRadius = new CornerRadius(6),
                        Cursor = Cursors.Hand,
                        Background = isSelected ? VectorStudio.Brush(p.SelectBg) : VectorStudio.Brush(p.BgChrome),
                        BorderBrush = (isSelected || isCursor) ? VectorStudio.Brush(p.Accent) : VectorStudio.Brush(p.Border),
                        BorderThickness = new Thickness(isSelected || isCursor ? 1.4 : 1)
                    };

                    var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    var icon = item.IsDirectory
                        ? VectorStudio.CreateFolderGraphic(42, p, "Breeze")
                        : VectorStudio.CreateFileGraphic(42, p, item.Extension);
                    icon.Margin = new Thickness(0, 0, 12, 0);
                    row.Children.Add(icon);

                    var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    info.Children.Add(new TextBlock
                    {
                        Text = item.Name,
                        Foreground = VectorStudio.Brush(p.TextMain),
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12.5,
                        MaxWidth = 148,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    info.Children.Add(new TextBlock
                    {
                        Text = item.IsDirectory ? item.ModifiedText : item.SizeText + " • " + item.Extension,
                        Foreground = VectorStudio.Brush(p.TextMuted),
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                    row.Children.Add(info);

                    card.Child = row;
                    AttachTileEvents(card, idx, item);
                    wrap.Children.Add(card);
                }
                _itemsPanel = wrap;
                _canvasScroll.Content = wrap;
            }
            else
            {
                // Vista de carpetas geométricas planas Breeze (idéntica a la imagen 2)
                var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
                double tileW = zoom + 44;
                double tileH = zoom + 38;

                for (int i = 0; i < CurrentItems.Count; i++)
                {
                    int idx = i;
                    var item = CurrentItems[i];
                    bool isSelected = SelectedPaths.Contains(item.FullPath);
                    bool isCursor = (i == CursorIndex);

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

                    var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                    var art = item.IsDirectory
                        ? VectorStudio.CreateFolderGraphic(zoom, p, model)
                        : VectorStudio.CreateFileGraphic(zoom, p, item.Extension);
                    stack.Children.Add(art);

                    var lbl = new TextBlock
                    {
                        Text = item.Name,
                        Foreground = VectorStudio.Brush(p.TextMain),
                        FontSize = 12.2,
                        TextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = tileW - 10,
                        Margin = new Thickness(0, 5, 0, 0)
                    };
                    stack.Children.Add(lbl);

                    tile.Child = stack;
                    AttachTileEvents(tile, idx, item);
                    wrap.Children.Add(tile);
                }

                _itemsPanel = wrap;
                _canvasScroll.Content = wrap;
            }
        }

        private void AttachTileEvents(Border tile, int idx, FileItem item)
        {
            var p = _owner.ActivePalette;
            bool isCards = string.Equals(_owner.Config.FolderModel, "Cards", StringComparison.OrdinalIgnoreCase);

            tile.MouseEnter += (s, e) =>
            {
                if (!SelectedPaths.Contains(item.FullPath))
                    tile.Background = VectorStudio.Brush(p.HoverBg);
            };

            tile.MouseLeave += (s, e) =>
            {
                if (!SelectedPaths.Contains(item.FullPath))
                    tile.Background = isCards ? VectorStudio.Brush(p.BgChrome) : Brushes.Transparent;
            };

            tile.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                _owner.SetActivePane(this);
                Focus();
                CursorIndex = idx;

                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (SelectedPaths.Contains(item.FullPath)) SelectedPaths.Remove(item.FullPath);
                    else SelectedPaths.Add(item.FullPath);
                }
                else
                {
                    SelectedPaths.Clear();
                    SelectedPaths.Add(item.FullPath);
                }

                RenderItems();
                _owner.UpdateInfoDock(item);
                _owner.UpdateBottomStatusBar();

                if (e.ClickCount == 2)
                {
                    OpenCursorItem();
                }
            };

            tile.MouseRightButtonUp += (s, e) =>
            {
                e.Handled = true;
                _owner.SetActivePane(this);
                Focus();
                CursorIndex = idx;
                if (!SelectedPaths.Contains(item.FullPath))
                {
                    SelectedPaths.Clear();
                    SelectedPaths.Add(item.FullPath);
                }
                RenderItems();
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
                double tileW = isCards ? 242 : (_owner.Config.IconSize + 60);
                cols = Math.Max(1, (int)((_canvasScroll.ActualWidth - 36) / tileW));
            }

            if (e.Key == Key.Right || (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.None && !isDetails))
            {
                MoveCursorBy(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Left || (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.None && !isDetails))
            {
                MoveCursorBy(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Down || (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.None))
            {
                MoveCursorBy(cols);
                e.Handled = true;
            }
            else if (e.Key == Key.Up || (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.None))
            {
                MoveCursorBy(-cols);
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
                OpenCursorItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                GoUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                var item = GetCurrentCursorItem();
                if (item != null)
                {
                    if (SelectedPaths.Contains(item.FullPath)) SelectedPaths.Remove(item.FullPath);
                    else SelectedPaths.Add(item.FullPath);
                    RenderItems();
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
            RenderItems();

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

        public MainWindow(string startupPath)
        {
            InjectModernDarkScrollBarStyle();

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
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Centro + Drawers integrados
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Terminal (F4)
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Barra estado inferior

            _customTitleBar = BuildCustomTitleBar();
            Grid.SetRow(_customTitleBar, 0);
            rootGrid.Children.Add(_customTitleBar);

            _toolbarBorder = BuildToolbar();
            Grid.SetRow(_toolbarBorder, 1);
            rootGrid.Children.Add(_toolbarBorder);

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

            // Overlays integrados dentro del área central (Analizador de Espacio y Editor de Código)
            _analyzerOverlay = BuildDiskSpaceAnalyzerOverlay();
            _editorOverlay = BuildIntegratedCodeEditorOverlay();
            _panesGrid.Children.Add(_analyzerOverlay);
            _panesGrid.Children.Add(_editorOverlay);
            Grid.SetColumnSpan(_analyzerOverlay, 2);
            Grid.SetColumnSpan(_editorOverlay, 2);

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
            Grid.SetRow(centerContainer, 2);
            rootGrid.Children.Add(centerContainer);

            _terminalBorder = BuildTerminalDock();
            Grid.SetRow(_terminalBorder, 3);
            rootGrid.Children.Add(_terminalBorder);

            _statusBorder = BuildStatusBar();
            Grid.SetRow(_statusBorder, 4);
            rootGrid.Children.Add(_statusBorder);

            _outerWindowBorder = new Border
            {
                BorderThickness = new Thickness(1),
                Child = rootGrid
            };
            Content = _outerWindowBorder;

            PreviewKeyDown += OnGlobalPreviewKeyDown;
            ApplyCurrentThemeAndConfig();
            SetActivePane(LeftPane);

            StartExplorerInterceptorTimer();
            if (Config.ReplaceWindowsExplorer)
            {
                ApplyWindowsExplorerRegistry(true);
            }

            Loaded += (s, e) => LeftPane.Focus();
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
        // INTERCEPTOR EN TIEMPO REAL DEL EXPLORADOR DE WINDOWS
        // ====================================================================
        private void StartExplorerInterceptorTimer()
        {
            _explorerInterceptorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(220)
            };
            _explorerInterceptorTimer.Tick += (s, e) =>
            {
                if (!Config.ReplaceWindowsExplorer) return;

                IntPtr explorerHwnd = NativeWin32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "CabinetWClass", null);
                if (explorerHwnd == IntPtr.Zero) return;

                string targetPath = TryInterceptOpenWindowsExplorer();

                IntPtr remaining = NativeWin32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "CabinetWClass", null);
                if (remaining != IntPtr.Zero)
                {
                    NativeWin32.PostMessage(remaining, NativeWin32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }

                if (!IsVisible) Show();
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                Activate();
                IntPtr myHwnd = new WindowInteropHelper(this).Handle;
                if (myHwnd != IntPtr.Zero)
                {
                    NativeWin32.ShowWindow(myHwnd, NativeWin32.SW_RESTORE);
                    NativeWin32.SetForegroundWindow(myHwnd);
                }

                if (!string.IsNullOrEmpty(targetPath) && Directory.Exists(targetPath) && ActivePane != null)
                {
                    ActivePane.LoadDirectory(targetPath, true);
                }
            };
            _explorerInterceptorTimer.Start();
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

        public void ApplyWindowsExplorerRegistry(bool enable)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                string icoPath = System.IO.Path.Combine(exeDir, "assets", "nexus-folder.ico");
                if (!File.Exists(icoPath)) icoPath = exePath;

                if (enable)
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell"))
                        key.SetValue("", "openin_nexusfm");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\openin_nexusfm"))
                    {
                        key.SetValue("", "Abrir en NexusFM");
                        key.SetValue("Icon", "\"" + icoPath + "\",0");
                    }

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\openin_nexusfm\command"))
                        key.SetValue("", "\"" + exePath + "\" \"%1\"");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell"))
                        key.SetValue("", "openin_nexusfm");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell\openin_nexusfm\command"))
                        key.SetValue("", "\"" + exePath + "\" \"%1\"");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\DefaultIcon"))
                        key.SetValue("", "\"" + icoPath + "\",0");
                }
                else
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell"))
                        key.SetValue("", "none");

                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Drive\shell"))
                        key.SetValue("", "none");

                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\openin_nexusfm", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Drive\shell\openin_nexusfm", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\DefaultIcon", false);
                }

                NativeWin32.RefreshShell();
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
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_HOME, "Carpeta personal", () => ActivePane.LoadDirectory(home, true)));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_DESKTOP, "Escritorio", () => ActivePane.LoadDirectory(desktop, true)));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_FOLDER, "Descargas", () => ActivePane.LoadDirectory(downloads, true)));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_DOC, "Documentos", () => ActivePane.LoadDirectory(docs, true)));
            stack.Children.Add(CreateSidebarVectorRow(VectorStudio.SVG_CODE, "Proyecto NexusFM", () => ActivePane.LoadDirectory(Environment.CurrentDirectory, true)));

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

        private Border CreateSidebarVectorRow(string svgPath, string label, Action onClick)
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
                Child = rowGrid
            };

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
                _infoArtworkHolder.Child = VectorStudio.CreateFolderGraphic(96, p, Config.FolderModel);
                _infoTitle.Text = ActivePane != null ? ActivePane.ActiveTab.Title : "Carpeta";
                AddMetaRow("Tipo:", "Carpeta");
                AddMetaRow("Elementos:", (ActivePane != null ? ActivePane.CurrentItems.Count : 0).ToString());
                _infoTextPreview.Visibility = Visibility.Collapsed;
                return;
            }

            _infoArtworkHolder.Child = item.IsDirectory
                ? VectorStudio.CreateFolderGraphic(96, p, Config.FolderModel)
                : VectorStudio.CreateFileGraphic(92, p, item.Extension);

            _infoTitle.Text = item.Name;
            AddMetaRow("Tipo:", item.IsDirectory ? "Carpeta" : "Archivo " + item.Extension);
            if (!item.IsDirectory) AddMetaRow("Tamaño:", item.SizeText);
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

            if (targetItem != null && targetItem.IsDirectory)
            {
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
                    Cursor = Cursors.Hand,
                    Child = crumbLbl
                };
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

        public void SetActivePane(NexusPaneControl pane)
        {
            if (pane == RightPane && !IsSplitView) return;
            ActivePane = pane;

            LeftPane.RenderItems();
            if (IsSplitView) RightPane.RenderItems();

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
        }

        private void PasteFromClipboard()
        {
            if (_clipboardPaths == null || _clipboardPaths.Count == 0) return;
            string dstDir = ActivePane.ActiveTab.CurrentPath;

            foreach (var src in _clipboardPaths)
            {
                try
                {
                    string name = System.IO.Path.GetFileName(src);
                    string dst = System.IO.Path.Combine(dstDir, name);
                    if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase)) continue;

                    if (Directory.Exists(src))
                    {
                        if (_clipboardIsCut) Directory.Move(src, dst);
                        else CopyDirRecursive(src, dst);
                    }
                    else if (File.Exists(src))
                    {
                        if (_clipboardIsCut)
                        {
                            if (File.Exists(dst)) File.Delete(dst);
                            File.Move(src, dst);
                        }
                        else File.Copy(src, dst, true);
                    }
                }
                catch { }
            }

            if (_clipboardIsCut) _clipboardPaths.Clear();
            LeftPane.ReadEntries(); LeftPane.RenderItems();
            if (IsSplitView) { RightPane.ReadEntries(); RightPane.RenderItems(); }
        }

        private void DuplicateItem(FileItem item)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(item.FullPath);
                string ext = System.IO.Path.GetExtension(item.Name);
                string baseName = System.IO.Path.GetFileNameWithoutExtension(item.Name);
                string dst = System.IO.Path.Combine(dir, baseName + " (copia)" + ext);

                if (item.IsDirectory) CopyDirRecursive(item.FullPath, dst);
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
                if (item.IsDirectory) CopyDirRecursive(item.FullPath, dst);
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
            if (!IsSplitView) ToggleSplitView();
            var items = ActivePane.GetSelectedItems();
            var otherPane = (ActivePane == LeftPane) ? RightPane : LeftPane;
            string dstDir = otherPane.ActiveTab.CurrentPath;

            foreach (var item in items)
            {
                try
                {
                    string dst = System.IO.Path.Combine(dstDir, item.Name);
                    if (string.Equals(item.FullPath, dst, StringComparison.OrdinalIgnoreCase)) continue;
                    if (item.IsDirectory)
                    {
                        if (isMove) Directory.Move(item.FullPath, dst);
                        else CopyDirRecursive(item.FullPath, dst);
                    }
                    else
                    {
                        if (isMove)
                        {
                            if (File.Exists(dst)) File.Delete(dst);
                            File.Move(item.FullPath, dst);
                        }
                        else File.Copy(item.FullPath, dst, true);
                    }
                }
                catch { }
            }

            LeftPane.ReadEntries(); LeftPane.RenderItems();
            RightPane.ReadEntries(); RightPane.RenderItems();
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
                if (item.IsDirectory) Directory.Move(item.FullPath, dst);
                else File.Move(item.FullPath, dst);
                ActivePane.ReadEntries();
                ActivePane.RenderItems();
            }
            catch { }
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
            else if (e.Key == Key.F11) { ToggleInfoDock(); e.Handled = true; }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.T) { ActivePane.AddNewTab(ActivePane.ActiveTab.CurrentPath); e.Handled = true; }
                else if (e.Key == Key.W) { ActivePane.CloseCurrentTab(); e.Handled = true; }
                else if (e.Key == Key.L) { ActivateManualPathInput(); e.Handled = true; }
                else if (e.Key == Key.F) { ToggleSearchBox(); e.Handled = true; }
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
        [STAThread]
        public static void Main(string[] args)
        {
            string startupPath = (args != null && args.Length > 0) ? args[0] : null;
            var app = new Application();
            app.Run(new MainWindow(startupPath));
        }
    }
}

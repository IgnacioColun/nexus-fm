using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class IconGen
{
    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "nexus-folder.ico";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));

        using (Bitmap bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Parte trasera de la carpeta (pestaña Dolphin Teal oscuro)
            using (GraphicsPath backPath = RoundedRect(new Rectangle(4, 10, 26, 12), 4))
            using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(255, 19, 132, 110)))
            {
                g.FillPath(backBrush, backPath);
            }

            using (GraphicsPath backBody = RoundedRect(new Rectangle(4, 15, 56, 40), 5))
            using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(255, 19, 132, 110)))
            {
                g.FillPath(backBrush, backBody);
            }

            // Cuerpo frontal de la carpeta (Dolphin Emerald Teal brillante)
            using (GraphicsPath frontPath = RoundedRect(new Rectangle(4, 21, 56, 36), 5))
            using (LinearGradientBrush frontBrush = new LinearGradientBrush(
                new Rectangle(4, 21, 56, 36),
                Color.FromArgb(255, 30, 195, 162),
                Color.FromArgb(255, 20, 150, 125),
                LinearGradientMode.Vertical))
            {
                g.FillPath(frontBrush, frontPath);
            }

            // Brillo superior sutil estilo KDE Breeze
            using (Pen highlightPen = new Pen(Color.FromArgb(140, 120, 255, 230), 1.5f))
            {
                g.DrawLine(highlightPen, 7, 22, 57, 22);
            }

            SavePngAsIco(bmp, outPath);
        }
    }

    static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static void SavePngAsIco(Bitmap bmp, string icoPath)
    {
        using (MemoryStream pngStream = new MemoryStream())
        {
            bmp.Save(pngStream, ImageFormat.Png);
            byte[] pngBytes = pngStream.ToArray();

            using (FileStream fs = new FileStream(icoPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((short)0);
                bw.Write((short)1);
                bw.Write((short)1);

                // ICONDIRENTRY
                bw.Write((byte)bmp.Width);
                bw.Write((byte)bmp.Height);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(pngBytes.Length);
                bw.Write(22); // offset

                bw.Write(pngBytes);
            }
        }
    }
}

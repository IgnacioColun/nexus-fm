using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class IconGen
{
    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "nexus-folder.ico";
        string accentHex = args.Length > 1 ? args[1] : "#4DA2DB";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));

        Color mainColor = ColorTranslator.FromHtml(accentHex);
        Color darkColor = Color.FromArgb(255,
            (int)(mainColor.R * 0.58),
            (int)(mainColor.G * 0.58),
            (int)(mainColor.B * 0.58));
        Color lightColor = Color.FromArgb(255,
            Math.Min(255, mainColor.R + 42),
            Math.Min(255, mainColor.G + 42),
            Math.Min(255, mainColor.B + 42));

        int[] sizes = new int[] { 256, 64, 48, 32, 16 };
        List<byte[]> pngFrames = new List<byte[]>();

        foreach (int sz in sizes)
        {
            using (Bitmap bmp = RenderFolderBitmap(sz, mainColor, darkColor, lightColor))
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                pngFrames.Add(ms.ToArray());
            }
        }

        SaveMultiFrameIco(sizes, pngFrames, outPath);
    }

    static Bitmap RenderFolderBitmap(int size, Color mainColor, Color darkColor, Color lightColor)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float s = size / 64f;

            // 1. Parte trasera con pestaña angulada estilo NexusFM Breeze
            using (GraphicsPath backPath = new GraphicsPath())
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(4 * s, 10 * s),
                    new PointF(25 * s, 10 * s),
                    new PointF(30 * s, 16 * s),
                    new PointF(60 * s, 16 * s),
                    new PointF(60 * s, 52 * s),
                    new PointF(4 * s, 52 * s)
                };
                backPath.AddPolygon(pts);
                using (SolidBrush backBrush = new SolidBrush(darkColor))
                {
                    g.FillPath(backBrush, backPath);
                }
            }

            // 2. Sombra interior de bolsillo
            using (SolidBrush pocketShadow = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
            {
                g.FillRectangle(pocketShadow, 4 * s, 20 * s, 56 * s, Math.Max(1f, 2f * s));
            }

            // 3. Cuerpo frontal Breeze con gradiente y emblema sutil NexusFM
            RectangleF frontRect = new RectangleF(4 * s, 22 * s, 56 * s, 34 * s);
            using (GraphicsPath frontPath = RoundedRect(frontRect, Math.Max(2f, 4f * s)))
            using (LinearGradientBrush frontBrush = new LinearGradientBrush(
                frontRect, lightColor, mainColor, LinearGradientMode.Vertical))
            {
                g.FillPath(frontBrush, frontPath);
            }

            // 4. Brillo superior e inferior
            using (Pen topHighlight = new Pen(Color.FromArgb(150, 255, 255, 255), Math.Max(1f, 1.4f * s)))
            {
                g.DrawLine(topHighlight, 6 * s, 23 * s, 58 * s, 23 * s);
            }
            using (SolidBrush bottomLip = new SolidBrush(darkColor))
            {
                g.FillRectangle(bottomLip, 5 * s, 54 * s, 54 * s, Math.Max(1f, 2f * s));
            }

            // 5. Detalle geométrico Nexus en el centro de la carpeta (para tamaños >= 32)
            if (size >= 32)
            {
                float cx = 32 * s;
                float cy = 39 * s;
                float r = 6.5f * s;
                PointF[] diamond = new PointF[]
                {
                    new PointF(cx, cy - r),
                    new PointF(cx + r, cy),
                    new PointF(cx, cy + r),
                    new PointF(cx - r, cy)
                };
                using (SolidBrush emblemFill = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
                using (Pen emblemPen = new Pen(Color.FromArgb(165, 255, 255, 255), Math.Max(1f, 1.4f * s)))
                {
                    g.FillPolygon(emblemFill, diamond);
                    g.DrawPolygon(emblemPen, diamond);
                }
            }
        }
        return bmp;
    }

    static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        float d = radius * 2f;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static void SaveMultiFrameIco(int[] sizes, List<byte[]> frames, string icoPath)
    {
        using (FileStream fs = new FileStream(icoPath, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
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
            {
                bw.Write(frames[i]);
            }
        }
    }
}

using System.Runtime.InteropServices;

namespace OpenScrape.App.Helpers
{
    public static class CaptureWindowsHelper
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public static Bitmap CaptureWindow(IntPtr handle)
        {
            // Configurar DPI Awareness
            SetProcessDPIAware();

            // Obtener dimensiones
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;

            // Capturar con alta resolución
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);

            // Usar PrintWindow para mejor calidad
            bool success = User32.PrintWindow(handle, hdcDest, 0x02);
            if (!success) GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, 0x00CC0020);

            // Convertir a Bitmap y ajustar DPI
            var bitmap = (Bitmap)Image.FromHbitmap(hBitmap);
            bitmap.SetResolution(300, 300); // 300 DPI para OCR

            // Limpieza
            GDI32.SelectObject(hdcDest, hOld);
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            GDI32.DeleteObject(hBitmap);

            return bitmap;
        }
                
        //public static Image CaptureWindow(IntPtr handle)
        //{
        //    // obtener hDC de la ventana deseada
        //    IntPtr hdcSrc = User32.GetWindowDC(handle);
        //    // Obtener el tamano
        //    User32.RECT windowRect = new User32.RECT();
        //    User32.GetWindowRect(handle, ref windowRect);
        //    int width = windowRect.right - windowRect.left;
        //    int height = windowRect.bottom - windowRect.top;
        //    // Crea un contexto en el que se copiara la imagen
        //    IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
        //    // create a bitmap we can copy it to,
        //    // using GetDeviceCaps to get the width/height
        //    IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
        //    // Seleccionar el objeto bitmap 
        //    IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
        //    // finalizar bitblt
        //    GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
        //    // Restaurar seleccion
        //    GDI32.SelectObject(hdcDest, hOld);
        //    // Limpiar
        //    GDI32.DeleteDC(hdcDest);
        //    User32.ReleaseDC(handle, hdcSrc);
        //    // Obtener una imagen .NET image del bitmap
        //    Image img = Image.FromHbitmap(hBitmap);
        //    // Liberar objeto Bitmab
        //    GDI32.DeleteObject(hBitmap);
        //    return img;
        //}

        public static Image CaptureWindowByPosition(IntPtr handle)
        {
            // obtener hDC de la ventana deseada
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // Obtener el tamano
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // Crea un contexto en el que se copiara la imagen
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, 256, 170);
            // Seleccionar el objeto bitmap 
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // finalizar bitblt
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 590, 705, GDI32.SRCCOPY);
            // Restaurar seleccion
            GDI32.SelectObject(hdcDest, hOld);
            // Limpiar
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // Obtener una imagen .NET image del bitmap
            Image img = Image.FromHbitmap(hBitmap);
            // Liberar objeto Bitmab
            GDI32.DeleteObject(hBitmap);
            return img;
        }

        public static class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

            [DllImport("gdi32.dll")]
            public static extern bool SetBitmapDpi(IntPtr hbm, IntPtr hdc, float dpiX, float dpiY);
        }

        public static class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")]
            public static extern int GetPixel(IntPtr hdc, int x, int y);
            [DllImport("user32.dll")]
            public static extern IntPtr GetDC(IntPtr hwnd);

            [DllImport("user32.dll")]
            public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

            [DllImport("shcore.dll")]
            public static extern int GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

            public const uint PW_RENDERFULLCONTENT = 0x00000002;
            public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

            public enum MONITOR_DPI_TYPE
            {
                MDT_EFFECTIVE_DPI = 0
            }
        }

        public static Bitmap BinaryImage(Bitmap source, int umb)
        {
            // Bitmap con la imagen binaria
            Bitmap target = new Bitmap(source.Width, source.Height, source.PixelFormat);
            // Recorrer pixel de la imagen
            for (int i = 0; i < source.Width; i++)
            {
                for (int e = 0; e < source.Height; e++)
                {
                    // Color del pixel
                    Color col = source.GetPixel(i, e);
                    // Escala de grises
                    byte gray = (byte)(col.R * 0.3f + col.G * 0.59f + col.B * 0.11f);
                    // Blanco o negro
                    byte value = 0;
                    if (gray > umb)
                    {
                        value = 255;
                    }
                    // Asginar nuevo color
                    Color newColor = System.Drawing.Color.FromArgb(value, value, value);
                    target.SetPixel(i, e, newColor);

                }
            }

            return target;
        }

        
    }
}

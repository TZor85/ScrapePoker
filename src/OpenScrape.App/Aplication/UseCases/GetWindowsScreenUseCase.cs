using OpenScrape.App.Helpers;
using System.Drawing.Imaging;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetWindowsScreenUseCase
    {
        private IntPtr _handle = IntPtr.Zero;

        public IntPtr Handle => _handle;

        public Image Execute()
        {
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("No hay ventana de poker configurada. Usa GetWindow() primero.");

            return CaptureWindowsHelper.CaptureWindow(_handle);
        }

        public Image Execute(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Handle de ventana inválido (IntPtr.Zero).");

            return CaptureWindowsHelper.CaptureWindow(handle);
        }

        public Bitmap ExecuteImage(string path)
        {
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("No hay ventana de poker configurada. Usa GetWindow() primero.");

            Image img = CaptureWindowsHelper.CaptureWindow(_handle);

            img.Save(path, ImageFormat.Png);
            return (Bitmap)img;
        }

        /// <summary>
        /// Almacena el handle de la ventana de poker proporcionado.
        /// Ya no usa GetForegroundWindow() internamente — el caller debe pasar el handle correcto.
        /// </summary>
        public IntPtr GetWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException("El handle proporcionado es IntPtr.Zero.", nameof(handle));

            _handle = handle;
            return _handle;
        }
    }
}

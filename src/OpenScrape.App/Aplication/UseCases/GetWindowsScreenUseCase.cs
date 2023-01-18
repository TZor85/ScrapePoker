using OpenScrape.App.Helpers;
using System.Drawing.Imaging;
using static OpenScrape.App.Helpers.CaptureWindowsHelper;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetWindowsScreenUseCase
    {

        private IntPtr _handle = IntPtr.Zero;

        public Image Execute()
        {
            return CaptureWindowsHelper.CaptureWindow(_handle);
        }

        public Bitmap ExecuteImage(string path)
        {
            Image img = CaptureWindowsHelper.CaptureWindow(_handle);

            return (Bitmap)img;
            //img.Save(path, ImageFormat.Png);
        }

        public void GetWindow(IntPtr handle)
        {
            //_handle = CaptureWindowsHelper.User32.GetForegroundWindow();
            _handle = handle;
            
        }
    }
}

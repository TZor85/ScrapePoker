using OpenScrape.App.Helpers;
using System.Drawing.Imaging;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetWindowsScreenUseCase
    {

        private IntPtr _handle = IntPtr.Zero;

        public Image Execute()
        {
            return CaptureWindowsHelper.CaptureWindow(_handle);
        }

        public void ExecuteImage(string path)
        {
            Image img = CaptureWindowsHelper.CaptureWindow(_handle);

            img.Save(path, ImageFormat.Png);
        }

        public void GetWindow()
        {
            _handle = CaptureWindowsHelper.User32.GetForegroundWindow();
        }
    }
}

using OpenScrape.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.UseCases.UseCase
{
    public class GetWindowsScreenUseCase
    {

        private IntPtr _handle = IntPtr.Zero;

        public Image Execute()
        {
            return CaptureWindowsHelper.CaptureWindow(_handle);
        }

        public void GetWindow()
        {
            _handle = CaptureWindowsHelper.User32.GetForegroundWindow();
        }
    }
}

using OpenScrape.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.UseCases
{
    public class GetWindowsScreenUseCase 
    { 

        private IntPtr _handle = IntPtr.Zero;

        public Image Execute()
        {
            Thread.Sleep(2000);
            GetWindow();
            return CaptureWindowsHelper.CaptureWindow(_handle);
        }

        private void GetWindow()
        {
            _handle = CaptureWindowsHelper.User32.GetForegroundWindow();
        }
    }
}

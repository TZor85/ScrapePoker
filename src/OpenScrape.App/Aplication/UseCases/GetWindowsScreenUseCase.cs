﻿using OpenScrape.App.Helpers;
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
            
            img.Save(path, ImageFormat.Png);
            return (Bitmap)img;
            
        }

        public void GetWindow(IntPtr handle)
        {
            _handle = CaptureWindowsHelper.User32.GetForegroundWindow();
            //_handle = handle;
            
        }
    }
}

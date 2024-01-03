﻿using OpenScrape.App.Helpers;
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

        public Image Execute(IntPtr handle)
        {
            return CaptureWindowsHelper.CaptureWindow(handle);
        }

        public Bitmap ExecuteImage(string path)
        {
            Image img = CaptureWindowsHelper.CaptureWindow(_handle);
            
            img.Save(path, ImageFormat.Png);
            return (Bitmap)img;
            
        }

        public IntPtr GetWindow(IntPtr handle)
        {
            _handle = CaptureWindowsHelper.User32.GetForegroundWindow();
            return _handle;
            
        }
    }
}

using System;
using System.Windows;
using PdfSharp.Fonts;

namespace SZR_LaboratoryApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Настройка шрифтов для PDFsharp (Windows)
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            base.OnStartup(e);
        }
    }
}
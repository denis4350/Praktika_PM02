using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SZR_LaboratoryApp.Helpers
{
    public class WpfCaptchaHelper
    {
        private static Random random = new Random();

        public static string GenerateCaptchaCode(int length = 5)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
            char[] code = new char[length];
            for (int i = 0; i < length; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            return new string(code);
        }

        public static BitmapSource GenerateCaptchaImage(string captchaCode, int width = 250, int height = 80)
        {
            // Создаем DrawingVisual для рисования
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Белый фон
                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Рисуем рамку
                drawingContext.DrawRectangle(null, new Pen(Brushes.LightGray, 1), new Rect(0, 0, width, height));

                // Рисуем шум - случайные линии
                for (int i = 0; i < 15; i++)
                {
                    var pen = new Pen(GetRandomBrush(), 1);
                    drawingContext.DrawLine(pen,
                        new Point(random.Next(width), random.Next(height)),
                        new Point(random.Next(width), random.Next(height)));
                }

                // Рисуем шум - случайные точки
                for (int i = 0; i < 300; i++)
                {
                    drawingContext.DrawRectangle(GetRandomBrush(), null,
                        new Rect(random.Next(width), random.Next(height), 1, 1));
                }

                // Рисуем текст капчи с искажением
                int charWidth = width / captchaCode.Length;

                for (int i = 0; i < captchaCode.Length; i++)
                {
                    // Выбираем случайный шрифт
                    var fontFamily = GetRandomFont();
                    var fontSize = random.Next(24, 32);
                    var fontStyle = FontStyles.Normal;
                    var fontWeight = FontWeights.Bold;

                    // Случайное смещение по Y
                    double yOffset = random.Next(15, 35);
                    double xOffset = i * charWidth + random.Next(-5, 10);

                    // Поворот буквы
                    var transform = new RotateTransform(random.Next(-25, 25), xOffset + 10, yOffset + 15);

                    // Рисуем букву
                    var formattedText = new FormattedText(
                        captchaCode[i].ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(fontFamily, fontStyle, fontWeight, FontStretches.Normal),
                        fontSize,
                        GetRandomTextBrush(),
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.PushTransform(transform);
                    drawingContext.DrawText(formattedText, new Point(xOffset, yOffset));
                    drawingContext.Pop();
                }
            }

            // Конвертируем в BitmapSource
            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);

            return renderTarget;
        }

        private static SolidColorBrush GetRandomBrush()
        {
            return new SolidColorBrush(Color.FromRgb(
                (byte)random.Next(100, 200),
                (byte)random.Next(100, 200),
                (byte)random.Next(100, 200)));
        }

        private static SolidColorBrush GetRandomTextBrush()
        {
            return new SolidColorBrush(Color.FromRgb(
                (byte)random.Next(50, 150),
                (byte)random.Next(50, 150),
                (byte)random.Next(50, 150)));
        }

        private static FontFamily GetRandomFont()
        {
            string[] fonts = { "Arial", "Verdana", "Tahoma", "Times New Roman", "Courier New" };
            return new FontFamily(fonts[random.Next(fonts.Length)]);
        }
    }
}
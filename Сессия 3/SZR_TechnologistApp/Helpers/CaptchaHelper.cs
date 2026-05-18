using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SZR_TechnologistApp.Helpers
{
    public enum CaptchaDifficulty
    {
        Low,
        Medium,
        High
    }

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

        public static BitmapSource GenerateCaptchaImage(string captchaCode, int width = 250, int height = 80, CaptchaDifficulty difficulty = CaptchaDifficulty.Medium)
        {
            int lineCount = 10, pointCount = 300, maxRotate = 25, maxXOffset = 10, minYOffset = 15, maxYOffset = 35;

            switch (difficulty)
            {
                case CaptchaDifficulty.Low:
                    lineCount = 5;
                    pointCount = 100;
                    maxRotate = 15;
                    maxXOffset = 5;
                    minYOffset = 10;
                    maxYOffset = 25;
                    break;
                case CaptchaDifficulty.Medium:
                    lineCount = 10;
                    pointCount = 300;
                    maxRotate = 25;
                    maxXOffset = 10;
                    minYOffset = 15;
                    maxYOffset = 35;
                    break;
                case CaptchaDifficulty.High:
                    lineCount = 20;
                    pointCount = 500;
                    maxRotate = 35;
                    maxXOffset = 15;
                    minYOffset = 20;
                    maxYOffset = 45;
                    break;
            }

            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Фон
                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Рамка
                drawingContext.DrawRectangle(null, new Pen(Brushes.LightGray, 1), new Rect(0, 0, width, height));

                // Линии шума
                for (int i = 0; i < lineCount; i++)
                {
                    var pen = new Pen(GetRandomBrush(), 1);
                    drawingContext.DrawLine(pen,
                        new Point(random.Next(width), random.Next(height)),
                        new Point(random.Next(width), random.Next(height)));
                }

                // Точки шума
                for (int i = 0; i < pointCount; i++)
                {
                    drawingContext.DrawRectangle(GetRandomBrush(), null,
                        new Rect(random.Next(width), random.Next(height), 1, 1));
                }

                int charWidth = width / captchaCode.Length;

                for (int i = 0; i < captchaCode.Length; i++)
                {
                    var fontFamily = GetRandomFont();
                    var fontSize = random.Next(24, 32);
                    var fontStyle = FontStyles.Normal;
                    var fontWeight = FontWeights.Bold;

                    double yOffset = random.Next(minYOffset, maxYOffset);
                    double xOffset = i * charWidth + random.Next(-maxXOffset, maxXOffset);

                    var transform = new RotateTransform(random.Next(-maxRotate, maxRotate), xOffset + 10, yOffset + 15);

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
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SZR_LaboratoryApp.Models;

namespace SZR_LaboratoryApp.Services
{
    public class PdfExportService
    {
        public static void ExportTestProtocol(LabTest test, object batch, string outputPath)
        {
            using (var document = new PdfDocument())
            {
                document.Info.Title = $"Протокол испытаний {test.testNumber}";
                document.Info.Author = "СЗР Производство";
                document.Info.Subject = "Результаты лабораторных испытаний";

                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210);
                page.Height = XUnit.FromMillimeter(297);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    // Используем XFontStyleEx для новой версии
                    var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
                    var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
                    var normalFont = new XFont("Arial", 10, XFontStyleEx.Regular);
                    var smallFont = new XFont("Arial", 8, XFontStyleEx.Regular);

                    double yPos = 40;
                    double pageWidth = page.Width.Point;

                    // Заголовок
                    gfx.DrawString("ПРОТОКОЛ ЛАБОРАТОРНЫХ ИСПЫТАНИЙ", titleFont, XBrushes.Black,
                        new XRect(0, yPos, pageWidth, 30), XStringFormats.TopCenter);
                    yPos += 40;

                    // Информация об испытании
                    gfx.DrawString("1. ИНФОРМАЦИЯ ОБ ИСПЫТАНИИ", headerFont, XBrushes.Black,
                        new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                    yPos += 25;

                    DrawInfoRow(gfx, normalFont, ref yPos, "Номер испытания:", test.testNumber);
                    DrawInfoRow(gfx, normalFont, ref yPos, "Тип испытания:", test.testType);
                    DrawInfoRow(gfx, normalFont, ref yPos, "Дата назначения:", test.assignedAt.ToString("dd.MM.yyyy HH:mm"));
                    DrawInfoRow(gfx, normalFont, ref yPos, "Приоритет:", test.priority);

                    yPos += 10;

                    // Информация об объекте
                    gfx.DrawString("2. ИНФОРМАЦИЯ ОБ ОБЪЕКТЕ", headerFont, XBrushes.Black,
                        new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                    yPos += 25;

                    if (batch is RawMaterialBatch rawBatch)
                    {
                        DrawInfoRow(gfx, normalFont, ref yPos, "Тип:", "Партия сырья");
                        DrawInfoRow(gfx, normalFont, ref yPos, "Номер партии:", rawBatch.batchNumber);
                        DrawInfoRow(gfx, normalFont, ref yPos, "Материал:", rawBatch.materialName);
                        DrawInfoRow(gfx, normalFont, ref yPos, "Поставщик:", rawBatch.supplier);
                        DrawInfoRow(gfx, normalFont, ref yPos, "Количество:", $"{rawBatch.quantity} {rawBatch.unit}");
                        DrawInfoRow(gfx, normalFont, ref yPos, "Дата поступления:", rawBatch.arrivalDate.ToString("dd.MM.yyyy"));
                    }
                    else if (batch is ProductBatch productBatch)
                    {
                        DrawInfoRow(gfx, normalFont, ref yPos, "Тип:", "Партия готовой продукции");
                        DrawInfoRow(gfx, normalFont, ref yPos, "Номер партии:", productBatch.batchNumber);
                        DrawInfoRow(gfx, normalFont, ref yPos, "Продукт:", productBatch.productName);
                        DrawInfoRow(gfx, normalFont, ref yPos, "Линия:", productBatch.line);
                    }

                    yPos += 10;

                    // Результаты испытаний
                    gfx.DrawString("3. РЕЗУЛЬТАТЫ ИСПЫТАНИЙ", headerFont, XBrushes.Black,
                        new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                    yPos += 25;

                    DrawResultsTable(gfx, smallFont, ref yPos, test);

                    yPos += 20;

                    // Заключение
                    gfx.DrawString("4. ЗАКЛЮЧЕНИЕ", headerFont, XBrushes.Black,
                        new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                    yPos += 25;

                    string conclusion = GetConclusion(test);
                    gfx.DrawString(conclusion, normalFont, XBrushes.Black,
                        new XRect(40, yPos, pageWidth - 80, 60), XStringFormats.TopLeft);

                    // Подписи
                    double signatureY = page.Height.Point - 80;
                    gfx.DrawLine(XPens.Black, 50, signatureY, 150, signatureY);
                    gfx.DrawString("Лаборант", smallFont, XBrushes.Black,
                        new XRect(50, signatureY + 5, 100, 20), XStringFormats.TopCenter);

                    gfx.DrawLine(XPens.Black, pageWidth - 150, signatureY, pageWidth - 50, signatureY);
                    gfx.DrawString("Руководитель лаборатории", smallFont, XBrushes.Black,
                        new XRect(pageWidth - 150, signatureY + 5, 120, 20), XStringFormats.TopCenter);
                }

                document.Save(outputPath);
            }
        }

        private static void DrawInfoRow(XGraphics gfx, XFont font, ref double yPos, string label, string value)
        {
            gfx.DrawString(label, font, XBrushes.Black,
                new XRect(50, yPos, 120, 20), XStringFormats.TopLeft);
            gfx.DrawString(value, font, XBrushes.Black,
                new XRect(170, yPos, 300, 20), XStringFormats.TopLeft);
            yPos += 22;
        }

        private static void DrawResultsTable(XGraphics gfx, XFont font, ref double yPos, LabTest test)
        {
            if (test.parameters == null || !test.parameters.Any())
            {
                gfx.DrawString("Нет данных о параметрах", font, XBrushes.Black,
                    new XRect(50, yPos, 400, 20), XStringFormats.TopLeft);
                yPos += 25;
                return;
            }

            double startX = 40;
            double[] colWidths = { 40, 120, 80, 80, 80, 80, 60 };
            string[] headers = { "№", "Параметр", "Норма мин", "Норма макс", "Факт. значение", "Ед.изм", "Результат" };

            double currentX = startX;
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[i], 25);
                gfx.DrawString(headers[i], font, XBrushes.Black,
                    new XRect(currentX + 2, yPos + 5, colWidths[i] - 4, 20), XStringFormats.TopLeft);
                currentX += colWidths[i];
            }
            yPos += 25;

            int rowNum = 1;
            foreach (var param in test.parameters)
            {
                currentX = startX;

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[0], 22);
                gfx.DrawString(rowNum.ToString(), font, XBrushes.Black,
                    new XRect(currentX + 2, yPos + 3, colWidths[0] - 4, 20), XStringFormats.TopCenter);
                currentX += colWidths[0];

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[1], 22);
                gfx.DrawString(param.ParameterName ?? "", font, XBrushes.Black,
                    new XRect(currentX + 2, yPos + 3, colWidths[1] - 4, 20), XStringFormats.TopLeft);
                currentX += colWidths[1];

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[2], 22);
                gfx.DrawString(param.NormMin?.ToString() ?? "—", font, XBrushes.Black,
                    new XRect(currentX + 2, yPos + 3, colWidths[2] - 4, 20), XStringFormats.TopCenter);
                currentX += colWidths[2];

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[3], 22);
                gfx.DrawString(param.NormMax?.ToString() ?? "—", font, XBrushes.Black,
                    new XRect(currentX + 2, yPos + 3, colWidths[3] - 4, 20), XStringFormats.TopCenter);
                currentX += colWidths[3];

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[4], 22);
                var color = param.IsPassed == true ? XBrushes.Green : (param.IsPassed == false ? XBrushes.Red : XBrushes.Black);
                gfx.DrawString(param.ActualValue?.ToString("0.00") ?? "—", font, color,
                    new XRect(currentX + 2, yPos + 3, colWidths[4] - 4, 20), XStringFormats.TopCenter);
                currentX += colWidths[4];

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[5], 22);
                gfx.DrawString(param.Unit ?? "", font, XBrushes.Black,
                    new XRect(currentX + 2, yPos + 3, colWidths[5] - 4, 20), XStringFormats.TopCenter);
                currentX += colWidths[5];

                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[6], 22);
                string result = param.IsPassed == true ? "✓" : (param.IsPassed == false ? "✗" : "—");
                var resultColor = param.IsPassed == true ? XBrushes.Green : (param.IsPassed == false ? XBrushes.Red : XBrushes.Black);
                gfx.DrawString(result, font, resultColor,
                    new XRect(currentX + 2, yPos + 3, colWidths[6] - 4, 20), XStringFormats.TopCenter);

                yPos += 22;
                rowNum++;
            }
        }

        private static string GetConclusion(LabTest test)
        {
            if (test.parameters == null || !test.parameters.Any())
                return "Испытание не проведено. Данные отсутствуют.";

            var allPassed = test.parameters.All(p => p.IsPassed == true);
            var anyFailed = test.parameters.Any(p => p.IsPassed == false);

            if (allPassed)
                return "По результатам испытаний партия СООТВЕТСТВУЕТ установленным требованиям.";
            else if (anyFailed)
                return "По результатам испытаний партия НЕ СООТВЕТСТВУЕТ установленным требованиям.";
            else
                return "Результаты испытаний требуют дополнительной проверки.";
        }
    }
}
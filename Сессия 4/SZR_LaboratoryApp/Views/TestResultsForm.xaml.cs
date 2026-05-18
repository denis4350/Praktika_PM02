using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SZR_LaboratoryApp.Models;
using SZR_LaboratoryApp.Services;

namespace SZR_LaboratoryApp.Views
{
    public partial class TestResultsForm : Window
    {
        private readonly ApiService _apiService;
        private readonly LabTest _test;
        private readonly RawMaterialBatch _rawBatch;
        private readonly ProductBatch _productBatch;
        private readonly UserInfoDto _currentUser;
        private readonly string _batchType;
        private List<TestParameterItem> _editableParameters;

        // Конструктор для сырья
        public TestResultsForm(ApiService apiService, LabTest test, RawMaterialBatch batch, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _test = test;
            _rawBatch = batch;
            _productBatch = null;
            _currentUser = currentUser;
            _batchType = "RawMaterial";
            LoadData();
            LoadParameters();
        }

        // Конструктор для продукции
        public TestResultsForm(ApiService apiService, LabTest test, ProductBatch batch, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _test = test;
            _rawBatch = null;
            _productBatch = batch;
            _currentUser = currentUser;
            _batchType = "Product";
            LoadData();
            LoadParameters();
        }

        private void LoadData()
        {
            TestNumberText.Text = _test.testNumber;
            TestTypeText.Text = _test.testType;
            PriorityText.Text = _test.priority;
            CommentBox.Text = _test.comment;

            if (_batchType == "RawMaterial" && _rawBatch != null)
            {
                BatchNumberText.Text = _rawBatch.batchNumber;
                MaterialNameText.Text = _rawBatch.materialName;
                BatchTypeText.Text = "Партия сырья";
                SupplierText.Text = _rawBatch.supplier;
                QuantityText.Text = $"{_rawBatch.quantity} {_rawBatch.unit}";
                ArrivalDateText.Text = _rawBatch.arrivalDate.ToString("dd.MM.yyyy");
                ShowFieldsByType(true);
            }
            else if (_batchType == "Product" && _productBatch != null)
            {
                BatchNumberText.Text = _productBatch.batchNumber;
                MaterialNameText.Text = _productBatch.productName;
                BatchTypeText.Text = "Партия готовой продукции";
                LineText.Text = _productBatch.line;
                StatusText.Text = _productBatch.status;
                FinishedAtText.Text = _productBatch.finishedAt?.ToString("dd.MM.yyyy") ?? "—";
                ShowFieldsByType(false);
            }
        }

        private void ShowFieldsByType(bool isRawMaterial)
        {
            if (SupplierLabel != null) SupplierLabel.Visibility = isRawMaterial ? Visibility.Visible : Visibility.Collapsed;
            if (SupplierText != null) SupplierText.Visibility = isRawMaterial ? Visibility.Visible : Visibility.Collapsed;
            if (QuantityLabel != null) QuantityLabel.Visibility = isRawMaterial ? Visibility.Visible : Visibility.Collapsed;
            if (QuantityText != null) QuantityText.Visibility = isRawMaterial ? Visibility.Visible : Visibility.Collapsed;
            if (ArrivalDateLabel != null) ArrivalDateLabel.Visibility = isRawMaterial ? Visibility.Visible : Visibility.Collapsed;
            if (ArrivalDateText != null) ArrivalDateText.Visibility = isRawMaterial ? Visibility.Visible : Visibility.Collapsed;

            if (LineLabel != null) LineLabel.Visibility = isRawMaterial ? Visibility.Collapsed : Visibility.Visible;
            if (LineText != null) LineText.Visibility = isRawMaterial ? Visibility.Collapsed : Visibility.Visible;
            if (StatusLabel != null) StatusLabel.Visibility = isRawMaterial ? Visibility.Collapsed : Visibility.Visible;
            if (StatusText != null) StatusText.Visibility = isRawMaterial ? Visibility.Collapsed : Visibility.Visible;
            if (FinishedAtLabel != null) FinishedAtLabel.Visibility = isRawMaterial ? Visibility.Collapsed : Visibility.Visible;
            if (FinishedAtText != null) FinishedAtText.Visibility = isRawMaterial ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void LoadParameters()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== LoadParameters START ===");
                System.Diagnostics.Debug.WriteLine($"Test Id: {_test.Id}");

                if (_test.parameters != null && _test.parameters.Count > 0)
                {
                    _editableParameters = _test.parameters.Select(p => new TestParameterItem
                    {
                        Id = p.Id,                     // ← важно!
                        parameterName = p.ParameterName,
                        normMin = p.NormMin,
                        normMax = p.NormMax,
                        unit = p.Unit,
                        ActualValue = p.ActualValue
                    }).ToList();

                    ParametersGrid.ItemsSource = _editableParameters;
                    UpdateOverallResult();
                    return;
                }

                var parameters = await _apiService.GetTestParametersAsync(_test.Id);

                System.Diagnostics.Debug.WriteLine($"Parameters from API: {parameters?.Count ?? 0}");

                if (parameters != null && parameters.Count > 0)
                {
                    _editableParameters = parameters.Select(p => new TestParameterItem
                    {
                        Id = p.Id,                     // ← важно!
                        parameterName = p.ParameterName,   // ✅
                        normMin = p.NormMin,               // ✅
                        normMax = p.NormMax,               // ✅
                        unit = p.Unit,                     // ✅
                        ActualValue = p.ActualValue
                    }).ToList();

                    _test.parameters = parameters;
                    ParametersGrid.ItemsSource = _editableParameters;
                    UpdateOverallResult();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No parameters found!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadParameters error: {ex.Message}");
            }
        }

        private void UpdateOverallResult()
        {
            if (_editableParameters == null || !_editableParameters.Any())
            {
                OverallResultText.Text = "Нет данных";
                return;
            }

            var allPassed = _editableParameters.All(p => p.IsPassed == true);
            var anyFailed = _editableParameters.Any(p => p.IsPassed == false);

            if (allPassed)
            {
                OverallResultText.Text = "СООТВЕТСТВУЕТ";
                OverallResultText.Foreground = Brushes.Green;
            }
            else if (anyFailed)
            {
                OverallResultText.Text = "НЕ СООТВЕТСТВУЕТ";
                OverallResultText.Foreground = Brushes.Red;
            }
            else
            {
                OverallResultText.Text = "ТРЕБУЕТ ПРОВЕРКИ";
                OverallResultText.Foreground = Brushes.Orange;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_editableParameters == null) return;

                // Формируем список LabTestParameter (свойства в PascalCase)
                var apiParams = _editableParameters.Select(p => new LabTestParameter
                {
                    Id = p.Id,
                    ParameterName = p.parameterName,
                    NormMin = p.normMin,
                    NormMax = p.normMax,
                    ActualValue = p.ActualValue,
                    Unit = p.unit,
                    IsPassed = p.IsPassed
                }).ToList();

                var result = await _apiService.UpdateTestResultsAsync(_test.Id, apiParams);
                if (result)
                {
                    UpdateOverallResult();
                    MessageBox.Show("Результаты сохранены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editableParameters == null || !_editableParameters.Any())
            {
                MessageBox.Show("Нет параметров для завершения испытания.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hasEmptyValues = _editableParameters.Any(p => !p.ActualValue.HasValue);
            if (hasEmptyValues)
            {
                MessageBox.Show("Заполните все значения параметров перед завершением испытания.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем перед завершением
            var apiParams = _editableParameters.Select(p => new LabTestParameter
            {
                Id = p.Id,
                ParameterName = p.parameterName,
                NormMin = p.normMin,
                NormMax = p.normMax,
                ActualValue = p.ActualValue,
                Unit = p.unit,
                IsPassed = p.IsPassed
            }).ToList();

            await _apiService.UpdateTestResultsAsync(_test.Id, apiParams);

            var resultMessage = await _apiService.CompleteTestAsync(_test.Id);
            if (resultMessage != null)
            {
                MessageBox.Show($"Испытание завершено! Результат: {resultMessage}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Не удалось завершить испытание.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PDF файл (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    FileName = $"Протокол_{_test.testNumber}_{DateTime.Now:yyyyMMdd_HHmmss}",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dialog.ShowDialog() != true)
                    return;

                // Сохраняем результаты перед экспортом
                if (_editableParameters != null && _editableParameters.Any())
                {
                    var apiParams = _editableParameters.Select(p => new LabTestParameter
                    {
                        Id = p.Id,
                        ParameterName = p.parameterName,
                        NormMin = p.normMin,
                        NormMax = p.normMax,
                        ActualValue = p.ActualValue,
                        Unit = p.unit,
                        IsPassed = p.IsPassed
                    }).ToList();

                    await _apiService.UpdateTestResultsAsync(_test.Id, apiParams);
                    _test.parameters = apiParams;
                }

                await GeneratePdfReport(dialog.FileName);

                MessageBox.Show($"PDF экспорт готов!\nФайл сохранён:\n{dialog.FileName}",
                    "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GeneratePdfReport(string filePath)
        {
            await Task.Run(() =>
            {
                using (var document = new PdfDocument())
                {
                    document.Info.Title = $"Протокол испытаний {_test.testNumber}";
                    document.Info.Author = "СЗР Производство";

                    var page = document.AddPage();
                    page.Width = XUnit.FromMillimeter(210);
                    page.Height = XUnit.FromMillimeter(297);

                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
                        var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
                        var normalFont = new XFont("Arial", 10, XFontStyleEx.Regular);

                        double yPos = 40;
                        double pageWidth = page.Width.Point;

                        gfx.DrawString("ПРОТОКОЛ ЛАБОРАТОРНЫХ ИСПЫТАНИЙ", titleFont, XBrushes.Black,
                            new XRect(0, yPos, pageWidth, 30), XStringFormats.TopCenter);
                        yPos += 40;

                        gfx.DrawString("1. ИНФОРМАЦИЯ ОБ ИСПЫТАНИИ", headerFont, XBrushes.Black,
                            new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                        yPos += 25;

                        gfx.DrawString($"Номер испытания: {_test.testNumber}", normalFont, XBrushes.Black,
                            new XRect(50, yPos, pageWidth - 100, 20), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Тип испытания: {_test.testType}", normalFont, XBrushes.Black,
                            new XRect(50, yPos, pageWidth - 100, 20), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Дата: {_test.assignedAt:dd.MM.yyyy}", normalFont, XBrushes.Black,
                            new XRect(50, yPos, pageWidth - 100, 20), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Приоритет: {_test.priority}", normalFont, XBrushes.Black,
                            new XRect(50, yPos, pageWidth - 100, 20), XStringFormats.TopLeft);
                        yPos += 30;

                        gfx.DrawString("2. РЕЗУЛЬТАТЫ ИСПЫТАНИЙ", headerFont, XBrushes.Black,
                            new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                        yPos += 25;

                        if (_editableParameters != null && _editableParameters.Count > 0)
                        {
                            double startX = 40;
                            double[] colWidths = { 30, 120, 60, 60, 80, 60, 70 };
                            string[] headers = { "№", "Параметр", "Норма мин", "Норма макс", "Факт. значение", "Ед.изм", "Результат" };

                            double currentX = startX;
                            for (int i = 0; i < headers.Length; i++)
                            {
                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[i], 25);
                                gfx.DrawString(headers[i], headerFont, XBrushes.Black,
                                    new XRect(currentX + 2, yPos + 5, colWidths[i] - 4, 20), XStringFormats.TopLeft);
                                currentX += colWidths[i];
                            }
                            yPos += 25;

                            int rowNum = 1;
                            foreach (var param in _editableParameters)
                            {
                                currentX = startX;

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[0], 22);
                                gfx.DrawString(rowNum.ToString(), normalFont, XBrushes.Black,
                                    new XRect(currentX + 2, yPos + 3, colWidths[0] - 4, 20), XStringFormats.TopCenter);
                                currentX += colWidths[0];

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[1], 22);
                                gfx.DrawString(param.parameterName ?? "", normalFont, XBrushes.Black,
                                    new XRect(currentX + 2, yPos + 3, colWidths[1] - 4, 20), XStringFormats.TopLeft);
                                currentX += colWidths[1];

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[2], 22);
                                gfx.DrawString(param.normMin?.ToString() ?? "—", normalFont, XBrushes.Black,
                                    new XRect(currentX + 2, yPos + 3, colWidths[2] - 4, 20), XStringFormats.TopCenter);
                                currentX += colWidths[2];

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[3], 22);
                                gfx.DrawString(param.normMax?.ToString() ?? "—", normalFont, XBrushes.Black,
                                    new XRect(currentX + 2, yPos + 3, colWidths[3] - 4, 20), XStringFormats.TopCenter);
                                currentX += colWidths[3];

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[4], 22);
                                var color = param.IsPassed == true ? XBrushes.Green : (param.IsPassed == false ? XBrushes.Red : XBrushes.Black);
                                gfx.DrawString(param.ActualValue?.ToString("0.00") ?? "—", normalFont, color,
                                    new XRect(currentX + 2, yPos + 3, colWidths[4] - 4, 20), XStringFormats.TopCenter);
                                currentX += colWidths[4];

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[5], 22);
                                gfx.DrawString(param.unit ?? "", normalFont, XBrushes.Black,
                                    new XRect(currentX + 2, yPos + 3, colWidths[5] - 4, 20), XStringFormats.TopCenter);
                                currentX += colWidths[5];

                                gfx.DrawRectangle(XPens.Black, currentX, yPos, colWidths[6], 22);
                                string result = param.IsPassed == true ? "✓ Соответствует" : (param.IsPassed == false ? "✗ Не соответствует" : "—");
                                var resultColor = param.IsPassed == true ? XBrushes.Green : (param.IsPassed == false ? XBrushes.Red : XBrushes.Black);
                                gfx.DrawString(result, normalFont, resultColor,
                                    new XRect(currentX + 2, yPos + 3, colWidths[6] - 4, 20), XStringFormats.TopCenter);

                                yPos += 22;
                                rowNum++;
                            }
                        }
                        else
                        {
                            gfx.DrawString("Нет данных о параметрах", normalFont, XBrushes.Black,
                                new XRect(50, yPos, pageWidth - 100, 20), XStringFormats.TopLeft);
                        }

                        yPos += 20;

                        gfx.DrawString("3. ЗАКЛЮЧЕНИЕ", headerFont, XBrushes.Black,
                            new XRect(40, yPos, pageWidth, 20), XStringFormats.TopLeft);
                        yPos += 25;

                        string conclusion = GetConclusion();
                        gfx.DrawString(conclusion, normalFont, XBrushes.Black,
                            new XRect(50, yPos, pageWidth - 100, 60), XStringFormats.TopLeft);
                    }

                    document.Save(filePath);
                }
            });
        }

        private string GetConclusion()
        {
            if (_editableParameters == null || !_editableParameters.Any())
                return "Испытание не проведено. Данные отсутствуют.";

            var allPassed = _editableParameters.All(p => p.IsPassed == true);
            var anyFailed = _editableParameters.Any(p => p.IsPassed == false);

            if (allPassed)
                return "По результатам испытаний партия СООТВЕТСТВУЕТ установленным требованиям.";
            else if (anyFailed)
                return "По результатам испытаний партия НЕ СООТВЕТСТВУЕТ установленным требованиям.";
            else
                return "Результаты испытаний требуют дополнительной проверки.";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
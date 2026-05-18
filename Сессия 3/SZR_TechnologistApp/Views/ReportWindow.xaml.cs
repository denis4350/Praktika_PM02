using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ReportWindow : Window
    {
        private readonly ApiService _apiService;
        private List<object> _currentReportData;
        private bool _isLoading;

        public ReportWindow(ApiService apiService)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _currentReportData = new List<object>();

            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;

            Loaded += ReportWindow_Loaded;
        }

        private async void ReportWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string reportType = GetSelectedText(ReportTypeComboBox);
            await LoadStatusesForReportType(reportType);
            await GenerateReportAsync();
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateReportAsync();
        }

        private async void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            string reportType = GetSelectedText(ReportTypeComboBox);
            await LoadStatusesForReportType(reportType);   // ← динамическая загрузка

            await GenerateReportAsync();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToCsvOnly();
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcelOnly();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task GenerateReportAsync()
        {
            if (_isLoading)
                return;

            try
            {
                HideError();
                SetLoadingState(true, "Загрузка данных...");

                string reportType = GetSelectedText(ReportTypeComboBox);
                DateTime? startDate = StartDatePicker.SelectedDate;
                DateTime? endDate = EndDatePicker.SelectedDate;
                string status = GetSelectedText(StatusComboBox);

                if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
                {
                    ShowError("Дата начала не может быть больше даты окончания.");
                    return;
                }

                if (reportType.Contains("партиям"))
                {
                    await LoadBatchesReportAsync(startDate, endDate, status);
                }
                else if (reportType.Contains("отклонениям"))
                {
                    await LoadDeviationsReportAsync(startDate, endDate);
                }
                else if (reportType.Contains("продукции"))
                {
                    await LoadProductsReportAsync();
                }
                else if (reportType.Contains("рецептурам"))
                {
                    await LoadRecipesReportAsync(status);
                }
                else if (reportType.Contains("Сводный") || reportType.Contains("сводный"))
                {
                    await LoadSummaryReportAsync();
                }

                ReportGrid.ItemsSource = null;
                ReportGrid.ItemsSource = _currentReportData;

                RowsCountText.Text = (_currentReportData == null ? 0 : _currentReportData.Count) + " записей";
                ReportSubtitleText.Text = reportType + " / сформирован " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                SetStatus("Загружено записей: " + (_currentReportData == null ? 0 : _currentReportData.Count), false);
            }
            catch (Exception ex)
            {
                _currentReportData = new List<object>();
                ReportGrid.ItemsSource = null;
                RowsCountText.Text = "0 записей";

                ShowError("Ошибка формирования отчёта: " + ex.Message);
                SetStatus("Ошибка формирования отчёта", true);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task LoadBatchesReportAsync(DateTime? startDate, DateTime? endDate, string status)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                _currentReportData = new List<object>();
                return;
            }

            var batches = await _apiService.GetBatchesReportJsonAsync(startDate.Value, endDate.Value);
            if (batches == null) batches = new List<BatchReportItem>();

            var rows = new List<object>();

            foreach (var batch in batches)
            {
                if (!IsAllStatus(status) && batch.Status != status)
                    continue;

                rows.Add(new
                {
                    batch.BatchNumber,
                    batch.ProductName,
                    batch.Date,           // поле из BatchReportItem
                    batch.Status,
                    batch.HasDeviations,
                    batch.LabDecision
                });
            }

            _currentReportData = rows;
        }

        private async Task LoadDeviationsReportAsync(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                _currentReportData = new List<object>();
                return;
            }

            var deviations = await _apiService.GetDeviationsReportJsonAsync(startDate.Value, endDate.Value);
            if (deviations == null) deviations = new List<DeviationReportItem>();

            var rows = new List<object>();

            foreach (var dev in deviations)
            {
                rows.Add(new
                {
                    dev.BatchNumber,
                    dev.StepNumber,
                    dev.ParameterName,
                    dev.PlannedValue,
                    dev.ActualValue,
                    dev.Severity,
                    dev.CreatedAt
                });
            }

            _currentReportData = rows;
        }

        private async Task LoadProductsReportAsync()
        {
            var result = await _apiService.GetProductsAsync(1, 10000);
            var products = result != null && result.Items != null
    ? result.Items.Cast<object>().ToList()
    : new List<object>();

            _currentReportData = products
                .Select(product => new
                {
                    Id = GetPropertyValue<int>(product, "Id", "id"),
                    Code = GetPropertyValue<string>(product, "Code", "code"),
                    Name = GetPropertyValue<string>(product, "Name", "name"),
                    ProductType = GetPropertyValue<string>(product, "ProductType", "productType"),
                    Form = GetPropertyValue<string>(product, "Form", "form"),
                    Status = GetPropertyValue<string>(product, "Status", "status"),
                    CreatedAt = GetNullableDateTime(product, "CreatedAt", "createdAt")
                })
                .Cast<object>()
                .ToList();
        }

        private async Task LoadRecipesReportAsync(string status)
        {
            var result = await _apiService.GetRecipesAsync(1, 10000);
            var recipes = result != null && result.Items != null
     ? result.Items.Cast<object>().ToList()
     : new List<object>();

            var rows = new List<object>();

            foreach (object recipe in recipes)
            {
                string recipeStatus = GetPropertyValue<string>(recipe, "Status", "status");

                if (!IsAllStatus(status) && recipeStatus != status)
                    continue;

                rows.Add(new
                {
                    Id = GetPropertyValue<int>(recipe, "Id", "id"),
                    ProductName = GetPropertyValue<string>(recipe, "ProductName", "productName"),
                    Version = GetPropertyValue<string>(recipe, "Version", "version"),
                    Status = recipeStatus,
                    TotalPercentage = GetPropertyValue<decimal?>(recipe, "TotalPercentage", "totalPercentage"),
                    ComponentCount = GetPropertyValue<int?>(recipe, "ComponentCount", "componentCount"),
                    CreatedAt = GetNullableDateTime(recipe, "CreatedAt", "createdAt"),
                    ApprovedAt = GetNullableDateTime(recipe, "ApprovedAt", "approvedAt")
                });
            }

            _currentReportData = rows;
        }
        private async Task LoadStatusesForReportType(string reportType)
        {
            StatusComboBox.Items.Clear();
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Все статусы" });

            List<StatusItem> statuses = null;

            if (reportType.Contains("партиям"))
            {
                statuses = await _apiService.GetBatchStatusesAsync();
            }
            else if (reportType.Contains("рецептурам"))
            {
                statuses = await _apiService.GetRecipeStatusesAsync();
            }
            // Для отклонений, продукции и сводного отчёта статусы не применимы – оставляем только "Все статусы"

            if (statuses != null)
            {
                foreach (var s in statuses)
                    StatusComboBox.Items.Add(new ComboBoxItem { Content = s.Label });
            }

            StatusComboBox.SelectedIndex = 0; // "Все статусы"
        }

        private async Task LoadSummaryReportAsync()
        {
            var batchesResult = await _apiService.GetBatchesAsync(1, 10000);
            var deviationsResult = await _apiService.GetDeviationsAsync(null, 1, 10000);
            var productsResult = await _apiService.GetProductsAsync(1, 10000);
            var recipesResult = await _apiService.GetRecipesAsync(1, 10000);

            var batches = batchesResult != null && batchesResult.Items != null
    ? batchesResult.Items.Cast<object>().ToList()
    : new List<object>();

            var deviations = deviationsResult != null && deviationsResult.Items != null
                ? deviationsResult.Items.Cast<object>().ToList()
                : new List<object>();

            var products = productsResult != null && productsResult.Items != null
                ? productsResult.Items.Cast<object>().ToList()
                : new List<object>();

            var recipes = recipesResult != null && recipesResult.Items != null
                ? recipesResult.Items.Cast<object>().ToList()
                : new List<object>();

            int completedBatches = batches.Count(b => GetPropertyValue<string>(b, "Status", "status") == "Завершена");
            int inProgressBatches = batches.Count(b => GetPropertyValue<string>(b, "Status", "status") == "В работе");
            int criticalDeviations = deviations.Count(d => GetPropertyValue<string>(d, "Severity", "severity") == "Критично");
            int warnings = deviations.Count(d => GetPropertyValue<string>(d, "Severity", "severity") == "Предупреждение");
            int approvedRecipes = recipes.Count(r => GetPropertyValue<string>(r, "Status", "status") == "Утверждена");

            var summary = new List<object>
            {
                new SummaryRow { Indicator = "Всего продукции", Value = products.Count.ToString() },
                new SummaryRow { Indicator = "Всего рецептур", Value = recipes.Count.ToString() },
                new SummaryRow { Indicator = "Утверждённые рецептуры", Value = approvedRecipes.ToString() },
                new SummaryRow { Indicator = "Всего партий", Value = batches.Count.ToString() },
                new SummaryRow { Indicator = "Завершённые партии", Value = completedBatches.ToString() },
                new SummaryRow { Indicator = "Партии в работе", Value = inProgressBatches.ToString() },
                new SummaryRow { Indicator = "Критические отклонения", Value = criticalDeviations.ToString() },
                new SummaryRow { Indicator = "Предупреждения", Value = warnings.ToString() },
                new SummaryRow { Indicator = "Процент завершённых партий", Value = batches.Count > 0 ? (completedBatches * 100 / batches.Count) + "%" : "0%" }
            };

            _currentReportData = summary;
        }

        private void ExportToCsvOnly()
        {
            try
            {
                if (!HasReportData())
                {
                    MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string reportType = GetSelectedText(ReportTypeComboBox);
                string fileName = GetReportFileName(reportType);

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = fileName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToCsv(dialog.FileName);
                    MessageBox.Show("CSV файл сохранён:\n" + dialog.FileName, "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта CSV:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcelOnly()
        {
            try
            {
                if (!HasReportData())
                {
                    MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string reportType = GetSelectedText(ReportTypeComboBox);
                string fileName = GetReportFileName(reportType);

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel файл (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = fileName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToExcelFile(dialog.FileName, reportType);
                    MessageBox.Show("Excel файл сохранён:\n" + dialog.FileName, "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта Excel:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            var keys = GetExportKeys();

            if (keys.Count == 0)
                return;

            var sb = new StringBuilder();

            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append(EscapeCsv(GetRussianHeader(keys[i])));
                if (i < keys.Count - 1)
                    sb.Append(";");
            }

            sb.AppendLine();

            foreach (object item in _currentReportData)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    object value = GetRawPropertyValue(item, keys[i]);
                    sb.Append(EscapeCsv(FormatValue(value)));

                    if (i < keys.Count - 1)
                        sb.Append(";");
                }

                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }

        private void ExportToExcelFile(string filePath, string reportType)
        {


            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Отчёт");
                var keys = GetExportKeys();

                if (keys.Count == 0)
                    return;

                worksheet.Cells[1, 1, 1, keys.Count].Merge = true;
                worksheet.Cells[1, 1].Value = reportType;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                for (int i = 0; i < keys.Count; i++)
                {
                    worksheet.Cells[2, i + 1].Value = GetRussianHeader(keys[i]);
                    worksheet.Cells[2, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[2, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[2, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int row = 0; row < _currentReportData.Count; row++)
                {
                    object item = _currentReportData[row];

                    for (int col = 0; col < keys.Count; col++)
                    {
                        object value = GetRawPropertyValue(item, keys[col]);
                        worksheet.Cells[row + 3, col + 1].Value = FormatValue(value);
                    }
                }

                if (worksheet.Dimension != null)
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private List<string> GetExportKeys()
        {
            if (_currentReportData == null || _currentReportData.Count == 0)
                return new List<string>();

            object first = _currentReportData[0];

            if (first is JObject jObject)
                return jObject.Properties().Select(p => p.Name).ToList();

            return first.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToList();
        }

        private bool HasReportData()
        {
            return _currentReportData != null && _currentReportData.Count > 0;
        }

        private string GetReportFileName(string reportType)
        {
            if (reportType.Contains("партиям"))
                return "Отчет_по_партиям";

            if (reportType.Contains("отклонениям"))
                return "Отчет_по_отклонениям";

            if (reportType.Contains("продукции"))
                return "Отчет_по_продукции";

            if (reportType.Contains("рецептурам"))
                return "Отчет_по_рецептурам";

            if (reportType.Contains("Сводный") || reportType.Contains("сводный"))
                return "Сводный_отчет";

            return "Отчет";
        }

        private bool IsAllStatus(string status)
        {
            return string.IsNullOrWhiteSpace(status) || status == "Все статусы";
        }

        private bool IsDateInRange(DateTime? value, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue && !endDate.HasValue)
                return true;

            if (!value.HasValue)
                return false;

            DateTime date = value.Value.Date;

            if (startDate.HasValue && date < startDate.Value.Date)
                return false;

            if (endDate.HasValue && date > endDate.Value.Date)
                return false;

            return true;
        }

        private string GetSelectedText(ComboBox comboBox)
        {
            if (comboBox == null || comboBox.SelectedItem == null)
                return string.Empty;

            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;

            if (item != null && item.Content != null)
                return item.Content.ToString();

            return comboBox.SelectedItem.ToString();
        }

        private T GetPropertyValue<T>(object source, params string[] names)
        {
            object value = GetRawPropertyValue(source, names);

            if (value == null)
                return default(T);

            try
            {
                if (value is T typed)
                    return typed;

                if (value is JValue jValue)
                    value = jValue.Value;

                if (value == null)
                    return default(T);

                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)value.ToString();

                if (targetType == typeof(DateTime))
                {
                    DateTime date;

                    if (DateTime.TryParse(value.ToString(), out date))
                        return (T)(object)date;

                    return default(T);
                }

                if (targetType == typeof(decimal))
                {
                    decimal decimalValue;

                    if (decimal.TryParse(
                            value.ToString().Replace(',', '.'),
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out decimalValue))
                    {
                        return (T)(object)decimalValue;
                    }

                    return default(T);
                }

                object converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

                return (T)converted;
            }
            catch
            {
                return default(T);
            }
        }

        private DateTime? GetNullableDateTime(object source, params string[] names)
        {
            object value = GetRawPropertyValue(source, names);

            if (value == null)
                return null;

            if (value is JValue jValue)
                value = jValue.Value;

            if (value == null)
                return null;

            DateTime date;

            if (DateTime.TryParse(value.ToString(), out date))
                return date;

            return null;
        }

        private object GetRawPropertyValue(object source, params string[] names)
        {
            if (source == null || names == null)
                return null;

            JToken token = source as JToken;

            if (token != null)
            {
                JToken child = GetChildToken(token, names);

                if (child == null || child.Type == JTokenType.Null)
                    return null;

                return child;
            }

            foreach (string name in names)
            {
                PropertyInfo property = source.GetType().GetProperty(name);

                if (property == null)
                    continue;

                object value = property.GetValue(source, null);

                if (value != null)
                    return value;
            }

            try
            {
                token = JToken.FromObject(source);
                JToken child = GetChildToken(token, names);

                if (child == null || child.Type == JTokenType.Null)
                    return null;

                return child;
            }
            catch
            {
                return null;
            }
        }

        private JToken GetChildToken(JToken token, params string[] names)
        {
            if (token == null || names == null)
                return null;

            JObject obj = token as JObject;

            if (obj == null && token.Type == JTokenType.Object)
                obj = (JObject)token;

            if (obj == null)
                return null;

            foreach (string name in names)
            {
                JProperty property = obj.Properties()
                    .FirstOrDefault(p => string.Equals(
                        p.Name,
                        name,
                        StringComparison.InvariantCultureIgnoreCase));

                if (property != null)
                    return property.Value;
            }

            return null;
        }

        private string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is JValue jValue)
                value = jValue.Value;

            if (value == null)
                return string.Empty;

            if (value is DateTime dateTime)
                return dateTime.ToString("dd.MM.yyyy HH:mm");

            DateTime parsedDate;

            if (DateTime.TryParse(value.ToString(), out parsedDate))
                return parsedDate.ToString("dd.MM.yyyy HH:mm");

            return value.ToString();
        }

        private string EscapeCsv(string value)
        {
            if (value == null)
                return string.Empty;

            value = value.Replace("\"", "\"\"");

            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value + "\"";

            return value;
        }

        private string GetRussianHeader(string englishName)
        {
            var dict = new Dictionary<string, string>
            {
                { "Id", "ID" },
                { "id", "ID" },

                { "BatchNumber", "Номер партии" },
                { "batchNumber", "Номер партии" },

                { "ProductName", "Продукт" },
                { "productName", "Продукт" },

                { "Line", "Линия" },
                { "line", "Линия" },

                { "Status", "Статус" },
                { "status", "Статус" },

                { "StartedAt", "Дата начала" },
                { "startedAt", "Дата начала" },

                { "FinishedAt", "Дата окончания" },
                { "finishedAt", "Дата окончания" },

                { "LabStatus", "Лабораторный статус" },
                { "labStatus", "Лабораторный статус" },

                { "OrderId", "ID заказа" },
                { "orderId", "ID заказа" },

                { "Code", "Код" },
                { "code", "Код" },

                { "Name", "Наименование" },
                { "name", "Наименование" },

                { "ProductType", "Тип продукта" },
                { "productType", "Тип продукта" },

                { "Form", "Форма выпуска" },
                { "form", "Форма выпуска" },

                { "CreatedAt", "Дата создания" },
                { "createdAt", "Дата создания" },

                { "OrderNumber", "Номер заказа" },
                { "orderNumber", "Номер заказа" },

                { "PlannedQuantity", "Плановое количество" },
                { "plannedQuantity", "Плановое количество" },

                { "PlannedStartDate", "Плановая дата" },
                { "plannedStartDate", "Плановая дата" },

                { "Unit", "Единица измерения" },
                { "unit", "Единица измерения" },

                { "Version", "Версия" },
                { "version", "Версия" },

                { "TotalPercentage", "Сумма, %" },
                { "totalPercentage", "Сумма, %" },

                { "ComponentCount", "Кол-во компонентов" },
                { "componentCount", "Кол-во компонентов" },

                { "ApprovedAt", "Дата утверждения" },
                { "approvedAt", "Дата утверждения" },

                { "EventType", "Тип события" },
                { "eventType", "Тип события" },

                { "ParameterName", "Параметр" },
                { "parameterName", "Параметр" },

                { "PlannedValue", "Плановое значение" },
                { "plannedValue", "Плановое значение" },

                { "ActualValue", "Фактическое значение" },
                { "actualValue", "Фактическое значение" },

                { "Severity", "Критичность" },
                { "severity", "Критичность" },

                { "Description", "Описание" },
                { "description", "Описание" },

                { "Indicator", "Показатель" },
                { "Value", "Значение" }
            };

            return dict.ContainsKey(englishName) ? dict[englishName] : englishName;
        }

        private void ReportGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = GetRussianHeader(e.PropertyName);

            if (e.PropertyType == typeof(DateTime) || e.PropertyType == typeof(DateTime?))
            {
                var textColumn = e.Column as DataGridTextColumn;

                if (textColumn != null)
                    textColumn.Binding.StringFormat = "dd.MM.yyyy HH:mm";
            }
        }

        private void SetLoadingState(bool isLoading, string message = null)
        {
            _isLoading = isLoading;

            ReportTypeComboBox.IsEnabled = !isLoading;
            StartDatePicker.IsEnabled = !isLoading;
            EndDatePicker.IsEnabled = !isLoading;
            StatusComboBox.IsEnabled = !isLoading;

            GenerateButton.IsEnabled = !isLoading;
            ExportCsvButton.IsEnabled = !isLoading && HasReportData();
            ExportExcelButton.IsEnabled = !isLoading && HasReportData();
            CloseButton.IsEnabled = !isLoading;

            if (!string.IsNullOrWhiteSpace(message))
                SetStatus(message, false);

            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void SetStatus(string text, bool isError)
        {
            StatusText.Text = text;
            StatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = string.Empty;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                GenerateButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private class SummaryRow
        {
            public string Indicator { get; set; }
            public string Value { get; set; }
        }
    }
}
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class BatchCardWindow : Window
    {
        private readonly ApiService _apiService;
        private dynamic _batch;
        private List<dynamic> _steps;
        private bool _isLoading;

        public BatchCardWindow(ApiService apiService, dynamic batch)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _batch = batch ?? throw new ArgumentNullException(nameof(batch));
            _steps = new List<dynamic>();

            Loaded += BatchCardWindow_Loaded;
        }

        private async void BatchCardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAllAsync();
        }

        private async Task RefreshAllAsync()
        {
            if (_isLoading)
                return;

            try
            {
                _isLoading = true;
                SetLoadingState(true);

                await LoadBatchData();
                await LoadSteps();
                await LoadBatchData();
                UpdateProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки данных партии:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
                _isLoading = false;
            }
        }

        private Task LoadBatchData()
        {
            try
            {
                string batchNumber = GetString(_batch, "batchNumber", "BatchNumber");
                string productName = GetString(_batch, "productName", "ProductName");
                string line = GetString(_batch, "line", "Line");
                string status = GetString(_batch, "status", "Status");
                string startedAt = GetDateString(_batch, "startedAt", "StartedAt");

                BatchNumberText.Text = string.IsNullOrWhiteSpace(batchNumber) ? "—" : batchNumber;
                BatchNumberBadge.Text = string.IsNullOrWhiteSpace(batchNumber) ? "—" : batchNumber;

                ProductNameText.Text = string.IsNullOrWhiteSpace(productName) ? "—" : productName;
                LineText.Text = string.IsNullOrWhiteSpace(line) ? "—" : line;
                StatusText.Text = string.IsNullOrWhiteSpace(status) ? "—" : status;
                StartedAtText.Text = string.IsNullOrWhiteSpace(startedAt) ? "Дата начала не указана" : startedAt;

                ApplyBatchStatusColor(status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка LoadBatchData: " + ex.Message);
            }

            return Task.CompletedTask;
        }

        private async Task LoadSteps()
        {
            try
            {
                string batchNumber = GetString(_batch, "batchNumber", "BatchNumber");
                if (string.IsNullOrWhiteSpace(batchNumber))
                {
                    _steps = new List<object>();
                    StepsListView.ItemsSource = null;
                    UpdateProgress();
                    return;
                }

                object batchData = await _apiService.GetBatchProgramAsync(batchNumber);
                if (batchData == null)
                {
                    _steps = new List<object>();
                    StepsListView.ItemsSource = null;
                    UpdateProgress();
                    return;
                }

                JToken token = JToken.FromObject(batchData);
                JToken stepsToken = GetChildToken(token, "steps", "Steps");

                if (stepsToken != null && stepsToken.Type == JTokenType.Array)
                {
                    _steps = stepsToken.Select(s => new StepDisplayItem
                    {
                        stepNumber = (int)s["stepNumber"],
                        name = (string)s["name"],
                        instruction = (string)s["instruction"] ?? "",
                        status = (string)s["status"]
                    }).Cast<object>().ToList();
                }
                else
                {
                    _steps = new List<object>();
                }

                StepsListView.ItemsSource = null;
                StepsListView.ItemsSource = _steps;
                UpdateProgress();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка LoadSteps: " + ex.Message);
                _steps = new List<object>();
                StepsListView.ItemsSource = null;
                UpdateProgress();
                throw;
            }
        }

        private void UpdateProgress()
        {
            if (_steps == null || _steps.Count == 0)
            {
                ProgressBar.Value = 0;
                ProgressPercentText.Text = "0% (0/0)";
                return;
            }

            int completed = _steps.Count(step =>
            {
                var item = step as StepDisplayItem;
                return item != null && IsCompletedStatus(item.status);
            });

            int total = _steps.Count;
            int percent = total > 0 ? completed * 100 / total : 0;

            ProgressBar.Value = percent;
            ProgressPercentText.Text = $"{percent}% ({completed}/{total})";
        }

        private async void StartStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            object step = button.DataContext;
            if (step == null)
                return;

            int stepNumber = GetInt(step, "stepNumber", "StepNumber");
            string stepName = GetString(step, "name", "Name");
            string batchNumber = GetString(_batch, "batchNumber", "BatchNumber");

            if (string.IsNullOrWhiteSpace(batchNumber) || stepNumber <= 0)
            {
                MessageBox.Show(
                    "Не удалось определить номер партии или номер шага.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string confirmText = string.IsNullOrWhiteSpace(stepName)
                ? $"Начать шаг №{stepNumber}?"
                : $"Начать шаг №{stepNumber}: {stepName}?";

            var result = MessageBox.Show(
                confirmText,
                "Подтверждение запуска шага",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                SetLoadingState(true);

                bool success = await _apiService.StartStepAsync(batchNumber, stepNumber);
             

                if (success)
                {
                    await RefreshAllAsync();

                    MessageBox.Show(
                        $"Шаг №{stepNumber} начат.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка запуска шага:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void CompleteStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            object step = button.DataContext;
            if (step == null)
                return;

            int stepNumber = GetInt(step, "stepNumber", "StepNumber");
            string batchNumber = GetString(_batch, "batchNumber", "BatchNumber");

            if (string.IsNullOrWhiteSpace(batchNumber) || stepNumber <= 0)
            {
                MessageBox.Show(
                    "Не удалось определить номер партии или номер шага.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dialog = new CompleteStepDialog(stepNumber)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                SetLoadingState(true);

                bool success = await _apiService.CompleteStepAsync(batchNumber, stepNumber, dialog.ActualParams);

                if (success)
                {
                    await RefreshAllAsync();

                    MessageBox.Show(
                        $"Шаг №{stepNumber} завершён.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка завершения шага:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAllAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyBatchStatusColor(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                StatusText.Foreground = new SolidColorBrush(Colors.Gray);
                return;
            }

            if (status == "Завершена" || status == "Разрешена")
            {
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
            }
            else if (status == "В работе" || status == "Подготовлена")
            {
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else if (status == "Заблокирована" || status == "Отменена")
            {
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            }
            else
            {
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            if (RefreshButton != null)
                RefreshButton.IsEnabled = !isLoading;

            if (CloseButton != null)
                CloseButton.IsEnabled = !isLoading;

            Cursor = isLoading
                ? System.Windows.Input.Cursors.Wait
                : System.Windows.Input.Cursors.Arrow;
        }

        private bool IsCompletedStatus(string status)
        {
            return status == "Завершён" || status == "Завершен";
        }

        private string GetString(object source, params string[] names)
        {
            JToken token = GetToken(source, names);

            if (token == null || token.Type == JTokenType.Null)
                return string.Empty;

            return token.ToString();
        }

        private int GetInt(object source, params string[] names)
        {
            JToken token = GetToken(source, names);

            if (token == null || token.Type == JTokenType.Null)
                return 0;

            int value;
            if (int.TryParse(token.ToString(), out value))
                return value;

            return 0;
        }

        private string GetDateString(object source, params string[] names)
        {
            JToken token = GetToken(source, names);

            if (token == null || token.Type == JTokenType.Null)
                return string.Empty;

            DateTime date;

            if (token.Type == JTokenType.Date)
            {
                date = token.Value<DateTime>();
                return date.ToString("dd.MM.yyyy HH:mm");
            }

            if (DateTime.TryParse(token.ToString(), out date))
                return date.ToString("dd.MM.yyyy HH:mm");

            return token.ToString();
        }

        private JToken GetToken(object source, params string[] names)
        {
            if (source == null || names == null || names.Length == 0)
                return null;

            JToken token = source as JToken;

            if (token == null)
            {
                try
                {
                    token = JToken.FromObject(source);
                }
                catch
                {
                    return null;
                }
            }

            foreach (string name in names)
            {
                JToken child = GetChildToken(token, name);
                if (child != null)
                    return child;
            }

            return null;
        }

        private JToken GetChildToken(JToken token, params string[] names)
        {
            if (token == null || names == null || names.Length == 0)
                return null;

            JObject obj = token as JObject;

            if (obj == null && token.Type == JTokenType.Object)
                obj = (JObject)token;

            if (obj == null)
                return null;

            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

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
        public class StepDisplayItem
        {
            public int stepNumber { get; set; }
            public string name { get; set; }
            public string instruction { get; set; }
            public string status { get; set; }
        }
    }
}
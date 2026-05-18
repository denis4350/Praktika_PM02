using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;

namespace SZR_OperatorApp.Views
{
    public partial class BatchProgramView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;
        private BatchInfo _batchInfo;
        private List<BatchStep> _steps;
        private BatchStep _selectedStep;
        private string _batchNumber;

        public event Action StepCompleted;

        public BatchProgramView(ApiService apiService, UserInfoDto currentUser, string batchNumber = null)
        {
            InitializeComponent();
            _apiService = apiService;
            _currentUser = currentUser;
            _batchNumber = batchNumber;

            // Подписка на события изменения текста
            TemperatureBox.TextChanged += (s, e) => ValidateTemperature();
            PressureBox.TextChanged += (s, e) => ValidatePressure();
            SpeedBox.TextChanged += (s, e) => ValidateSpeed();

            Loaded += async (s, e) => await LoadBatchData();
        }

        private async Task LoadBatchData()
        {
            try
            {
                if (string.IsNullOrEmpty(_batchNumber))
                {
                    var activeBatches = await _apiService.GetActiveBatchesAsync();
                    var activeBatch = activeBatches?.FirstOrDefault();
                    if (activeBatch == null)
                    {
                        MessageBox.Show("Нет активных партий для выполнения", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    _batchNumber = activeBatch.BatchNumber;
                }

                // Получаем BatchInfo напрямую
                _batchInfo = await _apiService.GetBatchProgramAsync(_batchNumber);
                if (_batchInfo == null)
                {
                    MessageBox.Show("Не удалось загрузить данные партии", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _steps = _batchInfo.Steps;
                if (_steps == null) _steps = new List<BatchStep>();

                // Если в BatchInfo нет списка Steps, можно попытаться загрузить отдельно,
                // но ваш API возвращает шаги внутри ответа, так что они должны быть.

                UpdateHeader();
                UpdateStepsList();

                _selectedStep = _steps.FirstOrDefault(s => s.Status != "Завершен");
                if (_selectedStep != null)
                {
                    UpdateStepUI();
                }

                foreach (var step in _steps)
                {
                    System.Diagnostics.Debug.WriteLine($"Шаг {step.StepNumber}: Статус = '{step.Status}'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadBatchData error: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateHeader()
        {
            BatchNumberText.Text = _batchInfo.BatchNumber;
            ProductNameText.Text = _batchInfo.ProductName;
            LineText.Text = _batchInfo.Line;
            StatusText.Text = _batchInfo.Status;
            CurrentStepText.Text = _batchInfo.CurrentStepName ?? "—";
            StartedAtText.Text = _batchInfo.StartedAt.ToString("dd.MM.yyyy HH:mm");

            var statusColor = GetStatusColor(_batchInfo.Status);
            StatusText.Background = statusColor;
        }

        private void UpdateStepsList()
        {
            StepsList.ItemsSource = _steps;
        }

        private void UpdateStepUI()
        {
            if (_selectedStep == null) return;

            System.Diagnostics.Debug.WriteLine($"=== UpdateStepUI ===");
            System.Diagnostics.Debug.WriteLine($"Step Number: {_selectedStep.StepNumber}");
            System.Diagnostics.Debug.WriteLine($"Step Status: '{_selectedStep.Status}'");

            StepTitleText.Text = $"Шаг {_selectedStep.StepNumber}: {_selectedStep.Name}";
            InstructionText.Text = _selectedStep.Instruction ?? "Нет инструкции";

            TemperatureBox.Text = "";
            PressureBox.Text = "";
            SpeedBox.Text = "";

            if (_selectedStep.ActualParams != null)
            {
                try
                {
                    var paramsObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(_selectedStep.ActualParams.ToString());
                    if (paramsObj.ContainsKey("temperature")) TemperatureBox.Text = paramsObj["temperature"]?.ToString();
                    if (paramsObj.ContainsKey("pressure")) PressureBox.Text = paramsObj["pressure"]?.ToString();
                    if (paramsObj.ContainsKey("speed")) SpeedBox.Text = paramsObj["speed"]?.ToString();
                }
                catch { }
            }

            if (_selectedStep.ToleranceParams != null)
            {
                ToleranceText.Text = $"Допустимые значения: {_selectedStep.ToleranceParams}";
            }
            else
            {
                ToleranceText.Text = "";
            }

            // Обновляем видимость кнопок в зависимости от статуса
            if (_selectedStep.Status == "Не начат")
            {
                StartStepButton.Visibility = Visibility.Visible;
                CompleteStepButton.Visibility = Visibility.Collapsed;
            }
            else if (_selectedStep.Status == "Выполняется")
            {
                StartStepButton.Visibility = Visibility.Collapsed;
                CompleteStepButton.Visibility = Visibility.Visible;
            }
            else
            {
                StartStepButton.Visibility = Visibility.Collapsed;
                CompleteStepButton.Visibility = Visibility.Collapsed;
            }
        }


        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _selectedStep = button?.Tag as BatchStep;
            if (_selectedStep != null)
            {
                UpdateStepUI();
            }
        }

        private async void StartStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStep == null) return;

            // Если статус уже "Выполняется", предложим сбросить
            if (_selectedStep.Status == "Выполняется")
            {
                var reset = MessageBox.Show("Шаг уже в процессе выполнения. Сбросить статус?", "Внимание",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (reset == MessageBoxResult.Yes)
                {
                    // Временно меняем статус локально
                    _selectedStep.Status = "Не начат";
                    UpdateStepUI();
                }
                return;
            }

            var result = MessageBox.Show($"Начать шаг {_selectedStep.StepNumber}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.StartStepAsync(_batchNumber, _selectedStep.StepNumber);

                    if (success)
                    {
                        _selectedStep.Status = "Выполняется";
                        _selectedStep.StartedAt = DateTime.Now;
                        UpdateStepsList();
                        UpdateStepUI();
                        MessageBox.Show($"Шаг {_selectedStep.StepNumber} начат!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось начать шаг", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CompleteStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStep == null) return;

            var actualParams = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(TemperatureBox.Text))
                actualParams["temperature"] = decimal.TryParse(TemperatureBox.Text, out var temp) ? temp : 0;
            if (!string.IsNullOrEmpty(PressureBox.Text))
                actualParams["pressure"] = decimal.TryParse(PressureBox.Text, out var press) ? press : 0;
            if (!string.IsNullOrEmpty(SpeedBox.Text))
                actualParams["speed"] = int.TryParse(SpeedBox.Text, out var speed) ? speed : 0;

            if (actualParams.Count == 0)
            {
                var confirm = MessageBox.Show("Вы не ввели параметры. Продолжить?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;
            }

            var result = MessageBox.Show($"Завершить шаг {_selectedStep.StepNumber}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.CompleteStepAsync(_batchNumber, _selectedStep.StepNumber, actualParams);
                    if (success)
                    {
                        // Обновляем статус шага
                        _selectedStep.Status = "Завершен";
                        _selectedStep.FinishedAt = DateTime.Now;
                        _selectedStep.ActualParams = actualParams;
                        UpdateStepsList();

                        // Ищем следующий шаг
                        var nextStep = _steps.FirstOrDefault(s => s.Status != "Завершен");
                        if (nextStep != null)
                        {
                            _selectedStep = nextStep;
                            UpdateStepUI();
                            MessageBox.Show($"Шаг завершен! Теперь выполняйте шаг {nextStep.StepNumber}.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            // Все шаги завершены
                            MessageBox.Show("Все шаги партии завершены!", "Поздравляем",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            StepCompleted?.Invoke();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось завершить шаг", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ReportDeviationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStep == null) return;

            // Пытаемся извлечь плановое значение для температуры (можно расширить логику)
            string plannedValue = null;
            if (_selectedStep.PlannedParams != null)
            {
                try
                {
                    var plannedParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        _selectedStep.PlannedParams.ToString());
                    if (plannedParams.ContainsKey("temperature"))
                        plannedValue = plannedParams["temperature"]?.ToString();
                }
                catch { }
            }

            var dialog = new DeviationDialog(_batchNumber, _selectedStep.StepNumber, plannedValue);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var success = await _apiService.RegisterDeviationAsync(
                        _batchNumber,
                        _selectedStep.StepNumber,
                        dialog.ParameterName,
                        dialog.PlannedValue,
                        dialog.ActualValue,
                        dialog.Description,
                        dialog.Severity
                    );

                    if (success)
                    {
                        _selectedStep.HasDeviation = true;
                        UpdateStepsList();
                        MessageBox.Show("Отклонение зарегистрировано!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== ВАЛИДАЦИЯ ПАРАМЕТРОВ ==========
        private void ValidateTemperature()
        {
            if (_selectedStep?.ToleranceParams != null)
            {
                decimal? min = null, max = null;
                try
                {
                    var tolerance = _selectedStep.ToleranceParams.ToString();
                    if (tolerance.Contains("temp"))
                    {
                        var parts = tolerance.Split('-');
                        if (parts.Length == 2)
                        {
                            min = decimal.Parse(parts[0].Replace("temp_range:", "").Trim());
                            max = decimal.Parse(parts[1].Trim());
                        }
                    }
                }
                catch { }

                if (!min.HasValue) min = 140;
                if (!max.HasValue) max = 180;
                ValidateParameter(TemperatureBox, "Температура", min, max);
            }
        }

        private void ValidatePressure()
        {
            if (_selectedStep?.ToleranceParams != null)
            {
                decimal? min = null, max = null;
                try
                {
                    var tolerance = _selectedStep.ToleranceParams.ToString();
                    if (tolerance.Contains("pressure"))
                    {
                        var parts = tolerance.Split('-');
                        if (parts.Length == 2)
                        {
                            min = decimal.Parse(parts[0].Replace("pressure:", "").Trim());
                            max = decimal.Parse(parts[1].Trim());
                        }
                    }
                }
                catch { }

                if (!min.HasValue) min = 45;
                if (!max.HasValue) max = 60;
                ValidateParameter(PressureBox, "Давление", min, max);
            }
        }

        private void ValidateSpeed()
        {
            if (_selectedStep?.ToleranceParams != null)
            {
                decimal? min = null, max = null;
                try
                {
                    var tolerance = _selectedStep.ToleranceParams.ToString();
                    if (tolerance.Contains("speed"))
                    {
                        var parts = tolerance.Split('-');
                        if (parts.Length == 2)
                        {
                            min = decimal.Parse(parts[0].Replace("speed:", "").Trim());
                            max = decimal.Parse(parts[1].Trim());
                        }
                    }
                }
                catch { }

                if (!min.HasValue) min = 250;
                if (!max.HasValue) max = 350;
                ValidateParameter(SpeedBox, "Скорость", min, max);
            }
        }

        private void ValidateParameter(TextBox textBox, string parameterName, decimal? min, decimal? max)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Background = Brushes.White;
                return;
            }

            if (!decimal.TryParse(textBox.Text, out decimal value))
            {
                textBox.Background = Brushes.LightCoral;
                ShowParameterWarning(textBox, "Введите корректное число");
                return;
            }

            bool isError = false;
            string errorMsg = "";

            if (min.HasValue && value < min.Value)
            {
                isError = true;
                errorMsg = $"Значение ниже нормы (мин: {min.Value})";
            }
            else if (max.HasValue && value > max.Value)
            {
                isError = true;
                errorMsg = $"Значение выше нормы (макс: {max.Value})";
            }

            if (isError)
            {
                textBox.Background = Brushes.LightCoral;
                ShowParameterWarning(textBox, errorMsg);
            }
            else
            {
                textBox.Background = Brushes.LightGreen;
                ClearParameterWarning(textBox);
            }
        }

        private void ShowParameterWarning(TextBox textBox, string message)
        {
            if (textBox.ToolTip is ToolTip tooltip && tooltip.Content?.ToString() == message)
                return;

            textBox.ToolTip = new ToolTip
            {
                Content = message,
                Background = Brushes.Red,
                Foreground = Brushes.White
            };
        }

        private void ClearParameterWarning(TextBox textBox)
        {
            textBox.ToolTip = null;
        }

        private Brush GetStatusColor(string status)
        {
            switch (status)
            {
                case "В работе": return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                case "Подготовлена": return new SolidColorBrush(Color.FromRgb(52, 152, 219));
                case "Ожидает контроля": return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                default: return new SolidColorBrush(Color.FromRgb(149, 165, 166));
            }
        }
    }
}
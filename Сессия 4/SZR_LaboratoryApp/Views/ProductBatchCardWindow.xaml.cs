using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SZR_LaboratoryApp.Models;
using SZR_LaboratoryApp.Services;

namespace SZR_LaboratoryApp.Views
{
    public partial class ProductBatchCardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly ProductBatch _batch;
        private readonly UserInfoDto _currentUser;
        private List<LabTest> _tests;

        public ProductBatchCardWindow(ApiService apiService, ProductBatch batch, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _batch = batch;
            _currentUser = currentUser;

            LoadBatchData();
            LoadTests();
            UpdateButtonsVisibility();
        }

        private void LoadBatchData()
        {
            BatchNumberText.Text = _batch.batchNumber;
            ProductNameText.Text = _batch.productName;
            LineText.Text = _batch.line;
            StatusText.Text = _batch.status;
            StartedAtText.Text = _batch.startedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            FinishedAtText.Text = _batch.finishedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            CurrentStatusText.Text = _batch.labStatus ?? "Ожидает";

            var statusColor = GetStatusColor(_batch.labStatus);
            StatusText.Foreground = statusColor;
            CurrentStatusText.Foreground = statusColor;
        }

        private async void LoadTests()
        {
            try
            {
                _tests = await _apiService.GetTestsByObjectAsync(_batch.Id, "Product");

                if (_tests != null && _tests.Count > 0)
                {
                    TestsGrid.ItemsSource = _tests;
                }
                else
                {
                    TestsGrid.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки испытаний: {ex.Message}");
            }
        }

        private void UpdateButtonsVisibility()
        {
            if (_batch.labStatus == "Разрешена")
            {
                ApproveButton.Visibility = Visibility.Collapsed;
                BlockButton.Visibility = Visibility.Visible;
                ApproveButton.IsEnabled = false;
            }
            else if (_batch.labStatus == "Заблокирована")
            {
                ApproveButton.Visibility = Visibility.Visible;
                BlockButton.Visibility = Visibility.Collapsed;
                BlockButton.IsEnabled = false;
            }
        }

        private async void NewTestButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем наличие незавершённого испытания
            var hasUnfinished = _tests != null && _tests.Any(t => t.status != "Завершено");
            if (hasUnfinished)
            {
                MessageBox.Show("Для этой партии уже есть незавершённое испытание. Завершите его перед созданием нового.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Открываем универсальную форму создания испытания
            var testForm = new TestForm(_apiService, _batch, "Product", _currentUser);
            testForm.Owner = this;
            if (testForm.ShowDialog() == true)
            {
                LoadTests(); // обновляем список испытаний
            }
        }

        private async void ViewTest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var test = button?.Tag as LabTest;
            if (test == null) return;

            var result = MessageBox.Show($"Открыть результаты испытания {test.testNumber}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var resultsForm = new TestResultsForm(_apiService, test, _batch, _currentUser);
                    resultsForm.Owner = this;
                    resultsForm.ShowDialog();
                    LoadTests(); // обновить после просмотра/завершения
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}\n\nПроверьте, что TestResultsForm существует.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task<bool> CanMakeDecision()
        {
            var tests = await _apiService.GetTestsByObjectAsync(_batch.Id, "Product");
            var completedTest = tests?.FirstOrDefault(t => t.status == "Завершено");
            if (completedTest == null)
                return false;

            var parameters = await _apiService.GetTestParametersAsync(completedTest.Id);
            return parameters != null && parameters.All(p => p.ActualValue.HasValue);
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await CanMakeDecision())
            {
                MessageBox.Show("Невозможно разрешить партию: нет завершённого испытания со всеми заполненными результатами.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Разрешить использование партии?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.DecideProductBatchAsync(_batch.Id, "Разрешена");
                    if (success)
                    {
                        MessageBox.Show("Партия разрешена к использованию!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await CanMakeDecision())
            {
                MessageBox.Show("Невозможно заблокировать партию: нет завершённого испытания со всеми заполненными результатами.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Заблокировать партию? Укажите причину в следующем диалоге (если требуется).",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.DecideProductBatchAsync(_batch.Id, "Заблокирована", "Блокировка лабораторией");
                    if (success)
                    {
                        MessageBox.Show("Партия заблокирована!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Brush GetStatusColor(string status)
        {
            switch (status)
            {
                case "Ожидает": return Brushes.Gray;
                case "В работе": return Brushes.Orange;
                case "Разрешена": return Brushes.Green;
                case "Заблокирована": return Brushes.Red;
                default: return Brushes.Black;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
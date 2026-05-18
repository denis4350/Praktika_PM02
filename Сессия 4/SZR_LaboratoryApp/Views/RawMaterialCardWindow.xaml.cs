using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SZR_LaboratoryApp.Models;
using SZR_LaboratoryApp.Services;

namespace SZR_LaboratoryApp.Views
{
    public partial class RawMaterialCardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly RawMaterialBatch _batch;
        private readonly UserInfoDto _currentUser;
        private List<LabTest> _tests;
        private List<AuditLog> _history;

        public RawMaterialCardWindow(ApiService apiService, RawMaterialBatch batch, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _batch = batch;
            _currentUser = currentUser;

            LoadBatchData();
            LoadTests();
            UpdateDecisionUI();

            // Подписки на вкладки
            TestsTab.MouseLeftButtonUp += TestsTab_Click;
            HistoryTab.MouseLeftButtonUp += HistoryTab_Click;
            DecisionTab.MouseLeftButtonUp += DecisionTab_Click;
        }

        private void LoadBatchData()
        {
            BatchNumberText.Text = _batch.batchNumber;
            SupplierBatchText.Text = _batch.supplierBatch ?? "—";
            MaterialNameText.Text = _batch.materialName;
            CategoryText.Text = _batch.category ?? "—";
            SupplierText.Text = _batch.supplier;
            ArrivalDateText.Text = _batch.arrivalDate.ToString("dd.MM.yyyy");
            QuantityText.Text = $"{_batch.quantity} {_batch.unit}";
            LabStatusText.Text = _batch.labStatus;
            CurrentStatusText.Text = _batch.labStatus;

            var statusColor = GetStatusColor(_batch.labStatus);
            LabStatusText.Foreground = statusColor;
            CurrentStatusText.Foreground = statusColor;
        }

        private async void LoadTests()
        {
            try
            {
                _tests = await _apiService.GetTestsByObjectAsync(_batch.Id, "RawMaterial");
                TestsGrid.ItemsSource = _tests;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTests error: {ex.Message}");
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                LoadingHistory.Visibility = Visibility.Visible;
                _history = await _apiService.GetBatchHistoryAsync(_batch.Id, "RawMaterial");

                if (_history != null && _history.Count > 0)
                {
                    HistoryGrid.ItemsSource = _history;
                }
                else
                {
                    var emptyHistory = new List<AuditLog>
                    {
                        new AuditLog { Action = "Нет записей", CreatedAt = DateTime.Now, UserName = "Система" }
                    };
                    HistoryGrid.ItemsSource = emptyHistory;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
                HistoryGrid.ItemsSource = null;
            }
            finally
            {
                LoadingHistory.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateDecisionUI()
        {
            if (_batch.labStatus == "Разрешена" || _batch.labStatus == "Заблокирована")
            {
                DecisionComboBox.IsEnabled = false;
                DecisionCommentBox.IsEnabled = false;
                ApplyDecisionButton.IsEnabled = false;
            }
            else if (_batch.labStatus == "В работе")
            {
                DecisionComboBox.IsEnabled = false;
                DecisionCommentBox.IsEnabled = false;
                ApplyDecisionButton.IsEnabled = false;
                var tooltip = new ToolTip { Content = "Сначала завершите все испытания" };
                ApplyDecisionButton.ToolTip = tooltip;
            }
            else
            {
                DecisionComboBox.IsEnabled = true;
                DecisionCommentBox.IsEnabled = true;
                ApplyDecisionButton.IsEnabled = true;
                ApplyDecisionButton.ToolTip = null;
            }
        }

        // ========== ВКЛАДКИ ==========
        private void TestsTab_Click(object sender, MouseButtonEventArgs e)
        {
            TestsGrid.Visibility = Visibility.Visible;
            HistoryGrid.Visibility = Visibility.Collapsed;
            DecisionPanel.Visibility = Visibility.Collapsed;

            TestsTab.Background = new SolidColorBrush(Color.FromRgb(232, 240, 254));
            HistoryTab.Background = Brushes.Transparent;
            DecisionTab.Background = Brushes.Transparent;

            TestsTabText.FontWeight = FontWeights.Bold;
            HistoryTabText.FontWeight = FontWeights.Normal;
            DecisionTabText.FontWeight = FontWeights.Normal;
        }

        private async void HistoryTab_Click(object sender, MouseButtonEventArgs e)
        {
            TestsGrid.Visibility = Visibility.Collapsed;
            HistoryGrid.Visibility = Visibility.Visible;
            DecisionPanel.Visibility = Visibility.Collapsed;

            TestsTab.Background = Brushes.Transparent;
            HistoryTab.Background = new SolidColorBrush(Color.FromRgb(232, 240, 254));
            DecisionTab.Background = Brushes.Transparent;

            TestsTabText.FontWeight = FontWeights.Normal;
            HistoryTabText.FontWeight = FontWeights.Bold;
            DecisionTabText.FontWeight = FontWeights.Normal;

            await LoadHistory();
        }

        private void DecisionTab_Click(object sender, MouseButtonEventArgs e)
        {
            TestsGrid.Visibility = Visibility.Collapsed;
            HistoryGrid.Visibility = Visibility.Collapsed;
            DecisionPanel.Visibility = Visibility.Visible;

            TestsTab.Background = Brushes.Transparent;
            HistoryTab.Background = Brushes.Transparent;
            DecisionTab.Background = new SolidColorBrush(Color.FromRgb(232, 240, 254));

            TestsTabText.FontWeight = FontWeights.Normal;
            HistoryTabText.FontWeight = FontWeights.Normal;
            DecisionTabText.FontWeight = FontWeights.Bold;
        }

        // ========== ДЕЙСТВИЯ ==========
        private async void NewTestButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли незавершенное испытание (используем camelCase)
            var hasUnfinished = _tests != null && _tests.Any(t => t.status != "Завершено");

            if (hasUnfinished)
            {
                MessageBox.Show("Для этой партии уже есть незавершенное испытание. Завершите его перед созданием нового.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_batch.labStatus == "Разрешена" || _batch.labStatus == "Заблокирована")
            {
                MessageBox.Show("Нельзя создать испытание для уже разрешённой или заблокированной партии.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Универсальный конструктор TestForm (ApiService, object batch, string batchType, UserInfoDto)
            var testForm = new TestForm(_apiService, _batch, "RawMaterial", _currentUser);
            testForm.Owner = this;
            if (testForm.ShowDialog() == true)
            {
                LoadTests();
                // После создания испытания статус становится "В работе"
                _batch.labStatus = "В работе";
                LabStatusText.Text = "В работе";
                CurrentStatusText.Text = "В работе";
            }
        }

        private void ViewTest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            if (button.Tag is LabTest test)
            {
                var resultsForm = new TestResultsForm(_apiService, test, _batch, _currentUser);
                resultsForm.Owner = this;
                resultsForm.ShowDialog();
                LoadTests();
            }
            else
            {
                MessageBox.Show("Ошибка: неверный формат данных испытания", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApplyDecisionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, есть ли завершенные испытания
                var hasCompletedTest = _tests != null && _tests.Any(t => t.status == "Завершено");

                if (!hasCompletedTest)
                {
                    MessageBox.Show("Невозможно принять решение: нет завершенных испытаний.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, не принято ли уже решение
                if (_batch.labStatus == "Разрешена" || _batch.labStatus == "Заблокирована")
                {
                    MessageBox.Show("Решение по этой партии уже принято.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var decision = (DecisionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var comment = DecisionCommentBox.Text;

                if (string.IsNullOrEmpty(decision))
                {
                    MessageBox.Show("Выберите решение.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (decision == "Заблокирована" && string.IsNullOrWhiteSpace(comment))
                {
                    MessageBox.Show("При блокировке партии необходимо указать причину.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Подтвердите решение: {decision} партию?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ApplyDecisionButton.IsEnabled = false;

                    var success = await _apiService.DecideRawMaterialBatchAsync(_batch.Id, decision, comment);

                    if (success)
                    {
                        _batch.labStatus = decision;
                        LabStatusText.Text = decision;
                        CurrentStatusText.Text = decision;

                        var statusColor = GetStatusColor(decision);
                        LabStatusText.Foreground = statusColor;
                        CurrentStatusText.Foreground = statusColor;

                        MessageBox.Show($"Партия {decision}.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при изменении статуса. Попробуйте позже.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    ApplyDecisionButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyDecisionButton_Click error: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ApplyDecisionButton.IsEnabled = true;
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
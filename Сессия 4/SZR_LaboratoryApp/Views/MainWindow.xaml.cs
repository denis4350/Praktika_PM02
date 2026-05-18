using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SZR_LaboratoryApp.Models;
using SZR_LaboratoryApp.Services;
using SZR_LaboratoryApp.Views;

namespace SZR_LaboratoryApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;
        private List<RawMaterialBatch> _allRawMaterialBatches;
        private List<ProductBatch> _allProductBatches;
        private string _currentView = "raw";

        // Фильтры
        private string _currentSearch = "";
        private string _currentStatusFilter = "";
        private string _currentSupplierFilter = "";
        private DateTime? _startDate = null;
        private DateTime? _endDate = null;

        public MainWindow(ApiService apiService, UserInfoDto user)
        {
            InitializeComponent();

            _apiService = apiService;
            _currentUser = user;

            UserInfoText.Text = $"{user.FullName} ({user.Role})";

            // Подписка на меню (с автоматическим сбросом фильтров)
            RawMaterialsMenu.Click += (s, e) => { ResetFilters(); LoadRawMaterialBatches(); };
            ProductsMenu.Click += (s, e) => { ResetFilters(); LoadProductBatches(); };
            HistoryMenu.Click += (s, e) => { ResetFilters(); LoadTestHistory(); };

            LogoutButton.Click += (s, e) => Logout();
            RefreshButton.Click += (s, e) => RefreshCurrentView();

            // Подписка на фильтры
            SearchBox.TextChanged += SearchBox_TextChanged;
            StatusFilterBox.SelectionChanged += StatusFilter_SelectionChanged;
            SupplierFilterBox.SelectionChanged += SupplierFilter_SelectionChanged;
            StartDatePicker.SelectedDateChanged += DateFilter_Changed;
            EndDatePicker.SelectedDateChanged += DateFilter_Changed;

            LoadRawMaterialBatches();
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                StatusBarText.Text = message;
                StatusBarText.Foreground = isError ? Brushes.Red : Brushes.Green;
            });
        }

        private void RefreshCurrentView()
        {
            if (_currentView == "raw") LoadRawMaterialBatches();
            else if (_currentView == "product") LoadProductBatches();
            else if (_currentView == "history") LoadTestHistory();
        }

        private void ResetFilters()
        {
            SearchBox.Text = "";
            _currentSearch = "";
            if (StatusFilterBox != null) StatusFilterBox.SelectedIndex = 0;
            if (SupplierFilterBox != null) SupplierFilterBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            _startDate = null;
            _endDate = null;
            _currentStatusFilter = "";
            _currentSupplierFilter = "";
        }

        // ========== ФИЛЬТРАЦИЯ ==========
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearch = SearchBox.Text?.ToLower() ?? "";
            ApplyFilters();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterBox.SelectedItem is ComboBoxItem item)
            {
                _currentStatusFilter = item.Content.ToString() == "Все статусы" ? "" : item.Content.ToString();
                ApplyFilters();
            }
        }

        private void SupplierFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SupplierFilterBox.SelectedItem is ComboBoxItem item)
            {
                _currentSupplierFilter = item.Content.ToString() == "Все поставщики" ? "" : item.Content.ToString();
                ApplyFilters();
            }
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _startDate = StartDatePicker.SelectedDate;
            _endDate = EndDatePicker.SelectedDate?.AddDays(1);
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_currentView == "raw") ApplyRawMaterialFilters();
            else if (_currentView == "product") ApplyProductFilters();
        }

        // ФИЛЬТРАЦИЯ ДЛЯ ПАРТИЙ СЫРЬЯ (свойства в camelCase)
        private void ApplyRawMaterialFilters()
        {
            if (RawMaterialGrid == null) return;
            if (_allRawMaterialBatches == null || _allRawMaterialBatches.Count == 0)
            {
                RawMaterialGrid.ItemsSource = null;
                UpdateStatus("Нет данных", false);
                return;
            }

            var filtered = _allRawMaterialBatches.AsEnumerable();

            if (!string.IsNullOrEmpty(_currentSearch))
            {
                filtered = filtered.Where(b =>
                    (b.batchNumber?.ToLower().Contains(_currentSearch) ?? false) ||
                    (b.materialName?.ToLower().Contains(_currentSearch) ?? false) ||
                    (b.supplier?.ToLower().Contains(_currentSearch) ?? false));
            }

            if (!string.IsNullOrEmpty(_currentStatusFilter))
                filtered = filtered.Where(b => b.labStatus == _currentStatusFilter);

            if (!string.IsNullOrEmpty(_currentSupplierFilter))
                filtered = filtered.Where(b => b.supplier == _currentSupplierFilter);

            if (_startDate.HasValue)
                filtered = filtered.Where(b => b.arrivalDate >= _startDate.Value);
            if (_endDate.HasValue)
                filtered = filtered.Where(b => b.arrivalDate <= _endDate.Value);

            RawMaterialGrid.ItemsSource = filtered.ToList();
            UpdateStatus($"Найдено: {filtered.Count()} записей");
        }

        // ФИЛЬТРАЦИЯ ДЛЯ ПАРТИЙ ПРОДУКЦИИ (свойства в camelCase)
        private void ApplyProductFilters()
        {
            if (_allProductBatches == null || _allProductBatches.Count == 0)
            {
                ProductGrid.ItemsSource = null;
                UpdateStatus("Нет данных", false);
                return;
            }

            var filtered = _allProductBatches.AsEnumerable();

            if (!string.IsNullOrEmpty(_currentSearch))
            {
                filtered = filtered.Where(b =>
                    (b.batchNumber?.ToLower().Contains(_currentSearch) ?? false) ||
                    (b.productName?.ToLower().Contains(_currentSearch) ?? false));
            }

            if (!string.IsNullOrEmpty(_currentStatusFilter))
                filtered = filtered.Where(b => b.labStatus == _currentStatusFilter);

            if (_startDate.HasValue)
                filtered = filtered.Where(b => b.finishedAt >= _startDate.Value);
            if (_endDate.HasValue)
                filtered = filtered.Where(b => b.finishedAt <= _endDate.Value);

            ProductGrid.ItemsSource = filtered.ToList();
            UpdateStatus($"Найдено: {filtered.Count()} записей");
        }

        // ========== ЗАГРУЗКА ДАННЫХ ==========
        private async void LoadRawMaterialBatches()
        {
            try
            {
                UpdateStatus("Загрузка партий сырья...");
                _currentView = "raw";
                ContentTitle.Text = "📦 Партии сырья";

                RawMaterialGrid.Visibility = Visibility.Visible;
                ProductGrid.Visibility = Visibility.Collapsed;
                HistoryGrid.Visibility = Visibility.Collapsed;
                SupplierFilterBox.Visibility = Visibility.Visible;

                var batches = await _apiService.GetRawMaterialBatchesAsync();
                _allRawMaterialBatches = batches;

                // Загружаем список поставщиков
                var suppliers = batches.Select(b => b.supplier).Distinct().OrderBy(s => s).ToList();
                SupplierFilterBox.Items.Clear();
                SupplierFilterBox.Items.Add(new ComboBoxItem { Content = "Все поставщики", IsSelected = true });
                foreach (var supplier in suppliers)
                {
                    SupplierFilterBox.Items.Add(new ComboBoxItem { Content = supplier });
                }

                RawMaterialGrid.ItemsSource = batches;
                UpdateStatus($"Загружено: {batches.Count} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async void LoadProductBatches()
        {
            try
            {
                UpdateStatus("Загрузка партий продукции...");
                _currentView = "product";
                ContentTitle.Text = "🏭 Готовая продукция";

                RawMaterialGrid.Visibility = Visibility.Collapsed;
                ProductGrid.Visibility = Visibility.Visible;
                HistoryGrid.Visibility = Visibility.Collapsed;
                SupplierFilterBox.Visibility = Visibility.Collapsed;

                var batches = await _apiService.GetProductBatchesAsync();
                _allProductBatches = batches;

                ProductGrid.ItemsSource = batches;
                UpdateStatus($"Загружено: {batches.Count} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private async void LoadTestHistory()
        {
            try
            {
                UpdateStatus("Загрузка истории испытаний...");
                _currentView = "history";
                ContentTitle.Text = "📋 История испытаний";

                RawMaterialGrid.Visibility = Visibility.Collapsed;
                ProductGrid.Visibility = Visibility.Collapsed;
                HistoryGrid.Visibility = Visibility.Visible;
                SupplierFilterBox.Visibility = Visibility.Collapsed;

                var response = await _apiService.GetTestArchiveAsync();

                if (response?.Data != null && response.Data.Count > 0)
                {
                    var historyList = response.Data.Select(t => new
                    {
                        TestNumber = t.testNumber,
                        TestType = t.testType,
                        ObjectName = t.objectType == "RawMaterial" ? "Сырье" : "Продукция",
                        AssignedAt = t.assignedAt,
                        ExecutedAt = t.executedAt,
                        Status = t.status,
                        result = t.Result
                    }).ToList();

                    HistoryGrid.ItemsSource = historyList;
                    UpdateStatus($"Загружено: {historyList.Count} записей");
                }
                else
                {
                    HistoryGrid.ItemsSource = null;
                    UpdateStatus("Нет данных", false);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка: {ex.Message}", true);
            }
        }

        // ========== ДЕЙСТВИЯ С ПАРТИЯМИ ==========
        private void Batch_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_currentView == "raw" && RawMaterialGrid.SelectedItem is RawMaterialBatch rawBatch)
            {
                OpenRawMaterialCard(rawBatch);
            }
            else if (_currentView == "product" && ProductGrid.SelectedItem is ProductBatch productBatch)
            {
                OpenProductBatchCard(productBatch);
            }
        }

        private void CreateTest_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == "raw" && RawMaterialGrid.SelectedItem is RawMaterialBatch rawBatch)
                OpenRawMaterialCard(rawBatch);
            else if (_currentView == "product" && ProductGrid.SelectedItem is ProductBatch productBatch)
                OpenProductBatchCard(productBatch);
        }

        private void ViewTest_Click(object sender, RoutedEventArgs e)
        {
            // То же, что и создать испытание – открыть карточку партии
            CreateTest_Click(sender, e);
        }

        private void OpenRawMaterialCard(RawMaterialBatch batch)
        {
            var cardWindow = new RawMaterialCardWindow(_apiService, batch, _currentUser);
            cardWindow.Owner = this;
            cardWindow.ShowDialog();
            LoadRawMaterialBatches();
        }

        private void OpenProductBatchCard(ProductBatch batch)
        {
            var cardWindow = new ProductBatchCardWindow(_apiService, batch, _currentUser);
            cardWindow.Owner = this;
            cardWindow.ShowDialog();
            LoadProductBatches();
        }

        // ========== ОБЩИЕ ==========
        private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshCurrentView();

        private void LogoutButton_Click(object sender, RoutedEventArgs e) => Logout();

        private void Logout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }
    }
}
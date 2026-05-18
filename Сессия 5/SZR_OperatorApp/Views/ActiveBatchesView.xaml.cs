using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;

namespace SZR_OperatorApp.Views
{
    public partial class ActiveBatchesView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;
        private ObservableCollection<ActiveBatchCard> _allBatches;

        public event Action<string> BatchSelected;

        public ActiveBatchesView(ApiService apiService, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _currentUser = currentUser;

            Loaded += async (s, e) => await LoadBatches();
        }

        public async void RefreshData()
        {
            await LoadBatches();
        }

        private async Task LoadBatches()
        {
            try
            {
                await LoadProducts();
                var batches = await _apiService.GetActiveBatchesAsync();

                System.Diagnostics.Debug.WriteLine($"Загружено партий: {batches?.Count ?? 0}");

                if (batches != null && batches.Count > 0)
                {
                    _allBatches = new ObservableCollection<ActiveBatchCard>(
     batches.Select(b => new ActiveBatchCard
     {
         Id = b.Id,
         ProductId = b.ProductId,
         BatchNumber = b.BatchNumber,
         ProductName = b.ProductName,
         Line = b.Line,
         CurrentStep = b.CurrentStep,
         CurrentStepName = b.CurrentStepName ?? (b.CurrentStep.HasValue ? "Шаг " + b.CurrentStep.Value : "—"),
         BatchStatus = b.BatchStatus ?? "В работе",
         StepStatus = GetStepStatusText(b.StepStatus),  // ← ДОБАВИТЬ
         HasWarning = b.HasWarning,
         HasCriticalDeviation = b.HasCriticalDeviation,
         StartedAt = b.StartedAt,
         StatusColor = GetStatusColor(b.BatchStatus ?? "В работе")
     }));

                    ApplyFilters();

                }
                else
                {
                    _allBatches = new ObservableCollection<ActiveBatchCard>();
                    BatchesItemsControl.ItemsSource = null;
                    StatusTextBlock.Text = "Нет активных партий";
                    StatusTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadBatches error: {ex.Message}");
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
                StatusTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void ApplyFilters()
        {
            if (_allBatches == null || _allBatches.Count == 0)
            {
                BatchesItemsControl.ItemsSource = null;
                return;
            }

            var filtered = _allBatches.AsEnumerable();

            // Поиск
            string search = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(b =>
                    (b.BatchNumber?.ToLower().Contains(search) ?? false) ||
                    (b.ProductName?.ToLower().Contains(search) ?? false));
            }

            // Фильтр по линии
            if (LineFilterBox?.SelectedItem is ComboBoxItem lineItem && lineItem.Content.ToString() != "Все линии")
            {
                filtered = filtered.Where(b => b.Line == lineItem.Content.ToString());
            }

            // Фильтр по статусу
            if (StatusFilterBox?.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "Все статусы")
            {
                filtered = filtered.Where(b => b.BatchStatus == statusItem.Content.ToString());
            }

            // ФИЛЬТР ПО ПРОДУКТУ
            if (ProductFilterBox?.SelectedItem is ComboBoxItem productItem && productItem.Tag != null)
            {
                int selectedProductId = (int)productItem.Tag;
                System.Diagnostics.Debug.WriteLine($"Фильтр по продукту: ProductId={selectedProductId}");

                filtered = filtered.Where(b => b.ProductId == selectedProductId);
                System.Diagnostics.Debug.WriteLine($"После фильтрации: {filtered.Count()} записей");
            }

            var resultList = filtered.ToList();
            BatchesItemsControl.ItemsSource = resultList;

            // Выводим все ProductId для отладки
            System.Diagnostics.Debug.WriteLine("Все ProductId в списке:");
            foreach (var batch in _allBatches)
            {
                System.Diagnostics.Debug.WriteLine($"Batch: {batch.BatchNumber}, ProductId: {batch.ProductId}, ProductName: {batch.ProductName}");
            }

            if (resultList.Count == 0 && _allBatches.Count > 0)
            {
                StatusTextBlock.Text = "Нет партий, соответствующих фильтрам";
                StatusTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                StatusTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private Brush GetStatusColor(string status)
        {
            switch (status)
            {
                case "В работе": return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // зеленый
                case "Подготовлена": return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // синий
                case "Ожидает контроля": return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // оранжевый
                default: return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // серый
            }
        }

        private async Task LoadProducts()
        {
            try
            {
                
                var products = await _apiService.GetProductsAsync();
                ProductFilterBox.Items.Clear();
                ProductFilterBox.Items.Add(new ComboBoxItem { Content = "Все продукты" });
                foreach (var product in products)
                {
                    ProductFilterBox.Items.Add(new ComboBoxItem { Content = product.Name, Tag = product.Id });
                }
                ProductFilterBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProducts error: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadBatches();
        }

        private void BatchCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var batch = border?.DataContext as ActiveBatchCard;
            if (batch != null)
            {
                System.Diagnostics.Debug.WriteLine($"Выбрана партия: {batch.BatchNumber}");
                BatchSelected?.Invoke(batch.BatchNumber);
            }
        }
        private string GetStepStatusText(string status)
        {
            switch (status)
            {
                case "Не начат":
                    return "⚪ Не начат";
                case "Выполняется":
                    return "🟡 Выполняется";
                case "Завершен":
                    return "🟢 Завершен";
                default:
                    return "⚪ Не начат";
            }
        }

    }

    public class ActiveBatchCard
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string BatchNumber { get; set; }
        public string ProductName { get; set; }
        public string Line { get; set; }
        public int? CurrentStep { get; set; }
        public string CurrentStepName { get; set; }
        public string BatchStatus { get; set; }
        public string StepStatus { get; set; }  // ← ДОБАВИТЬ
        public bool HasWarning { get; set; }
        public bool HasCriticalDeviation { get; set; }
        public DateTime? StartedAt { get; set; }
        public Brush StatusColor { get; set; }
    }
}
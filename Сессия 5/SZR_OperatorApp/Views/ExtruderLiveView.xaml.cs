using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;

namespace SZR_OperatorApp.Views
{
    public partial class ExtruderLiveView : UserControl
    {
        private readonly ApiService _apiService;
        private DispatcherTimer _refreshTimer;
        private string _selectedBatchNumber;
        private List<BatchInfo> _batches;

        public ExtruderLiveView(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;

            Loaded += async (s, e) => await LoadData();

            // Таймер для автообновления каждые 5 секунд
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += async (s, e) => await RefreshTelemetry();
            _refreshTimer.Start();
        }

private async Task LoadData()
{
    try
    {
        StatusText.Text = "Загрузка данных...";
        StatusText.Foreground = Brushes.Orange;

        var activeBatches = await _apiService.GetActiveBatchesAsync();
        if (activeBatches == null || activeBatches.Count == 0)
        {
            StatusText.Text = "Нет активных партий";
            StatusText.Foreground = Brushes.Gray;
            return;
        }

        // Создаём простые объекты только с номером партии
        _batches = activeBatches.Select(b => new BatchInfo { BatchNumber = b.BatchNumber }).ToList();
        BatchComboBox.ItemsSource = _batches;
        BatchComboBox.DisplayMemberPath = "BatchNumber";
        BatchComboBox.SelectedValuePath = "BatchNumber";

        if (_batches.Any())
        {
            BatchComboBox.SelectedIndex = 0;
            _selectedBatchNumber = _batches[0].BatchNumber;
            await LoadTelemetry(_selectedBatchNumber);
        }

        StatusText.Text = "Готов";
        StatusText.Foreground = Brushes.Green;
    }
    catch (Exception ex)
    {
        StatusText.Text = $"Ошибка: {ex.Message}";
        StatusText.Foreground = Brushes.Red;
    }
}

        private async Task LoadTelemetry(string batchNumber)
        {
            try
            {
                var telemetry = await _apiService.GetExtruderTelemetryAsync(batchNumber);

                System.Diagnostics.Debug.WriteLine($"Telemetry count: {telemetry?.Count ?? 0}");

                if (telemetry != null && telemetry.Count > 0)
                {
                    foreach (var item in telemetry)
                    {
                        SetDefaultNormValues(item);
                    }

                    DisplayTelemetry(telemetry);
                    LastUpdateText.Text = $"Последнее обновление: {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Нет данных телеметрии для партии: " + batchNumber);
                    // Показываем сообщение
                    ZonesPanel.Children.Clear();
                    var emptyText = new TextBlock
                    {
                        Text = $"Нет данных телеметрии для партии {batchNumber}",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                        Foreground = Brushes.Gray
                    };
                    ZonesPanel.Children.Add(emptyText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTelemetry error: {ex.Message}");
            }
        }

        private async Task RefreshTelemetry()
        {
            if (!string.IsNullOrEmpty(_selectedBatchNumber))
            {
                await LoadTelemetry(_selectedBatchNumber);
            }
        }

        private void DisplayTelemetry(List<ExtruderTelemetryItem> telemetry)
        {
            ZonesPanel.Children.Clear();

            if (telemetry == null || !telemetry.Any())
            {
                var emptyText = new TextBlock
                {
                    Text = "Нет данных телеметрии",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Foreground = Brushes.Gray
                };
                ZonesPanel.Children.Add(emptyText);
                return;
            }

            var zones = telemetry.GroupBy(t => t.ZoneNumber).OrderBy(g => g.Key);

            foreach (var zone in zones)
            {
                var latest = zone.OrderByDescending(t => t.Timestamp).FirstOrDefault();
                if (latest == null) continue;

                var zoneCard = CreateZoneCard(latest);
                ZonesPanel.Children.Add(zoneCard);
            }
        }

        private Border CreateZoneCard(ExtruderTelemetryItem telemetry)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ZoneCardStyle"),
                Width = 220,
                Height = 180,
                Tag = telemetry.ZoneNumber
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Название зоны
            string zoneName = telemetry.ZoneName ?? GetZoneName(telemetry.ZoneNumber);
            var titleText = new TextBlock
            {
                Text = $"🔥 {zoneName}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            // Температура
            var tempLabel = new TextBlock
            {
                Text = "🌡️ Температура",
                FontSize = 11,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(tempLabel, 1);
            grid.Children.Add(tempLabel);

            var tempValue = new TextBlock
            {
                Text = $"{telemetry.CurrentTemperature}°C",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = GetTemperatureColor(telemetry.CurrentTemperature, telemetry.TemperatureMin, telemetry.TemperatureMax)
            };
            Grid.SetRow(tempValue, 2);
            grid.Children.Add(tempValue);

            // Норма температуры
            if (telemetry.TemperatureMin.HasValue || telemetry.TemperatureMax.HasValue)
            {
                var tempNorm = new TextBlock
                {
                    Text = $"Норма: {telemetry.TemperatureMin?.ToString() ?? "—"} - {telemetry.TemperatureMax?.ToString() ?? "—"}°C",
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                Grid.SetRow(tempNorm, 3);
                grid.Children.Add(tempNorm);
            }

            // Давление
            var pressLabel = new TextBlock
            {
                Text = "💨 Давление",
                FontSize = 11,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(pressLabel, 4);
            grid.Children.Add(pressLabel);

            var pressValue = new TextBlock
            {
                Text = $"{telemetry.CurrentPressure} бар",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = GetPressureColor(telemetry.CurrentPressure, telemetry.PressureMin, telemetry.PressureMax)
            };
            Grid.SetRow(pressValue, 5);
            grid.Children.Add(pressValue);

            // Норма давления
            if (telemetry.PressureMin.HasValue || telemetry.PressureMax.HasValue)
            {
                var pressNorm = new TextBlock
                {
                    Text = $"Норма: {telemetry.PressureMin?.ToString() ?? "—"} - {telemetry.PressureMax?.ToString() ?? "—"} бар",
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                Grid.SetRow(pressNorm, 6);
                grid.Children.Add(pressNorm);
            }

            border.Child = grid;
            return border;
        }

        private string GetZoneName(int zoneNumber)
        {
            switch (zoneNumber)
            {
                case 1: return "Зона загрузки";
                case 2: return "Зона плавления";
                case 3: return "Зона гомогенизации";
                case 4: return "Зона дозирования";
                default: return $"Зона {zoneNumber}";
            }
        }

        private Brush GetTemperatureColor(decimal temp, decimal? min, decimal? max)
        {
            // Если значения нормы не заданы - показываем зелёный
            if (!min.HasValue && !max.HasValue)
                return Brushes.Green;

            // Критическое отклонение (выше максимума)
            if (max.HasValue && temp > max.Value)
                return Brushes.Red;

            // Предупреждение (ниже минимума)
            if (min.HasValue && temp < min.Value)
                return Brushes.Orange;

            // Предупреждение (выше нормы, но ниже критического)
            if (max.HasValue && temp > max.Value * 0.9m)
                return Brushes.Orange;

            return Brushes.Green;
        }

        private Brush GetPressureColor(decimal pressure, decimal? min, decimal? max)
        {
            if (!min.HasValue && !max.HasValue)
                return Brushes.Green;

            if (max.HasValue && pressure > max.Value)
                return Brushes.Red;

            if (min.HasValue && pressure < min.Value)
                return Brushes.Orange;

            if (max.HasValue && pressure > max.Value * 0.9m)
                return Brushes.Orange;

            return Brushes.Green;
        }

        private async void BatchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchComboBox.SelectedItem is BatchInfo selectedBatch)
            {
                _selectedBatchNumber = selectedBatch.BatchNumber;
                await LoadTelemetry(_selectedBatchNumber);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshTelemetry();
        }
        private void SetDefaultNormValues(ExtruderTelemetryItem item)
        {
            switch (item.ZoneNumber)
            {
                case 1:
                    item.TemperatureMin = 140; item.TemperatureMax = 160;
                    item.PressureMin = 45; item.PressureMax = 55;
                    item.ZoneName = "Зона загрузки";
                    break;
                case 2:
                    item.TemperatureMin = 160; item.TemperatureMax = 180;
                    item.PressureMin = 50; item.PressureMax = 60;
                    item.ZoneName = "Зона плавления";
                    break;
                case 3:
                    item.TemperatureMin = 155; item.TemperatureMax = 175;
                    item.PressureMin = 48; item.PressureMax = 56;
                    item.ZoneName = "Зона гомогенизации";
                    break;
                case 4:
                    item.TemperatureMin = 150; item.TemperatureMax = 170;
                    item.PressureMin = 44; item.PressureMax = 52;
                    item.ZoneName = "Зона дозирования";
                    break;
                default:
                    item.ZoneName = $"Зона {item.ZoneNumber}";
                    break;
            }
        }
    }


}
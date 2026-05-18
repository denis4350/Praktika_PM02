using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;

namespace SZR_OperatorApp.Views
{
    public partial class BatchJournalView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;
        private List<BatchInfo> _batches;

        public BatchJournalView(ApiService apiService, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _currentUser = currentUser;

            Loaded += async (s, e) => await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var activeBatches = await _apiService.GetActiveBatchesAsync();
                if (activeBatches == null || activeBatches.Count == 0)
                {
                    _batches = new List<BatchInfo>();
                    return;
                }

                _batches = new List<BatchInfo>();
                foreach (var b in activeBatches)
                {
                    _batches.Add(new BatchInfo
                    {
                        BatchNumber = b.BatchNumber,
                        ProductName = b.ProductName
                    });
                }

                BatchComboBox.ItemsSource = _batches;
                BatchComboBox.DisplayMemberPath = "BatchNumber";

                if (_batches.Any())
                {
                    BatchComboBox.SelectedIndex = 0;
                    await LoadJournal(_batches[0].BatchNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData error: {ex.Message}");
            }
        }

        private async Task LoadJournal(string batchNumber)
        {
            try
            {
                var batchInfo = await _apiService.GetBatchProgramAsync(batchNumber);
                if (batchInfo == null) return;

                ProductText.Text = batchInfo.ProductName;
                LineText.Text = batchInfo.Line;
                StatusText.Text = batchInfo.Status;
                StartedAtText.Text = batchInfo.StartedAt.ToString("dd.MM.yyyy HH:mm");

                // Цвет статуса
                StatusText.Foreground = GetStatusColor(batchInfo.Status);

                // Загружаем события (отклонения и шаги)
                var events = new List<JournalEvent>();

                // Добавляем шаги
                if (batchInfo.Steps != null)
                {
                    foreach (var step in batchInfo.Steps)
                    {
                        if (step.StartedAt.HasValue)
                        {
                            events.Add(new JournalEvent
                            {
                                EventTime = step.StartedAt.Value,
                                StepNumber = step.StepNumber,
                                EventType = "Начало шага",
                                Description = $"Начат шаг {step.StepNumber}: {step.Name}",
                                Severity = "Информация"
                            });
                        }

                        if (step.FinishedAt.HasValue)
                        {
                            events.Add(new JournalEvent
                            {
                                EventTime = step.FinishedAt.Value,
                                StepNumber = step.StepNumber,
                                EventType = "Завершение шага",
                                Description = $"Завершён шаг {step.StepNumber}: {step.Name}",
                                Severity = "Информация"
                            });
                        }
                    }
                }

                // Добавляем отклонения
                var deviations = await _apiService.GetDeviationsAsync(batchInfo.Id);
                if (deviations != null)
                {
                    foreach (var dev in deviations)
                    {
                        events.Add(new JournalEvent
                        {
                            EventTime = dev.CreatedAt,
                            StepNumber = dev.StepNumber ?? 0,
                            EventType = "Отклонение",
                            Description = dev.Description,
                            Severity = dev.Severity
                        });
                    }
                }

                JournalGrid.ItemsSource = events.OrderByDescending(e => e.EventTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadJournal error: {ex.Message}");
            }
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

        private async void BatchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchComboBox.SelectedItem is BatchInfo selectedBatch)
            {
                await LoadJournal(selectedBatch.BatchNumber);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatchComboBox.SelectedItem is BatchInfo selectedBatch)
            {
                await LoadJournal(selectedBatch.BatchNumber);
            }
        }
    }

    public class JournalEvent
    {
        public DateTime EventTime { get; set; }
        public int StepNumber { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }
}
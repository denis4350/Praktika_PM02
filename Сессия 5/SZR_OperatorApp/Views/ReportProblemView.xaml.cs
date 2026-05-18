using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;

namespace SZR_OperatorApp.Views
{
    public partial class ReportProblemView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;
        private List<BatchInfo> _batches;

        public ReportProblemView(ApiService apiService, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _currentUser = currentUser;

            Loaded += async (s, e) => {
                await LoadBatches();
                await LoadEquipment();   // <-- добавьте эту строку
            };

        }

        private async Task LoadBatches()
        {
            try
            {
                var activeBatches = await _apiService.GetActiveBatchesAsync();
                if (activeBatches == null || activeBatches.Count == 0)
                    return;

                _batches = activeBatches.Select(b => new BatchInfo { BatchNumber = b.BatchNumber }).ToList();
                BatchBox.ItemsSource = _batches;

                BatchBox.ItemsSource = _batches;
                BatchBox.DisplayMemberPath = "BatchNumber";
                BatchBox.SelectedValuePath = "BatchNumber";

                if (_batches.Any())
                    BatchBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadBatches error: {ex.Message}");
            }
        }
        private async Task LoadEquipment()
        {
            try
            {
                var equipmentList = await _apiService.GetEquipmentAsync();
                EquipmentBox.ItemsSource = equipmentList;
                EquipmentBox.DisplayMemberPath = "Name";
            }
            catch { /* оставить статический список */ }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
                {
                    MessageBox.Show("Введите описание проблемы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var problemType = (ProblemTypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var batchNumber = BatchBox.SelectedValue?.ToString();
                var equipment = (EquipmentBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var severity = (SeverityBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                var result = await _apiService.ReportProblemAsync(
                    batchNumber,
                    problemType,
                    equipment,
                    DescriptionBox.Text,
                    severity,
                    _currentUser.Id
                );

                if (result)
                {
                    MessageBox.Show("Сообщение о проблеме отправлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем счетчик уведомлений в главном окне
                    var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    mainWindow?.UpdateNotificationsBadge();

                    // Очищаем форму
                    DescriptionBox.Text = "";
                    ProblemTypeBox.SelectedIndex = 0;
                    SeverityBox.SelectedIndex = 0;
                    EquipmentBox.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Ошибка при отправке сообщения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            DescriptionBox.Text = "";
            ProblemTypeBox.SelectedIndex = 0;
            SeverityBox.SelectedIndex = 0;
            if (EquipmentBox.Items.Count > 0)
                EquipmentBox.SelectedIndex = 0;
        }
    }
}
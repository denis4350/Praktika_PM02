using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ComponentDialog : Window
    {
        private readonly ApiService _apiService;
        private List<RawMaterialDto> _materials;
        private bool _isLoading;

        public ComponentItemDto Component { get; private set; }

        public ComponentDialog(ApiService apiService)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _materials = new List<RawMaterialDto>();

            Loaded += ComponentDialog_Loaded;
        }

        private async void ComponentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMaterialsAsync();
        }

        private async Task LoadMaterialsAsync()
        {
            if (_isLoading)
                return;

            try
            {
                HideError();
                SetLoadingState(true);

                _materials = await _apiService.GetRawMaterialsAsync(true) ?? new List<RawMaterialDto>();

                MaterialComboBox.ItemsSource = _materials;

                if (_materials.Any())
                {
                    MaterialComboBox.SelectedIndex = 0;
                    PercentageBox.Focus();
                }
                else
                {
                    ShowError("Нет активного сырья. Сначала добавьте сырьё в справочник.");
                    OkButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки сырья: " + ex.Message);
                OkButton.IsEnabled = false;
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SaveComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveComponent();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void SaveComponent()
        {
            HideError();

            var selectedMaterial = MaterialComboBox.SelectedItem as RawMaterialDto;

            if (selectedMaterial == null)
            {
                ShowError("Выберите компонент.");
                MaterialComboBox.Focus();
                return;
            }

            if (!TryParseDecimal(PercentageBox.Text, out decimal percentage))
            {
                ShowError("Введите корректную долю. Можно использовать запятую или точку.");
                PercentageBox.Focus();
                PercentageBox.SelectAll();
                return;
            }

            if (percentage <= 0 || percentage > 100)
            {
                ShowError("Доля должна быть больше 0 и не больше 100.");
                PercentageBox.Focus();
                PercentageBox.SelectAll();
                return;
            }

            decimal? toleranceMin = null;
            decimal? toleranceMax = null;

            if (!string.IsNullOrWhiteSpace(ToleranceMinBox.Text))
            {
                if (!TryParseDecimal(ToleranceMinBox.Text, out decimal min))
                {
                    ShowError("Введите корректный минимальный допуск.");
                    ToleranceMinBox.Focus();
                    ToleranceMinBox.SelectAll();
                    return;
                }

                toleranceMin = min;
            }

            if (!string.IsNullOrWhiteSpace(ToleranceMaxBox.Text))
            {
                if (!TryParseDecimal(ToleranceMaxBox.Text, out decimal max))
                {
                    ShowError("Введите корректный максимальный допуск.");
                    ToleranceMaxBox.Focus();
                    ToleranceMaxBox.SelectAll();
                    return;
                }

                toleranceMax = max;
            }

            if (toleranceMin.HasValue && toleranceMax.HasValue && toleranceMin.Value > toleranceMax.Value)
            {
                ShowError("Минимальный допуск не может быть больше максимального.");
                ToleranceMinBox.Focus();
                ToleranceMinBox.SelectAll();
                return;
            }

            int loadOrder;

            if (string.IsNullOrWhiteSpace(LoadOrderBox.Text))
            {
                loadOrder = 1;
            }
            else if (!int.TryParse(LoadOrderBox.Text.Trim(), out loadOrder))
            {
                ShowError("Порядок загрузки должен быть целым числом.");
                LoadOrderBox.Focus();
                LoadOrderBox.SelectAll();
                return;
            }

            if (loadOrder <= 0)
            {
                ShowError("Порядок загрузки должен быть больше 0.");
                LoadOrderBox.Focus();
                LoadOrderBox.SelectAll();
                return;
            }

            Component = new ComponentItemDto
            {
                RawMaterialId = selectedMaterial.Id,
                RawMaterialName = selectedMaterial.Name,
                Percentage = percentage,
                ToleranceMin = toleranceMin,
                ToleranceMax = toleranceMax,
                LoadOrder = loadOrder
            };

            DialogResult = true;
            Close();
        }

        private bool TryParseDecimal(string text, out decimal value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string normalized = text.Trim().Replace(',', '.');

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value);
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            MaterialComboBox.IsEnabled = !isLoading;
            PercentageBox.IsEnabled = !isLoading;
            ToleranceMinBox.IsEnabled = !isLoading;
            ToleranceMaxBox.IsEnabled = !isLoading;
            LoadOrderBox.IsEnabled = !isLoading;

            OkButton.IsEnabled = !isLoading;
            CancelButton.IsEnabled = !isLoading;

            OkButton.Content = isLoading ? "Загрузка..." : "Добавить";
            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
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
    }
}
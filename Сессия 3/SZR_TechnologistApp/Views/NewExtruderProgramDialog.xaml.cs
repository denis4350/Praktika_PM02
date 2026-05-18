using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class NewExtruderProgramDialog : Window
    {
        private readonly ApiService _apiService;
        private readonly ObservableCollection<ExtruderZoneItem> _zones;
        private List<ProductDto> _products;
        private bool _isSaving;
        private bool _isLoading;

        public NewExtruderProgramDialog(ApiService apiService)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _products = new List<ProductDto>();

            _zones = new ObservableCollection<ExtruderZoneItem>
            {
                new ExtruderZoneItem { ZoneNumber = 1, ZoneName = "Зона 1 - Загрузка", TemperatureSetpoint = 150, TemperatureMin = 140, TemperatureMax = 160, PressureSetpoint = 50, PressureMin = 45, PressureMax = 55, ScrewSpeed = 300, FeedRate = 100 },
                new ExtruderZoneItem { ZoneNumber = 2, ZoneName = "Зона 2 - Плавление", TemperatureSetpoint = 170, TemperatureMin = 160, TemperatureMax = 180, PressureSetpoint = 55, PressureMin = 50, PressureMax = 60, ScrewSpeed = 320, FeedRate = 100 },
                new ExtruderZoneItem { ZoneNumber = 3, ZoneName = "Зона 3 - Гомогенизация", TemperatureSetpoint = 165, TemperatureMin = 155, TemperatureMax = 175, PressureSetpoint = 52, PressureMin = 48, PressureMax = 56, ScrewSpeed = 310, FeedRate = 100 },
                new ExtruderZoneItem { ZoneNumber = 4, ZoneName = "Зона 4 - Дозирование", TemperatureSetpoint = 160, TemperatureMin = 150, TemperatureMax = 170, PressureSetpoint = 48, PressureMin = 44, PressureMax = 52, ScrewSpeed = 290, FeedRate = 100 }
            };

            ZonesGrid.ItemsSource = _zones;

            Loaded += NewExtruderProgramDialog_Loaded;
        }

        private async void NewExtruderProgramDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
            NameBox.Focus();
        }

        private async Task LoadProductsAsync()
        {
            if (_isLoading) return;

            try
            {
                HideError();
                SetLoadingState(true);

                var result = await _apiService.GetProductsAsync(1, 100);
                _products = result?.Items ?? new List<ProductDto>();
                ProductComboBox.ItemsSource = _products;

                if (_products.Any())
                    ProductComboBox.SelectedIndex = 0;
                else
                {
                    ShowError("Нет активных продуктов. Сначала добавьте продукт в справочник.");
                    SaveButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки продуктов: " + ex.Message);
                SaveButton.IsEnabled = false;
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void AddDefaultZoneButton_Click(object sender, RoutedEventArgs e)
        {
            int nextNumber = _zones.Any() ? _zones.Max(z => z.ZoneNumber) + 1 : 1;
            _zones.Add(new ExtruderZoneItem
            {
                ZoneNumber = nextNumber,
                ZoneName = "Зона " + nextNumber,
                TemperatureSetpoint = 160,
                TemperatureMin = 150,
                TemperatureMax = 170,
                PressureSetpoint = 50,
                PressureMin = 45,
                PressureMax = 55,
                ScrewSpeed = 300,
                FeedRate = 100
            });
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e) => await SaveProgramAsync();

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await SaveProgramAsync();
                e.Handled = true;
            }
        }

        private async Task SaveProgramAsync()
        {
            if (_isSaving) return;

            try
            {
                HideError();
                CommitGridEdit();

                if (!ValidateForm(out List<ExtruderZoneItem> validZones))
                    return;

                var selectedProduct = ProductComboBox.SelectedItem as ProductDto;

                // Правильный DTO
                var createDto = new CreateExtruderProgramDto
                {
                    Name = NameBox.Text.Trim(),
                    Description = DescriptionBox.Text?.Trim(),
                    ProductId = selectedProduct.Id,
                    Zones = validZones
                        .OrderBy(z => z.ZoneNumber)
                        .Select(z => new ExtruderZoneDto
                        {
                            ZoneNumber = z.ZoneNumber,
                            ZoneName = z.ZoneName?.Trim(),
                            TemperatureSetpoint = z.TemperatureSetpoint,
                            TemperatureMin = z.TemperatureMin,
                            TemperatureMax = z.TemperatureMax,
                            PressureSetpoint = z.PressureSetpoint,
                            PressureMin = z.PressureMin,
                            PressureMax = z.PressureMax,
                            ScrewSpeed = z.ScrewSpeed,
                            FeedRate = z.FeedRate
                        })
                        .ToList()
                };

                SetSavingState(true);

                var result = await _apiService.CreateExtruderProgramAsync(createDto);

                if (result != null)
                {
                    MessageBox.Show(
                        "Программа экструдера успешно создана.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError("Сервер не вернул подтверждение создания программы.");
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения программы: " + ex.Message);
            }
            finally
            {
                SetSavingState(false);
            }
        }

        private bool ValidateForm(out List<ExtruderZoneItem> validZones)
        {
            validZones = new List<ExtruderZoneItem>();

            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                ShowError("Введите название программы.");
                NameBox.Focus();
                return false;
            }

            if (ProductComboBox.SelectedItem == null)
            {
                ShowError("Выберите продукт.");
                ProductComboBox.Focus();
                return false;
            }

            validZones = _zones
                .Where(z => z != null)
                .Where(z => z.ZoneNumber > 0 || !string.IsNullOrWhiteSpace(z.ZoneName) || z.TemperatureSetpoint != 0 || z.PressureSetpoint != 0)
                .ToList();

            if (!validZones.Any())
            {
                ShowError("Добавьте хотя бы одну зону экструдера.");
                return false;
            }

            var duplicateZones = validZones.GroupBy(z => z.ZoneNumber).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateZones.Any())
            {
                ShowError("Повторяются номера зон: " + string.Join(", ", duplicateZones) + ".");
                return false;
            }

            foreach (var zone in validZones)
            {
                if (zone.ZoneNumber <= 0)
                {
                    ShowError("Номер зоны должен быть больше 0.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(zone.ZoneName))
                {
                    ShowError("У зоны №" + zone.ZoneNumber + " не указано название.");
                    return false;
                }
                if (zone.TemperatureMin > zone.TemperatureSetpoint || zone.TemperatureSetpoint > zone.TemperatureMax)
                {
                    ShowError("У зоны №" + zone.ZoneNumber + " температура должна быть в диапазоне: min ≤ уставка ≤ max.");
                    return false;
                }
                if (zone.PressureMin > zone.PressureSetpoint || zone.PressureSetpoint > zone.PressureMax)
                {
                    ShowError("У зоны №" + zone.ZoneNumber + " давление должно быть в диапазоне: min ≤ уставка ≤ max.");
                    return false;
                }
                if (zone.ScrewSpeed <= 0)
                {
                    ShowError("У зоны №" + zone.ZoneNumber + " скорость шнека должна быть больше 0.");
                    return false;
                }
                if (zone.FeedRate <= 0)
                {
                    ShowError("У зоны №" + zone.ZoneNumber + " скорость подачи должна быть больше 0.");
                    return false;
                }
            }

            return true;
        }

        private void CommitGridEdit()
        {
            ZonesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ZonesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            ProductComboBox.IsEnabled = !isLoading;
            SaveButton.IsEnabled = !isLoading;
            AddDefaultZoneButton.IsEnabled = !isLoading;
            CancelButton.IsEnabled = !isLoading;
            SaveButton.Content = isLoading ? "Загрузка..." : "Сохранить";
            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void SetSavingState(bool isSaving)
        {
            _isSaving = isSaving;
            NameBox.IsEnabled = !isSaving;
            DescriptionBox.IsEnabled = !isSaving;
            ProductComboBox.IsEnabled = !isSaving;
            ZonesGrid.IsEnabled = !isSaving;
            AddDefaultZoneButton.IsEnabled = !isSaving;
            SaveButton.IsEnabled = !isSaving;
            CancelButton.IsEnabled = !isSaving;
            SaveButton.Content = isSaving ? "Сохранение..." : "Сохранить";
            Cursor = isSaving ? Cursors.Wait : Cursors.Arrow;
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

    public class ExtruderZoneItem
    {
        public int ZoneNumber { get; set; }
        public string ZoneName { get; set; }
        public decimal TemperatureSetpoint { get; set; }
        public decimal TemperatureMin { get; set; }
        public decimal TemperatureMax { get; set; }
        public decimal PressureSetpoint { get; set; }
        public decimal PressureMin { get; set; }
        public decimal PressureMax { get; set; }
        public int ScrewSpeed { get; set; }
        public int FeedRate { get; set; }

        public ExtruderZoneItem()
        {
            ZoneName = "";
            FeedRate = 100;
            ScrewSpeed = 300;
        }
    }
}
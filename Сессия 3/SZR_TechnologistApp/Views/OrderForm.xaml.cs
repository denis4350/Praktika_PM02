using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class OrderForm : Window
    {
        private readonly ApiService _apiService;
        private readonly object _editingOrder;

        private List<ProductDto> _products;
        private bool _isLoading;
        private bool _isSaving;

        public OrderForm(ApiService apiService) : this(apiService, null)
        {
        }

        public OrderForm(ApiService apiService, object order)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _editingOrder = order;
            _products = new List<ProductDto>();

            if (_editingOrder == null)
            {
                Title = "Создание производственного заказа";
                TitleText.Text = "Создание заказа";
                SubtitleText.Text = "Выберите продукт, количество и плановую дату запуска.";
                SaveButton.Content = "Создать";
                StartDatePicker.SelectedDate = DateTime.Now.AddDays(7);
            }
            else
            {
                Title = "Редактирование производственного заказа";
                TitleText.Text = "Редактирование заказа";
                SubtitleText.Text = "Измените количество и плановую дату запуска. Продукт в существующем заказе не меняется.";
                SaveButton.Content = "Сохранить";
                LoadOrderData();
            }

            Loaded += OrderForm_Loaded;
        }

        private async void OrderForm_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
        }

        private void LoadOrderData()
        {
            decimal quantity = GetPropertyValue<decimal>(_editingOrder, "PlannedQuantity", "plannedQuantity");
            QuantityBox.Text = quantity > 0
                ? quantity.ToString(CultureInfo.CurrentCulture)
                : "";

            DateTime? startDate = GetPropertyValue<DateTime?>(_editingOrder, "PlannedStartDate", "plannedStartDate");

            if (startDate.HasValue)
                StartDatePicker.SelectedDate = startDate.Value;
            else
                StartDatePicker.SelectedDate = DateTime.Now.AddDays(7);
        }

        private async Task LoadProductsAsync()
        {
            if (_isLoading)
                return;

            try
            {
                HideError();
                SetLoadingState(true);

                var result = await _apiService.GetProductsAsync(1, 100);

                _products = result?.Items ?? new List<ProductDto>();
                ProductComboBox.ItemsSource = _products;

                if (!_products.Any())
                {
                    ShowError("Нет доступных продуктов. Сначала добавьте продукт в справочник.");
                    SaveButton.IsEnabled = false;
                    return;
                }

                if (_editingOrder == null)
                {
                    ProductComboBox.SelectedIndex = 0;
                }
                else
                {
                    int productId = GetPropertyValue<int>(_editingOrder, "ProductId", "productId");

                    ProductDto selectedProduct = _products.FirstOrDefault(p => p.Id == productId);

                    if (selectedProduct != null)
                        ProductComboBox.SelectedItem = selectedProduct;
                    else
                        ProductComboBox.SelectedIndex = 0;

                    ProductComboBox.IsEnabled = false;
                }

                QuantityBox.Focus();
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveOrderAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SaveOrderAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        private async Task SaveOrderAsync()
        {
            if (_isSaving)
                return;

            try
            {
                HideError();

                ProductDto product = ProductComboBox.SelectedItem as ProductDto;

                if (product == null)
                {
                    ShowError("Выберите продукт.");
                    ProductComboBox.Focus();
                    return;
                }

                if (!TryParseDecimal(QuantityBox.Text, out decimal quantity))
                {
                    ShowError("Введите корректное количество. Можно использовать запятую или точку.");
                    QuantityBox.Focus();
                    QuantityBox.SelectAll();
                    return;
                }

                if (quantity <= 0)
                {
                    ShowError("Количество должно быть больше 0.");
                    QuantityBox.Focus();
                    QuantityBox.SelectAll();
                    return;
                }

                if (quantity > 1000000)
                {
                    ShowError("Количество слишком большое. Проверьте введённое значение.");
                    QuantityBox.Focus();
                    QuantityBox.SelectAll();
                    return;
                }

                DateTime? plannedStartDate = StartDatePicker.SelectedDate;

                if (!plannedStartDate.HasValue)
                {
                    ShowError("Выберите плановую дату запуска.");
                    StartDatePicker.Focus();
                    return;
                }

                SetSavingState(true);

                if (_editingOrder == null)
                {
                    var created = await _apiService.CreateOrderAsync(new CreateOrderDto
                    {
                        ProductId = product.Id,
                        PlannedQuantity = quantity,
                        PlannedStartDate = plannedStartDate
                    });

                    if (created == null)
                    {
                        ShowError("Сервер не подтвердил создание заказа.");
                        return;
                    }

                    MessageBox.Show(
                        "Производственный заказ успешно создан.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    int orderId = GetPropertyValue<int>(_editingOrder, "Id", "id");

                    if (orderId <= 0)
                    {
                        ShowError("Не удалось определить ID заказа.");
                        return;
                    }

                    var updated = await _apiService.UpdateOrderAsync(orderId, new UpdateOrderDto
                    {
                        PlannedQuantity = quantity,
                        PlannedStartDate = plannedStartDate
                    });

                    if (updated == null)
                    {
                        ShowError("Сервер не подтвердил обновление заказа.");
                        return;
                    }

                    MessageBox.Show(
                        "Производственный заказ успешно обновлён.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения заказа: " + ex.Message);
            }
            finally
            {
                SetSavingState(false);
            }
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

        private T GetPropertyValue<T>(object obj, params string[] propNames)
        {
            if (obj == null || propNames == null)
                return default(T);

            foreach (string propName in propNames)
            {
                try
                {
                    PropertyInfo property = obj.GetType().GetProperty(propName);

                    if (property == null)
                        continue;

                    object value = property.GetValue(obj, null);

                    if (value == null)
                        continue;

                    if (value is T typedValue)
                        return typedValue;

                    Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                    object convertedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

                    return (T)convertedValue;
                }
                catch
                {
                    // Переходим к следующему имени свойства.
                }
            }

            return default(T);
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            ProductComboBox.IsEnabled = !isLoading && _editingOrder == null;
            QuantityBox.IsEnabled = !isLoading;
            StartDatePicker.IsEnabled = !isLoading;
            SaveButton.IsEnabled = !isLoading;
            CancelButton.IsEnabled = !isLoading;

            SaveButton.Content = isLoading
                ? "Загрузка..."
                : (_editingOrder == null ? "Создать" : "Сохранить");

            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void SetSavingState(bool isSaving)
        {
            _isSaving = isSaving;

            ProductComboBox.IsEnabled = !isSaving && _editingOrder == null;
            QuantityBox.IsEnabled = !isSaving;
            StartDatePicker.IsEnabled = !isSaving;
            SaveButton.IsEnabled = !isSaving;
            CancelButton.IsEnabled = !isSaving;

            SaveButton.Content = isSaving
                ? "Сохранение..."
                : (_editingOrder == null ? "Создать" : "Сохранить");

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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ProductForm : Window
    {
        private readonly ApiService _apiService;
        private readonly ProductDto _editingProduct; // null = создание, не null = редактирование
        private bool _isSaving;

        public ProductForm(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _editingProduct = null;



            Loaded += async (s, e) => await LoadReferenceDataAsync();
        }

        public ProductForm(ApiService apiService, ProductDto product) : this(apiService)
        {
            _editingProduct = product;

            if (product != null)
            {
                CodeBox.Text = product.Code;
                NameBox.Text = product.Name;
                // ComboBox'ы заполнятся после загрузки справочников, затем выставим значения
                Loaded += (s, e) =>
                {
                    SetComboBoxValue(TypeBox, product.ProductType);
                    SetComboBoxValue(FormBox, product.Form);
                    SetComboBoxValue(StatusBox, product.Status);
                };
            }
        }

        private async Task LoadReferenceDataAsync()
        {
            try
            {
                // Загружаем справочники из ReferenceController
                var referenceResponse = await _apiService.GetAsync<ApiResponse<object>>("api/reference/all");
                if (referenceResponse?.Data == null) return;

                // Используем dynamic для удобства
                dynamic refData = Newtonsoft.Json.Linq.JToken.FromObject(referenceResponse.Data);

                // Типы продуктов
                if (refData.dictionaries?.productTypes != null)
                {
                    TypeBox.Items.Clear();
                    foreach (string type in refData.dictionaries.productTypes)
                        TypeBox.Items.Add(new ComboBoxItem { Content = type });
                }

                // Формы продуктов
                if (refData.dictionaries?.productForms != null)
                {
                    FormBox.Items.Clear();
                    foreach (string form in refData.dictionaries.productForms)
                        FormBox.Items.Add(new ComboBoxItem { Content = form });
                }

                // Статусы продуктов (из словаря)
                if (refData.statuses?.productStatuses != null)
                {
                    StatusBox.Items.Clear();
                    foreach (string status in refData.statuses.productStatuses)
                    {
                        // Архивный статус не предлагаем при создании нового продукта
                        if (_editingProduct == null && status.Equals("Архивирован", StringComparison.OrdinalIgnoreCase))
                            continue;
                        StatusBox.Items.Add(new ComboBoxItem { Content = status });
                    }
                }

                // Устанавливаем значения по умолчанию
                if (TypeBox.Items.Count > 0 && TypeBox.SelectedItem == null)
                    TypeBox.SelectedIndex = 0;
                if (FormBox.Items.Count > 0 && FormBox.SelectedItem == null)
                    FormBox.SelectedIndex = 0;
                if (StatusBox.Items.Count > 0 && StatusBox.SelectedItem == null)
                    StatusBox.SelectedIndex = 0;
            }
            catch
            {
                // В случае ошибки оставляем статические элементы
                if (TypeBox.Items.Count == 0) TypeBox.Items.Add(new ComboBoxItem { Content = "Гранулы" });
                if (FormBox.Items.Count == 0) FormBox.Items.Add(new ComboBoxItem { Content = "Сухая" });
                if (StatusBox.Items.Count == 0) StatusBox.Items.Add(new ComboBoxItem { Content = "Активен" });
            }
        }

        private void SetComboBoxValue(ComboBox combo, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;

            try
            {
                if (string.IsNullOrWhiteSpace(CodeBox.Text))
                {
                    MessageBox.Show("Введите код продукта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    MessageBox.Show("Введите наименование продукта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SetSavingState(true);

                if (_editingProduct == null)
                {
                    // Создание
                    var createDto = new CreateProductDto
                    {
                        Code = CodeBox.Text.Trim(),
                        Name = NameBox.Text.Trim(),
                        ProductType = (TypeBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        Form = (FormBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                    };
                    await _apiService.CreateProductAsync(createDto);
                    MessageBox.Show("Продукт успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Редактирование
                    var updateDto = new UpdateProductDto
                    {
                        Name = NameBox.Text.Trim(),
                        ProductType = (TypeBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        Form = (FormBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        Status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                    };
                    await _apiService.UpdateProductAsync(_editingProduct.Id, updateDto);
                    MessageBox.Show("Продукт успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetSavingState(false);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SetSavingState(bool isSaving)
        {
            _isSaving = isSaving;
            SaveButton.IsEnabled = !isSaving;
            CancelButton.IsEnabled = !isSaving;
            CodeBox.IsEnabled = !isSaving;
            NameBox.IsEnabled = !isSaving;
            TypeBox.IsEnabled = !isSaving;
            FormBox.IsEnabled = !isSaving;
            StatusBox.IsEnabled = !isSaving;
            SaveButton.Content = isSaving ? "Сохранение..." : "Сохранить";
            Cursor = isSaving ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }
    }
}
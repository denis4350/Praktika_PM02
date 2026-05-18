using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class RecipeForm : Window
    {
        private readonly ApiService _apiService;
        private readonly object _editingRecipe;

        private readonly ObservableCollection<ComponentItemDto> _components;
        private List<ProductDto> _products;

        private bool _isLoading;
        private bool _isSaving;

        public RecipeForm(ApiService apiService) : this(apiService, null)
        {
        }

        public RecipeForm(ApiService apiService, dynamic recipe)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _editingRecipe = recipe;

            _products = new List<ProductDto>();
            _components = new ObservableCollection<ComponentItemDto>();

            ComponentsGrid.ItemsSource = _components;

            if (_editingRecipe == null)
            {
                Title = "Создание рецептуры";
                TitleText.Text = "Создание рецептуры";
                SubtitleText.Text = "Выберите продукт, задайте версию и добавьте компоненты рецептуры.";
                SaveButton.Content = "Создать";
            }
            else
            {
                Title = "Редактирование рецептуры";
                TitleText.Text = "Редактирование рецептуры";
                SubtitleText.Text = "Изменение состава рецептуры. Продукт и версия уже созданной рецептуры не изменяются.";
                SaveButton.Content = "Сохранить";

                ProductComboBox.IsEnabled = false;
                VersionBox.IsEnabled = false;
            }

            Loaded += RecipeForm_Loaded;
        }

        private async void RecipeForm_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();

            if (_editingRecipe != null)
                await LoadRecipeForEditAsync();

            UpdateTotalPercentage();
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
                    ShowError("Нет активных продуктов. Сначала добавьте продукт в справочник.");
                    SaveButton.IsEnabled = false;
                    return;
                }

                if (_editingRecipe == null)
                {
                    ProductComboBox.SelectedIndex = 0;
                    VersionBox.Focus();
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

        private async Task LoadRecipeForEditAsync()
        {
            try
            {
                HideError();

                int recipeId = GetPropertyValue<int>(_editingRecipe, "Id", "id");

                if (recipeId <= 0)
                {
                    ShowError("Не удалось определить ID рецептуры.");
                    return;
                }

                string version = GetPropertyValue<string>(_editingRecipe, "Version", "version");
                int productId = GetPropertyValue<int>(_editingRecipe, "ProductId", "productId");

                VersionBox.Text = string.IsNullOrWhiteSpace(version) ? "" : version;

                ProductDto selectedProduct = _products.FirstOrDefault(p => p.Id == productId);

                if (selectedProduct != null)
                    ProductComboBox.SelectedItem = selectedProduct;

                object recipeData = await _apiService.GetRecipeByIdAsync(recipeId);

                IEnumerable<object> componentSource = null;

                if (recipeData != null)
                    componentSource = GetCollection(recipeData, "Components", "components");

                if (componentSource == null)
                    componentSource = GetCollection(_editingRecipe, "Components", "components");

                _components.Clear();

                foreach (ComponentItemDto component in ToComponentItems(componentSource))
                {
                    _components.Add(component);
                }

                UpdateTotalPercentage();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки рецептуры: " + ex.Message);
            }
        }

        private void AddComponentButton_Click(object sender, RoutedEventArgs e)
        {

            AddComponent();
        }

        private void AddComponent()
        {
            try
            {
                HideError();

                var dialog = new ComponentDialog(_apiService) { Owner = this };

                if (dialog.ShowDialog() == true && dialog.Component != null)
                {
                    ComponentItemDto existing = _components
                        .FirstOrDefault(c => c.RawMaterialId == dialog.Component.RawMaterialId);

                    if (existing != null)
                    {
                        ShowError("Этот компонент уже добавлен в рецептуру.");
                        return;
                    }

                    if (dialog.Component.LoadOrder <= 0)
                        dialog.Component.LoadOrder = _components.Count + 1;

                    _components.Add(dialog.Component);
                    UpdateTotalPercentage();
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка добавления компонента: " + ex.Message);
            }
        }

        private void RemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var component = button.DataContext as ComponentItemDto;
            if (component == null) return;

            if (MessageBox.Show(
                    "Удалить компонент?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _components.Remove(component);  // ObservableCollection автоматически обновляет DataGrid
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveRecipeAsync();
        }

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
                await SaveRecipeAsync();
                e.Handled = true;
            }
        }

        private void ComponentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(UpdateTotalPercentage));
        }

        private void ComponentsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            ComponentsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            UpdateTotalPercentage();
        }

        private async Task SaveRecipeAsync()
        {
            if (_isSaving)
                return;

            try
            {
                HideError();
                CommitGridEdit();

                if (!ValidateForm())
                    return;

                ProductDto product = ProductComboBox.SelectedItem as ProductDto;
                string version = VersionBox.Text.Trim();

                SetSavingState(true);

                if (_editingRecipe == null)
                {
                    object recipe = await _apiService.CreateRecipeAsync(new CreateRecipeDto
                    {
                        ProductId = product.Id,
                        Version = version
                    });

                    int recipeId = GetPropertyValue<int>(recipe, "Id", "id");

                    if (recipeId <= 0)
                    {
                        ShowError("Сервер не вернул ID созданной рецептуры.");
                        return;
                    }

                    await SaveComponentsAsync(recipeId);

                    MessageBox.Show(
                        "Рецептура успешно создана.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    int recipeId = GetPropertyValue<int>(_editingRecipe, "Id", "id");

                    if (recipeId <= 0)
                    {
                        ShowError("Не удалось определить ID рецептуры.");
                        return;
                    }

                    await SaveComponentsAsync(recipeId);

                    MessageBox.Show(
                        "Рецептура успешно обновлена.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения рецептуры: " + ex.Message);
            }
            finally
            {
                SetSavingState(false);
            }
        }

        private async Task SaveComponentsAsync(int recipeId)
        {
            NormalizeLoadOrder();

            var componentsDto = new UpdateRecipeComponentsDto
            {
                Components = _components
                    .OrderBy(c => c.LoadOrder)
                    .Select((c, index) => new RecipeComponentDto
                    {
                        RawMaterialId = c.RawMaterialId,
                        Percentage = c.Percentage,
                        ToleranceMin = c.ToleranceMin,
                        ToleranceMax = c.ToleranceMax,
                        LoadOrder = index + 1
                    })
                    .ToArray()
            };

            bool success = await _apiService.UpdateRecipeComponentsAsync(recipeId, componentsDto);

            if (!success)
                throw new Exception("Сервер не подтвердил сохранение компонентов рецептуры.");
        }

        private bool ValidateForm()
        {
            if (ProductComboBox.SelectedItem == null)
            {
                ShowError("Выберите продукт.");
                ProductComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(VersionBox.Text))
            {
                ShowError("Введите версию рецептуры.");
                VersionBox.Focus();
                return false;
            }

            if (!_components.Any())
            {
                ShowError("Добавьте хотя бы один компонент.");
                return false;
            }

            foreach (ComponentItemDto component in _components)
            {
                if (component.RawMaterialId <= 0)
                {
                    ShowError("В рецептуре есть компонент без сырья.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(component.RawMaterialName))
                {
                    ShowError("В рецептуре есть компонент без названия сырья.");
                    return false;
                }

                if (component.Percentage <= 0 || component.Percentage > 100)
                {
                    ShowError("Доля компонента должна быть больше 0 и не больше 100%.");
                    return false;
                }

                if (component.ToleranceMin.HasValue &&
                    component.ToleranceMax.HasValue &&
                    component.ToleranceMin.Value > component.ToleranceMax.Value)
                {
                    ShowError("Минимальный допуск не может быть больше максимального.");
                    return false;
                }

                if (component.LoadOrder <= 0)
                {
                    ShowError("Порядок загрузки должен быть больше 0.");
                    return false;
                }
            }

            var duplicates = _components
                .GroupBy(c => c.RawMaterialId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First().RawMaterialName)
                .ToList();

            if (duplicates.Any())
            {
                ShowError("Повторяются компоненты: " + string.Join(", ", duplicates) + ".");
                return false;
            }

            decimal total = _components.Sum(c => c.Percentage);

            if (Math.Abs(total - 100m) > 0.01m)
            {
                ShowError("Сумма долей компонентов должна быть 100%. Сейчас: " + total.ToString("0.###") + "%.");
                return false;
            }

            return true;
        }

        private void UpdateTotalPercentage()
        {
            decimal total = _components.Sum(c => c.Percentage);

            TotalPercentText.Text = total.ToString("0.###") + "%";

            if (Math.Abs(total - 100m) <= 0.01m)
            {
                WarningText.Text = "Сумма корректна";
                WarningText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                WarningText.Text = "Сумма должна быть 100%";
                WarningText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void NormalizeLoadOrder()
        {
            int index = 1;

            foreach (ComponentItemDto component in _components.OrderBy(c => c.LoadOrder).ToList())
            {
                if (component.LoadOrder <= 0)
                    component.LoadOrder = index;

                index++;
            }

            ComponentsGrid.Items.Refresh();
        }

        private void CommitGridEdit()
        {
            ComponentsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ComponentsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private List<ComponentItemDto> ToComponentItems(IEnumerable<object> source)
        {
            var result = new List<ComponentItemDto>();

            if (source == null)
                return result;

            foreach (object item in source)
            {
                if (item == null)
                    continue;

                result.Add(new ComponentItemDto
                {
                    RawMaterialId = GetPropertyValue<int>(item, "RawMaterialId", "rawMaterialId"),
                    RawMaterialName = GetPropertyValue<string>(item, "RawMaterialName", "rawMaterialName"),
                    Percentage = GetPropertyValue<decimal>(item, "Percentage", "percentage"),
                    ToleranceMin = GetNullableDecimal(item, "ToleranceMin", "toleranceMin"),
                    ToleranceMax = GetNullableDecimal(item, "ToleranceMax", "toleranceMax"),
                    LoadOrder = GetPropertyValue<int>(item, "LoadOrder", "loadOrder")
                });
            }

            return result
                .OrderBy(c => c.LoadOrder)
                .ToList();
        }

        private IEnumerable<object> GetCollection(object source, params string[] names)
        {
            if (source == null || names == null)
                return null;

            JToken token = source as JToken;

            if (token != null)
            {
                JToken child = GetChildToken(token, names);

                if (child != null && child.Type == JTokenType.Array)
                    return child.Children().Cast<object>().ToList();

                return null;
            }

            foreach (string name in names)
            {
                PropertyInfo property = source.GetType().GetProperty(name);

                if (property == null)
                    continue;

                object value = property.GetValue(source, null);

                if (value == null || value is string)
                    continue;

                if (value is IEnumerable enumerable)
                {
                    var list = new List<object>();

                    foreach (object item in enumerable)
                        list.Add(item);

                    return list;
                }
            }

            try
            {
                token = JToken.FromObject(source);

                JToken child = GetChildToken(token, names);

                if (child != null && child.Type == JTokenType.Array)
                    return child.Children().Cast<object>().ToList();
            }
            catch
            {
                return null;
            }

            return null;
        }

        private T GetPropertyValue<T>(object source, params string[] names)
        {
            object value = GetRawPropertyValue(source, names);

            if (value == null)
                return default(T);

            try
            {
                if (value is T typed)
                    return typed;

                if (value is JValue jValue)
                    value = jValue.Value;

                if (value == null)
                    return default(T);

                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)value.ToString();

                if (targetType == typeof(decimal))
                {
                    decimal result;

                    if (decimal.TryParse(
                            value.ToString().Replace(',', '.'),
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out result))
                    {
                        return (T)(object)result;
                    }

                    return default(T);
                }

                if (targetType == typeof(int))
                {
                    int result;

                    if (int.TryParse(value.ToString(), out result))
                        return (T)(object)result;

                    return default(T);
                }

                object converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

                return (T)converted;
            }
            catch
            {
                return default(T);
            }
        }

        private decimal? GetNullableDecimal(object source, params string[] names)
        {
            object value = GetRawPropertyValue(source, names);

            if (value == null)
                return null;

            if (value is JValue jValue)
                value = jValue.Value;

            if (value == null)
                return null;

            decimal result;

            if (decimal.TryParse(
                    value.ToString().Replace(',', '.'),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            return null;
        }

        private object GetRawPropertyValue(object source, params string[] names)
        {
            if (source == null || names == null)
                return null;

            JToken token = source as JToken;

            if (token != null)
            {
                JToken child = GetChildToken(token, names);

                if (child == null || child.Type == JTokenType.Null)
                    return null;

                return child;
            }

            foreach (string name in names)
            {
                PropertyInfo property = source.GetType().GetProperty(name);

                if (property == null)
                    continue;

                object value = property.GetValue(source, null);

                if (value != null)
                    return value;
            }

            try
            {
                token = JToken.FromObject(source);

                JToken child = GetChildToken(token, names);

                if (child == null || child.Type == JTokenType.Null)
                    return null;

                return child;
            }
            catch
            {
                return null;
            }
        }

        private JToken GetChildToken(JToken token, params string[] names)
        {
            if (token == null || names == null)
                return null;

            JObject obj = token as JObject;

            if (obj == null && token.Type == JTokenType.Object)
                obj = (JObject)token;

            if (obj == null)
                return null;

            foreach (string name in names)
            {
                JProperty property = obj.Properties()
                    .FirstOrDefault(p => string.Equals(
                        p.Name,
                        name,
                        StringComparison.InvariantCultureIgnoreCase));

                if (property != null)
                    return property.Value;
            }

            return null;
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            ProductComboBox.IsEnabled = !isLoading && _editingRecipe == null;
            VersionBox.IsEnabled = !isLoading && _editingRecipe == null;
            AddComponentButton.IsEnabled = !isLoading;
            ComponentsGrid.IsEnabled = !isLoading;
            SaveButton.IsEnabled = !isLoading;
            CancelButton.IsEnabled = !isLoading;

            SaveButton.Content = isLoading
                ? "Загрузка..."
                : (_editingRecipe == null ? "Создать" : "Сохранить");

            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void SetSavingState(bool isSaving)
        {
            _isSaving = isSaving;

            ProductComboBox.IsEnabled = !isSaving && _editingRecipe == null;
            VersionBox.IsEnabled = !isSaving && _editingRecipe == null;
            AddComponentButton.IsEnabled = !isSaving;
            ComponentsGrid.IsEnabled = !isSaving;
            SaveButton.IsEnabled = !isSaving;
            CancelButton.IsEnabled = !isSaving;

            SaveButton.Content = isSaving
                ? "Сохранение..."
                : (_editingRecipe == null ? "Создать" : "Сохранить");

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
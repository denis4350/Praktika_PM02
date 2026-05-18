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
    public partial class TechCardForm : Window
    {
        private readonly ApiService _apiService;
        private readonly object _editingTechCard;

        private readonly ObservableCollection<TechStepItem> _steps;
        private List<ProductDto> _products;

        private bool _isLoading;
        private bool _isSaving;

        public TechCardForm(ApiService apiService) : this(apiService, null)
        {
        }

        public TechCardForm(ApiService apiService, dynamic techCard)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _editingTechCard = techCard;

            _products = new List<ProductDto>();
            _steps = new ObservableCollection<TechStepItem>();

            StepsGrid.ItemsSource = _steps;

            if (_editingTechCard == null)
            {
                Title = "Создание технологической карты";
                TitleText.Text = "Создание технологической карты";
                SubtitleText.Text = "Выберите продукт, задайте версию и добавьте технологические шаги.";
                SaveButton.Content = "Создать";
            }
            else
            {
                Title = "Редактирование технологической карты";
                TitleText.Text = "Редактирование технологической карты";
                SubtitleText.Text = "Просмотр и подготовка изменений технологической карты.";
                SaveButton.Content = "Сохранить";

                ProductComboBox.IsEnabled = false;
                VersionBox.IsEnabled = false;
            }

            Loaded += TechCardForm_Loaded;
        }

        private async void TechCardForm_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();

            if (_editingTechCard != null)
                await LoadTechCardDataAsync();

            UpdateStepsCount();
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

                _products = result != null && result.Items != null
                    ? result.Items
                    : new List<ProductDto>();

                ProductComboBox.ItemsSource = _products;

                if (!_products.Any())
                {
                    ShowError("Нет активных продуктов. Сначала добавьте продукт в справочник.");
                    SaveButton.IsEnabled = false;
                    return;
                }

                if (_editingTechCard == null)
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

        private async Task LoadTechCardDataAsync()
        {
            try
            {
                HideError();

                int techCardId = GetPropertyValue<int>(_editingTechCard, "Id", "id");

                if (techCardId <= 0)
                {
                    ShowError("Не удалось определить ID технологической карты.");
                    return;
                }

                object techCardData = await _apiService.GetTechCardByIdAsync(techCardId);

                object source = techCardData ?? _editingTechCard;

                string version = GetPropertyValue<string>(source, "Version", "version");
                int productId = GetPropertyValue<int>(source, "ProductId", "productId");

                VersionBox.Text = string.IsNullOrWhiteSpace(version) ? "" : version;

                ProductDto selectedProduct = _products.FirstOrDefault(p => p.Id == productId);

                if (selectedProduct != null)
                    ProductComboBox.SelectedItem = selectedProduct;

                IEnumerable<object> stepsSource = GetCollection(source, "Steps", "steps", "TechSteps", "techSteps");

                _steps.Clear();

                foreach (TechStepItem step in ToTechStepItems(stepsSource))
                {
                    _steps.Add(step);
                }

                NormalizeStepNumbers();
                UpdateStepsCount();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки технологической карты: " + ex.Message);
            }
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            AddStep();
        }

        private void AddStep()
        {
            try
            {
                HideError();

                var dialog = new TechStepDialog(_apiService) { Owner = this };


                if (dialog.ShowDialog() == true)
                {
                    var step = new TechStepItem
                    {
                        StepNumber = _steps.Count + 1,
                        StepType = dialog.StepType,
                        Name = dialog.StepName,
                        Instruction = dialog.Instruction,
                        IsMandatory = dialog.IsMandatory,
                        PlannedParams = dialog.PlannedParams,
                        ToleranceParams = dialog.ToleranceParams
                    };

                    _steps.Add(step);
                    NormalizeStepNumbers();
                    UpdateStepsCount();
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка добавления шага: " + ex.Message);
            }
        }

        private void EditStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;

                if (button == null)
                    return;

                var step = button.DataContext as TechStepItem;

                if (step == null)
                    return;

                var dialog = new TechStepDialog(_apiService, step) { Owner = this };


                if (dialog.ShowDialog() == true)
                {
                    step.StepType = dialog.StepType;
                    step.Name = dialog.StepName;
                    step.Instruction = dialog.Instruction;
                    step.IsMandatory = dialog.IsMandatory;
                    step.PlannedParams = dialog.PlannedParams;
                    step.ToleranceParams = dialog.ToleranceParams;

                    StepsGrid.Items.Refresh();
                    UpdateStepsCount();
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка редактирования шага: " + ex.Message);
            }
        }

        private void DeleteStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var step = button.DataContext as TechStepItem;
            if (step == null) return;

            if (MessageBox.Show(
                    "Удалить шаг?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _steps.Remove(step);  // обновление UI автоматически
            NormalizeStepNumbers(); // если у тебя есть пересчёт номеров шагов
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveTechCardAsync();
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
                await SaveTechCardAsync();
                e.Handled = true;
            }
        }

        private async Task SaveTechCardAsync()
        {
            if (_isSaving)
                return;

            try
            {
                HideError();

                if (!ValidateForm())
                    return;

                ProductDto product = ProductComboBox.SelectedItem as ProductDto;
                string version = VersionBox.Text.Trim();

                SetSavingState(true);

                if (_editingTechCard == null)
                {
                    object techCard = await _apiService.CreateTechCardAsync(new CreateTechCardDto
                    {
                        ProductId = product.Id,
                        Version = version
                    });

                    int techCardId = GetPropertyValue<int>(techCard, "Id", "id");

                    if (techCardId <= 0)
                    {
                        ShowError("Сервер не вернул ID созданной технологической карты.");
                        return;
                    }

                    await SaveStepsAsync(techCardId);

                    MessageBox.Show(
                        "Технологическая карта успешно создана.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    // 1. Получаем ID технологической карты, которую редактируем
                    int techCardId = GetPropertyValue<int>(_editingTechCard, "Id", "id");
                    if (techCardId <= 0)
                    {
                        ShowError("Не удалось определить ID технологической карты.");
                        return;
                    }

                    // 2. Загружаем текущие шаги карты, чтобы узнать их ID
                    object techCardData = await _apiService.GetTechCardByIdAsync(techCardId);
                    var oldSteps = GetCollection(techCardData, "Steps", "steps");

                    // 3. Удаляем все старые шаги по одному
                    if (oldSteps != null)
                    {
                        foreach (object stepObj in oldSteps)
                        {
                            int stepId = GetPropertyValue<int>(stepObj, "Id", "id");
                            if (stepId > 0)
                                await _apiService.DeleteTechStepAsync(techCardId, stepId);
                        }
                    }

                    // 4. Добавляем новые шаги, которые сейчас в таблице
                    await SaveStepsAsync(techCardId);

                    MessageBox.Show(
                        "Технологическая карта успешно обновлена.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения технологической карты: " + ex.Message);
            }
            finally
            {
                SetSavingState(false);
            }
        }

        private async Task SaveStepsAsync(int techCardId)
        {
            NormalizeStepNumbers();

            foreach (TechStepItem step in _steps.OrderBy(s => s.StepNumber))
            {
                object result = await _apiService.AddTechStepAsync(techCardId, new AddTechStepDto
                {
                    StepType = step.StepType,
                    Name = step.Name,
                    Instruction = step.Instruction,
                    IsMandatory = step.IsMandatory,
                    PlannedParams = step.PlannedParams,
                    ToleranceParams = step.ToleranceParams
                });

                if (result == null)
                    throw new Exception("Сервер не подтвердил сохранение шага: " + step.Name);
            }
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
                ShowError("Введите версию технологической карты.");
                VersionBox.Focus();
                return false;
            }

            if (!_steps.Any())
            {
                ShowError("Добавьте хотя бы один технологический шаг.");
                return false;
            }

            foreach (TechStepItem step in _steps)
            {
                if (string.IsNullOrWhiteSpace(step.StepType))
                {
                    ShowError("У одного из шагов не указан тип.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    ShowError("У одного из шагов не указано название.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(step.Instruction))
                {
                    ShowError("У шага \"" + step.Name + "\" не указана инструкция.");
                    return false;
                }

                if (step.StepNumber <= 0)
                {
                    ShowError("Номер шага должен быть больше 0.");
                    return false;
                }
            }

            return true;
        }

        private void NormalizeStepNumbers()
        {
            int number = 1;

            foreach (TechStepItem step in _steps.OrderBy(s => s.StepNumber).ToList())
            {
                step.StepNumber = number;
                number++;
            }

            StepsGrid.Items.Refresh();
        }

        private void UpdateStepsCount()
        {
            StepsCountText.Text = _steps.Count + " шагов";
        }

        private List<TechStepItem> ToTechStepItems(IEnumerable<object> source)
        {
            var result = new List<TechStepItem>();

            if (source == null)
                return result;

            foreach (object item in source)
            {
                if (item == null)
                    continue;

                result.Add(new TechStepItem
                {
                    StepNumber = GetPropertyValue<int>(item, "StepNumber", "stepNumber"),
                    StepType = GetPropertyValue<string>(item, "StepType", "stepType"),
                    Name = GetPropertyValue<string>(item, "Name", "name"),
                    Instruction = GetPropertyValue<string>(item, "Instruction", "instruction"),
                    IsMandatory = GetPropertyValue<bool>(item, "IsMandatory", "isMandatory"),
                    PlannedParams = GetRawPropertyValue(item, "PlannedParams", "plannedParams"),
                    ToleranceParams = GetRawPropertyValue(item, "ToleranceParams", "toleranceParams")
                });
            }

            return result
                .OrderBy(s => s.StepNumber)
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

                if (targetType == typeof(int))
                {
                    int result;

                    if (int.TryParse(value.ToString(), out result))
                        return (T)(object)result;

                    return default(T);
                }

                if (targetType == typeof(bool))
                {
                    bool result;

                    if (bool.TryParse(value.ToString(), out result))
                        return (T)(object)result;

                    return default(T);
                }

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

                object converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

                return (T)converted;
            }
            catch
            {
                return default(T);
            }
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

            ProductComboBox.IsEnabled = !isLoading && _editingTechCard == null;
            VersionBox.IsEnabled = !isLoading && _editingTechCard == null;
            AddStepButton.IsEnabled = !isLoading;
            StepsGrid.IsEnabled = !isLoading;
            SaveButton.IsEnabled = !isLoading;
            CancelButton.IsEnabled = !isLoading;

            SaveButton.Content = isLoading
                ? "Загрузка..."
                : (_editingTechCard == null ? "Создать" : "Сохранить");

            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void SetSavingState(bool isSaving)
        {
            _isSaving = isSaving;

            ProductComboBox.IsEnabled = !isSaving && _editingTechCard == null;
            VersionBox.IsEnabled = !isSaving && _editingTechCard == null;
            AddStepButton.IsEnabled = !isSaving;
            StepsGrid.IsEnabled = !isSaving;
            SaveButton.IsEnabled = !isSaving;
            CancelButton.IsEnabled = !isSaving;

            SaveButton.Content = isSaving
                ? "Сохранение..."
                : (_editingTechCard == null ? "Создать" : "Сохранить");

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

    public class TechStepItem
    {
        public int StepNumber { get; set; }
        public string StepType { get; set; }
        public string Name { get; set; }
        public string Instruction { get; set; }
        public bool IsMandatory { get; set; }
        public object PlannedParams { get; set; }
        public object ToleranceParams { get; set; }
    }
}
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
using System.Windows.Input;
using System.Windows.Media;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SZR_TechnologistApp.Views
{
    public partial class RecipeCardWindow : Window
    {
        private readonly ApiService _apiService; private readonly object _recipe;

        private bool _componentsLoaded;
        private bool _techCardsLoaded;
        private bool _isLoading;
        private string _activeTab = "components";
        private ObservableCollection<ProductDto> _products;

        public RecipeCardWindow(ApiService apiService, dynamic recipe)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));

            Loaded += RecipeCardWindow_Loaded;
        }

        private async void RecipeCardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRecipeData();
            UpdateActionButtons();
            SetActiveTab("components");
            await LoadComponentsAsync();
        }

        private void LoadRecipeData()
        {
            int id = GetPropertyValue<int>(_recipe, "Id", "id");
            string productName = GetPropertyValue<string>(_recipe, "ProductName", "productName");
            string version = GetPropertyValue<string>(_recipe, "Version", "version");
            string status = GetPropertyValue<string>(_recipe, "Status", "status");
            DateTime createdAt = GetPropertyValue<DateTime>(_recipe, "CreatedAt", "createdAt");

            IdText.Text = id > 0 ? id.ToString() : "—";
            ProductNameText.Text = string.IsNullOrWhiteSpace(productName) ? "—" : productName;
            VersionText.Text = string.IsNullOrWhiteSpace(version) ? "—" : version;
            StatusText.Text = string.IsNullOrWhiteSpace(status) ? "—" : status;

            CreatedAtText.Text = createdAt == default(DateTime)
                ? "—"
                : createdAt.ToString("dd.MM.yyyy HH:mm");

            HeaderSubtitleText.Text = string.IsNullOrWhiteSpace(productName)
                ? "Состав рецептуры и связанные технологические карты"
                : productName + " / версия " + (string.IsNullOrWhiteSpace(version) ? "—" : version);

            ApplyStatusStyle(status);
        }

        private void UpdateActionButtons()
        {
            string status = GetPropertyValue<string>(_recipe, "Status", "status");

            ApproveButton.Visibility = Visibility.Collapsed;
            ArchiveButton.Visibility = Visibility.Collapsed;

            if (status == "Черновик")
            {
                ApproveButton.Visibility = Visibility.Visible;
            }
            else if (status == "Утверждена")
            {
                ArchiveButton.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadComponentsAsync(bool force = false)
        {
            if (_isLoading || (_componentsLoaded && !force))
                return;

            try
            {
                HideError();
                SetLoadingState(true, "Загрузка компонентов рецептуры...");

                int recipeId = GetPropertyValue<int>(_recipe, "Id", "id");

                if (recipeId <= 0)
                {
                    ShowError("Не удалось определить ID рецептуры.");
                    ComponentsGrid.ItemsSource = null;
                    ItemsCountText.Text = "0 записей";
                    return;
                }

                object recipeData = await _apiService.GetRecipeByIdAsync(recipeId);

                IEnumerable<object> componentSource = null;

                if (recipeData != null)
                {
                    componentSource = GetCollection(recipeData, "Components", "components");
                }

                if (componentSource == null)
                {
                    componentSource = GetCollection(_recipe, "Components", "components");
                }

                List<ComponentRow> components = ToComponentRows(componentSource)
                    .OrderBy(c => c.LoadOrder)
                    .ThenBy(c => c.Id)
                    .ToList();

                ComponentsGrid.ItemsSource = components;
                ItemsCountText.Text = components.Count + " записей";

                _componentsLoaded = true;
            }
            catch (Exception ex)
            {
                ComponentsGrid.ItemsSource = null;
                ItemsCountText.Text = "0 записей";
                ShowError("Ошибка загрузки компонентов: " + ex.Message);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task LoadTechCardsAsync(bool force = false)
        {
            if (_isLoading || (_techCardsLoaded && !force))
                return;

            try
            {
                HideError();
                SetLoadingState(true, "Загрузка связанных технологических карт...");

                int productId = GetPropertyValue<int>(_recipe, "ProductId", "productId");

                if (productId <= 0)
                {
                    TechCardsGrid.ItemsSource = null;
                    ItemsCountText.Text = "0 записей";
                    ShowError("Не удалось определить продукт рецептуры.");
                    return;
                }

                var result = await _apiService.GetTechCardsAsync(1, 100);

                List<TechCardRow> techCards = new List<TechCardRow>();

                if (result != null && result.Items != null)
                {
                    foreach (object card in result.Items)
                    {
                        int cardProductId = GetPropertyValue<int>(card, "ProductId", "productId");

                        if (cardProductId == productId)
                        {
                            techCards.Add(new TechCardRow
                            {
                                Id = GetPropertyValue<int>(card, "Id", "id"),
                                Version = GetPropertyValue<string>(card, "Version", "version"),
                                Status = GetPropertyValue<string>(card, "Status", "status"),
                                CreatedAt = GetNullableDateTime(card, "CreatedAt", "createdAt"),
                                ApprovedAt = GetNullableDateTime(card, "ApprovedAt", "approvedAt")
                            });
                        }
                    }
                }

                techCards = techCards
                    .OrderByDescending(t => t.CreatedAt ?? DateTime.MinValue)
                    .ToList();

                TechCardsGrid.ItemsSource = techCards;
                ItemsCountText.Text = techCards.Count + " записей";

                _techCardsLoaded = true;
            }
            catch (Exception ex)
            {
                TechCardsGrid.ItemsSource = null;
                ItemsCountText.Text = "0 записей";
                ShowError("Ошибка загрузки технологических карт: " + ex.Message);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private List<ComponentRow> ToComponentRows(IEnumerable<object> source)
        {
            var result = new List<ComponentRow>();

            if (source == null)
                return result;

            foreach (object item in source)
            {
                if (item == null)
                    continue;

                result.Add(new ComponentRow
                {
                    Id = GetPropertyValue<int>(item, "Id", "id"),
                    RawMaterialId = GetPropertyValue<int>(item, "RawMaterialId", "rawMaterialId"),
                    RawMaterialName = GetPropertyValue<string>(item, "RawMaterialName", "rawMaterialName"),
                    Percentage = GetPropertyValue<decimal>(item, "Percentage", "percentage"),
                    ToleranceMin = GetNullableDecimal(item, "ToleranceMin", "toleranceMin"),
                    ToleranceMax = GetNullableDecimal(item, "ToleranceMax", "toleranceMax"),
                    LoadOrder = GetPropertyValue<int>(item, "LoadOrder", "loadOrder")
                });
            }

            return result;
        }

        private void ComponentsTab_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTab("components");
            _ = LoadComponentsAsync();
        }

        private async void TechCardsTab_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTab("techcards");
            await LoadTechCardsAsync();
        }

        private void SetActiveTab(string tab)
        {
            _activeTab = tab;

            ComponentsGrid.Visibility = Visibility.Collapsed;
            TechCardsGrid.Visibility = Visibility.Collapsed;

            ComponentsTab.Background = Brushes.Transparent;
            TechCardsTab.Background = Brushes.Transparent;

            ComponentsTabText.Foreground = FindResource("MutedTextBrush") as Brush;
            TechCardsTabText.Foreground = FindResource("MutedTextBrush") as Brush;

            switch (tab)
            {
                case "components":
                    ComponentsGrid.Visibility = Visibility.Visible;
                    ComponentsTab.Background = Brushes.White;
                    ComponentsTabText.Foreground = FindResource("PrimaryBrush") as Brush;
                    TabTitleText.Text = "Компоненты рецептуры";
                    TabSubtitleText.Text = "Сырьё, доли, допустимые отклонения и порядок загрузки";
                    ItemsCountText.Text = ComponentsGrid.ItemsSource == null ? "0 записей" : ComponentsGrid.Items.Count + " записей";
                    break;

                case "techcards":
                    TechCardsGrid.Visibility = Visibility.Visible;
                    TechCardsTab.Background = Brushes.White;
                    TechCardsTabText.Foreground = FindResource("PrimaryBrush") as Brush;
                    TabTitleText.Text = "Связанные технологические карты";
                    TabSubtitleText.Text = "Технологические карты, связанные с продуктом рецептуры";
                    ItemsCountText.Text = TechCardsGrid.ItemsSource == null ? "0 записей" : TechCardsGrid.Items.Count + " записей";
                    break;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == "components")
            {
                _componentsLoaded = false;
                await LoadComponentsAsync(true);
            }
            else if (_activeTab == "techcards")
            {
                _techCardsLoaded = false;
                await LoadTechCardsAsync(true);
            }
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            int recipeId = GetPropertyValue<int>(_recipe, "Id", "id");

            if (recipeId <= 0)
            {
                MessageBox.Show(
                    "Не удалось определить ID рецептуры.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    "Утвердить рецептуру?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SetLoadingState(true, "Утверждение рецептуры...");

                bool success = await _apiService.ApproveRecipeAsync(recipeId);

                if (success)
                {
                    MessageBox.Show(
                        "Рецептура утверждена.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    TrySetDialogResult(true);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка утверждения рецептуры:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            int recipeId = GetPropertyValue<int>(_recipe, "Id", "id");

            if (recipeId <= 0)
            {
                MessageBox.Show(
                    "Не удалось определить ID рецептуры.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    "Архивировать рецептуру?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SetLoadingState(true, "Архивирование рецептуры...");

                bool success = await _apiService.DeleteRecipeAsync(recipeId);

                if (success)
                {
                    MessageBox.Show(
                        "Рецептура архивирована.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    TrySetDialogResult(true);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка архивирования рецептуры:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void ApplyStatusStyle(string status)
        {
            if (status == "Черновик")
            {
                StatusPanel.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                StatusBadge.Text = "Черновик";
                StatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(253, 230, 138));
            }
            else if (status == "Утверждена")
            {
                StatusPanel.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                StatusBadge.Text = "Утверждена";
                StatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(187, 247, 208));
            }
            else if (status == "Архив")
            {
                StatusPanel.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                StatusBadge.Text = "Архив";
                StatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            }
            else
            {
                StatusPanel.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
                StatusBadge.Text = string.IsNullOrWhiteSpace(status) ? "—" : status;
                StatusBadge.Foreground = Brushes.White;
            }
        }

        private void SetLoadingState(bool isLoading, string message = null)
        {
            _isLoading = isLoading;

            ApproveButton.IsEnabled = !isLoading;
            ArchiveButton.IsEnabled = !isLoading;
            RefreshButton.IsEnabled = !isLoading;
            CloseButton.IsEnabled = !isLoading;
            ComponentsGrid.IsEnabled = !isLoading;
            TechCardsGrid.IsEnabled = !isLoading;

            if (!string.IsNullOrWhiteSpace(message))
                FooterText.Text = message;
            else if (!isLoading)
                FooterText.Text = "Просмотр рецептуры";

            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private IEnumerable<object> GetCollection(object source, params string[] names)
        {
            if (source == null || names == null)
                return null;

            JToken token = source as JToken;

            if (token != null)
            {
                JToken child = GetChildToken(token, names);

                if (child == null || child.Type == JTokenType.Null)
                    return null;

                if (child.Type == JTokenType.Array)
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
                    {
                        list.Add(item);
                    }

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
                if (value is T typedValue)
                    return typedValue;

                if (value is JValue jValue)
                    value = jValue.Value;

                if (value == null)
                    return default(T);

                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)value.ToString();

                if (targetType == typeof(DateTime))
                {
                    DateTime date;

                    if (DateTime.TryParse(value.ToString(), out date))
                        return (T)(object)date;

                    return default(T);
                }

                if (targetType == typeof(decimal))
                {
                    decimal decimalValue;

                    if (decimal.TryParse(
                            value.ToString().Replace(',', '.'),
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out decimalValue))
                    {
                        return (T)(object)decimalValue;
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

        private DateTime? GetNullableDateTime(object source, params string[] names)
        {
            object value = GetRawPropertyValue(source, names);

            if (value == null)
                return null;

            if (value is JValue jValue)
                value = jValue.Value;

            if (value == null)
                return null;

            DateTime result;

            if (DateTime.TryParse(value.ToString(), out result))
                return result;

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

        private void HideError()
        {
            ErrorText.Text = string.Empty;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void TrySetDialogResult(bool value)
        {
            try
            {
                DialogResult = value;
            }
            catch
            {
                // Если окно открыто не через ShowDialog, DialogResult может бросить исключение.
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TrySetDialogResult(false);
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                RefreshButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            TrySetDialogResult(false);
            Close();
        }

        private class ComponentRow
        {
            public int Id { get; set; }
            public int RawMaterialId { get; set; }
            public string RawMaterialName { get; set; }
            public decimal Percentage { get; set; }
            public decimal? ToleranceMin { get; set; }
            public decimal? ToleranceMax { get; set; }
            public int LoadOrder { get; set; }
        }

        private class TechCardRow
        {
            public int Id { get; set; }
            public string Version { get; set; }
            public string Status { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? ApprovedAt { get; set; }
        }
    }

}
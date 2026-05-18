using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class TechCardCardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly object _techCard;

        private bool _isLoading;
        private bool _stepsLoaded;

        public TechCardCardWindow(ApiService apiService, dynamic techCard)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _techCard = techCard ?? throw new ArgumentNullException(nameof(techCard));

            Loaded += TechCardCardWindow_Loaded;
        }

        private async void TechCardCardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTechCardData();
            UpdateActionButtons();
            await LoadStepsAsync();
        }

        private void LoadTechCardData()
        {
            int id = GetPropertyValue<int>(_techCard, "Id", "id");
            string productName = GetPropertyValue<string>(_techCard, "ProductName", "productName");
            string version = GetPropertyValue<string>(_techCard, "Version", "version");
            string status = GetPropertyValue<string>(_techCard, "Status", "status");
            DateTime? createdAt = GetNullableDateTime(_techCard, "CreatedAt", "createdAt");

            IdText.Text = id > 0 ? id.ToString() : "—";
            ProductNameText.Text = string.IsNullOrWhiteSpace(productName) ? "—" : productName;
            VersionText.Text = string.IsNullOrWhiteSpace(version) ? "—" : version;
            StatusText.Text = string.IsNullOrWhiteSpace(status) ? "—" : status;

            CreatedAtText.Text = createdAt.HasValue
                ? createdAt.Value.ToString("dd.MM.yyyy HH:mm")
                : "—";

            HeaderSubtitleText.Text = string.IsNullOrWhiteSpace(productName)
                ? "Технологические шаги, инструкции и параметры выполнения"
                : productName + " / версия " + (string.IsNullOrWhiteSpace(version) ? "—" : version);

            ApplyStatusStyle(status);
        }

        private void UpdateActionButtons()
        {
            string status = GetPropertyValue<string>(_techCard, "Status", "status");

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

        private async Task LoadStepsAsync(bool force = false)
        {
            if (_isLoading || (_stepsLoaded && !force))
                return;

            try
            {
                HideError();
                SetLoadingState(true, "Загрузка технологических шагов...");

                int techCardId = GetPropertyValue<int>(_techCard, "Id", "id");

                if (techCardId <= 0)
                {
                    StepsGrid.ItemsSource = null;
                    StepsCountText.Text = "0 шагов";
                    ShowError("Не удалось определить ID технологической карты.");
                    return;
                }

                object techCardData = await _apiService.GetTechCardByIdAsync(techCardId);

                IEnumerable<object> stepSource = null;

                if (techCardData != null)
                {
                    stepSource = GetCollection(techCardData, "Steps", "steps", "TechSteps", "techSteps");
                }

                if (stepSource == null)
                {
                    stepSource = GetCollection(_techCard, "Steps", "steps", "TechSteps", "techSteps");
                }

                List<StepRow> steps = ToStepRows(stepSource)
                    .OrderBy(s => s.StepNumber)
                    .ToList();

                StepsGrid.ItemsSource = steps;
                StepsCountText.Text = steps.Count + " шагов";

                _stepsLoaded = true;
            }
            catch (Exception ex)
            {
                StepsGrid.ItemsSource = null;
                StepsCountText.Text = "0 шагов";
                ShowError("Ошибка загрузки шагов: " + ex.Message);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private List<StepRow> ToStepRows(IEnumerable<object> source)
        {
            var result = new List<StepRow>();

            if (source == null)
                return result;

            foreach (object item in source)
            {
                if (item == null)
                    continue;

                result.Add(new StepRow
                {
                    Id = GetPropertyValue<int>(item, "Id", "id"),
                    StepNumber = GetPropertyValue<int>(item, "StepNumber", "stepNumber"),
                    StepType = GetPropertyValue<string>(item, "StepType", "stepType"),
                    Name = GetPropertyValue<string>(item, "Name", "name"),
                    Instruction = GetPropertyValue<string>(item, "Instruction", "instruction"),
                    IsMandatory = GetPropertyValue<bool>(item, "IsMandatory", "isMandatory"),
                    PlannedParams = GetPropertyValue<string>(item, "PlannedParams", "plannedParams"),
                    ToleranceParams = GetPropertyValue<string>(item, "ToleranceParams", "toleranceParams")
                });
            }

            return result;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _stepsLoaded = false;
            await LoadStepsAsync(true);
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            int techCardId = GetPropertyValue<int>(_techCard, "Id", "id");

            if (techCardId <= 0)
            {
                MessageBox.Show("Не удалось определить ID технологической карты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    "Утвердить технологическую карту?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SetLoadingState(true, "Утверждение технологической карты...");

                bool success = await _apiService.ApproveTechCardAsync(techCardId);

                if (success)
                {
                    MessageBox.Show("Технологическая карта утверждена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    TrySetDialogResult(true);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка утверждения технологической карты:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            int techCardId = GetPropertyValue<int>(_techCard, "Id", "id");

            if (techCardId <= 0)
            {
                MessageBox.Show("Не удалось определить ID технологической карты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    "Архивировать технологическую карту?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SetLoadingState(true, "Архивирование технологической карты...");

                bool success = await _apiService.DeleteTechCardAsync(techCardId);

                if (success)
                {
                    MessageBox.Show("Технологическая карта архивирована.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    TrySetDialogResult(true);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка архивирования технологической карты:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            StepsGrid.IsEnabled = !isLoading;

            if (!string.IsNullOrWhiteSpace(message))
                FooterText.Text = message;
            else if (!isLoading)
                FooterText.Text = "Просмотр технологической карты";

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

                if (targetType == typeof(bool))
                {
                    bool boolValue;

                    if (bool.TryParse(value.ToString(), out boolValue))
                        return (T)(object)boolValue;

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

        private DateTime? GetNullableDateTime(object source, params string[] names)
        {
            object value = GetRawPropertyValue(source, names);

            if (value == null)
                return null;

            if (value is JValue jValue)
                value = jValue.Value;

            if (value == null)
                return null;

            DateTime date;

            if (DateTime.TryParse(value.ToString(), out date))
                return date;

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

        private void TrySetDialogResult(bool value)
        {
            try
            {
                DialogResult = value;
            }
            catch
            {
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

        private class StepRow
        {
            public int Id { get; set; }
            public int StepNumber { get; set; }
            public string StepType { get; set; }
            public string Name { get; set; }
            public string Instruction { get; set; }
            public bool IsMandatory { get; set; }
            public string PlannedParams { get; set; }
            public string ToleranceParams { get; set; }
        }
    }
}
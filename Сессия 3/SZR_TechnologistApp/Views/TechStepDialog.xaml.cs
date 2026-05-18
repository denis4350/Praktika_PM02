using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class TechStepDialog : Window
    {
        private readonly ApiService _apiService;

        public string StepType { get; private set; }
        public string StepName { get; private set; }
        public string Instruction { get; private set; }
        public bool IsMandatory { get; private set; }
        public object PlannedParams { get; private set; }
        public object ToleranceParams { get; private set; }
        public int? EquipmentId { get; private set; }

        public TechStepDialog(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            StepTypeBox.SelectedIndex = 1;
            MandatoryBox.IsChecked = true;
            NameBox.Focus();

            Loaded += TechStepDialog_Loaded;
        }

        public TechStepDialog(ApiService apiService, TechStepItem step) : this(apiService)
        {
            if (step == null) return;

            // Заполнение полей при редактировании — вызовется после загрузки справочников,
            // но можно сделать сразу, если значения не зависят от списков.
            // Для типа шага и оборудования установим значения в TechStepDialog_Loaded.
            _pendingStep = step;
        }

        private TechStepItem _pendingStep;

        private async void TechStepDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReferenceDataAsync();

            if (_pendingStep != null)
            {
                SetComboBoxValue(StepTypeBox, _pendingStep.StepType);
                NameBox.Text = _pendingStep.Name ?? "";
                InstructionBox.Text = _pendingStep.Instruction ?? "";
                MandatoryBox.IsChecked = _pendingStep.IsMandatory;
                PlannedParamsBox.Text = FormatJsonValue(_pendingStep.PlannedParams);
                ToleranceParamsBox.Text = FormatJsonValue(_pendingStep.ToleranceParams);

                // Установим оборудование, если оно было привязано (в будущем)
                if (_pendingStep != null)
                {
                    SetComboBoxValue(StepTypeBox, _pendingStep.StepType);
                    NameBox.Text = _pendingStep.Name ?? "";
                    InstructionBox.Text = _pendingStep.Instruction ?? "";
                    MandatoryBox.IsChecked = _pendingStep.IsMandatory;
                    PlannedParamsBox.Text = FormatJsonValue(_pendingStep.PlannedParams);
                    ToleranceParamsBox.Text = FormatJsonValue(_pendingStep.ToleranceParams);
                    // EquipmentId пока не хранится в TechStepItem, поэтому пропускаем
                }
            }
        }

        private List<string> _stepTypes;
        private List<EquipmentDto> _equipmentList;

        private async Task LoadReferenceDataAsync()
        {
            try
            {
                // Загружаем справочные данные
                object refData = await _apiService.GetReferenceAllAsync();
                JObject jRef = refData as JObject;
                if (jRef?["dictionaries"]?["techStepTypes"] != null)
                {
                    _stepTypes = jRef["dictionaries"]["techStepTypes"].ToObject<List<string>>();
                    StepTypeBox.ItemsSource = _stepTypes;
                }
                else
                {
                    StepTypeBox.ItemsSource = new[] { "Загрузка", "Смешивание", "Выдержка", "Экструзия", "Охлаждение", "Контроль" };
                }

                // Загружаем оборудование
                _equipmentList = await _apiService.GetEquipmentAsync(true);
                EquipmentBox.ItemsSource = _equipmentList;
                EquipmentBox.DisplayMemberPath = "Name";
                EquipmentBox.SelectedValuePath = "Id";
            }
            catch
            {
                if (StepTypeBox.ItemsSource == null)
                    StepTypeBox.ItemsSource = new[] { "Загрузка", "Смешивание", "Выдержка", "Экструзия", "Охлаждение", "Контроль" };
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAndClose();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveAndClose();
                e.Handled = true;
            }
        }

        private void SaveAndClose()
        {
            try
            {
                HideError();

                if (StepTypeBox.SelectedItem == null)
                {
                    ShowError("Выберите тип шага.");
                    StepTypeBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    ShowError("Введите название шага.");
                    NameBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(InstructionBox.Text))
                {
                    ShowError("Введите инструкцию для шага.");
                    InstructionBox.Focus();
                    return;
                }

                object plannedParams;
                if (!TryParseJsonOrNull(PlannedParamsBox.Text, "плановых параметрах", out plannedParams))
                    return;

                object toleranceParams;
                if (!TryParseJsonOrNull(ToleranceParamsBox.Text, "допусках", out toleranceParams))
                    return;

                StepType = StepTypeBox.SelectedItem as string;
                StepName = NameBox.Text.Trim();
                Instruction = InstructionBox.Text.Trim();
                IsMandatory = MandatoryBox.IsChecked ?? true;
                PlannedParams = plannedParams;
                ToleranceParams = toleranceParams;
                EquipmentId = (EquipmentBox.SelectedItem as EquipmentDto)?.Id;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения шага: " + ex.Message);
            }
        }

        private bool TryParseJsonOrNull(string text, string fieldName, out object value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text))
                return true;
            try
            {
                value = JToken.Parse(text.Trim());
                return true;
            }
            catch
            {
                ShowError("Неверный JSON в поле \"" + fieldName + "\".");
                return false;
            }
        }

        private string FormatJsonValue(object value)
        {
            if (value == null) return "";
            try
            {
                JToken token = value as JToken;
                if (token != null) return token.ToString(Formatting.Indented);
                string stringValue = value as string;
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    try { return JToken.Parse(stringValue).ToString(Formatting.Indented); }
                    catch { return stringValue; }
                }
                return JsonConvert.SerializeObject(value, Formatting.Indented);
            }
            catch
            {
                return value?.ToString() ?? "";
            }
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(value)) return;
            foreach (var item in comboBox.Items)
            {
                string itemStr = item as string ?? (item as ComboBoxItem)?.Content?.ToString();
                if (string.Equals(itemStr, value, StringComparison.InvariantCultureIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = "";
            ErrorPanel.Visibility = Visibility.Collapsed;
        }
    }
}
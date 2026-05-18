using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace SZR_TechnologistApp.Views
{
    public partial class CompleteStepDialog : Window
    {
        private readonly int _stepNumber;

        public object ActualParams { get; private set; }

        public CompleteStepDialog(
            int stepNumber,
            decimal? plannedTemp = null,
            decimal? plannedPressure = null)
        {
            InitializeComponent();

            _stepNumber = stepNumber;

            Title = $"Завершение шага {_stepNumber}";
            TitleText.Text = $"Завершение шага №{_stepNumber}";

            if (plannedTemp.HasValue)
            {
                TemperatureBox.Text = plannedTemp.Value.ToString(CultureInfo.CurrentCulture);
            }

            if (plannedPressure.HasValue)
            {
                PressureBox.Text = plannedPressure.Value.ToString(CultureInfo.CurrentCulture);
            }

            Loaded += (s, e) => TemperatureBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            CompleteStep();
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
                CompleteStep();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void CompleteStep()
        {
            HideError();

            if (!TryValidateInputs(out decimal temperature, out decimal pressure))
            {
                return;
            }

            ActualParams = new
            {
                temperature = temperature,
                pressure = pressure,
                completed_at = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            DialogResult = true;
            Close();
        }

        private bool TryValidateInputs(out decimal temperature, out decimal pressure)
        {
            temperature = 0;
            pressure = 0;

            string temperatureText = TemperatureBox.Text?.Trim();
            string pressureText = PressureBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(temperatureText))
            {
                ShowError("Введите температуру.");
                TemperatureBox.Focus();
                return false;
            }

            if (!TryParseDecimal(temperatureText, out temperature))
            {
                ShowError("Температура должна быть числом. Можно использовать запятую или точку.");
                TemperatureBox.Focus();
                TemperatureBox.SelectAll();
                return false;
            }

            if (temperature < -50 || temperature > 500)
            {
                ShowError("Температура должна быть в диапазоне от -50 до 500 °C.");
                TemperatureBox.Focus();
                TemperatureBox.SelectAll();
                return false;
            }

            if (string.IsNullOrWhiteSpace(pressureText))
            {
                ShowError("Введите давление.");
                PressureBox.Focus();
                return false;
            }

            if (!TryParseDecimal(pressureText, out pressure))
            {
                ShowError("Давление должно быть числом. Можно использовать запятую или точку.");
                PressureBox.Focus();
                PressureBox.SelectAll();
                return false;
            }

            if (pressure < 0 || pressure > 1000)
            {
                ShowError("Давление должно быть в диапазоне от 0 до 1000 бар.");
                PressureBox.Focus();
                PressureBox.SelectAll();
                return false;
            }

            return true;
        }

        private bool TryParseDecimal(string text, out decimal value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string normalized = text.Trim().Replace(',', '.');

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value);
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
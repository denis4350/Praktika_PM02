using System.Windows;
using System.Windows.Controls;

namespace SZR_OperatorApp.Views
{
    public partial class DeviationDialog : Window
    {
        public string ParameterName { get; private set; }
        public string PlannedValue { get; private set; }
        public string ActualValue { get; private set; }
        public string Description { get; private set; }
        public string Severity { get; private set; }

        // Добавляем необязательный параметр plannedValue
        public DeviationDialog(string batchNumber, int stepNumber, string plannedValue = null)
        {
            InitializeComponent();
            Title = $"Отклонение в партии {batchNumber}, шаг {stepNumber}";

            // Предзаполняем плановое значение, если передано
            if (!string.IsNullOrWhiteSpace(plannedValue))
            {
                PlannedValueBox.Text = plannedValue;
            }

            OkButton.Click += (s, e) =>
            {
                ParameterName = (ParameterBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                PlannedValue = PlannedValueBox.Text;
                ActualValue = ActualValueBox.Text;
                Description = DescriptionBox.Text;
                Severity = (SeverityBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrEmpty(ParameterName))
                {
                    MessageBox.Show("Выберите параметр", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка заполнения планового значения (по желанию)
                if (string.IsNullOrWhiteSpace(PlannedValue))
                {
                    MessageBox.Show("Введите плановое значение", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ActualValue))
                {
                    MessageBox.Show("Введите фактическое значение", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            };

            CancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close(); // Явно закрываем окно
            };
        }
    }
}
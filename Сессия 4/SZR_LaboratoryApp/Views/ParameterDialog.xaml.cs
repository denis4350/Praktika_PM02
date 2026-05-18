using System.Windows;

namespace SZR_LaboratoryApp.Views
{
    public partial class ParameterDialog : Window
    {
        public string ParameterName { get; private set; }
        public decimal? NormMin { get; private set; }
        public decimal? NormMax { get; private set; }
        public string Unit { get; private set; }

        public ParameterDialog()
        {
            InitializeComponent();

            OkButton.Click += (s, e) =>
            {
                ParameterName = ParameterNameBox.Text?.Trim();
                Unit = UnitBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(ParameterName))
                {
                    MessageBox.Show("Введите название параметра", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ParameterNameBox.Focus();
                    return;
                }

                NormMin = decimal.TryParse(NormMinBox.Text, out var min) ? min : (decimal?)null;
                NormMax = decimal.TryParse(NormMaxBox.Text, out var max) ? max : (decimal?)null;

                if (NormMin.HasValue && NormMax.HasValue && NormMin.Value > NormMax.Value)
                {
                    MessageBox.Show("Минимальная норма не может быть больше максимальной", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NormMinBox.Focus();
                    return;
                }

                DialogResult = true;
                Close();
            };

            CancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SZR_LaboratoryApp.Models;
using SZR_LaboratoryApp.Services;

namespace SZR_LaboratoryApp.Views
{
    public partial class TestForm : Window
    {
        private readonly ApiService _apiService;
        private readonly object _batch;               // RawMaterialBatch или ProductBatch
        private readonly string _batchType;           // "RawMaterial" или "Product"
        private readonly UserInfoDto _currentUser;
        private ObservableCollection<TestParameterItem> _parameters
            = new ObservableCollection<TestParameterItem>();

        public TestForm(ApiService apiService, object batch, string batchType, UserInfoDto currentUser)
        {
            InitializeComponent();
            _apiService = apiService;
            _batch = batch;
            _batchType = batchType;
            _currentUser = currentUser;

            // Заполняем поля в зависимости от типа партии
            if (batchType == "RawMaterial" && batch is RawMaterialBatch raw)
            {
                BatchNumberText.Text = raw.batchNumber;
                MaterialNameText.Text = raw.materialName;
                SupplierText.Text = raw.supplier;
                ArrivalDateText.Text = raw.arrivalDate.ToString("dd.MM.yyyy");
                QuantityText.Text = $"{raw.quantity} {raw.unit}";
                CurrentStatusText.Text = raw.labStatus;
            }
            else if (batchType == "Product" && batch is ProductBatch product)
            {
                BatchNumberText.Text = product.batchNumber;
                MaterialNameText.Text = product.productName;
                SupplierText.Text = "";                       // у продукции нет поставщика
                ArrivalDateText.Text = product.startedAt?.ToString("dd.MM.yyyy") ?? "—";
                QuantityText.Text = "—";
                CurrentStatusText.Text = product.labStatus;
            }

            ExecutorBox.Text = currentUser.FullName;
            AssignedDatePicker.SelectedDate = DateTime.Now;

            AddDefaultParameters();

            SaveButton.Click += async (s, e) => await SaveTest();
            CancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
        }

        private void AddDefaultParameters()
        {
            _parameters.Add(new TestParameterItem { parameterName = "Концентрация", normMin = null, normMax = 97, unit = "%" });
            _parameters.Add(new TestParameterItem { parameterName = "Влажность", normMin = null, normMax = 2.5m, unit = "%" });
            _parameters.Add(new TestParameterItem { parameterName = "pH", normMin = 6.5m, normMax = 7.0m, unit = "" });
            ParametersGrid.ItemsSource = _parameters;
        }

        private void AddParameterButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ParameterDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.ShowDialog() == true)
            {
                _parameters.Add(new TestParameterItem
                {
                    parameterName = dialog.ParameterName,
                    normMin = dialog.NormMin,
                    normMax = dialog.NormMax,
                    unit = dialog.Unit
                });
                ParametersGrid.ItemsSource = _parameters;
            }
        }

        private void RemoveParameter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var parameter = button?.Tag as TestParameterItem;
            if (parameter != null)
            {
                _parameters.Remove(parameter);
                ParametersGrid.ItemsSource = _parameters;
            }
        }

        private async Task SaveTest()
        {
            try
            {
                // Проверка обязательных полей
                if (TestTypeBox.SelectedItem == null ||
                    string.IsNullOrWhiteSpace((TestTypeBox.SelectedItem as ComboBoxItem)?.Content.ToString()))
                {
                    MessageBox.Show("Выберите тип испытания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PriorityBox.SelectedItem == null ||
                    string.IsNullOrWhiteSpace((PriorityBox.SelectedItem as ComboBoxItem)?.Content.ToString()))
                {
                    MessageBox.Show("Выберите приоритет.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_parameters.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы один контролируемый параметр.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var testType = (TestTypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var priority = (PriorityBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                var testDto = new CreateTestDto
                {
                    TestType = testType,
                    ObjectType = _batchType,       // "RawMaterial" или "Product"
                    ObjectId = GetBatchId(),       // динамически получаем ID
                    Priority = priority,
                    Comment = CommentBox.Text,
                    Parameters = _parameters.Select(p => new TestParameterDto
                    {
                        ParameterName = p.parameterName,
                        NormMin = p.normMin,
                        NormMax = p.normMax,
                        Unit = p.unit
                    }).ToArray()
                };

                var result = await _apiService.CreateTestAsync(testDto);

                if (result != null)
                {
                    MessageBox.Show("Испытание успешно создано!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;        // сигнал вызывающему окну
                    Close();
                }
                else
                {
                    MessageBox.Show("Сервер не подтвердил создание испытания.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Извлекает Id из переданного объекта партии (RawMaterialBatch или ProductBatch)
        /// </summary>
        private int GetBatchId()
        {
            if (_batch is RawMaterialBatch raw)
                return raw.Id;
            else if (_batch is ProductBatch product)
                return product.Id;
            throw new InvalidOperationException("Не удалось определить Id партии");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
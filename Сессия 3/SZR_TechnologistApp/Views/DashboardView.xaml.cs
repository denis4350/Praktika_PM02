using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly MainWindow _mainWindow;
        private bool _isLoading;

        public DashboardView(ApiService apiService, MainWindow mainWindow)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboard();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboard();
        }

        private async void Card_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as FrameworkElement;

            if (border == null || border.Tag == null)
                return;

            string section = border.Tag.ToString();

            await NavigateToSection(section);
        }

        private async void EventsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = EventsListBox.SelectedItem;
            EventsListBox.SelectedItem = null;

            await NavigateToBatchFromItem(selected);
        }

        private async void CriticalDeviationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = CriticalDeviationsListBox.SelectedItem;
            CriticalDeviationsListBox.SelectedItem = null;

            await NavigateToBatchFromItem(selected);
        }

        private async void BatchesForAnalysisListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = BatchesForAnalysisListBox.SelectedItem;
            BatchesForAnalysisListBox.SelectedItem = null;

            await NavigateToBatchFromItem(selected);
        }

        private async Task NavigateToSection(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            try
            {
                switch (section)
                {
                    case "products":
                        await _mainWindow.LoadProductsAsync();
                        break;

                    case "recipes":
                        await _mainWindow.LoadRecipesAsync();
                        break;

                    case "techcards":
                        await _mainWindow.LoadTechCardsAsync();
                        break;

                    case "orders":
                        await _mainWindow.LoadOrdersAsync();
                        break;

                    case "batches":
                        await _mainWindow.LoadBatchesAsync();
                        break;

                    case "deviations":
                    case "laboratory":
                        await _mainWindow.LoadDeviationsAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка перехода в раздел: " + ex.Message);
            }
        }

        private async Task NavigateToBatchFromItem(object item)
        {
            if (item == null)
                return;

            string batchNumber = GetStringProperty(item, "BatchNumber");

            if (string.IsNullOrWhiteSpace(batchNumber))
                return;

            try
            {
                await _mainWindow.LoadBatchesAsync();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка перехода к партии " + batchNumber + ": " + ex.Message);
            }
        }

        private async Task LoadDashboard()
        {
            if (_isLoading)
                return;

            try
            {
                HideError();
                SetLoadingState(true);

                dynamic data = await _apiService.GetDashboardDataAsync();

                if (data == null || data.KPIs == null)
                {
                    ShowEmptyDashboard();
                    ShowError("API вернул пустые данные dashboard.");
                    return;
                }

                ActiveProductsText.Text = SafeToString(data.KPIs.ActiveProducts);
                ActiveRecipesText.Text = SafeToString(data.KPIs.ActiveRecipes);
                ActiveTechCardsText.Text = SafeToString(data.KPIs.ActiveTechCards);
                OrdersInProgressText.Text = SafeToString(data.KPIs.OrdersInProgress);
                BatchesInProductionText.Text = SafeToString(data.KPIs.BatchesInProduction);
                BatchesWithDeviationsText.Text = SafeToString(data.KPIs.BatchesWithDeviations);
                BatchesWaitingForLabText.Text = SafeToString(data.KPIs.BatchesWaitingLab);

                EventsListBox.ItemsSource = data.RecentEvents as IEnumerable;
                CriticalDeviationsListBox.ItemsSource = data.CriticalDeviations as IEnumerable;
                BatchesForAnalysisListBox.ItemsSource = data.BatchesForAnalysis as IEnumerable;

                LastUpdatedText.Text = "Обновлено: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            }
            catch (Exception ex)
            {
                ShowEmptyDashboard();
                ShowError("Ошибка загрузки dashboard: " + ex.Message);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void ShowEmptyDashboard()
        {
            ActiveProductsText.Text = "0";
            ActiveRecipesText.Text = "0";
            ActiveTechCardsText.Text = "0";
            OrdersInProgressText.Text = "0";
            BatchesInProductionText.Text = "0";
            BatchesWithDeviationsText.Text = "0";
            BatchesWaitingForLabText.Text = "0";

            EventsListBox.ItemsSource = null;
            CriticalDeviationsListBox.ItemsSource = null;
            BatchesForAnalysisListBox.ItemsSource = null;

            LastUpdatedText.Text = "Данные не загружены";
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            RefreshButton.IsEnabled = !isLoading;
            RefreshButton.Content = isLoading ? "Загрузка..." : "Обновить";

            Cursor = isLoading
                ? System.Windows.Input.Cursors.Wait
                : System.Windows.Input.Cursors.Arrow;
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

        private string SafeToString(object value)
        {
            return value == null ? "0" : value.ToString();
        }

        private string GetStringProperty(object source, string propertyName)
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName))
                return string.Empty;

            PropertyInfo property = source.GetType().GetProperty(propertyName);

            if (property == null)
                return string.Empty;

            object value = property.GetValue(source, null);

            return value == null ? string.Empty : value.ToString();
        }
    }
}
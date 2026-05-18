using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;

        private string _currentView = "dashboard";
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;
        private string _currentSearch = "";
        private string _currentStatusFilter = "";
        private bool _suppressFilterEvents;

        public MainWindow(ApiService apiService, UserInfoDto user)
        {
            _suppressFilterEvents = true;
            InitializeComponent();
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));

            InitializeUserPanel();
            SubscribeEvents();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
            LoadAvatar();
            _suppressFilterEvents = false;
        }

        private void InitializeUserPanel()
        {
            UserFullNameText.Text = string.IsNullOrWhiteSpace(_currentUser.FullName)
                ? _currentUser.Login
                : _currentUser.FullName;

            UserRoleText.Text = string.IsNullOrWhiteSpace(_currentUser.Role)
                ? "Пользователь"
                : _currentUser.Role;

            UserInitials.Text = GetInitials(_currentUser.FullName);
        }

        private void SubscribeEvents()
        {
            DashboardMenu.Click += (s, e) => LoadDashboard();
            ProductsMenu.Click += async (s, e) => { ResetPaging(); await LoadProductsAsync(); };
            RecipesMenu.Click += async (s, e) => { ResetPaging(); await LoadRecipesAsync(); };
            TechCardsMenu.Click += async (s, e) => { ResetPaging(); await LoadTechCardsAsync(); };
            OrdersMenu.Click += async (s, e) => { ResetPaging(); await LoadOrdersAsync(); };
            BatchesMenu.Click += async (s, e) => { ResetPaging(); await LoadBatchesAsync(); };
            DeviationsMenu.Click += async (s, e) => { ResetPaging(); await LoadDeviationsAsync(); };
            ExtruderMenu.Click += (s, e) => LoadExtruder();
            ReportsMenu.Click += (s, e) => LoadReports();

            ProfileButton.MouseLeftButtonUp += (s, e) => ProfilePopup.IsOpen = !ProfilePopup.IsOpen;
            ProfileMenuItem.Click += (s, e) => OpenProfile();
            ChangePasswordMenuItem.Click += (s, e) => OpenChangePassword();
            LogoutMenuItem.Click += (s, e) => Logout();
        }

        private void ResetPaging()
        {
            _currentPage = 1;
            _currentSearch = "";
            _currentStatusFilter = "";

            _suppressFilterEvents = true;

            SearchBox.Text = "";
            StatusFilter.ItemsSource = null;
            StatusFilter.SelectedIndex = -1;

            _suppressFilterEvents = false;
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "?";

            string[] parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();

            return fullName.Substring(0, 1).ToUpper();
        }

        private async void LoadAvatar()
        {
            try
            {
                byte[] avatarBytes = await _apiService.GetAvatarAsync(_currentUser.Id);
                if (avatarBytes != null && avatarBytes.Length > 0)
                {
                    UpdateAvatar(avatarBytes);
                    return;
                }
            }
            catch { }

            UserAvatarImage.Visibility = Visibility.Collapsed;
            UserInitials.Visibility = Visibility.Visible;
            UserInitials.Text = GetInitials(_currentUser.FullName);
        }

        public void UpdateAvatar(byte[] avatarBytes)
        {
            if (avatarBytes == null || avatarBytes.Length == 0)
                return;

            using (var stream = new MemoryStream(avatarBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                UserAvatarImage.Source = bitmap;
                UserAvatarImage.Visibility = Visibility.Visible;
                UserInitials.Visibility = Visibility.Collapsed;
            }
        }

        private void Avatar_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            UserAvatarImage.Visibility = Visibility.Collapsed;
            UserInitials.Visibility = Visibility.Visible;
        }

        private void OpenProfile()
        {
            ProfilePopup.IsOpen = false;
            var profileWindow = new ProfileWindow(_apiService, _currentUser) { Owner = this };
            profileWindow.ShowDialog();
        }

        private void OpenChangePassword()
        {
            ProfilePopup.IsOpen = false;
            var changePasswordWindow = new ChangePasswordWindow(_apiService) { Owner = this };
            changePasswordWindow.ShowDialog();
        }

        private void Logout()
        {
            ProfilePopup.IsOpen = false;
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            StatusBarText.Text = message;
            StatusBarText.Foreground = isError
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Color.FromRgb(22, 163, 74));
        }

        private void HideActionButtons()
        {
            AddButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
        }

        private void ShowActionButtons(bool add, bool edit, bool delete)
        {
            AddButton.Visibility = add ? Visibility.Visible : Visibility.Collapsed;
            EditButton.Visibility = edit ? Visibility.Visible : Visibility.Collapsed;
            DeleteButton.Visibility = delete ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowTableView()
        {
            if (MainContent != null)
            {
                MainContent.Content = null;
                MainContent.Visibility = Visibility.Collapsed;
            }
            if (MainListView == null)
                throw new InvalidOperationException("В MainWindow.xaml отсутствует ListView с x:Name=\"MainListView\".");

            MainListView.Visibility = Visibility.Visible;
            if (FilterPanel != null) FilterPanel.Visibility = Visibility.Visible;
            if (PaginationPanel != null) PaginationPanel.Visibility = Visibility.Visible;
        }

        private void ShowContentView()
        {
            if (MainListView != null)
            {
                MainListView.ItemsSource = null;
                ClearListViewColumns();
                MainListView.Visibility = Visibility.Collapsed;
            }
            if (MainContent != null)
            {
                MainContent.Content = null;
                MainContent.Visibility = Visibility.Visible;
            }
            if (FilterPanel != null) FilterPanel.Visibility = Visibility.Collapsed;
            if (PaginationPanel != null) PaginationPanel.Visibility = Visibility.Collapsed;
        }

        private void ClearListViewColumns() => MainGridView.Columns.Clear();

        private void AddListViewColumn(string binding, string header, double width = double.NaN, string stringFormat = null)
        {
            var dataBinding = new Binding(binding);
            if (!string.IsNullOrWhiteSpace(stringFormat))
                dataBinding.StringFormat = stringFormat;

            var column = new GridViewColumn
            {
                Header = header,
                DisplayMemberBinding = dataBinding
            };
            if (!double.IsNaN(width)) column.Width = width;
            MainGridView.Columns.Add(column);
        }

        private void SetActiveMenu(string menuName)
        {
            Button[] buttons = { DashboardMenu, ProductsMenu, RecipesMenu, TechCardsMenu, OrdersMenu, BatchesMenu, DeviationsMenu, ReportsMenu, ExtruderMenu };
            foreach (Button button in buttons)
            {
                button.Background = Brushes.Transparent;
                button.Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                button.FontWeight = FontWeights.SemiBold;
            }

            Button active = null;
            switch (menuName)
            {
                case "dashboard": active = DashboardMenu; break;
                case "products": active = ProductsMenu; break;
                case "recipes": active = RecipesMenu; break;
                case "techcards": active = TechCardsMenu; break;
                case "orders": active = OrdersMenu; break;
                case "batches": active = BatchesMenu; break;
                case "deviations": active = DeviationsMenu; break;
                case "extruder": active = ExtruderMenu; break;
            }
            if (active != null)
            {
                active.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                active.Foreground = Brushes.White;
                active.FontWeight = FontWeights.Bold;
            }
        }

        private void ConfigureFilterPanel(bool showSearch, bool showStatus)
        {
            SearchBox.Visibility = showSearch ? Visibility.Visible : Visibility.Collapsed;
            StatusFilter.Visibility = showStatus ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task LoadStatusFilterAsync(string type)
        {
            try
            {
                _suppressFilterEvents = true;
                switch (type)
                {
                    case "orders": StatusFilter.ItemsSource = await _apiService.GetOrderStatusesAsync(); break;
                    case "recipes": StatusFilter.ItemsSource = await _apiService.GetRecipeStatusesAsync(); break;
                    case "batches": StatusFilter.ItemsSource = await _apiService.GetBatchStatusesAsync(); break;
                    case "steps": StatusFilter.ItemsSource = await _apiService.GetStepStatusesAsync(); break;
                    case "lab": StatusFilter.ItemsSource = await _apiService.GetLabStatusesAsync(); break;
                    default: StatusFilter.ItemsSource = null; break;
                }
                StatusFilter.SelectedIndex = -1;
            }
            catch { StatusFilter.ItemsSource = null; }
            finally { _suppressFilterEvents = false; }
        }

        private void UpdatePaginationInfo()
        {
            if (PaginationPanel == null || PageInfo == null) return;
            int total = _totalPages > 0 ? _totalPages : 1;
            if (_currentPage < 1) _currentPage = 1;
            if (_currentPage > total) _currentPage = total;

            PageInfo.Text = $"Страница {_currentPage} из {total}";
            if (FirstPageBtn != null) FirstPageBtn.IsEnabled = _currentPage > 1;
            if (PrevPageBtn != null) PrevPageBtn.IsEnabled = _currentPage > 1;
            if (NextPageBtn != null) NextPageBtn.IsEnabled = _currentPage < total;
            if (LastPageBtn != null) LastPageBtn.IsEnabled = _currentPage < total;
        }

        private async void Pagination_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button)?.Tag?.ToString();
            int newPage = _currentPage;
            switch (tag)
            {
                case "first": newPage = 1; break;
                case "prev": newPage = _currentPage - 1; break;
                case "next": newPage = _currentPage + 1; break;
                case "last": newPage = _totalPages; break;
            }
            if (newPage < 1) newPage = 1;
            if (newPage > _totalPages) newPage = _totalPages;
            if (newPage == _currentPage) return;

            _currentPage = newPage;
            await ReloadCurrentViewAsync();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressFilterEvents) return;
            _currentSearch = SearchBox.Text?.Trim() ?? "";
            _currentPage = 1;
            await ReloadCurrentViewAsync();
        }

        private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressFilterEvents) return;
            _currentStatusFilter = (StatusFilter.SelectedItem is StatusItem item) ? item.Value : "";
            _currentPage = 1;
            await ReloadCurrentViewAsync();
        }

        private async void PageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressFilterEvents) return;
            if (PageSizeCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Content.ToString(), out int pageSize))
                _pageSize = pageSize;
            else
                _pageSize = 20;
            _currentPage = 1;
            await ReloadCurrentViewAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _currentSearch = SearchBox.Text?.Trim() ?? "";
            _currentStatusFilter = (StatusFilter.SelectedItem is StatusItem item) ? item.Value : "";
            await ReloadCurrentViewAsync();
        }

        private async Task ReloadCurrentViewAsync()
        {
            switch (_currentView)
            {
                case "products": await LoadProductsAsync(); break;
                case "recipes": await LoadRecipesAsync(); break;
                case "techcards": await LoadTechCardsAsync(); break;
                case "orders": await LoadOrdersAsync(); break;
                case "batches": await LoadBatchesAsync(); break;
                case "deviations": await LoadDeviationsAsync(); break;
                case "dashboard": LoadDashboard(); break;
                case "extruder": LoadExtruder(); break;
            }
        }

        private void ClearDoubleClickHandlers()
        {
            MainListView.MouseDoubleClick -= OpenProductCard;
            MainListView.MouseDoubleClick -= OpenRecipeCard;
            MainListView.MouseDoubleClick -= OpenTechCardCard;
            MainListView.MouseDoubleClick -= OpenBatchCard;
        }

        public void LoadDashboard()
        {
            ShowContentView();
            var dashboard = new DashboardView(_apiService, this);
            MainContent.Content = dashboard;
            ContentTitle.Text = "Главная";
            ContentSubtitle.Text = "Сводка производственного модуля";
            _currentView = "dashboard";
            SetActiveMenu("dashboard");
            HideActionButtons();
            UpdateStatus("Готов");
        }

        public void LoadExtruder()
        {
            ShowContentView();
            var extruderView = new ExtruderView();
            extruderView.Initialize(_apiService, this);
            MainContent.Content = extruderView;
            ContentTitle.Text = "Программы экструдера";
            ContentSubtitle.Text = "Настройка параметров экструзии";
            _currentView = "extruder";
            SetActiveMenu("extruder");
            HideActionButtons();
            UpdateStatus("Готов");
        }

        public async Task LoadProductsAsync()
        {
            ShowTableView();
            ClearDoubleClickHandlers();
            ConfigureFilterPanel(true, false);

            try
            {
                UpdateStatus("Загрузка продукции...");
                var result = await _apiService.GetProductsAsync(_currentPage, _pageSize, _currentSearch);

                ClearListViewColumns();
                AddListViewColumn("Id", "ID", 60);
                AddListViewColumn("Code", "Код", 120);
                AddListViewColumn("Name", "Наименование", 260);
                AddListViewColumn("ProductType", "Тип", 140);
                AddListViewColumn("Form", "Форма", 120);
                AddListViewColumn("Status", "Статус", 120);
                AddListViewColumn("CreatedAt", "Дата создания", 160, "{0:dd.MM.yyyy}");

                MainListView.ItemsSource = result?.Items;
                _totalPages = result?.Pagination?.TotalPages ?? 1;
                UpdatePaginationInfo();

                ContentTitle.Text = "Продукция";
                ContentSubtitle.Text = "Справочник выпускаемой продукции";
                _currentView = "products";
                SetActiveMenu("products");
                ShowActionButtons(true, true, true);

                MainListView.MouseDoubleClick += OpenProductCard;
                UpdateStatus($"Загружено: {result?.Items?.Count ?? 0} из {result?.Pagination?.TotalCount ?? 0} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка загрузки продукции", true);
                MessageBox.Show("Ошибка загрузки продукции:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadRecipesAsync()
        {
            ShowTableView();
            ClearDoubleClickHandlers();
            ConfigureFilterPanel(false, false);

            try
            {
                UpdateStatus("Загрузка рецептур...");
                var result = await _apiService.GetRecipesAsync(_currentPage, _pageSize);

                ClearListViewColumns();
                AddListViewColumn("Id", "ID", 60);
                AddListViewColumn("ProductName", "Продукт", 260);
                AddListViewColumn("Version", "Версия", 100);
                AddListViewColumn("Status", "Статус", 140);
                AddListViewColumn("TotalPercentage", "Сумма, %", 100);
                AddListViewColumn("CreatedAt", "Дата создания", 160, "{0:dd.MM.yyyy}");

                MainListView.ItemsSource = result?.Items;
                _totalPages = result?.Pagination?.TotalPages ?? 1;
                UpdatePaginationInfo();

                ContentTitle.Text = "Рецептуры";
                ContentSubtitle.Text = "Состав и версии рецептур продукции";
                _currentView = "recipes";
                SetActiveMenu("recipes");
                ShowActionButtons(true, true, true);

                MainListView.MouseDoubleClick += OpenRecipeCard;
                UpdateStatus($"Загружено: {result?.Items?.Count ?? 0} из {result?.Pagination?.TotalCount ?? 0} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка загрузки рецептур", true);
                MessageBox.Show("Ошибка загрузки рецептур:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadTechCardsAsync()
        {
            ShowTableView();
            ClearDoubleClickHandlers();
            ConfigureFilterPanel(false, false);

            try
            {
                UpdateStatus("Загрузка технологических карт...");
                var result = await _apiService.GetTechCardsAsync(_currentPage, _pageSize);

                ClearListViewColumns();
                AddListViewColumn("Id", "ID", 60);
                AddListViewColumn("ProductName", "Продукт", 260);
                AddListViewColumn("Version", "Версия", 100);
                AddListViewColumn("Status", "Статус", 140);
                AddListViewColumn("CreatedAt", "Дата создания", 160, "{0:dd.MM.yyyy}");

                MainListView.ItemsSource = result?.Items;
                _totalPages = result?.Pagination?.TotalPages ?? 1;
                UpdatePaginationInfo();

                ContentTitle.Text = "Технологические карты";
                ContentSubtitle.Text = "Технологические операции и инструкции";
                _currentView = "techcards";
                SetActiveMenu("techcards");
                ShowActionButtons(true, true, true);

                MainListView.MouseDoubleClick += OpenTechCardCard;
                UpdateStatus($"Загружено: {result?.Items?.Count ?? 0} из {result?.Pagination?.TotalCount ?? 0} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка загрузки технологических карт", true);
                MessageBox.Show("Ошибка загрузки технологических карт:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadOrdersAsync()
        {
            ShowTableView();
            ClearDoubleClickHandlers();
            ConfigureFilterPanel(false, true);
            await LoadStatusFilterAsync("orders");

            try
            {
                UpdateStatus("Загрузка заказов...");
                var result = await _apiService.GetOrdersAsync(_currentPage, _pageSize, _currentStatusFilter);

                ClearListViewColumns();
                AddListViewColumn("Id", "ID", 60);
                AddListViewColumn("OrderNumber", "Номер заказа", 140);
                AddListViewColumn("ProductName", "Продукт", 260);
                AddListViewColumn("PlannedQuantity", "Количество", 120);
                AddListViewColumn("Unit", "Ед.", 80);
                AddListViewColumn("Status", "Статус", 140);
                AddListViewColumn("PlannedStartDate", "Плановая дата", 160, "{0:dd.MM.yyyy}");

                MainListView.ItemsSource = result?.Items;
                _totalPages = result?.Pagination?.TotalPages ?? 1;
                UpdatePaginationInfo();

                ContentTitle.Text = "Производственные заказы";
                ContentSubtitle.Text = "План выпуска продукции";
                _currentView = "orders";
                SetActiveMenu("orders");
                ShowActionButtons(true, true, true);

                UpdateStatus($"Загружено: {result?.Items?.Count ?? 0} из {result?.Pagination?.TotalCount ?? 0} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка загрузки заказов", true);
                MessageBox.Show("Ошибка загрузки заказов:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadBatchesAsync()
        {
            ShowTableView();
            ClearDoubleClickHandlers();
            ConfigureFilterPanel(false, false);

            try
            {
                UpdateStatus("Загрузка партий...");
                var result = await _apiService.GetBatchesAsync(_currentPage, _pageSize);

                ClearListViewColumns();
                AddListViewColumn("Id", "ID", 60);
                AddListViewColumn("BatchNumber", "Номер партии", 180);
                AddListViewColumn("ProductName", "Продукт", 260);
                AddListViewColumn("Line", "Линия", 100);
                AddListViewColumn("Status", "Статус", 140);
                AddListViewColumn("LabStatus", "Лаборатория", 140);
                AddListViewColumn("StartedAt", "Начало", 160, "{0:dd.MM.yyyy HH:mm}");

                MainListView.ItemsSource = result?.Items;
                _totalPages = result?.Pagination?.TotalPages ?? 1;
                UpdatePaginationInfo();

                ContentTitle.Text = "Производственные партии";
                ContentSubtitle.Text = "Мониторинг выполнения партий";
                _currentView = "batches";
                SetActiveMenu("batches");
                ShowActionButtons(true, false, false);

                MainListView.MouseDoubleClick += OpenBatchCard;
                UpdateStatus($"Загружено: {result?.Items?.Count ?? 0} из {result?.Pagination?.TotalCount ?? 0} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка загрузки партий", true);
                MessageBox.Show("Ошибка загрузки партий:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadDeviationsAsync()
        {
            ShowTableView();
            ClearDoubleClickHandlers();
            ConfigureFilterPanel(false, false);

            try
            {
                UpdateStatus("Загрузка отклонений...");
                var result = await _apiService.GetDeviationsAsync(0, _currentPage, _pageSize);

                ClearListViewColumns();
                AddListViewColumn("Id", "ID", 60);
                AddListViewColumn("BatchNumber", "Партия", 160);
                AddListViewColumn("EventType", "Тип события", 150);
                AddListViewColumn("ParameterName", "Параметр", 150);
                AddListViewColumn("PlannedValue", "План", 120);
                AddListViewColumn("ActualValue", "Факт", 120);
                AddListViewColumn("Severity", "Критичность", 120);
                AddListViewColumn("CreatedAt", "Дата", 160, "{0:dd.MM.yyyy HH:mm}");

                MainListView.ItemsSource = result?.Items;
                _totalPages = result?.Pagination?.TotalPages ?? 1;
                UpdatePaginationInfo();

                ContentTitle.Text = "Отклонения и события";
                ContentSubtitle.Text = "Журнал производственных отклонений";
                _currentView = "deviations";
                SetActiveMenu("deviations");
                ShowActionButtons(false, false, false);

                UpdateStatus($"Загружено: {result?.Items?.Count ?? 0} из {result?.Pagination?.TotalCount ?? 0} записей");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка загрузки отклонений", true);
                MessageBox.Show("Ошибка загрузки отклонений:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReports()
        {
            var reportWindow = new ReportWindow(_apiService) { Owner = this };
            reportWindow.ShowDialog();
        }

        private void OpenProductCard(object sender, MouseButtonEventArgs e)
        {
            if (MainListView.SelectedItem is ProductDto product)
            {
                var card = new ProductCardWindow(_apiService, product) { Owner = this };
                card.ShowDialog();
                _ = LoadProductsAsync();
            }
        }

        private void OpenRecipeCard(object sender, MouseButtonEventArgs e)
        {
            if (MainListView.SelectedItem != null)
            {
                var card = new RecipeCardWindow(_apiService, MainListView.SelectedItem) { Owner = this };
                card.ShowDialog();
                _ = LoadRecipesAsync();
            }
        }

        private void OpenTechCardCard(object sender, MouseButtonEventArgs e)
        {
            if (MainListView.SelectedItem != null)
            {
                var card = new TechCardCardWindow(_apiService, MainListView.SelectedItem) { Owner = this };
                card.ShowDialog();
                _ = LoadTechCardsAsync();
            }
        }

        private void OpenBatchCard(object sender, MouseButtonEventArgs e)
        {
            if (MainListView.SelectedItem != null)
            {
                var card = new BatchCardWindow(_apiService, MainListView.SelectedItem) { Owner = this };
                card.ShowDialog();
                _ = LoadBatchesAsync();
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_currentView)
                {
                    case "products":
                        var productForm = new ProductForm(_apiService) { Owner = this };
                        if (productForm.ShowDialog() == true) await LoadProductsAsync();
                        break;
                    case "recipes":
                        var recipeForm = new RecipeForm(_apiService) { Owner = this };
                        if (recipeForm.ShowDialog() == true) await LoadRecipesAsync();
                        break;
                    case "techcards":
                        var techCardForm = new TechCardForm(_apiService) { Owner = this };
                        if (techCardForm.ShowDialog() == true) await LoadTechCardsAsync();
                        break;
                    case "orders":
                        var orderForm = new OrderForm(_apiService) { Owner = this };
                        if (orderForm.ShowDialog() == true) await LoadOrdersAsync();
                        break;
                    case "batches":
                        MessageBox.Show("Создание партий выполняется через производственный заказ или форму создания партии.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainListView.SelectedItem == null)
            {
                MessageBox.Show("Выберите элемент для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int id = GetItemId(MainListView.SelectedItem);
            if (id <= 0)
            {
                MessageBox.Show("Не удалось определить ID выбранного элемента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                switch (_currentView)
                {
                    case "products":
                        var product = await _apiService.GetProductByIdAsync(id);
                        if (product != null)
                        {
                            var form = new ProductForm(_apiService, product) { Owner = this };
                            if (form.ShowDialog() == true) await LoadProductsAsync();
                        }
                        break;

                    case "recipes":
                        var recipeDetail = await _apiService.GetRecipeByIdAsync(id);
                        if (recipeDetail?.Recipe != null)
                        {
                            var form = new RecipeForm(_apiService, recipeDetail.Recipe) { Owner = this };
                            if (form.ShowDialog() == true) await LoadRecipesAsync();
                        }
                        break;

                    case "techcards":
                        var techCardDetail = await _apiService.GetTechCardByIdAsync(id);
                        if (techCardDetail?.TechCard != null)
                        {
                            var form = new TechCardForm(_apiService, techCardDetail.TechCard) { Owner = this };
                            if (form.ShowDialog() == true) await LoadTechCardsAsync();
                        }
                        break;

                    case "orders":
                        var order = await _apiService.GetOrderByIdAsync(id);
                        if (order != null)
                        {
                            var form = new OrderForm(_apiService, order) { Owner = this };
                            if (form.ShowDialog() == true) await LoadOrdersAsync();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка редактирования:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainListView.SelectedItem == null)
            {
                MessageBox.Show("Выберите элемент для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int id = GetItemId(MainListView.SelectedItem);
            if (id <= 0)
            {
                MessageBox.Show("Не удалось определить ID выбранного элемента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить выбранный элемент?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                bool success = false;
                switch (_currentView)
                {
                    case "products":
                        success = await _apiService.DeleteProductAsync(id);
                        if (success)
                        {
                            MessageBox.Show("Продукт архивирован.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadProductsAsync();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить продукт. Возможно, он уже архивирован или используется.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        break;
                    case "recipes":
                        success = await _apiService.DeleteRecipeAsync(id);
                        if (success) await LoadRecipesAsync();
                        break;
                    case "techcards":
                        success = await _apiService.DeleteTechCardAsync(id);
                        if (success) await LoadTechCardsAsync();
                        break;
                    case "orders":
                        success = await _apiService.CancelOrderAsync(id);
                        if (success) await LoadOrdersAsync();
                        break;
                }
                if (success)
                    MessageBox.Show("Удаление выполнено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetItemId(object item)
        {
            if (item == null) return 0;
            PropertyInfo property = item.GetType().GetProperty("Id") ?? item.GetType().GetProperty("id");
            if (property == null) return 0;
            object value = property.GetValue(item, null);
            return (value != null && int.TryParse(value.ToString(), out int id)) ? id : 0;
        }
    }
}
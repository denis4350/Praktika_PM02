using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ExtruderView : UserControl
    {
        private ApiService _apiService;
        private MainWindow _mainWindow;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private bool _isLoading;
        private bool _isInitialized;

        public ExtruderView()
        {
            InitializeComponent();
            UpdatePaginationControls();
        }

        public void Initialize(ApiService apiService, MainWindow mainWindow)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mainWindow = mainWindow;

            if (!_isInitialized)
            {
                Loaded += ExtruderView_Loaded;
                _isInitialized = true;
            }

            if (IsLoaded)
            {
                _ = LoadData();
            }
        }

        private async void ExtruderView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_apiService != null)
                await LoadData();
        }

        private async void NewProgramButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateNewProgram();
        }

        private async Task LoadData()
        {
            if (_isLoading || _apiService == null)
                return;

            try
            {
                SetLoadingState(true);
                SetStatus("Загрузка программ...");

                var result = await _apiService.GetExtruderProgramsAsync(_currentPage, _pageSize);

                if (result != null && result.Pagination != null)
                {
                    _totalPages = result.Pagination.TotalPages <= 0 ? 1 : result.Pagination.TotalPages;

                    if (_currentPage > _totalPages)
                        _currentPage = _totalPages;
                }
                else
                {
                    _totalPages = 1;
                }

                if (result != null && result.Items != null && result.Items.Count > 0)
                {
                    ProgramsListView.ItemsSource = result.Items;

                    int totalCount = result.Pagination != null ? result.Pagination.TotalCount : result.Items.Count;

                    SetStatus($"Загружено программ: {result.Items.Count} из {totalCount}");
                }
                else
                {
                    ProgramsListView.ItemsSource = null;
                    SetStatus("Нет данных. Создайте новую программу.");
                }

                LastUpdatedText.Text = "Обновлено: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                UpdatePaginationControls();
            }
            catch (Exception ex)
            {
                ProgramsListView.ItemsSource = null;
                SetStatus("Ошибка загрузки программ.");
                MessageBox.Show(
                    "Ошибка загрузки программ экструдера:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void PageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_apiService == null || PageSizeCombo == null)
                return;

            if (PageSizeCombo.SelectedItem is ComboBoxItem item)
            {
                if (int.TryParse(item.Content.ToString(), out int pageSize))
                    _pageSize = pageSize;
                else
                    _pageSize = 10;
            }

            _currentPage = 1;
            await LoadData();
        }

        private async void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1)
                return;

            _currentPage--;
            await LoadData();
        }

        private async void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage >= _totalPages)
                return;

            _currentPage++;
            await LoadData();
        }

        private async Task CreateNewProgram()
        {
            if (_apiService == null)
                return;

            try
            {
                var dialog = new NewExtruderProgramDialog(_apiService)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    _currentPage = 1;
                    await LoadData();
                    SetStatus("Программа успешно создана.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка создания программы:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ViewZones_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == null || button.Tag == null)
                return;

            if (!int.TryParse(button.Tag.ToString(), out int programId))
                return;

            try
            {
                SetLoadingState(true);
                SetStatus("Загрузка зон программы...");

                dynamic program = await _apiService.GetExtruderProgramByIdAsync(programId);

                if (program == null)
                {
                    MessageBox.Show(
                        "Не удалось загрузить данные программы.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var zonesDialog = new ExtruderZonesWindow(program)
                {
                    Owner = Window.GetWindow(this)
                };

                zonesDialog.ShowDialog();

                SetStatus("Готов");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки зон экструдера:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void ActivateProgram_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == null || button.Tag == null)
                return;

            if (!int.TryParse(button.Tag.ToString(), out int programId))
                return;

            var confirm = MessageBox.Show(
                "Активировать эту программу экструдера?\n\nПредыдущая активная программа для продукта может быть архивирована.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                SetLoadingState(true);
                SetStatus("Активация программы...");

                bool success = await _apiService.ActivateExtruderProgramAsync(programId);

                if (success)
                {
                    MessageBox.Show(
                        "Программа активирована.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка активации программы:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void LoadProgram_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == null || button.Tag == null)
                return;

            if (!int.TryParse(button.Tag.ToString(), out int programId))
                return;

            string batchNumber = await ShowBatchNumberDialog();

            if (string.IsNullOrWhiteSpace(batchNumber))
                return;

            try
            {
                SetLoadingState(true);
                SetStatus("Загрузка программы в экструдер...");

                bool success = await _apiService.LoadProgramToExtruderAsync(programId, batchNumber.Trim());

                if (success)
                {
                    MessageBox.Show(
                        $"Программа загружена для партии {batchNumber.Trim()}.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    SetStatus("Программа загружена в экструдер.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки программы в экструдер:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private Task<string> ShowBatchNumberDialog()
        {
            var dialog = new Window
            {
                Title = "Загрузка программы в экструдер",
                Width = 460,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(238, 242, 247)),
                Owner = Window.GetWindow(this)
            };

            var root = new Grid
            {
                Margin = new Thickness(22)
            };

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(22),
                BorderBrush = new SolidColorBrush(Color.FromRgb(221, 227, 236)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Загрузка программы",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            var label = new TextBlock
            {
                Text = "Номер партии",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            Grid.SetRow(label, 2);
            grid.Children.Add(label);

            var textBox = new TextBox
            {
                Height = 38,
                FontSize = 14,
                Padding = new Thickness(10, 6, 10, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(textBox, 4);
            grid.Children.Add(textBox);

            var infoText = new TextBlock
            {
                Text = "Введите номер существующей производственной партии, например B-20260516-001.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(infoText, 5);
            grid.Children.Add(infoText);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };

            var cancelButton = CreateDialogButton("Отмена", new SolidColorBrush(Color.FromRgb(71, 85, 105)));
            var okButton = CreateDialogButton("Загрузить", new SolidColorBrush(Color.FromRgb(22, 163, 74)));

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            Grid.SetRow(buttonPanel, 6);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            root.Children.Add(card);
            dialog.Content = root;

            string result = null;

            okButton.Click += (s, e) =>
            {
                string value = textBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    MessageBox.Show(
                        "Введите номер партии.",
                        "Проверка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    textBox.Focus();
                    return;
                }

                result = value;
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            dialog.Loaded += (s, e) => textBox.Focus();

            dialog.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    dialog.DialogResult = false;
                    dialog.Close();
                    e.Handled = true;
                }
            };

            dialog.ShowDialog();

            return Task.FromResult(result);
        }

        private Button CreateDialogButton(string text, Brush background)
        {
            return new Button
            {
                Content = text,
                Width = 110,
                Height = 38,
                Margin = new Thickness(8, 0, 0, 0),
                Background = background,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            if (NewProgramButton != null)
                NewProgramButton.IsEnabled = !isLoading;

            if (PageSizeCombo != null)
                PageSizeCombo.IsEnabled = !isLoading;

            if (PrevPageBtn != null)
                PrevPageBtn.IsEnabled = !isLoading && _currentPage > 1;

            if (NextPageBtn != null)
                NextPageBtn.IsEnabled = !isLoading && _currentPage < _totalPages;

            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void UpdatePaginationControls()
        {
            if (PageInfoText != null)
                PageInfoText.Text = $"Страница {_currentPage} из {_totalPages}";

            if (PrevPageBtn != null)
                PrevPageBtn.IsEnabled = !_isLoading && _currentPage > 1;

            if (NextPageBtn != null)
                NextPageBtn.IsEnabled = !_isLoading && _currentPage < _totalPages;
        }

        private void SetStatus(string text)
        {
            if (StatusText != null)
                StatusText.Text = text;
        }
    }
}
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_TechnologistApp.Helpers;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;

        private string _currentCaptchaCode;
        private string _currentRegCaptchaCode;

        private bool _isLoggingIn;
        private bool _isRegistering;

        public LoginWindow()
        {
            InitializeComponent();

            _apiService = new ApiService();
            if (_apiService == null)
                MessageBox.Show("ApiService не создан. Проверьте конфигурацию.");

            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCaptcha();
            LoadRegCaptcha();
            LoginBox.Focus();
        }

        private void LoginTab_Click(object sender, MouseButtonEventArgs e)
        {
            ShowLoginTab();
        }

        private void RegisterTab_Click(object sender, MouseButtonEventArgs e)
        {
            ShowRegisterTab();
        }

        private void ShowLoginTab()
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;

            LoginTabPanel.Background = System.Windows.Media.Brushes.White;
            RegisterTabPanel.Background = System.Windows.Media.Brushes.Transparent;

            LoginTabText.Foreground = FindResource("PrimaryBrush") as System.Windows.Media.Brush;
            RegisterTabText.Foreground = FindResource("MutedTextBrush") as System.Windows.Media.Brush;

            HideError();
            LoginBox.Focus();
        }

        private void ShowRegisterTab()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;

            LoginTabPanel.Background = System.Windows.Media.Brushes.Transparent;
            RegisterTabPanel.Background = System.Windows.Media.Brushes.White;

            LoginTabText.Foreground = FindResource("MutedTextBrush") as System.Windows.Media.Brush;
            RegisterTabText.Foreground = FindResource("SuccessBrush") as System.Windows.Media.Brush;

            HideError();
            RegisterLoginBox.Focus();
        }

        private void RefreshCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCaptcha();
        }

        private void RefreshRegCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRegCaptcha();
        }

        private void LoadCaptcha()
        {
            try
            {
                _currentCaptchaCode = WpfCaptchaHelper.GenerateCaptchaCode(5);
                CaptchaImage.Source = WpfCaptchaHelper.GenerateCaptchaImage(_currentCaptchaCode, 250, 80);
                CaptchaInputBox.Clear();
            }
            catch (Exception ex)
            {
                ShowError("Не удалось создать капчу: " + ex.Message);
            }
        }

        private void LoadRegCaptcha()
        {
            try
            {
                _currentRegCaptchaCode = WpfCaptchaHelper.GenerateCaptchaCode(5);
                RegCaptchaImage.Source = WpfCaptchaHelper.GenerateCaptchaImage(_currentRegCaptchaCode, 250, 80);
                RegCaptchaInputBox.Clear();
            }
            catch (Exception ex)
            {
                ShowError("Не удалось создать капчу: " + ex.Message);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await LoginAsync();
        }

        private async System.Threading.Tasks.Task LoginAsync()
        {
            if (_isLoggingIn)
                return;

            HideError();

            string login = LoginBox.Text?.Trim();
            string password = PasswordBox.Password;
            string captcha = CaptchaInputBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин.");
                LoginBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль.");
                PasswordBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(captcha))
            {
                ShowError("Введите код с картинки.");
                CaptchaInputBox.Focus();
                return;
            }

            if (!string.Equals(captcha, _currentCaptchaCode, StringComparison.InvariantCultureIgnoreCase))
            {
                ShowError("Неверный код с картинки.");
                LoadCaptcha();
                CaptchaInputBox.Focus();
                return;
            }

            try
            {
                SetLoginState(true);

                var result = await _apiService.LoginAsync(login, password);

                if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
                {
                    ShowError("Сервер не вернул токен доступа.");
                    LoadCaptcha();
                    return;
                }

                _apiService.SetToken(result.AccessToken);

                var mainWindow = new MainWindow(_apiService, result.User);
                mainWindow.Show();

                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка входа: " + ex.Message);
                LoadCaptcha();
            }
            finally
            {
                SetLoginState(false);
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            await RegisterAsync();
        }

        private async System.Threading.Tasks.Task RegisterAsync()
        {
            if (_isRegistering)
                return;

            HideError();

            string login = RegisterLoginBox.Text?.Trim();
            string password = RegisterPasswordBox.Password;
            string fullName = RegisterFullNameBox.Text?.Trim();
            string captcha = RegCaptchaInputBox.Text?.Trim();
            string department = GetSelectedDepartment();

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин.");
                RegisterLoginBox.Focus();
                return;
            }

            if (login.Length < 3)
            {
                ShowError("Логин должен быть не короче 3 символов.");
                RegisterLoginBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль.");
                RegisterPasswordBox.Focus();
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Пароль должен быть не короче 4 символов.");
                RegisterPasswordBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("Введите ФИО.");
                RegisterFullNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(captcha))
            {
                ShowError("Введите код с картинки.");
                RegCaptchaInputBox.Focus();
                return;
            }

            if (!string.Equals(captcha, _currentRegCaptchaCode, StringComparison.InvariantCultureIgnoreCase))
            {
                ShowError("Неверный код с картинки.");
                LoadRegCaptcha();
                RegCaptchaInputBox.Focus();
                return;
            }

            try
            {
                SetRegisterState(true);


                var result = await _apiService.RegisterAsync(
                    login,
                    password,
                    fullName,
                    department);

                if (result == null)
                {
                    ShowError("Сервер не подтвердил регистрацию.");
                    return;
                }

                MessageBox.Show(
                    "Регистрация выполнена. Теперь можно войти в систему.",
                    "Готово",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RegisterLoginBox.Clear();
                RegisterPasswordBox.Clear();
                RegisterFullNameBox.Clear();
                RegisterDepartmentComboBox.SelectedIndex = 0;

                ShowLoginTab();

                LoginBox.Text = login;
                PasswordBox.Clear();

                LoadCaptcha();
                LoadRegCaptcha();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка регистрации: " + ex.Message);
                LoadRegCaptcha();
            }
            finally
            {
                SetRegisterState(false);
            }
        }

        private string GetSelectedDepartment()
        {
            if (RegisterDepartmentComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
                return item.Content.ToString();

            return "Технологический отдел";
        }

        private void SetLoginState(bool isLoading)
        {
            _isLoggingIn = isLoading;

            LoginButton.IsEnabled = !isLoading;
            RefreshCaptchaButton.IsEnabled = !isLoading;
            LoginBox.IsEnabled = !isLoading;
            PasswordBox.IsEnabled = !isLoading;
            CaptchaInputBox.IsEnabled = !isLoading;

            LoginButton.Content = isLoading ? "ВХОД..." : "ВОЙТИ";
            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private void SetRegisterState(bool isLoading)
        {
            _isRegistering = isLoading;

            RegisterButton.IsEnabled = !isLoading;
            RefreshRegCaptchaButton.IsEnabled = !isLoading;
            RegisterLoginBox.IsEnabled = !isLoading;
            RegisterPasswordBox.IsEnabled = !isLoading;
            RegisterFullNameBox.IsEnabled = !isLoading;
            RegisterDepartmentComboBox.IsEnabled = !isLoading;
            RegCaptchaInputBox.IsEnabled = !isLoading;

            RegisterButton.Content = isLoading ? "РЕГИСТРАЦИЯ..." : "ЗАРЕГИСТРИРОВАТЬСЯ";
            Cursor = isLoading ? Cursors.Wait : Cursors.Arrow;
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (LoginPanel.Visibility == Visibility.Visible)
                    await LoginAsync();
                else
                    await RegisterAsync();

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
                e.Handled = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // DragMove может упасть при клике на интерактивный элемент. Игнорируем.
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
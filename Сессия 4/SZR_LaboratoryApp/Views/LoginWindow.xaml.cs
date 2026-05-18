using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SZR_LaboratoryApp.Helpers;
using SZR_LaboratoryApp.Services;

namespace SZR_LaboratoryApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;

        // Для входа
        private string _currentCaptchaCode;
        private bool _isLoggingIn = false;

        // Для регистрации
        private string _currentRegCaptchaCode;

        public LoginWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();

            // Устанавливаем значения для тестирования (лаборант)
            LoginBox.Text = "lab.sidorova";
            PasswordBox.Password = "12345";

            // Загружаем капчи
            LoadCaptcha();
            LoadRegCaptcha();

            // Подписки на события
            LoginButton.Click += LoginButton_Click;
            RefreshCaptchaButton.Click += (s, e) => LoadCaptcha();
            RefreshRegCaptchaButton.Click += (s, e) => LoadRegCaptcha();
            KeyDown += LoginWindow_KeyDown;

            // Настройка вкладок
            LoginTabPanel.MouseLeftButtonUp += LoginTab_Click;


            // Возможность перетаскивания окна
            MouseLeftButtonDown += (s, e) => DragMove();
        }

        // ========== ПЕРЕКЛЮЧЕНИЕ ВКЛАДОК ==========
        private void LoginTab_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
            LoginTabText.Foreground = new SolidColorBrush(Color.FromRgb(46, 117, 182));
            ErrorText.Visibility = Visibility.Collapsed;
        }



        // ========== КАПЧА ДЛЯ ВХОДА ==========
        private void LoadCaptcha()
        {
            try
            {
                _currentCaptchaCode = WpfCaptchaHelper.GenerateCaptchaCode(5);
                var bitmap = WpfCaptchaHelper.GenerateCaptchaImage(_currentCaptchaCode);
                CaptchaImage.Source = bitmap;
                CaptchaInputBox.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки капчи: {ex.Message}");
                var random = new Random();
                _currentCaptchaCode = random.Next(1000, 9999).ToString();
                CaptchaImage.Source = null;
            }
        }

        // ========== КАПЧА ДЛЯ РЕГИСТРАЦИИ ==========
        private void LoadRegCaptcha()
        {
            try
            {
                _currentRegCaptchaCode = WpfCaptchaHelper.GenerateCaptchaCode(5);
                var bitmap = WpfCaptchaHelper.GenerateCaptchaImage(_currentRegCaptchaCode);
                RegCaptchaImage.Source = bitmap;
                RegCaptchaInputBox.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки капчи: {ex.Message}");
                var random = new Random();
                _currentRegCaptchaCode = random.Next(1000, 9999).ToString();
                RegCaptchaImage.Source = null;
            }
        }

        // ========== ВХОД ==========
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await Login();
        }

        private async Task Login()
        {
            if (_isLoggingIn) return;

            // Проверка CAPTCHA
            if (CaptchaInputBox.Text != _currentCaptchaCode)
            {
                ShowError("Неверный код с картинки");
                LoadCaptcha();
                return;
            }

            _isLoggingIn = true;

            try
            {
                LoginButton.IsEnabled = false;
                ErrorText.Visibility = Visibility.Collapsed;

                var result = await _apiService.LoginAsync(LoginBox.Text, PasswordBox.Password);

                if (result != null && !string.IsNullOrEmpty(result.AccessToken))
                {
                    _apiService.SetToken(result.AccessToken);

                    var mainWindow = new MainWindow(_apiService, result.User);
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                    _isLoggingIn = false;
                    LoginButton.IsEnabled = true;
                    LoadCaptcha();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                _isLoggingIn = false;
                LoginButton.IsEnabled = true;
                LoadCaptcha();
            }
        }

      
        // ========== ОБЩИЕ МЕТОДЫ ==========
        private async void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (LoginPanel.Visibility == Visibility.Visible)
                {
                    await Login();
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                ErrorText.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
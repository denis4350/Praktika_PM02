using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;

namespace SZR_OperatorApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;
        private bool _isLoggingIn = false;

        public LoginWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();

            // Подписка на изменение роли
            RoleBox.SelectionChanged += (s, e) =>
            {
                var selected = RoleBox.SelectedItem as ComboBoxItem;
                if (selected != null && selected.Tag != null)
                {
                    LoginBox.Text = selected.Tag.ToString();
                }
            };

            // Устанавливаем начальное значение (Аппаратчик)
            RoleBox.SelectedIndex = 0;

            // Возможность перетаскивания окна
            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await Login();
        }

        private async Task Login()
        {
            if (_isLoggingIn) return;

            _isLoggingIn = true;
            LoginButton.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;

            try
            {
                System.Diagnostics.Debug.WriteLine("=== Login START ===");
                System.Diagnostics.Debug.WriteLine($"Login: {LoginBox.Text}");
                System.Diagnostics.Debug.WriteLine($"Password: {PasswordBox.Password}");

                var result = await _apiService.LoginAsync(LoginBox.Text, PasswordBox.Password);

                System.Diagnostics.Debug.WriteLine($"Result is null: {result == null}");

                if (result != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User Id: {result.Id}");
                    System.Diagnostics.Debug.WriteLine($"User Login: {result.Login}");
                    System.Diagnostics.Debug.WriteLine($"User Role: {result.Role}");
                    System.Diagnostics.Debug.WriteLine($"Token is null: {string.IsNullOrEmpty(result.Token)}");
                    System.Diagnostics.Debug.WriteLine($"Token length: {result.Token?.Length ?? 0}");

                    if (!string.IsNullOrEmpty(result.Token))
                    {
                        System.Diagnostics.Debug.WriteLine("Token получен, устанавливаем в ApiService");
                        _apiService.SetToken(result.Token);

                        // Проверяем, что токен установился
                        var testToken = _apiService.GetToken();
                        System.Diagnostics.Debug.WriteLine($"Токен в ApiService: {(string.IsNullOrEmpty(testToken) ? "НЕТ" : "ЕСТЬ")}");

                        System.Diagnostics.Debug.WriteLine("Открываем главное окно");
                        var mainWindow = new MainWindow(_apiService, result);
                        mainWindow.Show();
                        Close();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Токен пустой!");
                        ShowError("Ошибка авторизации: токен не получен");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("result == null, вход не удался");
                    ShowError("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                ShowError(ex.Message);
            }
            finally
            {
                _isLoggingIn = false;
                LoginButton.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine("=== Login END ===");
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
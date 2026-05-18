using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly ApiService _apiService;
        private bool _isSaving;

        public ChangePasswordWindow(ApiService apiService)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            Loaded += (s, e) => OldPasswordBox.Focus();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangePasswordAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ChangePasswordAsync();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private async Task ChangePasswordAsync()
        {
            if (_isSaving)
                return;

            try
            {
                HideError();

                string oldPassword = OldPasswordBox.Password;
                string newPassword = NewPasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(oldPassword))
                {
                    ShowError("Введите текущий пароль.");
                    OldPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ShowError("Введите новый пароль.");
                    NewPasswordBox.Focus();
                    return;
                }

                if (newPassword.Length < 4)
                {
                    ShowError("Новый пароль должен быть не менее 4 символов.");
                    NewPasswordBox.Focus();
                    return;
                }

                if (oldPassword == newPassword)
                {
                    ShowError("Новый пароль не должен совпадать с текущим.");
                    NewPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ShowError("Подтвердите новый пароль.");
                    ConfirmPasswordBox.Focus();
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    ShowError("Новый пароль и подтверждение не совпадают.");
                    ConfirmPasswordBox.Focus();
                    return;
                }

                SetSavingState(true);

                bool success = await _apiService.ChangePasswordAsync(oldPassword, newPassword);

                if (!success)
                {
                    ShowError("Не удалось изменить пароль. Проверьте текущий пароль и попробуйте снова.");
                    return;
                }

                MessageBox.Show(
                    "Пароль успешно изменён. При следующем входе используйте новый пароль.",
                    "Готово",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка смены пароля: " + ex.Message);
            }
            finally
            {
                SetSavingState(false);
            }
        }

        private void SetSavingState(bool isSaving)
        {
            _isSaving = isSaving;

            SaveButton.IsEnabled = !isSaving;
            CancelButton.IsEnabled = !isSaving;

            OldPasswordBox.IsEnabled = !isSaving;
            NewPasswordBox.IsEnabled = !isSaving;
            ConfirmPasswordBox.IsEnabled = !isSaving;

            SaveButton.Content = isSaving ? "Сохранение..." : "Сохранить";
            Cursor = isSaving ? Cursors.Wait : Cursors.Arrow;
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
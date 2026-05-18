using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SZR_TechnologistApp.Models;
using SZR_TechnologistApp.Services;

namespace SZR_TechnologistApp.Views
{
    public partial class ProfileWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _user;

        public ProfileWindow(ApiService apiService, UserInfoDto user)
        {
            InitializeComponent();
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _user = user ?? throw new ArgumentNullException(nameof(user));

            FullNameText.Text = _user.FullName;
            LoginText.Text = _user.Login;
            RoleText.Text = _user.Role;
            DepartmentText.Text = _user.Department ?? "Не указан";

            LoadAvatarInProfile();

            CloseButton.Click += (s, e) => Close();
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "?";
            var parts = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return fullName.Substring(0, 1).ToUpper();
        }

        private BitmapImage ByteArrayToBitmapImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private async void LoadAvatarInProfile()
        {
            try
            {
                var avatarBytes = await _apiService.GetAvatarAsync(_user.Id);
                if (avatarBytes != null && avatarBytes.Length > 0)
                {
                    var bitmap = ByteArrayToBitmapImage(avatarBytes);
                    if (bitmap != null)
                    {
                        AvatarImage.Source = bitmap;
                        AvatarImage.Visibility = Visibility.Visible;
                        InitialsText.Visibility = Visibility.Collapsed;
                        return;
                    }
                }

                AvatarImage.Visibility = Visibility.Collapsed;
                InitialsText.Visibility = Visibility.Visible;
                InitialsText.Text = GetInitials(_user.FullName);
            }
            catch { /* Игнорируем ошибки загрузки */ }
        }

        private async void ChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите фото для аватара"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imageBytes = File.ReadAllBytes(dialog.FileName);
                    if (imageBytes.Length > 2 * 1024 * 1024)
                    {
                        MessageBox.Show("Файл слишком большой. Максимальный размер 2 MB.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var success = await _apiService.UploadAvatarAsync(_user.Id, imageBytes, Path.GetFileName(dialog.FileName));

                    if (success)
                    {
                        var bitmap = ByteArrayToBitmapImage(imageBytes);
                        AvatarImage.Source = bitmap;
                        AvatarImage.Visibility = Visibility.Visible;
                        InitialsText.Visibility = Visibility.Collapsed;

                        if (Owner is MainWindow mainWindow)
                        {
                            mainWindow.UpdateAvatar(imageBytes);
                        }

                        MessageBox.Show("Фото профиля обновлено!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        MessageBox.Show("Ошибка при загрузке фото на сервер.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
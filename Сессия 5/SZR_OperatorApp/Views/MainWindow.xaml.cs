using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Services;
using SZR_OperatorApp.Views;

namespace SZR_OperatorApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly UserInfoDto _currentUser;
        private DispatcherTimer _refreshTimer;
        private ActiveBatchesView _activeBatchesView;

        public MainWindow(ApiService apiService, UserInfoDto user)
        {
            InitializeComponent();
            _apiService = apiService;
            _currentUser = user;

            UserInfoText.Text = $"{user.FullName} ({user.Role})";
            ShiftText.Text = GetCurrentShift();

            // Подписка на меню
            ActiveBatchesMenu.Click += (s, e) => LoadActiveBatches();
            BatchProgramMenu.Click += (s, e) => LoadBatchProgram();
            ExtruderLiveMenu.Click += (s, e) => LoadExtruderLive();
            JournalMenu.Click += (s, e) => LoadBatchJournal();
            ReportProblemMenu.Click += (s, e) => LoadReportProblem();
            LogoutButton.Click += (s, e) => Logout();
            NotificationsButton.Click += NotificationsButton_Click;




            LoadActiveBatches();
            UpdateNotificationsBadge();  // ← ВЫЗОВ ПРИ ЗАПУСКЕ
        }

        private string GetCurrentShift()
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 6 && hour < 14) return "Смена 1 (дневная)";
            if (hour >= 14 && hour < 22) return "Смена 2 (вечерняя)";
            return "Смена 3 (ночная)";
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                StatusBarText.Text = message;
                StatusBarText.Foreground = isError ?
                    System.Windows.Media.Brushes.Red :
                    System.Windows.Media.Brushes.Green;
            });
        }

        private void UpdateConnectionIndicator(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionIndicator.Background = isConnected ?
                    System.Windows.Media.Brushes.Green :
                    System.Windows.Media.Brushes.Red;
            });
        }

        private void RefreshCurrentView()
        {
            if (_activeBatchesView != null && _activeBatchesView.IsVisible)
            {
                _activeBatchesView.RefreshData();
            }
        }

        public void LoadActiveBatches()
        {
            UpdateSectionTitle("Активные партии");
            SetActiveMenu("active");
            if (_activeBatchesView == null)
            {
                _activeBatchesView = new ActiveBatchesView(_apiService, _currentUser);
                _activeBatchesView.BatchSelected += OnBatchSelected;
            }
            MainContent.Content = _activeBatchesView;
            _activeBatchesView.RefreshData();  // если есть такой метод, иначе просто загрузить
            BatchProgramMenu.IsEnabled = false;
        }

        public void LoadBatchProgram(string batchNumber = null)
        {
            UpdateSectionTitle($"Программа партии: {batchNumber ?? "..."}");
            SetActiveMenu("program");
            var programView = new BatchProgramView(_apiService, _currentUser, batchNumber);
            programView.StepCompleted += OnStepCompleted;
            MainContent.Content = programView;
        }

        public void LoadExtruderLive()
        {
            UpdateSectionTitle("Экструдер LIVE");
            SetActiveMenu("extruder");
            var extruderView = new ExtruderLiveView(_apiService);
            MainContent.Content = extruderView;
        }

        public void LoadBatchJournal()
        {
            UpdateSectionTitle("Журнал партии");
            SetActiveMenu("journal");
            var journalView = new BatchJournalView(_apiService, _currentUser);
            MainContent.Content = journalView;
        }

        public void LoadReportProblem()
        {
            UpdateSectionTitle("Сообщить о проблеме");
            SetActiveMenu("problem");
            var problemView = new ReportProblemView(_apiService, _currentUser);
            MainContent.Content = problemView;
        }

        private void OnBatchSelected(string batchNumber)
        {
            LoadBatchProgram(batchNumber);
            BatchProgramMenu.IsEnabled = true;
        }

        private void OnStepCompleted()
        {
            LoadActiveBatches();
        }

        private void SetActiveMenu(string menuName)
        {
            var buttons = new[] { ActiveBatchesMenu, BatchProgramMenu, ExtruderLiveMenu, JournalMenu, ReportProblemMenu };
            foreach (var btn in buttons)
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                btn.Foreground = System.Windows.Media.Brushes.White;
                btn.FontWeight = FontWeights.Normal;
            }

            Button activeButton = null;
            switch (menuName)
            {
                case "active": activeButton = ActiveBatchesMenu; break;
                case "program": activeButton = BatchProgramMenu; break;
                case "extruder": activeButton = ExtruderLiveMenu; break;
                case "journal": activeButton = JournalMenu; break;
                case "problem": activeButton = ReportProblemMenu; break;
            }

            if (activeButton != null)
            {
                activeButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 188, 156));
                activeButton.Foreground = System.Windows.Media.Brushes.White;
                activeButton.FontWeight = FontWeights.Bold;
            }
        }

        private void Logout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }
        public void UpdateSectionTitle(string title)
        {
            CurrentSectionTitle.Text = title;
        }

        private async void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var notifications = await _apiService.GetNotificationsAsync();
                var window = new NotificationsWindow(notifications);
                window.Owner = this;
                window.ShowDialog();
                UpdateNotificationsBadge();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}");
            }
        }
        public async void UpdateNotificationsBadge()
        {
            try
            {
                var notifications = await _apiService.GetNotificationsAsync();

                System.Diagnostics.Debug.WriteLine($"Notifications count: {notifications?.Count ?? 0}");

                int unreadCount = 0;
                if (notifications != null)
                {
                    unreadCount = notifications.Count(n => !n.IsRead);
                    foreach (var n in notifications)
                    {
                        System.Diagnostics.Debug.WriteLine($"Notif: {n.Title} - Read: {n.IsRead}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    if (unreadCount > 0)
                    {
                        NotificationsButton.Content = $"🔔 {unreadCount}";
                        NotificationsButton.Background = new SolidColorBrush(Colors.Red);
                        NotificationsButton.Foreground = Brushes.White;
                    }
                    else
                    {
                        NotificationsButton.Content = "🔔";
                        NotificationsButton.Background = Brushes.Transparent;
                        NotificationsButton.Foreground = Brushes.LightGray;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateNotificationsBadge error: {ex.Message}");
            }
        }

    }
}
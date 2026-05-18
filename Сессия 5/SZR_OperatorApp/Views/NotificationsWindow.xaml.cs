using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SZR_OperatorApp.Models;  // ← добавить

namespace SZR_OperatorApp.Views
{
    public partial class NotificationsWindow : Window
    {
        public NotificationsWindow(List<Models.Notification> notifications)
        {
            InitializeComponent();

            System.Diagnostics.Debug.WriteLine($"NotificationsWindow: {notifications?.Count ?? 0} notifications");

            if (notifications == null || notifications.Count == 0)
            {
                var emptyList = new List<Models.Notification>
                {
                    new Models.Notification { Title = "Нет уведомлений", Message = "У вас нет новых уведомлений", CreatedAt = DateTime.Now }
                };
                NotificationsList.ItemsSource = emptyList;
            }
            else
            {
                NotificationsList.ItemsSource = notifications;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
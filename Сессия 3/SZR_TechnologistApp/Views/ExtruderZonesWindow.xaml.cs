using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SZR_TechnologistApp.Models;

namespace SZR_TechnologistApp.Views
{
    public partial class ExtruderZonesWindow : Window
    {
        private readonly ExtruderProgramDetailDto _detail;

        public ExtruderZonesWindow(ExtruderProgramDetailDto detail)
        {
            InitializeComponent();
            _detail = detail ?? throw new ArgumentNullException(nameof(detail));
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadProgramData();
        }

        private void LoadProgramData()
        {
            var program = _detail.Program;

            ProgramNameText.Text = string.IsNullOrWhiteSpace(program?.Name) ? "—" : program.Name;
            ProductNameText.Text = string.IsNullOrWhiteSpace(program?.ProductName) ? "—" : program.ProductName;
            StatusText.Text = string.IsNullOrWhiteSpace(program?.Status) ? "—" : program.Status;

            CreatedAtText.Text = program?.CreatedAt == null || program.CreatedAt == default(DateTime)
                ? "—"
                : program.CreatedAt.ToString("dd.MM.yyyy HH:mm");

            HeaderSubtitleText.Text = string.IsNullOrWhiteSpace(program?.Description)
                ? "Параметры температурных и технологических зон"
                : program.Description;

            ApplyStatusStyle(program?.Status);

            var zones = _detail.Zones;

            if (zones == null || zones.Count == 0)
            {
                ZonesListView.ItemsSource = null;
                ZonesCountText.Text = "0";
                FooterText.Text = "Для этой программы зоны не заданы.";
                return;
            }

            var sortedZones = zones.OrderBy(z => z.ZoneNumber).ToList();
            ZonesListView.ItemsSource = sortedZones;
            ZonesCountText.Text = sortedZones.Count.ToString();

            decimal minTemperature = sortedZones.Min(z => z.TemperatureMin);
            decimal maxTemperature = sortedZones.Max(z => z.TemperatureMax);
            decimal minPressure = sortedZones.Min(z => z.PressureMin);
            decimal maxPressure = sortedZones.Max(z => z.PressureMax);

            FooterText.Text =
                $"Диапазон температур: {minTemperature}–{maxTemperature} °C; " +
                $"давление: {minPressure}–{maxPressure} бар.";
        }

        private void ApplyStatusStyle(string status)
        {
            if (status == "Активна")
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
            }
            else if (status == "Черновик")
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
            }
            else if (status == "Архивирована")
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter) Close();
        }
    }
}
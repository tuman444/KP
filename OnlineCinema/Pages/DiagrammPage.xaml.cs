using OnlineCinema.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization.Charting;   // Chart, Series, ChartArea...
using System.Windows.Forms.Integration;                   // WindowsFormsHost

namespace OnlineCinema.Pages
{
    /// <summary>
    /// Логика взаимодействия для DiagrammPage.xaml
    /// </summary>
    public partial class DiagrammPage : Page
    {
        // Chart — это элемент Windows Forms. В WPF его показывают через WindowsFormsHost.
        private readonly Chart _chart = new Chart();
        private bool _ready;

        public DiagrammPage()
        {
            InitializeComponent();

            // ---- настраиваем диаграмму ----
            _chart.ChartAreas.Add(new ChartArea("Main"));
            _chart.Series.Add(new Series("Данные") { IsValueShownAsLabel = true });
            _chart.Legends.Add(new Legend("Legend"));

            // Host создаём в коде: так не нужны ссылки в XAML и не бывает проблем дизайнера
            ChartContainer.Children.Add(new WindowsFormsHost { Child = _chart });

            // ---- заполняем выпадающие списки ----
            CmbMetric.ItemsSource = new[]
            {
                "Фильмов в каждом жанре",
                "Средняя оценка фильмов",
                "Просмотры пользователя по жанрам"
            };

            // Не все типы SeriesChartType подходят для простых данных (например, Stock или
            // Candlestick требуют по 4 значения и вызовут ошибку), поэтому даём проверенный список.
            CmbDiagram.ItemsSource = new List<KeyValuePair<string, SeriesChartType>>
            {
                new KeyValuePair<string, SeriesChartType>("Столбчатая", SeriesChartType.Column),
                new KeyValuePair<string, SeriesChartType>("Линейчатая", SeriesChartType.Bar),
                new KeyValuePair<string, SeriesChartType>("Круговая", SeriesChartType.Pie),
                new KeyValuePair<string, SeriesChartType>("Кольцевая", SeriesChartType.Doughnut),
                new KeyValuePair<string, SeriesChartType>("График", SeriesChartType.Line),
                new KeyValuePair<string, SeriesChartType>("Плавный график", SeriesChartType.Spline),
                new KeyValuePair<string, SeriesChartType>("Область", SeriesChartType.Area),
                new KeyValuePair<string, SeriesChartType>("Точечная", SeriesChartType.Point)
            };

            using (var db = new OnlineCinemaDBEntities())
                CmbUser.ItemsSource = db.Users.OrderBy(u => u.full_name).ToList();

            CmbMetric.SelectedIndex = 0;
            CmbDiagram.SelectedIndex = 0;
            _ready = true;
            UpdateChart();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Графики и отчёты: только для администратора
            if (!CurrentUser.IsAdmin)
            {
                MessageBox.Show("Доступ только для администратора.", "Нет доступа",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.Navigate(new HomePage());
            }
        }

        private void Param_Changed(object sender, SelectionChangedEventArgs e) => UpdateChart();

        private void UpdateChart()
        {
            if (!_ready) return;

            int metric = CmbMetric.SelectedIndex;
            CmbUser.IsEnabled = metric == 2;      // пользователь нужен только третьему показателю

            if (!(CmbDiagram.SelectedItem is KeyValuePair<string, SeriesChartType> chosen)) return;

            string title = "";
            var data = new List<KeyValuePair<string, double>>();

            switch (metric)
            {
                case 0:
                    title = "Количество фильмов по жанрам";
                    data = StatsService.MoviesPerGenre();
                    break;
                case 1:
                    title = "Средняя оценка фильмов (из 10)";
                    data = StatsService.AverageRatingPerMovie();
                    break;
                case 2:
                    {
                        var user = CmbUser.SelectedItem as Users;
                        if (user == null)
                        {
                            title = "Выберите пользователя";
                        }
                        else
                        {
                            title = "Просмотры пользователя «" + user.full_name + "» по жанрам";
                            data = StatsService.ViewsByGenreForUser(user.id);
                        }
                        break;
                    }
            }

            // ---- перерисовываем ----
            Series series = _chart.Series[0];
            series.ChartType = chosen.Value;
            series.Points.Clear();
            foreach (var kv in data)
                series.Points.AddXY(kv.Key, kv.Value);

            _chart.Titles.Clear();
            _chart.Titles.Add(data.Count == 0 && metric != 2 ? title + " (нет данных)" : title);
        }

        // ===================== Выгрузка =====================
        private void ButtonWord_Click(object sender, RoutedEventArgs e) => Export(ReportExporter.ExportToWord);
        private void ButtonExcel_Click(object sender, RoutedEventArgs e) => Export(ReportExporter.ExportToExcel);

        private void Export(Func<string> exporter)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;   // курсор "ожидание"
                string path = exporter();
                Mouse.OverrideCursor = null;

                MessageBox.Show("Отчёт сохранён:\n" + path, "Готово",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show("Не удалось создать отчёт.\nПроверьте, что установлен Microsoft Office.\n\n" + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

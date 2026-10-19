using OnlineCinema.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;
using static OnlineCinema.Helpers.PasswordHelper;

namespace OnlineCinema
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isLightTheme;
        public MainWindow()
        {
            InitializeComponent();
            CurrentUser.Changed += UpdateAuthButton; // кнопка в шапке реагирует на вход и выход
            UpdateAuthButton();

            // кнопка назад появляется только когда есть история переходов
            MainFrame.Navigated += (s, e) => BackBuuton.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            StartClock();
            MainFrame.Navigate(new Pages.HomePage());
        }

        // часы в нижней панели
        private void StartClock()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1)};
            timer.Tick += (s, e) => DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            timer.Start();
            DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        // подтверждение закрытия
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var answer = MessageBox.Show("Закрыть приложение?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(answer == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        // смена темы
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            _isLightTheme = !_isLightTheme;
            SwitchTheme(_isLightTheme ? "DictionaryLight" : "Dictionary");
            ThemeButton.Content = _isLightTheme ? "☀️" : "🌙";
        }

        private void SwitchTheme(string dictionaryFileName)
        {
            var uri = new Uri($"Styles/{dictionaryFileName}.xaml", UriKind.Relative);
            var dict = Application.LoadComponent(uri) as ResourceDictionary;

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        // кнопка входа/личного кабинета
        private void UpdateAuthButton()
        {
            AuthButton.Content = CurrentUser.IsLoggedIn ? CurrentUser.FullName : "Войти";
        }

        private void AuthButton_Click (object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.IsLoggedIn)
            {
                MainFrame.Navigate(new Pages.AuthPage());
                return;
            }
            var answer = MessageBox.Show("Выти из аккаунта?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                CurrentUser.Logout();
                MainFrame.Navigate(new Pages.HomePage());
            }
        }

        // навигация
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack) MainFrame.GoBack();
        }

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.HomePage());
        }

        // раздел только для вошедших пользователей
        private bool RequireLogin()
        {
            if (CurrentUser.IsLoggedIn) return true; 
            MessageBox.Show("Этот раздел доступен только для зарегстрированных пользователей", "Требуется вход", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            MainFrame.Navigate(new Pages.AuthPage());
            return false;
        }

        private void NavFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (RequireLogin()) MessageBox.Show("Странница избранного скоро появится");
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
            if (RequireLogin()) MessageBox.Show("Странница истории просмотров скоро появится");
        }

        private void NavRecommendations_Click(object sender, RoutedEventArgs e)
        {
            if (RequireLogin()) MessageBox.Show("Странница рекомендаций скоро появится");
        }
    }
}

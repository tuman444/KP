using OnlineCinema.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
using static OnlineCinema.Helpers.PasswordHelper;

namespace OnlineCinema.Pages
{
    /// <summary>
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private int failedAttempts = 0;
        private const int MaxAttempts = 3;                    // после стольких ошибок показываем капчу
        private static readonly Random Rnd = new Random();
        public AuthPage()
        {
            InitializeComponent();
        }

        private void ButtonEnter_Click(object sender, RoutedEventArgs e)
        {
            string email = TextBoxLogin.Text.Trim();
            string password = PasswordBox.Password;

            // поля не должны быть пустыми
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // хешируем введенный пароль
            string hash = PasswordHelper.GetHash(password);

            // ищем пользователя с таким email и хешем, Include подтягивает его роль
            using (var db = new OnlineCinemaDBEntities())
            {
                var user = db.Users.Include(u => u.roles)
                    .AsNoTracking()
                    .FirstOrDefault(u => u.email == email && u.password_hash == hash);

                // не нашли - ошибка
                if (user == null)
                {
                    failedAttempts++;
                    MessageBox.Show("Неверный email или пароль", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);

                    // Три ошибки подряд -> капча
                    if (failedAttempts >= MaxAttempts)
                        ShowCaptcha();
                    return;
                }

                // нашли - запоминаем в текущем пользователе
                CurrentUser.Login(user.id, user.full_name, user.email, user.roles.name);
            }

            NavigationService.Navigate(new Pages.HomePage());
        }

        // капча
        private void ShowCaptcha()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            CaptchaPanel.Visibility = Visibility.Visible;
            ButtonEnter.IsDefault = false;
            ButtonCaptchaOk.IsDefault = true;
            TitleText.Text = "Проверка";
            GenerateCaptcha();
        }

        private void HideCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            ButtonCaptchaOk.IsDefault = false;
            ButtonEnter.IsDefault = true;
            TitleText.Text = "Вход";
            PasswordBox.Clear();
            failedAttempts = 0;      // даём новые три попытки
        }

        private void GenerateCaptcha()
        {
            // Убраны похожие символы (0/O, 1/l/I), чтобы капчу было реально прочитать
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var chars = new char[6];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = alphabet[Rnd.Next(alphabet.Length)];

            CaptchaText.Text = new string(chars);
            CaptchaInput.Clear();
        }

        private void ButtonCaptchaOk_Click(object sender, RoutedEventArgs e)
        {
            // Сравнение с учётом регистра (Ordinal)
            if (string.Equals(CaptchaInput.Text.Trim(), CaptchaText.Text, StringComparison.Ordinal))
            {
                MessageBox.Show("Капча пройдена. Повторите вход.", "Проверка пройдена",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                HideCaptcha();
            }
            else
            {
                MessageBox.Show("Неверная капча. Символы изменены, попробуйте ещё раз.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                GenerateCaptcha();   // при ошибке сразу новая капча
            }
        }

        private void ButtonCaptchaRefresh_Click(object sender, RoutedEventArgs e) => GenerateCaptcha();

        // Запрет копирования, вырезания и вставки в поле капчи
        private void Captcha_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void ButtonReg_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Pages.RegPage());
        }

        private void ButtonGuest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Pages.HomePage());
        }

        private void ButtonChangePass_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChangePassPage());
        }
    }
}
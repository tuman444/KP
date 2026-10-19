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
                    MessageBox.Show("Неверный email или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // нашли - запоминаем в текущем пользователе
                CurrentUser.Login(user.id, user.full_name, user.email, user.roles.name);
            }

            NavigationService.Navigate(new Pages.HomePage());
        }

        private void ButtonReg_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Pages.RegPage());
        }

        private void ButtonGuest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Pages.HomePage());
        }

        
    }
}
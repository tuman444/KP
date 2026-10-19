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
using static OnlineCinema.Helpers.PasswordHelper;

namespace OnlineCinema.Pages
{
    /// <summary>
    /// Логика взаимодействия для ChangePassPage.xaml
    /// </summary>
    public partial class ChangePassPage : Page
    {
        public ChangePassPage()
        {
            InitializeComponent();
            // Если пользователь уже вошёл, подставляем его email, чтобы не вводить заново
            if (CurrentUser.IsLoggedIn)
                TbLogin.Text = CurrentUser.Email;
        }

        private static void Warn(string text) =>
            MessageBox.Show(text, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            string email = TbLogin.Text.Trim();
            string oldPass = PbCurrent.Password;
            string newPass = PbNew.Password;
            string confirm = PbConfirm.Password;

            // 1. Все поля обязательны
            if (email.Length == 0 || oldPass.Length == 0 || newPass.Length == 0 || confirm.Length == 0)
            {
                Warn("Все поля обязательны к заполнению!");
                return;
            }

            string oldHash = PasswordHelper.GetHash(oldPass);

            using (var db = new OnlineCinemaDBEntities())
            {
                // 2. Проверяем, что email и текущий пароль верные
                //    (контекст ОТСЛЕЖИВАЕТ найденный объект, поэтому после изменения
                //     свойства достаточно SaveChanges() — без отдельного UPDATE)
                var user = db.Users.FirstOrDefault(u => u.email == email && u.password_hash == oldHash);
                if (user == null)
                {
                    Warn("Email или текущий пароль указаны неверно!");
                    return;
                }

                // 3. Проверки нового пароля
                string error = PasswordValidator.Validate(newPass);
                if (error != null) { Warn(error); return; }

                if (newPass != confirm)
                {
                    Warn("Новые пароли не совпадают!");
                    return;
                }
                if (newPass == oldPass)
                {
                    Warn("Новый пароль должен отличаться от текущего!");
                    return;
                }

                // 4. Сохраняем ХЕШ нового пароля
                user.password_hash = PasswordHelper.GetHash(newPass);
                db.SaveChanges();
            }

            MessageBox.Show("Пароль успешно изменён.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            GoBack();
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e) => GoBack();

        // Вернуться туда, откуда пришли: в профиль (если вошли) или на страницу входа
        private void GoBack()
        {
            if (CurrentUser.IsLoggedIn)
                NavigationService?.Navigate(new ProfilePage());
            else
                NavigationService?.Navigate(new AuthPage());
        }
    }
}

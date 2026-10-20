using OnlineCinema.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        private void ProfilePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Гостю личный кабинет не показываем
            if (!CurrentUser.IsLoggedIn)
            {
                NavigationService?.Navigate(new AuthPage());
                return;
            }

            // Панель администратора видна только админу
            AdminPanel.Visibility = CurrentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            LoadProfile();
        }

        /// <summary>Читает из БД данные ТЕКУЩЕГО авторизованного пользователя и показывает их.</summary>
        private void LoadProfile()
        {
            try
            {
                using (var db = new OnlineCinemaDBEntities())
                {
                    // CurrentUser.Id запомнили при входе. Include подгружает роль и предпочтения.
                    var user = db.Users
                                 .Include(u => u.roles)
                                 .Include(u => u.genres)
                                 .Include(u => u.persons)
                                 .FirstOrDefault(u => u.id == CurrentUser.Id);

                    if (user == null)
                    {
                        MessageBox.Show("Ваша учётная запись не найдена в базе данных.\nВыйдите и войдите заново.",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    TxtName.Text = user.full_name;
                    TxtEmail.Text = user.email;
                    DatePickerBirth.SelectedDate = user.birth_date;
                    TxtRole.Text = user.roles.name;
                    TxtRegDate.Text = user.registration_date.ToString("dd.MM.yyyy");

                    // Заполняем списки и ОТМЕЧАЕМ то, что пользователь выбрал раньше
                    var allGenres = db.genres.OrderBy(g => g.name).ToList();
                    ListGenres.ItemsSource = allGenres;
                    var myGenreIds = user.genres.Select(g => g.id).ToList();
                    foreach (var item in allGenres.Where(g => myGenreIds.Contains(g.id)))
                        ListGenres.SelectedItems.Add(item);

                    var allPersons = db.persons.OrderBy(p => p.full_name).ToList();
                    ListPersons.ItemsSource = allPersons;
                    var myPersonIds = user.persons.Select(p => p.id).ToList();
                    foreach (var item in allPersons.Where(x => myPersonIds.Contains(x.id)))
                        ListPersons.SelectedItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Ошибку показываем, а не глотаем: так видно, что именно сломалось
                string details = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Не удалось загрузить профиль:\n" + details,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void Warn(string text) =>
            MessageBox.Show(text, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

        // ===================== Сохранение изменений =====================
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim();
            string email = TxtEmail.Text.Trim();

            // Сначала все проверки, потом запись в БД
            if (name.Length == 0 || email.Length == 0 || DatePickerBirth.SelectedDate == null)
            { Warn("Заполните все обязательные поля"); return; }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            { Warn("Введите корректный email"); return; }

            DateTime birth = DatePickerBirth.SelectedDate.Value;
            if (birth > DateTime.Today || birth < DateTime.Today.AddYears(-120))
            { Warn("Укажите корректную дату рождения"); return; }

            var genreIds = ListGenres.SelectedItems.Cast<genres>().Select(g => g.id).ToList();
            var personIds = ListPersons.SelectedItems.Cast<persons>().Select(p => p.id).ToList();

            try
            {
                using (var db = new OnlineCinemaDBEntities())
                {
                    // Email уникален среди ДРУГИХ пользователей (свой оставить можно)
                    if (db.Users.Any(u => u.email == email && u.id != CurrentUser.Id))
                    { Warn("Этот email уже занят другим пользователем"); return; }

                    var user = db.Users
                                 .Include(u => u.genres)
                                 .Include(u => u.persons)
                                 .First(u => u.id == CurrentUser.Id);

                    user.full_name = name;
                    user.email = email;
                    user.birth_date = birth;
                    // Роль (role_id) НЕ меняем: пользователь не должен сам себе давать права админа

                    user.genres.Clear();
                    foreach (var genre in db.genres.Where(x => genreIds.Contains(x.id)))
                        user.genres.Add(genre);

                    user.persons.Clear();
                    foreach (var person in db.persons.Where(x => personIds.Contains(x.id)))
                        user.persons.Add(person);

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                string details = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Не удалось сохранить изменения:\n" + details,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Обновляем "текущего пользователя": имя на кнопке в шапке поменяется само
            CurrentUser.Login(CurrentUser.Id, name, email, CurrentUser.RoleName);

            MessageBox.Show("Изменения сохранены.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ButtonChangePass_Click(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new ChangePassPage());

        private void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show("Выйти из аккаунта?", "Выход",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            CurrentUser.Logout();
            NavigationService?.Navigate(new HomePage());
        }

        // Администратор 
        // Кнопки скрыты у обычного пользователя, но роль проверяем и в коде:
        // защита не должна держаться только на скрытии элементов.
        private void ButtonUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.IsAdmin) return;
            //NavigationService?.Navigate(new UsersPage());
        }

        private void ButtonCharts_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.IsAdmin) return;
            //NavigationService?.Navigate(new DiagrammPage());
        }

        private void ButtonStub_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Этот раздел появится на следующем этапе:\nадминистратор сможет загружать и редактировать свои фильмы.",
                            "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using OnlineCinema.Helpers;


namespace OnlineCinema.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegPage.xaml
    /// </summary>
    public partial class RegPage : Page
    {
        public RegPage()
        {
            InitializeComponent();
        }

        // при открытие странниц заполняем выподающий список и списки выбора из бд
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = new OnlineCinemaDBEntities())
            {
                var rolelist = db.roles.OrderBy(r => r.id).ToList();

                ComboRole.ItemsSource = rolelist;
                ComboRole.SelectedItem = rolelist.FirstOrDefault(r => r.name == "Пользователь") ?? rolelist.FirstOrDefault();

                ListGenres.ItemsSource  = db.genres.OrderBy(g => g.name).ToList();
                ListPersons.ItemsSource = db.persons.OrderBy(p => p.full_name).ToList();
            }
        }

        private static void Warn(string text) => MessageBox.Show(text, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

        private void ButtonRegister_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string pass1 = firstPass.Password;
            string pass2 = secondPass.Password;

            if (name.Length == 0 || email.Length == 0 || pass1.Length == 0 || pass2.Length == 0 || DatePickerBirth.SelectedDate == null || ComboRole.SelectedIndex == null)
            {
                Warn("Заполните все обязательные поля");
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                Warn("Введите корректный email");
                return;
            }

            if (pass1.Length < 6)
            {
                Warn("Пароль должени содержать больше 6 символ");
                return;
            }

            if (pass2.Length != pass1.Length)
            {
                Warn("Пароли не совпадают");
                return;
            }

            DateTime birth = DatePickerBirth.SelectedDate.Value;
            if (birth > DateTime.Today || birth < DateTime.Today.AddYears(-120))
            {
                Warn("Введите корректнй возраст");
                return;
            }

            // Запомианаем Id выбранных жанрови персон, сами объект брать нельзя
            var genreIds = ListGenres.SelectedItems.Cast<genres>().Select(g => g.id).ToList();
            var personIds = ListPersons.SelectedItems.Cast<persons>().Select(p => p.id).ToList();

            using (var db = new OnlineCinemaDBEntities())
            {
                if (db.Users.Any(u => u.email == email))
                {
                    Warn("Пользователь с таким email уже сущетвует");
                    return;
                }

                var user = new Users
                {
                    full_name = name,
                    email = email,
                    password_hash = PasswordHelper.GetHash(pass1),
                    birth_date = birth,
                    registration_date = DateTime.Now,
                    role_id = (int)ComboRole.SelectedValue
                };

                //Сохраняем предпочтения пользователя
                foreach(var genre in db.genres.Where(x => genreIds.Contains(x.id)))user.genres.Add(genre);
                foreach (var person in db.persons.Where(x => personIds.Contains(x.id)))user.persons.Add(person);

                db.Users.Add(user);
                db.SaveChanges();
            }
            MessageBox.Show("Регистрация прошла успешно, теперь войдите в аккаунт", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigationService?.Navigate(new Pages.AuthPage());
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Pages.AuthPage());

        }
    }
}

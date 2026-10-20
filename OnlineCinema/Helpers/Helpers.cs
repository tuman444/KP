using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;


namespace OnlineCinema.Helpers
{
    /// <summary>Хеширование паролей (в БД хранится только хеш, столбец users.password_hash).</summary>
    public static class PasswordHelper
    {
        public static string GetHash(string password)
        {
            using (var sha = SHA256.Create())
            {
                // строка -> байты -> хеш -> строка из шестнадцатеричных цифр (64 символа)
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(password))
                                        .Select(b => b.ToString("X2")));
            }
        }
    }

    /// <summary>Правила для нового пароля. Используется при регистрации и смене пароля,
    /// чтобы правила были в ОДНОМ месте и не расходились.</summary>
    public static class PasswordValidator
    {
        /// <returns>Текст ошибки или null, если пароль подходит.</returns>
        public static string Validate(string password)
        {
            if (password.Length < 6)
                return "Пароль слишком короткий, должно быть минимум 6 символов!";

            bool hasDigit = false;
            foreach (char c in password)
            {
                if (c >= '0' && c <= '9')
                    hasDigit = true;
                else if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                    return "Используйте только английскую раскладку (буквы и цифры)!";
            }

            if (!hasDigit)
                return "Добавьте хотя бы одну цифру!";

            return null;   // всё в порядке
        }
    }

    /// <summary>Текущая сессия: кто сейчас вошёл в приложение.</summary>
    public static class CurrentUser
    {
        public static int Id { get; private set; }
        public static string FullName { get; private set; }
        public static string Email { get; private set; }
        public static string RoleName { get; private set; }

        public static bool IsLoggedIn => Id != 0;
        public static bool IsAdmin => RoleName == "Администратор";

        /// <summary>Срабатывает при входе и выходе (MainWindow обновляет кнопку "Войти"/имя).</summary>
        public static event Action Changed;

        public static void Login(int id, string fullName, string email, string roleName)
        {
            Id = id;
            FullName = fullName;
            Email = email;
            RoleName = roleName;
            Changed?.Invoke();
        }

        public static void Logout()
        {
            Id = 0;
            FullName = Email = RoleName = null;
            Changed?.Invoke();
        }
    }

    /// <summary>Превращает путь из БД (например res/posters/1.jpg) в картинку.
    /// Относительный путь считается от папки с .exe.</summary>
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return null;

            string full = Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if (!File.Exists(full)) return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;   // файл не блокируется
            bmp.UriSource = new Uri(full, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

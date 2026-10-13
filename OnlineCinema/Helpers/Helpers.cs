using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OnlineCinema.Helpers
{
    ///<summary>
    ///Хеширование пароля
    ///При входе хешируем введеный пароль и сравниваем 2 хеша
    /// </summary>
    public static class PasswordHelper
    {
        public static string GetHash(string password)
        {
            using (var sha = SHA256.Create())
            {
                // строка - байты - хеш - строка из шестнадцатеричных цифр
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(password)).Select(b => b.ToString("X2")));
            }
        }

        /// <summary>
        /// Хранит, кто сейчас вошёл. static = одно значение на всё приложение
        /// </summary>
        public static class CurrentUser
        {
            public static int Id {  get; private set; }
            public static string FullName { get; private set; }
            public static string Email { get; private set; }
            public static string RoleName { get; private set; }

            public static bool IsLoggedIn => Id != 0; 
            public static bool IsAdmin => RoleName == "Администратор";

            public static event Action Changed; // Событие: MainWindow подписывается на него и меняет кнопку "Войти" на имя

            public static void Login(int  id, string fullname, string email, string rolename)
            {
                Id = id;
                FullName = fullname;
                Email = email;
                RoleName = rolename;
            }

            public static void Logout()
            {
                Id = 0;
                FullName = Email = RoleName = null;
            }
        }
    }
}

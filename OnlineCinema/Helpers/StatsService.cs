using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace OnlineCinema.Helpers
{
    /// <summary>Данные одного пользователя для отчётов Word и Excel.</summary>
    public class UserReport
    {
        public string FullName { get; set; }

        /// <summary>Жанр -> сколько фильмов этого жанра пользователь смотрел.</summary>
        public List<KeyValuePair<string, int>> GenreViews { get; set; } = new List<KeyValuePair<string, int>>();

        public string BestTitle { get; set; }
        public int BestRating { get; set; }
        public string WorstTitle { get; set; }
        public int WorstRating { get; set; }

        public bool HasRatings => BestTitle != null;
    }

    /// <summary>
    /// Все запросы статистики в одном месте. Страницы графиков и методы экспорта
    /// не лезут в БД сами, а вызывают этот класс (так проще читать и исправлять).
    /// ВАЖНО: локальные переменные называем allUsers/allGenres, а не users/genres:
    /// иначе имя перекроет класс-модель с тем же именем.
    /// </summary>
    public static class StatsService
    {
        /// <summary>График 1: сколько фильмов в каждом жанре.</summary>
        public static List<KeyValuePair<string, double>> MoviesPerGenre()
        {
            using (var db = new OnlineCinemaDBEntities())
            {
                return db.genres.Include(g => g.movies)
                        .ToList()                                  // дальше считаем в памяти
                        .Select(g => new KeyValuePair<string, double>(g.name, g.movies.Count))
                        .OrderByDescending(kv => kv.Value)
                        .ToList();
            }
        }

        /// <summary>График 2: средняя оценка каждого фильма.</summary>
        public static List<KeyValuePair<string, double>> AverageRatingPerMovie()
        {
            using (var db = new OnlineCinemaDBEntities())
            {
                var allRatings = db.ratings.Include(r => r.movies).ToList();
                return allRatings
                    .GroupBy(r => r.movies.title)
                    .Select(g => new KeyValuePair<string, double>(
                        g.Key, Math.Round(g.Average(r => (double)r.rating_value), 2)))
                    .OrderByDescending(kv => kv.Value)
                    .ToList();
            }
        }

        /// <summary>График 3: сколько фильмов каждого жанра смотрел выбранный пользователь.</summary>
        public static List<KeyValuePair<string, double>> ViewsByGenreForUser(int userId)
        {
            using (var db = new OnlineCinemaDBEntities())
            {
                var history = db.watch_history
                                .Include(w => w.movies.genres)
                                .Where(w => w.user_id == userId)
                                .ToList();

                var counts = new Dictionary<string, double>();
                foreach (var item in history)
                    foreach (var genre in item.movies.genres)
                        counts[genre.name] = (counts.ContainsKey(genre.name) ? counts[genre.name] : 0) + 1;

                return counts.OrderByDescending(kv => kv.Value).ToList();
            }
        }

        /// <summary>Отчёт по каждому пользователю (для Word и Excel).</summary>
        public static List<UserReport> BuildUserReports()
        {
            var result = new List<UserReport>();

            using (var db = new OnlineCinemaDBEntities())
            {
                var allUsers = db.Users.OrderBy(u => u.full_name).ToList();
                var allGenres = db.genres.OrderBy(g => g.name).ToList();
                var allHistory = db.watch_history.Include(w => w.movies.genres).ToList();
                var allRatings = db.ratings.Include(r => r.movies).ToList();

                foreach (var u in allUsers)
                {
                    var rep = new UserReport { FullName = u.full_name };

                    var myHistory = allHistory.Where(h => h.user_id == u.id).ToList();
                    foreach (var g in allGenres)
                    {
                        int count = myHistory.Count(h => h.movies.genres.Any(x => x.id == g.id));
                        rep.GenreViews.Add(new KeyValuePair<string, int>(g.name, count));
                    }

                    var myRatings = allRatings.Where(r => r.user_id == u.id).ToList();
                    if (myRatings.Count > 0)
                    {
                        var best = myRatings.OrderByDescending(r => r.rating_value).First();
                        var worst = myRatings.OrderBy(r => r.rating_value).First();
                        rep.BestTitle = best.movies.title;
                        rep.BestRating = best.rating_value;
                        rep.WorstTitle = worst.movies.title;
                        rep.WorstRating = worst.rating_value;
                    }

                    result.Add(rep);
                }
            }
            return result;
        }
    }
}

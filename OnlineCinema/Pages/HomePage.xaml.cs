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
    /// Простой класс только для показа на карточке
    /// </summary>
    public class MovieCard
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Info { get; set; }
        public string PosterUrl { get; set; }
    }

    public partial class HomePage : Page
    {
        ///<summary>
        ///
        /// </summary>

        private bool _loaded;

        public HomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = new OnlineCinemaDBEntities())
            {
                var list = new List<genres>
                {
                    new genres
                    {
                        id = 0,
                        name = "Все жанры"
                    }
                };
                // первым пунктом добавляем "Все жанры" (id = 0), дальше жанры из БД
                list.AddRange(db.genres.OrderBy(g => g.name).ToList());
                GenreCombo.ItemsSource = list;
                GenreCombo.SelectedIndex = 0;
            }
            _loaded = true;
            LoadMovies();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => LoadMovies();

        private void LoadMovies()
        {
            if (!_loaded) return;

            string text = SearchBox.Text.Trim();
            int genreId = GenreCombo.SelectedValue is int sel ? sel : 0;

            using (var db = new OnlineCinemaDBEntities())
            {
                IQueryable<movies> query = db.movies.Include(m => m.genres);

                if(text.Length > 0) query = query.Where(m => m.title.Contains(text));
                if(genreId > 0) query = query.Where(m => m.genres.Any(g  => g.id == genreId));

                var cards = query.OrderByDescending(m => m.added_date)
                    .ToList()
                    .Select(m => new MovieCard
                    {
                        Id = m.id,
                        Title = m.title,
                        PosterUrl = m.poster_url,
                        Info = m.releae_year + " * " + string.Join(", ", m.genres.Select(g => g.name))
                    }).ToList();
                MoviesList.ItemsSource = cards;
                EmptyText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            int movieId = (int)((FrameworkElement)sender).Tag;
            if (!CurrentUser.IsLoggedIn)
            {
                MessageBox.Show("Чтобы открыть странницу фильма зайдите или зарегистрируйтесь", "Требуется вход", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                //NavigationService?.Navigate(new Pages.AuthPage);
                return;
            }
            //NavigationService.Navigate(new Pages.MoviePage);
        }


    }
}

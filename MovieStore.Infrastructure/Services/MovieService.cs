using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MovieStore.Core.Entities;
using MovieStore.Core.Helpers;
using MovieStore.Core.Models.Response;
using MovieStore.Core.RepositoryInterfaces;
using MovieStore.Core.ServiceInterfaces;

namespace MovieStore.Infrastructure.Services
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IMovieCastRepository _movieCastRepository;

        /*
         * Constructor Injection, injecting MovieRepository instance
         */
        public MovieService(IMovieRepository movieRepository, IFavoriteRepository favoriteRepository, IPurchaseRepository purchaseRepository, IMovieCastRepository movieCastRepository)
        {
            _movieRepository = movieRepository;
            _favoriteRepository = favoriteRepository;
            _purchaseRepository = purchaseRepository;
            _movieCastRepository = movieCastRepository;
        }

        public async Task<IEnumerable<MovieCardResponseModel>> GetHighestGrossingMovies()
        {
            var movies = await _movieRepository.GetHighestRevenueMovies();
            var response = movies.Select(m => new MovieCardResponseModel
                {Id = m.Id, Title = m.Title, PosterUrl = m.PosterUrl});
            return response;
        }

        public async Task<IEnumerable<MovieCardResponseModel>> GetTop25RatedMovies()
        {
            var movies = await _movieRepository.GetHighestRatedMovies();
            var response = movies.Select(m => new MovieCardResponseModel
                {Id = m.Id, Title = m.Title, PosterUrl = m.PosterUrl});
            return response;
        }

        public async Task<MovieDetailResponseModel> GetMovieById(int movieId, int? userId = null)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId);
            if (movie == null) throw new Exception("Movie not found");
            var response = new MovieDetailResponseModel
            {
                Id = movie.Id,
                Title = movie.Title,
                PosterUrl = movie.PosterUrl,
                Tagline = movie.Tagline,
                RunTime = movie.RunTime,
                Rating = movie.Rating,
                CreatedDate = movie.CreatedDate,
                Price = movie.Price,
                Overview = movie.Overview,
                Budget = movie.Budget,
                Revenue = movie.Revenue,
                Genres = movie.MovieGenres?.Select(mg => new GenreResponseModel{Id = mg.GenreId, Name = mg.Genre.Name}),
                // Casts = movie.MovieCasts?.Select(mc => new CastOverviewResponseModel{Id = mc.CastId, Name = mc.Cast.Name, Character = mc.Character, ProfilePath = mc.Cast.ProfilePath})
            };

            var casts = await _movieCastRepository.GetCastsByMovieIdAsync(movieId);
            response.Casts = casts.Select(mc =>
                new CastOverviewResponseModel
                    {Id = mc.CastId, Name = mc.Cast.Name, Character = mc.Character, ProfilePath = mc.Cast.ProfilePath});
            
            // Check if the user is login
            if (userId != null)
            {
                response.IsFavoriteByUser =
                    await _favoriteRepository.GetExistsAsync(f => f.UserId == userId && f.MovieId == movieId);
                response.IsPurchasedByUser =
                    await _purchaseRepository.GetExistsAsync(f => f.UserId == userId && f.MovieId == movieId);
            }
            return response;
        }

        public async Task<Movie> CreateMovie(Movie movie)
        {
            return await _movieRepository.AddAsync(movie);
        }

        public async Task<Movie> UpdateMovie(Movie movie)
        {
            return await _movieRepository.UpdateAsync(movie);
        }

        public async Task<int> GetMoviesCount(string title = "")
        {
            return await _movieRepository.GetMoviesCount(title);
        }

        public async Task<IEnumerable<RatedMovieCardResponseModel>> GetMoviesAboveRating(decimal rating)
        {
            var movies = await _movieRepository.GetMoviesAboveRatingAsync(rating);
            return movies;
        }

        public async Task<PagedResultSet<MovieCardResponseModel>> GetMoviesByPagination(int pageIndex = 1, int pageSize = 30, string title = "", string orderBy = "")
        {
            Expression<Func<Movie, bool>> filter = m => m.Title.Contains(title);
            Func<IQueryable<Movie>, IOrderedQueryable<Movie>> order = m => m.OrderBy(movie => orderBy);
            var pagedMovies = await _movieRepository.GetPaginatedMoviesBySearchTitle(pageIndex, pageSize, string.IsNullOrEmpty(orderBy) ? null : order, string.IsNullOrEmpty(title)? null : filter);
            return pagedMovies;
        }
    }
}
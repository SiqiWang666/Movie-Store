using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MovieStore.Core.Entities;
using MovieStore.Core.Models.Request;
using MovieStore.Core.Models.Response;
using MovieStore.Core.RepositoryInterfaces;
using MovieStore.Core.ServiceInterfaces;

namespace MovieStore.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICryptoService _cryptoService;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IReviewRepository _reviewRepository;

        public UserService(IUserRepository userRepository, ICryptoService cryptoService, IFavoriteRepository favoriteRepository, IPurchaseRepository purchaseRepository, IReviewRepository reviewRepository)
        {
            _userRepository = userRepository;
            _cryptoService = cryptoService;
            _favoriteRepository = favoriteRepository;
            _purchaseRepository = purchaseRepository;
            _reviewRepository = reviewRepository;
        }
        public async Task<UserRegisterResponseModel> RegisterUser(UserRegisterRequestModel requestModel)
        {
            /*
             * 1. check if the user is already exists
             * 2. create random salt
             * 3. hash password with salt, using industry tested/tried hashing algorithm
             * 4. Create user object
             * 5. Store to database
             * 6. Generate response
             */
            var dbUser = await _userRepository.GetUserByEmailAsync(requestModel.Email);
            if (dbUser != null)
            {
                // ToDo [refactor ]
                // Throw an exception if user exists.
                // throw new Exception("Email address already exists. Please try to login");
                return null;
            }

            var salt = _cryptoService.GenerateSalt();
            var hashedPassword = _cryptoService.HashPassword(requestModel.Password, salt);

            var user = new User
            {
                Email = requestModel.Email,
                Salt = salt,
                HashedPassword = hashedPassword,
                FirstName = requestModel.FirstName,
                LastName = requestModel.LastName
            };

            var createdUser = await _userRepository.AddAsync(user);
            
            var response = new UserRegisterResponseModel{Id = createdUser.Id, Email = createdUser.Email, FirstName = createdUser.FirstName, LastName = createdUser.LastName};

            return response;
        }

        public async Task<LoginResponseModel> ValidateUser(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                // throw exception if user doesn't exist
                // throw new Exception("Please Register first");
                return null;
            }

            var hashedPassword = _cryptoService.HashPassword(password, user.Salt);

            if (hashedPassword.Equals(user.HashedPassword))
            {
                var response = new LoginResponseModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    DateOfBirth = user.DateOfBirth,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };
                return response;
            }

            return null;
        }

        public async Task<bool> UserExistByEmail(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            return user != null;
        }

        public async Task<Favorite> FavoriteMovie(int movieId, int userId)
        {
            var isLiked = await IsMovieFavoriteByUser(userId, movieId);
            if (isLiked) return null;
            var favorite = new Favorite{MovieId = movieId, UserId = userId};
            return await _favoriteRepository.AddAsync(favorite);
        }

        public async Task<bool> RemoveFavoriteMovie(int movieId, int userId)
        {
            var favorites = await _favoriteRepository.ListAsync(f => f.MovieId == movieId && f.UserId == userId);
            var result = await _favoriteRepository.DeleteAsync(favorites.FirstOrDefault());
            return result > 0;
        }

        public async Task<bool> IsMovieFavoriteByUser(int userId, int movieId)
        {
            var isFavorite = await _favoriteRepository.GetExistsAsync(f => f.MovieId == movieId && f.UserId == userId);
            return isFavorite;
        }

        public async Task<Purchase> PurchaseMovie(UserPurchaseRequestModel requestModel)
        {
            var purchase = new Purchase
            {
                UserId = requestModel.UserId,
                MovieId = requestModel.MovieId,
                PurchaseNumber = requestModel.PurchaseNumber,
                TotalPrice = requestModel.Price,
                PurchaseDateTime = requestModel.PurchaseTime
            };
            return await _purchaseRepository.GetExistsAsync(p =>
                p.UserId == purchase.UserId && p.MovieId == purchase.MovieId) ? null : await _purchaseRepository.AddAsync(purchase);
        }

        public async Task<IEnumerable<Movie>> GetUserFavoriteMovies(int userId)
        {
            var movies = await _userRepository.GetUserFavoriteMoviesAsync(userId);
            return movies;
        }

        public async Task<IEnumerable<UserReviewedMovieResponseModel>> GetUserReviewedMovies(int userId)
        {
            var reviews = await _reviewRepository.GetUserReviewedMovies(userId);
            if(reviews == null) throw new Exception("You haven't reviewed any movies...");
            var response = reviews.Select(r => new UserReviewedMovieResponseModel
            {
                UserId = r.UserId,
                MovieId = r.MovieId,
                Title = r.Movie.Title,
                PosterUrl = r.Movie.PosterUrl,
                ReviewText = r.ReviewText,
                Rating = r.Rating,
                FirstName = r.User.FirstName,
                LastName = r.User.LastName,
                UserName = r.User.UserName
            });
            return response;
        }

        public async Task<UserReviewedMovieResponseModel> IsMovieReviewedByUser(int userId, int movieId)
        {
            var review = await _reviewRepository.IsReviewedByUserAsync(userId, movieId);
            if (review is null) return null;
            return new UserReviewedMovieResponseModel
            {
                ReviewText = review.ReviewText,
                Rating = review.Rating
            };
        }

        public async Task<UserProfileResponseModel> GetUserProfile(int userId)
        {
            var user = await _userRepository.GetUserProfileAsync(userId);
            var response = new UserProfileResponseModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEndDate = user.LockoutEndDate,
                LastLoginDateTime = user.LastLoginDateTime,
                Roles = user.UserRoles.Select(ur => new AccountRoleResponseModel{Id = ur.Role.Id, Name = ur.Role.Name})
            };
            return response;
        }
    }
}
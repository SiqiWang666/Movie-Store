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
                throw new Exception("Email address already exists. Please try to login");
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
                throw new Exception("Please Register first");
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

        public async Task FavoriteMovie(int movieId, int userId)
        {
            var favorite = new Favorite{MovieId = movieId, UserId = userId};
            await _favoriteRepository.AddAsync(favorite);
        }

        public async Task RemoveFavoriteMovie(int movieId, int userId)
        {
            var collection = await _favoriteRepository.ListAsync(f => f.MovieId == movieId && f.UserId == userId);
            var favorite = collection.FirstOrDefault();
            if(favorite != null) await _favoriteRepository.DeleteAsync(favorite);
        }

        public async Task<bool> IsMovieFavoriteByUser(int userId, int movieId)
        {
            var isFavorite = await _favoriteRepository.GetExistsAsync(f => f.MovieId == movieId && f.UserId == userId);
            return isFavorite;
        }

        public async Task PurchaseMovie(UserPurchaseRequestModel requestModel)
        {
            var purchase = new Purchase
            {
                UserId = requestModel.UserId,
                MovieId = requestModel.MovieId,
                PurchaseNumber = requestModel.PurchaseNumber,
                TotalPrice = requestModel.Price,
                PurchaseDateTime = requestModel.PurchaseTime
            };
            await _purchaseRepository.AddAsync(purchase);
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

        public async Task<bool> IsMovieReviewedByUser(int userId, int movieId)
        {
            return await _reviewRepository.GetExistsAsync(r => r.UserId == userId && r.MovieId == movieId);
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
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockStockBackend.DataModels;
using MockStockBackend.Helpers;

namespace MockStockBackend.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        public UserService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public async Task<User> GenerateNewUser(string username, string password)
        {
            // Create new user
            User user = new User();
            user.UserCurrency = 100000; // one hundred thousand
            user.UserName = username;
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(password);
            
            // Add user to the entity model
            var dbResponse = _context.Users.Add(user);
            var addedUser = dbResponse.Entity;
            
            // Add user to the database
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                addedUser = null;
            }
            
            // Returns the added User Object.
            return addedUser;
        }

        public User Authenticate(string username, string password)
        {
            var user = _context.Users.SingleOrDefault(x => x.UserName == username);
            if (user == null)
                return null;
            
            var validPassword = BCrypt.Net.BCrypt.Verify(password, user.UserPassword);
            if (validPassword == false)
                return null;

            // generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                {
                    new Claim(ClaimTypes.Name, user.UserId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            // remove password before returning
            user.UserPassword = null;

            return user;
        }

        public User GetUser(int userId)
        {
            // Obtain user object using their UserID.
            var user = _context.Users.SingleOrDefault(x => x.UserId == userId);
            return user;
        }
    }
}
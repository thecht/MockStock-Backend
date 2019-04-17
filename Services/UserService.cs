using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockStockBackend.DataModels;

namespace MockStockBackend.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GenerateNewUser(string username, string password)
        {
            // 1 - Create new user
            User user = new User();
            user.UserCurrency = 100000; // one hundred thousand
            user.UserName = username;
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(password);
            
            // 2 - Add user to the entity model
            var dbResponse = _context.Users.Add(user);
            var addedUser = dbResponse.Entity;
            
            // 3 - Add user to the database
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                addedUser = null;
            }
            
            // 4 - Returns the added User object.
            return addedUser;
        }

        public async Task<User> Authenticate(string username, string password)
        {
            // 1 - Get the claimed user from the database
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == username);
            if (user == null)
                return null;
            
            // 2 - Ensure the password is valid for the claimed user
            var validPassword = BCrypt.Net.BCrypt.Verify(password, user.UserPassword);
            if (validPassword == false)
                return null;

            // 3 - Generate JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("APP_TOKENSECRET"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                {
                    new Claim(ClaimTypes.Name, user.UserId.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            // 4 - Remove password before returning
            user.UserPassword = null;

            // 5 - Returns the user with a new JWT token
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
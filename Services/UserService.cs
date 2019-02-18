using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
            // Create new user
            User user = new User();
            user.UserCurrency = 10000000; // ten million
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
            
            // Returns the added user
            return addedUser;
        }

    }
}
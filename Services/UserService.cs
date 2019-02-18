using System;
using System.Threading.Tasks;
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

        public async Task<User> GenerateNewUser()
        {
            User newUser = new User();
            
            return newUser;
        }

    }
}
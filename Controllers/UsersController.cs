using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockStockBackend.DataModels;
using MockStockBackend.Services;

namespace MockStockBackend.Controllers
{
    [Authorize]
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateUser()
        {
            // 1 - Get user creation details (username, password)
            var username = (string)HttpContext.Request.Headers["username"];
            var password = (string)HttpContext.Request.Headers["password"];
            if(username == null || password == null)
            {
                return BadRequest();
            }
            
            // 2 - Generate a new user
            User newUser = await _userService.GenerateNewUser(username, password);
            if (newUser == null)
            {
                return StatusCode(409);
            }

            // 3 - Send request details + user info back to the client
            return StatusCode(201, newUser);
        }

        [AllowAnonymous]
        [HttpPost("token")]
        public IActionResult Authenticate()
        {
            // 1 - Get login details
            var username = (string)HttpContext.Request.Headers["username"];
            var password = (string)HttpContext.Request.Headers["password"];
            if (username == null || password == null)
            {
                return BadRequest();
            }

            // 2 - Generate a new token for the user
            var user = _userService.Authenticate(username, password);
            
            // 3 - Ensure the password was correct
            if(user == null)
                return BadRequest(new { message = "Username or password is incorrect." } );
            
            // 4 - Send back the user with new token
            return Ok(user);
        }

        [HttpGet]
        public IActionResult GetUser()
        {
            // Obtain UserID from token.
            var userId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Returns a User Object.
            var user = _userService.GetUser(userId);
            return Ok(user);
        }
    }
}
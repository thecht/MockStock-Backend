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
        public async Task<string> CreateUser()
        {
            // 1 - Get user creation details (username, password)
            var username = (string)HttpContext.Request.Headers["username"];
            var password = (string)HttpContext.Request.Headers["password"];
            if(username == null || password == null)
            {
                HttpContext.Response.StatusCode = 400;
                return "{\"error\": \"incomplete headers. expects username and password.\"}";
            }
            
            // 2 - Generate a new user
            User newUser = await _userService.GenerateNewUser(username, password);
            if (newUser == null)
            {
                HttpContext.Response.StatusCode = 409;
                return "{\"error\": \"username taken?\"}";
            }

            // 3 - Send request details + user info back to the client
            HttpContext.Response.StatusCode = 201;
            return Newtonsoft.Json.JsonConvert.SerializeObject(newUser);
        }

        [AllowAnonymous]
        [HttpPost("token")]
        public IActionResult Authenticate()
        {
            var username = (string)HttpContext.Request.Headers["username"];
            var password = (string)HttpContext.Request.Headers["password"];
            var user = _userService.Authenticate(username, password);
            
            if(user == null)
                return BadRequest(new { message = "Username or password is incorrect." } );
            
            return Ok(user);
}

        [HttpGet]
        public IActionResult GetUser()
        {
            // Obtain UserID.
            var userId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Returns a User Object.
            var user = _userService.GetUser(userId);
            return Ok(user);
        }
    }
}
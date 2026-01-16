using KickOffEvent.DTO;
using KickOffEvent.Interface;
using KickOffEvent.Models;
using KickOffEvent.Services;
using KickOffEventVoting.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KickOffEvent.Controller
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly IEncryptionService _Encrypt;

        public AuthController(AppDbContext db, IJwtTokenService jwt, IEncryptionService encrypt)
        {
            _db = db;
            _jwt = jwt;
            _Encrypt = encrypt;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(DTO.LoginRequest req)
        {
            var UserName = _Encrypt.Encrypt(req.UserName);
            var password = _Encrypt.Encrypt(req.Password);

            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == UserName && x.PasswordHash == password);
            if (user == null)
                return Unauthorized("Invalid username/password");
            var decryptPassword = _Encrypt.Decrypt(user.PasswordHash);
            var userName = _Encrypt.Decrypt(user.UserName);
            // Demo password check:
            // IMPORTANT: Replace with BCrypt verify in production
            if (decryptPassword != req.Password)
                return Unauthorized("Invalid username/password");

            var token = _jwt.CreateToken(user);
            return Ok(new { token, user = new { user.Id, userName,user.Gender, user.IsAdmin } });
        }
    }

}

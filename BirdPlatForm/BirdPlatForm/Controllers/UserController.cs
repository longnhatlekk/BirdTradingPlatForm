using BirdPlatForm.BirdPlatform;
using BirdPlatForm.UserRespon;
using BirdPlatForm.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BirdPlatForm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BirdPlatFormContext _context;
        private readonly IConfiguration _config;

        public UserController(BirdPlatFormContext bird, IConfiguration config)
        {
            _context = bird;
            _config = config;
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserView model)
        
        {
            var user =  _context.TbUsers.Include(x => x.Role).SingleOrDefault(o => o.Email == model.Email && model.Password == o.Password);
            if (user == null) return Ok(new ErrorRespon
            {
                Error = false,
                Message = "Email Or Password Incorrect :("
            });
            var token = await GeneratToken(user);
            return Ok(new ErrorRespon
            {
                Error = true,
                Message = "Login Success",
                Data = token
            });
        }

        private async Task<TokenRespon> GeneratToken(TbUser user)
        {
            var jwttokenHandler = new JwtSecurityTokenHandler();
            var secretkey = Encoding.UTF8.GetBytes(_config["JWT:Key"]);

            var tokendescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("Email", user.Email),
                    new Claim(ClaimTypes.Role, user.RoleId)


                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretkey), SecurityAlgorithms.HmacSha512)


            };


            var token = jwttokenHandler.CreateToken(tokendescription);
            var accessToken = jwttokenHandler.WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            //Lưu database  
            var refreshTokenEntity = new TbToken
            {

                Id = user.UserId,
                UserId = user.UserId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddSeconds(10),
            };

            await _context.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new TokenRespon
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        private string GenerateRefreshToken()
        {

            var random = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);

                return Convert.ToBase64String(random);
            }
        }
        [HttpPost("Register")]
        public async Task<ActionResult<TbUser>> Register(RegisterModel register)
        {
            var regis =  _context.TbUsers.FirstOrDefault(o => o.Email == register.Email);
            if(regis != null)
            {
                return Ok(new ErrorRespon
                {
                    Error = false,
                    Message ="Fail"
                });
            }
            var user = new TbUser
            {
                Dob = register.Dob,
                Gender = register.Gender,
                Email = register.Email,
                Name = register.Name,
                Password = register.Password,
                RoleId = register.RoleId,
                UpdateDate = register.UpdateDate,
                CreateDate = register.CreateDate,
                Avatar = register.Avatar,
                Phone = register.Phone,
                Address = register.Address,
                ShopId = register.ShopId,

            };
            await _context.TbUsers.AddAsync(user);
            await _context.SaveChangesAsync();
            return Ok(new ErrorRespon
            {
                Error = true,
                Message = "Success"
            });
        }
    }
}

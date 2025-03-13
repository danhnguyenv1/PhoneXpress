using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhoneXpressServer.Data;
using PhoneXpressSharedLibrary.Dtos;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Services
{
    public class UserAccountService(AppDbContext appDbContext, IConfiguration _configuration) : IUserAccount
    {
        public async Task<LoginResponse> Login(LoginDTO model)
        {
            if (model is null)
                return new LoginResponse(false, "Model is empty");
            var findUser = await appDbContext.UserAccounts
                .FirstOrDefaultAsync(_ => _.Email!.Equals(model.Email!));
            if (findUser is null)
                return new LoginResponse(false, "User not found");
            if (!BCrypt.Net.BCrypt.Verify(model!.Password, findUser.Password))
                return new LoginResponse(false, "Invalid UserName/Password");

            var (accessToken, refreshToken) = await GenerateTokens(findUser.Id);
            //Add or update Token info
            await SaveToTokenInfo(findUser.Id, accessToken, refreshToken);
            return new LoginResponse(true, "Login Successfull", accessToken, refreshToken);
        }


        private async Task SaveToTokenInfo(int userId, string accessToken, string refreshToken)
        {
            var getUser = await appDbContext.TokenInfo
            .FirstOrDefaultAsync(_ => _.UserId == userId);
            if (getUser is null)
            {
                appDbContext.TokenInfo.Add(new TokenInfo()
                { UserId = userId, AccessToken = accessToken, RefreshToken = refreshToken });
                await Commit();
            }
            else
            {
                getUser.RefreshToken = refreshToken;
                getUser.AccessToken = accessToken;
                getUser.ExpiryDate = DateTime.Now.AddDays(1);
                await Commit();
            }
        }

        private async Task<(string AccessToken, string RefreshToken)> GenerateTokens(int userId)
        {
            var role = await GetUserRole(userId);

            var secretKey = _configuration["Jwt:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey!);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role!)
                }),
                Expires = DateTime.UtcNow.AddMinutes(1),
                //Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string accessToken = tokenHandler.WriteToken(token);
            string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return (accessToken, refreshToken);
        }

        public async Task<ServiceResponse> Register(UserDTO model)
        {
            if (model is null)
                return new ServiceResponse(false, "Model is empty");

            // Check if the user already exists
            if (await appDbContext.UserAccounts.AnyAsync(u => u.Email!.ToLower() == model.Email!.ToLower()))
                return new ServiceResponse(false, "User already registered");

            // Create a new user account
            var user = new UserAccount
            {
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Name = model.Name,
                Email = model.Email
            };

            appDbContext.UserAccounts.Add(user);
            await Commit();

            // Check if "Admin" role exists, otherwise create it
            var adminRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Name.ToLower() == "admin")
                            ?? appDbContext.SystemRoles.Add(new SystemRole { Name = "Admin" }).Entity;
            await Commit();

            // Check if "User" role exists, otherwise create it
            var userRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Name.ToLower() == "user")
                           ?? appDbContext.SystemRoles.Add(new SystemRole { Name = "User" }).Entity;
            await Commit();

            // Assign "Admin" role to the first user, "User" role to others
            var roleToAssign = await appDbContext.UserRoles.AnyAsync() ? userRole : adminRole;

            appDbContext.UserRoles.Add(new UserRole { RoleId = roleToAssign.Id, UserId = user.Id });
            await Commit();

            return new ServiceResponse(true, "Account created");
        }

        private async Task Commit() => await appDbContext.SaveChangesAsync();

        public async Task<LoginResponse> GetRefreshToken(PostRefreshTokenDTO model)
        {
            var normalToken = model.RefreshToken;

            var getToken = await appDbContext.TokenInfo
                .FirstOrDefaultAsync(x => x.RefreshToken == normalToken);
            if (getToken is null) return null;

            //Generate new token
            var (newAccessToken, NewRefreshToken) = await GenerateTokens(getToken.UserId);

            //Add or update Token info
            await SaveToTokenInfo(getToken.UserId, newAccessToken, NewRefreshToken);
            return new LoginResponse(true, "refresh-token-completed", newAccessToken, NewRefreshToken);
        }

        public async Task<UserSession?> GetUserByToken(string token)
        {
            var result = await (from t in appDbContext.TokenInfo
                                join u in appDbContext.UserAccounts on t.UserId equals u.Id
                                join ur in appDbContext.UserRoles on u.Id equals ur.UserId
                                join r in appDbContext.SystemRoles on ur.RoleId equals r.Id
                                where t.AccessToken == token && t.ExpiryDate >= DateTime.UtcNow
                                select new { u.Email, u.Name, RoleName = r.Name })
                               .FirstOrDefaultAsync();

            return result is null ? null : new UserSession { Email = result.Email, Name = result.Name, Role = result.RoleName };
        }

        public async Task<string?> GetUserRole(int userId)
        {
            var role = await (from ur in appDbContext.UserRoles
                              join r in appDbContext.SystemRoles on ur.RoleId equals r.Id
                              where ur.UserId == userId
                              select r.Name)
                             .FirstOrDefaultAsync();

            return role;
        }
    }
}

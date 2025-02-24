﻿using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhoneXpressServer.Data;
using PhoneXpressSharedLibrary.Dtos;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Services
{
    public class UserAccountService(AppDbContext appDbContext, IConfiguration _configuration) : IUserAccount
    {
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

        public async Task<UserSession> GetUserByToken(string token)
        {
            var result = await appDbContext.TokenInfo
                .FirstOrDefaultAsync(_ => _.AccessToken!.Equals(token));
            if (result is null) return null!;

            var getUserInfo = await appDbContext.UserAccounts
                .FirstOrDefaultAsync(_ => _.Id == result.UserId);
            if (getUserInfo is null) return null!;

            if (result.ExpiryDate < DateTime.Now) return null!;
            var getUserRole = await appDbContext.UserRoles
                .FirstOrDefaultAsync(_ => _.UserId == getUserInfo.Id);
            if (getUserRole is null) return null!;

            var roleName = await appDbContext.SystemRoles
                .FirstOrDefaultAsync(_ => _.Id == getUserRole.RoleId);
            if (roleName is null) return null!;

            return new UserSession()
            { Email = getUserInfo.Email, Name = getUserInfo.Name, Role = roleName.Name };
        }

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
            // add or update Token info
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
            var secretKey = _configuration["Jwt:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey!);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string accessToken = tokenHandler.WriteToken(token);
            string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return (accessToken, refreshToken);
        }


        private async Task<bool> VerifyToken(string refreshToken = null!, string accessToken = null!)
        {
            TokenInfo tokenInfo = new();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var getRefreshToken = await appDbContext.TokenInfo
                    .FirstOrDefaultAsync(_ => _.RefreshToken!.Equals(refreshToken));
                return getRefreshToken is null;
            }
            else
            {
                var getAccessToken = await appDbContext.TokenInfo
                .FirstOrDefaultAsync(_ => _.AccessToken!.Equals(accessToken));
                return getAccessToken is null;
            }
        }

        private static string GenerateToken(int numberOfBytes) =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(numberOfBytes));

        public async Task<ServiceResponse> Register(UserDTO model)
        {
            if (model is null)
                return new ServiceResponse(false, "Model is empty");
            var findUser = await appDbContext.UserAccounts.
                FirstOrDefaultAsync(_ => _.Email!.ToLower().Equals(model.Email!.ToLower()));
            if (findUser is not null)
                return new ServiceResponse(false, "User Registered already");
            var user = appDbContext.UserAccounts.Add(new UserAccount()
            {
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Name = model.Name,
                Email = model.Email,
            }).Entity;

            await Commit();

            //asign role
            var checkIfAdminIsCreated = await appDbContext.SystemRoles
                .FirstOrDefaultAsync(_ => _.Name!.ToLower().Equals("admin"));

            if (checkIfAdminIsCreated is null)
            {
                var result = appDbContext.SystemRoles.Add(new SystemRole() { Name = "Admin" }).Entity;
                await Commit();

                appDbContext.UserRoles.Add(new UserRole() { RoleId = result.Id, UserId = user.Id });
                await Commit();
            }
            else
            {
                var checkIfUserIsCreated = await appDbContext.SystemRoles
                .FirstOrDefaultAsync(_ => _.Name!.ToLower().Equals("user"));
                int RoleId = 0;
                if (checkIfUserIsCreated is null)
                {
                    var userResult = appDbContext.SystemRoles.Add(new SystemRole() { Name = "User" }).Entity;
                    await Commit();
                    RoleId = userResult.Id;

                }
                appDbContext.UserRoles.Add(new UserRole()
                {
                    RoleId = RoleId == 0 ? checkIfUserIsCreated!.Id : RoleId,
                    UserId = user.Id
                });
                await Commit();
            }
            return new ServiceResponse(true, "Account created");
        }

        private async Task Commit() => await appDbContext.SaveChangesAsync();
    }
}

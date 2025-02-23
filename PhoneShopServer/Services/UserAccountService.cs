using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PhoneXpressServer.Data;
using PhoneXpressSharedLibrary.Dtos;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Services
{
    public class UserAccountService(AppDbContext appDbContext) : IUserAccount
    {
        public Task<LoginResponse> GetRefreshToken(PostRefreshTokenDTO model)
        {
            throw new NotImplementedException();
        }

        public Task<UserSession> GetUserByToken(string token)
        {
            throw new NotImplementedException();
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

            var (accessToken, refreshToken) = await GenerateTokens();
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

        private async Task<(string AccessToken, string RefreshToken)> GenerateTokens()
        {
            string accessToken = GenerateToken(256);
            string refreshToken = GenerateToken(64);
            while (!await VerifyToken(accessToken))
                accessToken = GenerateToken(256);
            while (!await VerifyToken(refreshToken))
                refreshToken = GenerateToken(256);
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

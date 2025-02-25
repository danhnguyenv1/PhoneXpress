using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using PhoneXpressClient.Services;

namespace PhoneXpressClient.Authentication
{
    public class CustomAuthenticationStateProvider(AuthenticationService authenticationService) : AuthenticationStateProvider
    {
        private ClaimsPrincipal anonymous = new(new ClaimsIdentity());
        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var getUserSession = await authenticationService.GetUserDetails();

                if (getUserSession is null || string.IsNullOrEmpty(getUserSession.Email))
                    return await Task.FromResult(new AuthenticationState(anonymous));

                var claimsPrincipal = authenticationService.SetClaimPrincipal(getUserSession);
                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                return await Task.FromResult(new AuthenticationState(anonymous));
            }
        }

        public async Task UpdateAuthenticationState(TokenProp tokenProp)
        {
            ClaimsPrincipal claimsPrincipal = new();
            if (tokenProp is not null || !string.IsNullOrEmpty(tokenProp!.Token))
            {
                await authenticationService.SetTokenToLocalStorage(General.SerializeObj(tokenProp));
                var getUserSession = await authenticationService.GetUserDetails();
                if (getUserSession is not null || !string.IsNullOrEmpty(getUserSession!.Email))
                    claimsPrincipal = authenticationService.SetClaimPrincipal(getUserSession);
            }
            else
            {
                claimsPrincipal = anonymous;
                await authenticationService.RemoveTokenFromLocalStorage();
            }
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }
    }
}

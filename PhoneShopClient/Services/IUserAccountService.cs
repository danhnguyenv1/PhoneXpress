using PhoneXpressSharedLibrary.Dtos;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressClient.Services
{
    public interface IUserAccountService
    {
        Task<ServiceResponse> Register(UserDTO model);
        Task<LoginResponse> Login(UserDTO model);
    }
}

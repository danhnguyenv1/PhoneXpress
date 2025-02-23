using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhoneXpressServer.Services;
using PhoneXpressSharedLibrary.Dtos;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(IUserAccount accountService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<ServiceResponse>> CreateAccount(UserDTO model)
        {
            if (model is null) return BadRequest("Model is Null");
            var response = await accountService.Register(model);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> LoginAccount(LoginDTO model)
        {
            if (model is null) return BadRequest("Model is Null");
            var response = await accountService.Login(model);
            return Ok(response);
        }
    }
}

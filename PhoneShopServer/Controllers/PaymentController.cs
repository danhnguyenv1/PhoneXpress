using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhoneXpressClient.PrivateModels;
using PhoneXpressServer.Services;

namespace PhoneXpressServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController(IPayment paymentService) : ControllerBase
    {
        [HttpPost("checkout")]
        public ActionResult CreateCheckoutSession(List<Order> cartItems)
        {
            var url = paymentService.CreateCheckoutSession(cartItems);
            return Ok(url);
        }
    }
}

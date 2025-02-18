using Microsoft.AspNetCore.Mvc;
using PhoneXpressServer.Repositories;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProduct productService) : ControllerBase
    {
        private readonly IProduct productService = productService;

        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAllProducts(bool featured)
        {
            var products = await productService.GetAllProducts(featured); return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<ServiceResponse>> AddProduct(Product model)
        {
            if (model == null) return BadRequest("Model is null");
            var response = await productService.AddProduct(model);
            return Ok(response);
        }
    }
}

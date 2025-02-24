using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneXpressServer.Repositories;
using PhoneXpressSharedLibrary.Models;
using PhoneXpressSharedLibrary.Responses;

namespace PhoneXpressServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategory categoryService) : ControllerBase
    {
        private readonly ICategory categoryService = categoryService;

        [HttpGet]
        //[Authorize]
        public async Task<ActionResult<List<Category>>> GetAllCategories()
        {
            var products = await categoryService.GetAllCategories(); return Ok(products);
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<ServiceResponse>> AddCategory(Category model)
        {
            if (model == null) return BadRequest("Model is null");
            var response = await categoryService.AddCategory(model);
            return Ok(response);
        }
    }
}

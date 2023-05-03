using LotteryServer.Interfaces;
using LotteryServer.Models.Category;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LotteryServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _repository;

        public CategoryController(ICategoryRepository repository)
        {
            _repository = repository;
        }
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<CategoryVM>>> GetCategoryList([FromQuery] FilterCategory filter)
        {
            var allGames = await _repository.GetCategoryList(filter);
            return Ok(allGames);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryVM>> GetCategoryById(int id)
        {
            var category = await _repository.GetCategoryById(id);
            if (category != null)
                return Ok(category);
            else
                return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CreateCategory game)
        {
            try
            {
                await _repository.AddCategory(game);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult> Put([FromBody] UpdateCategory game)
        {
            try
            {
                await _repository.UpdateCategory(game);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _repository.DeleteCategory(id);
            return Ok();
        }
    }
}

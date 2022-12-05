using System;
using System.Threading.Tasks;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api")]
    [ApiController]
    public class BoozeController : ControllerBase
    {
        private readonly ICocktailService _cocktailService;
        private readonly ILogger<BoozeController> _logger;

        public BoozeController(
            ICocktailService cocktailService,
            ILogger<BoozeController> logger
        )
        {
            _cocktailService = cocktailService;
            _logger = logger;
        }

        [HttpGet]
        [Route("search-ingredient/{ingredient}")]
        public async Task<IActionResult> GetIngredientSearch([FromRoute] string ingredient)
        {
            try
            {
                var cocktailList = await _cocktailService.GetIngredientSearchCocktails(ingredient);
                return Ok(cocktailList);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Occured");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        [Route("random")]
        public async Task<IActionResult> GetRandom()
        {
            try
            {
                var cocktail = await _cocktailService.GetRandomCocktail();
                return Ok(cocktail);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Occured");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
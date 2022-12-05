using api.Models.Response;
using System.Threading.Tasks;

namespace api.Services
{
    public interface ICocktailService
    {
        Task<CocktailList> GetIngredientSearchCocktails(string ingredient);
        Task<Cocktail> GetRandomCocktail();
    }
}
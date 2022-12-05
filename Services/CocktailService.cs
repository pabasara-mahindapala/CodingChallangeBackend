using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using api.Models.Response;
using System.Security.Policy;
using Microsoft.Extensions.Logging;
using api.Models.Request;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using api.Extensions;

namespace api.Services
{
    public class CocktailService : ICocktailService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<CocktailService> _logger;
        private const string BASE_URL = "https://www.thecocktaildb.com/api/json/v1/1";

        public CocktailService(
            IHttpClientFactory clientFactory,
            ILogger<CocktailService> logger
        )
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        private async Task<T> GetCocktailData<T>(string url)
        {
            var uri = new Uri(url);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Accept", "application/json");

            var client = _clientFactory.CreateClient();

            T result = default;

            try
            {
                var response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    result = await JsonSerializer.DeserializeAsync<T>(responseStream,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    ).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("Request {0} Failed Response Code: {1}", url, response.StatusCode);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{0}_{1}", nameof(CocktailService), nameof(GetCocktailData));
            }

            return result;
        }

        public async Task<Cocktail> GetRandomCocktail()
        {
            var url = string.Format("{0}/random.php", BASE_URL);
            var random = await GetCocktailData<CocktailResponse>(url);
            Cocktail cocktail = null;

            if (random != null)
            {
                var drink = random.drinks.FirstOrDefault();

                if (drink != null)
                {
                    cocktail = new Cocktail
                    {
                        Id = Int32.Parse(drink.idDrink),
                        ImageURL = drink.strDrinkThumb,
                        Instructions = drink.strInstructions,
                        Name = drink.strDrink,
                        Ingredients = GetIngredients(drink)
                    };
                }

            }

            return cocktail;
        }

        private List<string> GetIngredients(Drink drink)
        {
            var ingredients = new List<string>();

            foreach (PropertyInfo prop in drink.GetType().GetProperties())
            {
                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (prop.Name.StartsWith("strIngredient"))
                {
                    var ingredient = prop.GetValue(drink);
                    if (ingredient != null)
                    {
                        ingredients.Add(prop.GetValue(drink).ToString());
                    }
                }
            }

            return ingredients;
        }

        public async Task<CocktailList> GetIngredientSearchCocktails(string ingredient)
        {
            var url = string.Format("{0}/filter.php?i={1}", BASE_URL, ingredient);
            var ingredientSearch = await GetCocktailData<CocktailResponse>(url);

            var cocktails = await GetCocktails(ingredientSearch);
            var meta = GetMeta(cocktails);

            return new CocktailList
            {
                Cocktails = cocktails,
                meta = meta
            };
        }

        private ListMeta GetMeta(List<Cocktail> cocktails)
        {
            cocktails = cocktails.OrderBy(x => x.Id).ToList();

            return new ListMeta
            {
                count = cocktails.Count,
                firstId = cocktails.First().Id,
                lastId = cocktails.Last().Id,
                medianIngredientCount = (int)cocktails.Median(x => x.Ingredients.Count)
            };
        }

        private async Task<List<Cocktail>> GetCocktails(CocktailResponse cocktailResponse)
        {
            var cocktails = new List<Cocktail>();

            foreach (var item in cocktailResponse.drinks)
            {
                var id = Int32.Parse(item.idDrink);
                Drink drink = await GetDrinkById(id);
                cocktails.Add(new Cocktail
                {
                    Id = id,
                    ImageURL = drink?.strDrinkThumb,
                    Ingredients = GetIngredients(drink),
                    Instructions = drink?.strInstructions,
                    Name = drink?.strDrink
                });
            }

            return cocktails;
        }

        private async Task<Drink> GetDrinkById(int id)
        {
            var url = string.Format("{0}/lookup.php?i={1}", BASE_URL, id);
            var cocktailResponse = await GetCocktailData<CocktailResponse>(url);
            return cocktailResponse?.drinks?.FirstOrDefault();
        }
    }
}

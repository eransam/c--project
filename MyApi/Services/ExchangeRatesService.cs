using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyApi.Services
{
    public class ExchangeRatesService
    {
        private readonly HttpClient _httpClient;

        public ExchangeRatesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, decimal>> GetExchangeRatesAsync()
        {
            var response = await _httpClient.GetAsync("https://api.coingecko.com/api/v3/exchange_rates");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var exchangeRates = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(content);

            return exchangeRates;
        }

        public decimal CalculateMaxRate(Dictionary<string, decimal> exchangeRates)
        {
            return exchangeRates.Max(x => x.Value);
        }

        public Dictionary<string, decimal> MultiplyRatesBy1000(Dictionary<string, decimal> exchangeRates)
        {
            return exchangeRates.ToDictionary(x => x.Key, x => x.Value * 1000);
        }

        public IOrderedEnumerable<KeyValuePair<string, decimal>> SortRatesByTypeAndName(Dictionary<string, decimal> exchangeRates)
        {
            return exchangeRates.OrderBy(x => x.Key.Split('_')[0])
                               .ThenBy(x => x.Key.Split('_')[1]);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TaskService.Controllers
{
    [ApiController]
    [Route("api/currency")]
    public class CurrencyController : ControllerBase
    {
        private const string CacheKey = "cbr_daily_json";
        private const string CbrUrl = "https://www.cbr-xml-daily.ru/daily_json.js";

        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(IMemoryCache cache, IHttpClientFactory httpClientFactory, ILogger<CurrencyController> logger)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetRates()
        {
            if (_cache.TryGetValue(CacheKey, out string cachedJson))
            {
                return Content(cachedJson, "application/json");
            }

            var client = _httpClientFactory.CreateClient("cbr");

            // Simple retry policy (3 attempts) with small delays
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var resp = await client.GetAsync(CbrUrl);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("CBR returned status {Status} on attempt {Attempt}", resp.StatusCode, attempt);
                    }
                    else
                    {
                        var body = await resp.Content.ReadAsStringAsync();
                        // cache for 5 minutes
                        _cache.Set(CacheKey, body, TimeSpan.FromMinutes(5));
                        return Content(body, "application/json");
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "HTTP error when fetching CBR (attempt {Attempt})", attempt);
                }

                await Task.Delay(TimeSpan.FromSeconds(1 * attempt));
            }

            return StatusCode(503, new { error = "Failed to fetch currency rates" });
        }
    }
}

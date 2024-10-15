
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace WorkerServiceDev
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        private string? connectionString;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient(); // Initialize HttpClient once
        }

        public Worker(ILogger<Worker> logger, string? connectionString) : this(logger)
        {
            this.connectionString = connectionString;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                try
                {
                    string url = "https://latest.currency-api.pages.dev/v1/currencies/eur.json";
                    HttpResponseMessage response = await _httpClient.GetAsync(url, stoppingToken);

                    


                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        var obj = JsonConvert.DeserializeObject<dynamic>(data);

                        // Extract actual values from API response (example: USD, ADA, BTC, etc.)
                        float usdRate = obj?.eur?.usd ?? 0;   // Default to 0 if value is null
                        float aaveRate = obj?.eur?.aave ?? 0;
                        float adaRate = obj?.eur?.ada ?? 0;
                        float btcRate = obj?.eur?.btc ?? 0;
                        float inrRate = obj?.eur?.inr ?? 0;

                        // Insert extracted data into the database
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            SqlCommand cmd = new SqlCommand(
                                @"INSERT INTO CurrencyRates (aave, ada, usd, btc, inr, Timestamp) 
                                  VALUES (@aave, @ada, @usd, @btc, @inr, @timestamp)", conn);

                            // Pass the actual rates extracted from the API response
                            cmd.Parameters.AddWithValue("@aave", aaveRate);
                            cmd.Parameters.AddWithValue("@ada", adaRate);
                            cmd.Parameters.AddWithValue("@usd", usdRate);
                            cmd.Parameters.AddWithValue("@btc", btcRate);
                            cmd.Parameters.AddWithValue("@inr", inrRate);
                            cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);

                            await cmd.ExecuteNonQueryAsync();
                            Console.WriteLine("Data inserted successfully.");
                        }
                    }
                    else
                    {
                        _logger.LogError("API request failed with status code: {statusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error fetching or inserting currency rates: {message}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); // Delay for 15 minutes
            }
        }
    }
}



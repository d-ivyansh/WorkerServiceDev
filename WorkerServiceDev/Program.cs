using WorkerServiceDev;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Fetch the connection string from appsettings.json
        string connectionString = context.Configuration.GetConnectionString("CurrencyExchangeDB");

        // Pass the connection string to the Worker class
        services.AddHostedService<Worker>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Worker>>();
            return new Worker(logger, connectionString);
        });
    });

var host = builder.Build();
host.Run();



using tunman;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<TunManOptions>(ctx.Configuration.GetSection("TunMan"));
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

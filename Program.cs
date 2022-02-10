var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => { services.AddTransient<WalletNotifierService>(); })
    .Build();

var my = host.Services.GetRequiredService<WalletNotifierService>();
await my.ExecuteAsync();

using NotificationWorker;
using NotificationWorker.Services;
using NotificationWorker.Configuration;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hasContext, services) =>
    {
        IConfiguration configuration = hasContext.Configuration;
        services.AddHostedService<Worker>();
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddTransient<IEmailService, EmailService>();
    })
    .Build();

await host.RunAsync();

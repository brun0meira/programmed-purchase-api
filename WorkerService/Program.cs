using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkerService;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                hostContext.Configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(hostContext.Configuration.GetConnectionString("DefaultConnection"))
            ));

    services.AddScoped<IClienteRepository, ClienteRepository>();

    services.AddHostedService<IrConsumerWorker>();
});

var host = builder.Build();
host.Run();
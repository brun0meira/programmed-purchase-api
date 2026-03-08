using Domain.Business;
using Domain.ExternalServices;
using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Data.ExternalServices;
using Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using Infrastructure.Queue;
using Prometheus;
public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        Console.WriteLine("Startup constructor called. Configuration loaded.", Configuration.GetConnectionString("DefaultConnection"));
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        services.AddSwaggerGen(c =>
        {
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Programmed Purchase API",
                Version = "v1"
            });
        });

        // Configuração do Banco de Dados
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                Configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(Configuration.GetConnectionString("DefaultConnection"))
            ));

        // Repositories
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ICestaRepository, CestaRepository>();
        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<IRebalanceamentoRepository, RebalanceamentoRepository>();

        // Business (Services)
        services.AddScoped<IContaMasterService, ContaMasterService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ICestaService, CestaService>();
        services.AddScoped<IRebalanceamentoService, RebalanceamentoService>();
        services.AddScoped<IMotorCompraService, MotorCompraService>();
        services.AddScoped<IKafkaProducerService, KafkaProducerService>();

        // External Services
        services.AddScoped<ICotacaoB3Service, CotacaoB3Service>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.Migrate();
        }

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Programmed Purchase v1");
            c.RoutePrefix = "swagger";
        });

        app.UseRouting();

        app.UseHttpMetrics();

        app.UseCors("AllowAll");

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.MapMetrics();
        });
    }
}
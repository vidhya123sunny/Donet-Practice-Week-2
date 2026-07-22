using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using System.Reflection;

if (Assembly.GetEntryAssembly()?.GetName().Name is "ef" or "dotnet-ef")
{
    return;
}

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5058";

var host = new WebHostBuilder()
    .UseKestrel()
    .UseUrls(urls)
    .ConfigureServices(services =>
    {
        services.AddControllers();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(DatabaseSettings.SqliteConnectionString));
        services.AddCors(options =>
        {
            options.AddPolicy("ReactClient", policy =>
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    })
    .Configure(app =>
    {
        app.UseRouting();
        app.UseCors("ReactClient");
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    })
    .Build();

await host.RunAsync();

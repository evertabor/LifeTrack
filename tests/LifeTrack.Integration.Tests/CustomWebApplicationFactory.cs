using LifeTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Integration.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var removeTypes = new[]
            {
                typeof(AppDbContext),
                typeof(DbContextOptions<AppDbContext>),
                typeof(DbContextOptions<>),
                typeof(IDbContextOptionsConfiguration<AppDbContext>)
            };

            foreach (var removeType in removeTypes)
            {
                var descriptors = services.Where(s => s.ServiceType == removeType).ToList();
                foreach (var d in descriptors)
                    services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite("DataSource=:memory:");
            });
        });
    }
}

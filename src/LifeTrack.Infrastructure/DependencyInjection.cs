using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Identity;
using LifeTrack.Infrastructure.Identity;
using LifeTrack.Infrastructure.Persistence;
using LifeTrack.Infrastructure.Persistence.Repositories;
using LifeTrack.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Infrastructure;

public static class DependencyInjection
{
    private const int MinPasswordLength = 12;
    private const int MaxFailedAccessAttempts = 5;
    private const int LockoutMinutes = 15;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure()));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = MinPasswordLength;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;
            options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(LockoutMinutes);
        })
        .AddUserManager<UserManager<ApplicationUser>>()
        .AddSignInManager<SignInManager<ApplicationUser>>()
        .AddClaimsPrincipalFactory<ApplicationClaimsFactory>()
        .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddSingleton<TimeProvider>(CreateZonedTimeProvider(configuration));

        return services;
    }

    private static TimeProvider CreateZonedTimeProvider(IConfiguration configuration)
    {
        var timeZoneId = configuration["App:TimeZoneId"];

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("Missing required configuration 'App:TimeZoneId'.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone) || zone is null)
        {
            throw new InvalidOperationException($"Unknown time zone '{timeZoneId}' in configuration 'App:TimeZoneId'.");
        }

        return new ZonedTimeProvider(TimeProvider.System, zone);
    }
}

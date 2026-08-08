using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using Kaan.SecurityPlatform.Api.Hubs;
using Kaan.SecurityPlatform.Api.Infrastructure;
using Kaan.SecurityPlatform.Api.Infrastructure.Authorization;
using Kaan.SecurityPlatform.Api.Infrastructure.Filters;
using Kaan.SecurityPlatform.Api.Infrastructure.Middleware;
using Kaan.SecurityPlatform.Application;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Infrastructure;
using Kaan.SecurityPlatform.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Kaan Security Platform API başlıyor");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddDevelopmentInProcessWorkers(builder.Configuration, builder.Environment);

    builder.Services.AddScoped<IActivityEventPublisher, SignalRActivityEventPublisher>();

    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions
    {
        Issuer = "kaan-security-platform",
        Audience = "kaan-security-platform-clients",
        SigningKey = "dev-key-please-change-me-in-production-32chars-minimum"
    };
    builder.Services.AddSingleton(jwtOptions);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        ctx.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddKaanAuthorization();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FluentValidationActionFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("default", limiter =>
        {
            limiter.PermitLimit = 200;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
        options.AddFixedWindowLimiter("scan-start", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(5);
            limiter.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("lab-elevate", limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window = TimeSpan.FromMinutes(5);
            limiter.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("lab-start", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(10);
            limiter.QueueLimit = 0;
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("web-app", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "http://localhost:3001" };
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Kaan Security Platform API",
            Version = "v1",
            Description = "Firmalara pasif güvenlik doktorluğu sağlayan platformun API dokümantasyonu."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT bearer token. Örnek: 'Bearer eyJhbGciOi...'"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<Kaan.SecurityPlatform.Infrastructure.Persistence.SecurityPlatformDbContext>();
            if (dbContext.Database.IsRelational())
            {
                await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(dbContext.Database);
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Kaan.SecurityPlatform.Infrastructure.Identity.ApplicationUser>>();
            var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Kaan.SecurityPlatform.Infrastructure.Identity.ApplicationRole>>();
            var startupLogger = services.GetRequiredService<ILogger<Program>>();
            await Kaan.SecurityPlatform.Infrastructure.Persistence.Seed.DatabaseSeeder.SeedAsync(dbContext, userManager, roleManager, startupLogger);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Veritabanı migration/seed başarısız. Login çalışmaz.");
            if (app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ProblemDetailsMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kaan Security Platform API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Kaan Security Platform API";
        });
    }

    // Development'ta Next.js server-side fetch HTTP kullanır; HTTPS yönlendirme
    // self-signed sertifika yüzünden "fetch failed" üretir.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("web-app");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting("default");
    app.MapHub<ActivityHub>("/hubs/activity");

    app.MapGet("/", () => Results.Redirect("/swagger"));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API başlatılırken beklenmeyen bir hata oluştu");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;


using System.Text;
using System.Text.Json.Serialization;
using LostAndFound.Api.Services;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using LostAndFound.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Npgsql;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);
});

// EF Core - PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(cs))
    {
        // Fallback: építsük fel a kapcsolatot .env komponensekből helyi fejlesztéshez
        var host = builder.Configuration["EXTERNAL_DB_HOST"] ?? builder.Configuration["POSTGRES_HOST"];
        var portStr = builder.Configuration["POSTGRES_PORT"] ?? "5432";
        var isDev = builder.Environment.IsDevelopment();
        var dbFromEnv = builder.Configuration["POSTGRES_DB"];
        var db = !string.IsNullOrWhiteSpace(dbFromEnv)
            ? dbFromEnv
            : (isDev ? "lostandfound_dev" : "lostandfound");
        var user = builder.Configuration["POSTGRES_USER"];
        var pwd = builder.Configuration["POSTGRES_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pwd))
        {
            if (!int.TryParse(portStr, out var port)) port = 5432;
            var sb = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = db,
                Username = user,
                Password = pwd,
                SslMode = SslMode.Disable
            };
            cs = sb.ToString();
        }
    }
    options.UseNpgsql(cs);
});

// ASP.NET Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"]!;
var audience = jwtSection["Audience"]!;
var secret = jwtSection["Secret"]!;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true; // include details in WWW-Authenticate header
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var log = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuth");
                log.LogError(context.Exception, "JWT authentication failed. Path={Path}", context.Request.Path);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var log = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuth");
                log.LogWarning("JWT challenge issued. Error={Error} Description={Description} Path={Path}",
                    context.Error, context.ErrorDescription, context.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

// CORS (fejlesztéshez mindent engedünk, később szigorítani)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Controllers & Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LostAndFound API", Version = "v1" });
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Put only your access token here prefixed with 'Bearer ' is not required in this input.",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            jwtSecurityScheme,
            Array.Empty<string>()
        }
    });
});

// App services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddHostedService<AutoDisposalService>();
builder.Services.AddHttpContextAccessor();

// Reverse proxy (Nginx Proxy Manager) forwarded headers support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust all proxies/networks by clearing defaults. In hardened setups, set KnownProxies/KnownNetworks explicitly.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Respect X-Forwarded-* from reverse proxy before deciding on redirects
    app.UseForwardedHeaders();
    // In non-development, enforce HTTPS (will use X-Forwarded-Proto when behind proxy)
    app.UseHttpsRedirection();
}

app.UseCors("DevCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations and ensure default roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await DataSeeder.SeedAsync(roleManager, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration or seeding failed");
        throw;
    }
}

await app.RunAsync();

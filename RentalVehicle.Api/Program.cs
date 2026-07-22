using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.Hubs;
using RentalVehicle.Api.Services;
using RentalVehicle.Api.Settings;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Production
});


// ================================
// CONFIGURATION
// Fix Render Docker inotify issue
// ================================

builder.Configuration.Sources.Clear();

builder.Configuration
    .AddJsonFile(
        "appsettings.json",
        optional: false,
        reloadOnChange: false
    )
    .AddEnvironmentVariables();


// ================================
// MVC + SWAGGER
// ================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Rental Vehicle API",
            Version = "v1"
        });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                    new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});


// ================================
// SETTINGS
// ================================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<PayOsSettings>(
    builder.Configuration.GetSection(PayOsSettings.SectionName));


var jwt =
    builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>();


if (jwt == null)
{
    throw new InvalidOperationException(
        "JWT settings missing.");
}


if (string.IsNullOrWhiteSpace(jwt.Secret)
    || jwt.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Secret phải có ít nhất 32 ký tự.");
}


// ================================
// DATABASE
// Supabase PostgreSQL
// ================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration
        .GetConnectionString("DefaultConnection"));
});


// ================================
// JWT AUTH
// ================================

builder.Services
.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,

            ValidateAudience = true,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,


            ValidIssuer = jwt.Issuer,

            ValidAudience = jwt.Audience,


            IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Secret)),


            ClockSkew =
            TimeSpan.FromSeconds(30)
        };


    options.Events =
    new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token =
                context.Request.Query["access_token"];


            var path =
                context.HttpContext.Request.Path;


            if (!string.IsNullOrEmpty(token)
                &&
                (
                path.StartsWithSegments("/hubs/chat")
                ||
                path.StartsWithSegments("/hubs/availability")
                ))
            {
                context.Token = token;
            }


            return Task.CompletedTask;
        }
    };
});


builder.Services.AddAuthorization();


// ================================
// SIGNAL R
// ================================

builder.Services.AddSignalR();


// ================================
// SERVICES
// ================================

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddScoped<IDriverService, DriverService>();

builder.Services.AddScoped<IVerificationService, VerificationService>();

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IPromotionService, PromotionService>();


// ================================
// CORS
// Vercel + Local
// ================================

var corsOrigins =
    builder.Configuration
    .GetSection("Cors:Origins")
    .Get<string[]>();


if (corsOrigins == null || corsOrigins.Length == 0)
{
    corsOrigins =
    [
        "http://localhost:3000"
    ];
}


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});


// ================================
// RATE LIMIT
// ================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;


    options.AddFixedWindowLimiter(
        "api",
        limiter =>
        {
            limiter.PermitLimit = 120;

            limiter.Window =
                TimeSpan.FromMinutes(1);

            limiter.QueueLimit = 0;

            limiter.AutoReplenishment = true;
        });
});


// ================================
// RENDER PROXY
// ================================

builder.Services.Configure<ForwardedHeadersOptions>(
options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        |
        ForwardedHeaders.XForwardedProto;


    options.KnownNetworks.Clear();

    options.KnownProxies.Clear();
});


// ================================
// BUILD APP
// ================================

var app = builder.Build();


// ================================
// GLOBAL ERROR HANDLER
// ================================

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var error =
        context.Features
        .Get<IExceptionHandlerFeature>()
        ?.Error;


        app.Logger.LogError(
            error,
            "Unhandled error");


        var postgres =
            error as PostgresException
            ??
            (error as DbUpdateException)
            ?.InnerException as PostgresException;


        var conflict =
            postgres?.SqlState == "23505";


        context.Response.StatusCode =
            conflict
            ?
            409
            :
            500;


        context.Response.ContentType =
            "application/json";


        await context.Response.WriteAsJsonAsync(
        new
        {
            message =
            conflict
            ?
            "Dữ liệu bị trùng."
            :
            "Lỗi máy chủ."
        });
    });
});


// ================================
// SWAGGER
// Enable Render testing
// ================================

app.UseSwagger();

app.UseSwaggerUI();


// ================================
// PIPELINE
// ================================

app.UseForwardedHeaders();

app.UseCors();

app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();


// ================================
// ROUTES
// ================================

app.MapControllers()
   .RequireRateLimiting("api");


app.MapHub<ChatHub>(
    "/hubs/chat");


app.MapHub<AvailabilityHub>(
    "/hubs/availability");


app.MapGet(
    "/health",
    () =>
    Results.Ok(
        new
        {
            status = "healthy",
            utc = DateTime.UtcNow
        }));


// ================================
// DATABASE MIGRATION DISABLED
// Supabase managed manually
// ================================


app.Run();


public partial class Program
{
}
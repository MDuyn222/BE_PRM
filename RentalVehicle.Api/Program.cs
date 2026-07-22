using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.Hubs;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;
using RentalVehicle.Api.Settings;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rental Vehicle API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<PayOsSettings>(builder.Configuration.GetSection(PayOsSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt settings are missing.");
if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) || jwt.Secret.Length < 32)
    throw new InvalidOperationException("JWT secret phải được cấu hình bằng biến môi trường Jwt__Secret và dài ít nhất 32 ký tự.");

builder.Services.AddDbContext<AppDbContext>((sp, o) => o.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)), ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/availability"))) context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
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

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("api", x => { x.PermitLimit = 120; x.Window = TimeSpan.FromMinutes(1); x.QueueLimit = 0; x.AutoReplenishment = true; });
});
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(error, "Unhandled request error");
    var postgres = error as PostgresException ?? (error as DbUpdateException)?.InnerException as PostgresException;
    var conflict = postgres?.SqlState is "23P01" or "23505";
    context.Response.StatusCode = conflict ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new
    {
        message = conflict
            ? "Dữ liệu vừa được thay đổi bởi yêu cầu khác hoặc khoảng thời gian đã bị đặt. Vui lòng tải lại và thử lại."
            : "Đã xảy ra lỗi máy chủ."
    });
}));

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseForwardedHeaders(); app.UseCors(); app.UseStaticFiles(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllers().RequireRateLimiting("api");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<AvailabilityHub>("/hubs/availability");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }));

// Database schema is managed manually in Supabase.
// Auto migration and demo seeding are disabled for production deployment.


app.Run();
public partial class Program { }

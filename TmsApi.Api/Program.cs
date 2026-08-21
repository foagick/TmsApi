using System.Threading.Channels;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.AspNetCore.Antiforgery;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.Filters;
using TmsApi.Api.Hubs;
using TmsApi.Api.Middleware;
using TmsApi.Api.Notifications;
using TmsApi.Api.RateLimiting;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Notifications;
using TmsApi.Application.Services;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST—it must wrap ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSignalR();


builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

// Production-only — leave commented in lab
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
//     options.InstanceName = "tms:";
// });

// Add services.
// builder.Services.AddControllers();
builder.Services.AddControllers(options => { options.Filters.Add<AuditLogFilter>(); });

// Register services
// builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();
builder.Services.AddSingleton<ITranscriptNotificationService, SignalRTranscriptNotificationService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

builder.Services.AddHealthChecks();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);
        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };

    options.AddConcurrencyLimiter("transcripts", opt =>
    {
        opt.PermitLimit = 5;
        opt.QueueLimit = 20;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddTokenBucketLimiter("search", opt =>
    {
        opt.TokenLimit = 10;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase")));

// Register TmsDbContext scoped for incoming HTTP requests
// builder.Services.AddDbContext<TmsDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
//             .LogTo(Console.WriteLine, LogLevel.Information)
//             .EnableSensitiveDataLogging());

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add API versioning
builder.Services.AddOpenApi(documentName: "v1",
    configureOptions: options => { options.ShouldInclude = descriptor => descriptor.GroupName == "v1"; });

builder.Services.AddOpenApi(documentName: "v2",
    configureOptions: options => { options.ShouldInclude = descriptor => descriptor.GroupName == "v2"; });

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();

        //Optional: Combine multiple version readers (URL segment and custom header)
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("x-api-version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));

builder.Services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });

builder.Services.AddIdentityCore<TmsUser>(options =>
{
// Enterprise Password Policy
options.Password.RequiredLength = 12;
options.Password.RequireUppercase = true;
options.Password.RequireDigit = true;
options.Password.RequireNonAlphanumeric = true;
// Brute-Force Lockout Protection
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>();

var app = builder.Build();



app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("TmsClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<V1DeprecationMiddleware>();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || context.Request.Cookies.ContainsKey("tms_auth"))
    {
        var antiforgery = context.RequestServices
            .GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false, // MUST be false so Angular JavaScript can read it!
                Secure = !builder.Environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict
            });
    }

    await next(context);
});

app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");

app.MapHealthChecks("/health/live").DisableRateLimiting();
app.MapHealthChecks("/health/ready").DisableRateLimiting();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(configureOptions: options =>
    {
        options.WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

        options.AddDocument("v1", title: "API Version 1.0")
            .AddDocument("v2", title: "API Version 2.0");
    });
}

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}));





app.Run();
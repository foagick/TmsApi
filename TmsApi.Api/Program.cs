using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Scalar.AspNetCore;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.Filters;
using TmsApi.Api.Middleware;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Services;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST—it must wrap ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services.
// builder.Services.AddControllers();
builder.Services.AddControllers(options => { options.Filters.Add<AuditLogFilter>(); });

// Register services
// builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();


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
        // options.ApiVersionReader = ApiVersionReader.Combine(
        //     new UrlSegmentApiVersionReader(),
        //     new HeaderApiVersionReader("x-api-version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });


    builder.Services.AddHybridCache(options => 
    { 
        options.DefaultEntryOptions = new HybridCacheEntryOptions 
        { 
            Expiration = TimeSpan.FromMinutes(10), 
            LocalCacheExpiration = TimeSpan.FromMinutes(2) 
        }; 
    });


var app = builder.Build();

app.UseMiddleware<V1DeprecationMiddleware>();


app.UseExceptionHandler();
app.UseStatusCodePages();
// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


// if (app.Environment.IsDevelopment())
//     {
//         using var scope = app.Services.CreateScope();
//         var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
//         await DataSeeder.SeedAsync(context);

//         app.MapOpenApi();
//         app.MapScalarApiReference();

//     }

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


// Seed test data at startup
// using (var scope = app.Services.CreateScope())
//     {

//     var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
//     context.Database.Migrate(); // Applies any pending migrations; keeps migration history intact

//     if (!context.Students.Any())
//         {

//         var students = new List<Student>
//             {

//             new() { RegistrationNumber = "TMS-2026-0001", Name = "AliceSmith", GPA = 3.8m, IsActive = true },
//             new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
//             new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
//             new() { RegistrationNumber = "TMS-2026-0004", Name = "DianaPrince", GPA = 3.9m, IsActive = true },
//             new() { RegistrationNumber = "TMS-2026-0005", Name = "EvanWright", GPA = 2.5m, IsActive = true }

//             };

//         context.Students.AddRange(students);

//         var courses = new List<Course>
//             {

//             new() { Code = "CS-101", Title = "Introduction to ComputerScience", MaxCapacity = 30 },
//             new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
//             new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }

//             };

//         context.Courses.AddRange(courses);
//         context.SaveChanges();

//         var enrollments = new List<Enrollment>
//             {

//             new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
//             new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
//             new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
//             new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
//             };

//         context.Enrollments.AddRange(enrollments);
//         context.SaveChanges();
//             }
//     }

app.Run();
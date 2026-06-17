var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register services
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
// builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

builder.Services.AddAuthentication();   // minimal setup
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();


app.MapGet("/api/assessments/results", () => Results.Ok(new
{
courseCode = "CS-101",
studentId = "S-001",
letterGrade = "A"
}));
app.UseAuthentication();
app.UseAuthorization();


// var builder = WebApplication.CreateBuilder(args);
// Services: add authentication / authorization services
// var app = builder.Build();
// TODO1:Register routing in the pipeline where it belongs for your app.
// TODO2:Register authentication and authorization in the pipeline where your template and facilitator expect them for a protected minimal API route.
// TODO3:MapGET/api/assessments/results with the same response body as the starter, but require authorization for that route.
app.Run();
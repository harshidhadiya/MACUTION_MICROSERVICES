var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5005"; // your auth server
        options.Audience = "gateway";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

// Add YARP
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();   // This replaces your controller

app.Run();
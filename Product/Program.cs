using System.Text;
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using MACUTION.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Name;
using PRODUCT.GlobalErrorHandler;
using PRODUCT.Model;
using PRODUCT.Validation;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PasswordHasher<object>>();
builder.Services.AddDbContext<MACUTIONDB>(options=>options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddValidatorsFromAssemblyContaining<productCreateValidation>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5087") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddAuthentication(options =>   
{  
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;  
}).AddJwtBearer(options =>  
{  
    options.TokenValidationParameters = new TokenValidationParameters  
    {  
        ValidateIssuer = false,  
        ValidateAudience = false,  
        ValidateLifetime = true,  
        ValidateIssuerSigningKey = false,  
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };  
});
builder.Services.AddExceptionHandler<GlobalErrorHandler>();
builder.Services.AddAuthorization();
builder.Services.AddControllers().AddJsonOptions((option=>option.JsonSerializerOptions.UnmappedMemberHandling= System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddAutoMapper(typeof(Mapper));
var app = builder.Build();
app.UseCors("MyPolicy");
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MappingId>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/", () => "Creating Project For User Management System");
app.Run("http://localhost:8081");
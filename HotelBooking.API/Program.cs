using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Repositories;
using HotelBooking.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using HotelBooking.API.ExceptionHandlers;
using HotelBooking.Application.Authentication.Login;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<HotelBookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ILoginService, LoginService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.MapControllers();

app.Run();

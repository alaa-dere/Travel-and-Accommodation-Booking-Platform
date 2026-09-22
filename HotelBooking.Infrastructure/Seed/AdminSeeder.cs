using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Infrastructure.Seed;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var adminExists = await dbContext.Users.AnyAsync(user => user.Role == Role.Admin);
        if (adminExists)
        {
            return;
        }
        
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var firstName = configuration["Admin:FirstName"];
        var lastName = configuration["Admin:LastName"];
        var userName = configuration["Admin:UserName"];
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Admin bootstrap configuration is missing.");
        }
        
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = passwordHasher.HashPassword(password);

        User admin = new User(firstName, lastName, userName, email, passwordHash, Role.Admin);
        await dbContext.Users.AddAsync(admin);
        await dbContext.SaveChangesAsync();
    }
}
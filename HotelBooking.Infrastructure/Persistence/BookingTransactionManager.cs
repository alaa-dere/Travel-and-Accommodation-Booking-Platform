using System.Data;
using HotelBooking.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence;

public class BookingTransactionManager : IBookingTransactionManager
{
    private readonly HotelBookingDbContext _dbContext;

    public BookingTransactionManager(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteSerializableAsync(Func<Task> operation)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            await operation();
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
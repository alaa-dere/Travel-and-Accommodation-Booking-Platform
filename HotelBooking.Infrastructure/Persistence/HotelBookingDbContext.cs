using Microsoft.EntityFrameworkCore;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence;

public class HotelBookingDbContext : DbContext
{
    public HotelBookingDbContext(DbContextOptions<HotelBookingDbContext> options) : base(options)
    {
        
    }
    public DbSet<User> Users { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<HotelImage> HotelImages { get; set; }
    public DbSet<RoomImage> RoomImages { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<HotelAmenity> HotelAmenities { get; set; }
    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<NearbyAttraction> NearbyAttractions  { get; set; }
    public DbSet<RecentlyVisitedHotel> RecentlyVisitedHotels { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(HotelBookingDbContext).Assembly);
    }
}
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Repositories;
using HotelBooking.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using HotelBooking.API.ExceptionHandlers;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotelBooking.Application;
using HotelBooking.Application.AvailableRooms;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Cart;
using HotelBooking.Application.Cart.Create;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Cities.Delete;
using HotelBooking.Application.FeatureDeals;
using HotelBooking.Application.HotelDetails;
using HotelBooking.Application.HotelImages;
using HotelBooking.Application.HotelLocations;
using HotelBooking.Application.HotelReviews;
using HotelBooking.Application.Hotels.Create;
using HotelBooking.Application.Hotels.Delete;
using HotelBooking.Application.Hotels.Retrive;
using HotelBooking.Application.Hotels.Update;
using HotelBooking.Application.Payments;
using HotelBooking.Application.RecentlyVisitedHotels;
using HotelBooking.Application.Rooms.Create;
using HotelBooking.Application.Rooms.Delete;
using HotelBooking.Application.Rooms.Images;
using HotelBooking.Application.Rooms.Retrive;
using HotelBooking.Application.Rooms.Update;
using HotelBooking.Application.Search;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Application.TrendingDestinations;
using HotelBooking.Infrastructure.Seed;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using HotelBooking.Application.Invoices;
using HotelBooking.Infrastructure.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
});
builder.Services.AddDbContext<HotelBookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT Key is missing.");
}
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<IGetAllCitiesService, GetAllCities>();
builder.Services.AddScoped<ICreateCityService, CreateCity>();
builder.Services.AddScoped<IUpdateCityService, UpdateCity>();
builder.Services.AddScoped<IDeleteCityService, DeleteCity>();
builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IGetAllHotelsService, GetAllHotels>();
builder.Services.AddScoped<IUpdateHotelService, UpdateHotel>();
builder.Services.AddScoped<IChangeHotelStatusService, ChangeHotelStatus>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ICreateRoomService, CreateRoom>();
builder.Services.AddScoped<IGetAllRoomsService, GetAllRooms>();
builder.Services.AddScoped<IChangeRoomStatusService , ChangeRoomStatus>();
builder.Services.AddScoped<IUpdateRoomService, UpdateRoom>();
builder.Services.AddScoped<ICreateHotelService, CreateHotel>();
builder.Services.AddScoped<ISearchHotelsService, SearchHotels>();
builder.Services.AddScoped<IHotelSearchRepository, HotelSearchRepository>();
builder.Services.AddScoped<IFeaturedDealsRepository , FeaturedDealsRepository>();
builder.Services.AddScoped<IFeaturedDealsService, GetFeaturedDealsService>();
builder.Services.AddScoped<IGetHotelDetailsService , GetHotelDetails>();
builder.Services.AddScoped<IHotelImageRepository, HotelImageRepository>();
builder.Services.AddScoped<IGetHotelImagesService, GetHotelImages>();
builder.Services.AddScoped<IAvailableRoomRepository, AvailableRoomRepository>();
builder.Services.AddScoped<IGetAvailableRoomsService, GetAvailableRooms>();
builder.Services.AddScoped<IHotelReviewRepository, HotelReviewRepository>();
builder.Services.AddScoped<IGetHotelReviewsService, GetHotelReviews>();
builder.Services.AddScoped<IHotelLocationRepository, HotelLocationRepository>();
builder.Services.AddScoped<IGetHotelLocationService, GetHotelLocationService>();
builder.Services.AddScoped<ISelectAvailableRoomService, SelectAvailableRoom>();
builder.Services.AddScoped<IRecentlyVisitedHotelRepository, RecentlyVisitedHotelRepository>();
builder.Services.AddScoped<IRecordHotelVisitService, RecordHotelVisitService>();
builder.Services.AddScoped<IGetRecentlyVisitedHotelsService, GetRecentlyVisitedHotelsService>();
builder.Services.AddScoped<ITrendingDestinationRepository, TrendingDestinationRepository>();
builder.Services.AddScoped<IGetTrendingDestinationsService, GetTrendingDestinationsService>();
builder.Services.AddScoped<ISubmitHotelReviewService, SubmitHotelReviewService>();
builder.Services.AddScoped<IChangeRoomOperationalAvailabilityService, ChangeRoomOperationalAvailability>();
builder.Services.AddScoped<IRoomImageRepository, RoomImageRepository>();
builder.Services.AddScoped<IAddRoomImageService, AddRoomImageService>();
builder.Services.AddScoped<IDeleteRoomImageService, DeleteRoomImageService>();
builder.Services.AddScoped<IAddCartItemService, AddCartItemService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IGetCartService, GetCartService>();
builder.Services.AddScoped<IRemoveCartItemService, RemoveCartItemService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingAvailabilityService, BookingAvailabilityService>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IBookingPricingService, BookingPricingService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IBookingTransactionManager, BookingTransactionManager>();
builder.Services.AddScoped<ICreateBookingsService, CreateBookingsService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IGetInvoiceForPdfService, GetInvoiceForPdfService>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;
await AdminSeeder.SeedAsync(app.Services);

// HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
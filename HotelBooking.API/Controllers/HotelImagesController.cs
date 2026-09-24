using HotelBooking.Application.HotelImages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize(Roles = "Customer")]
public class HotelImagesController : ControllerBase
{
        private readonly IGetHotelImagesService _hotelImagesService;

        public HotelImagesController(IGetHotelImagesService hotelImagesService)
        {
            _hotelImagesService = hotelImagesService;
        }

        [HttpGet("{hotelId}/images")]
        public async Task<IActionResult> GetHotelImagesAsync(int hotelId)
        {
            var images  = await _hotelImagesService.GetHotelImagesAsync(hotelId);
            return Ok(images );
        }
}
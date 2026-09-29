using HotelBooking.Application.HotelImages;
using HotelBooking.Application.HotelImages.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize]
public class HotelImagesController : ControllerBase
{
    private readonly IGetHotelImagesService _hotelImagesService;
    private readonly IAddHotelImageService _addHotelImageService;
    private readonly IDeleteHotelImageService _deleteHotelImageService;

    public HotelImagesController(IGetHotelImagesService hotelImagesService, IAddHotelImageService addHotelImageService, IDeleteHotelImageService deleteHotelImageService)
    {
        _hotelImagesService = hotelImagesService;
        _addHotelImageService = addHotelImageService;
        _deleteHotelImageService = deleteHotelImageService;
    }

    [HttpGet("{hotelId:int:min(1)}/images")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetHotelImagesAsync(int hotelId)
    {
        var images = await _hotelImagesService.GetHotelImagesAsync(hotelId);
        return Ok(images);
    }

    [HttpPost("{hotelId:int:min(1)}/images")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddHotelImageAsync(int hotelId, AddHotelImageRequestDto request)
    {
        await _addHotelImageService.AddAsync(hotelId, request);
        return NoContent();
    }

    [HttpDelete("{hotelId:int:min(1)}/images/{imageId:int:min(1)}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteHotelImageAsync(int hotelId, int imageId)
    {
        await _deleteHotelImageService.DeleteAsync(hotelId, imageId);
        return NoContent();
    }
}

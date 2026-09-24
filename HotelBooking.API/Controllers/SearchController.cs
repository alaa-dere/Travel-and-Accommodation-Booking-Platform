using HotelBooking.Application.Search.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =  "Customer")]
public class SearchController : ControllerBase
{
   private readonly ISearchHotelsService _searchHotels;
   public SearchController(ISearchHotelsService  searchHotels)
   {
      _searchHotels = searchHotels;
   }

   [HttpGet]
   public async Task<IActionResult> GetAsync([FromQuery] HotelSearchRequestDto request)
   {
      var result = await _searchHotels.SearchHotelsAsync(request);
      return Ok(result);
   }
}
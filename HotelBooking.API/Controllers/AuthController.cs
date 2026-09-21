using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRegisterService _registerService;
    private readonly ILoginService _loginService;
    
    public AuthController(IRegisterService registerService, ILoginService loginService)
    {
        _registerService = registerService;
        _loginService = loginService;
    }

    [HttpPost("register")]  
    public async Task<IActionResult> RegisterAsync(RegisterRequestDto request)
    {
        await _registerService.RegisterAsync(request);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequestDto request)
    {
        await _loginService.LoginAsync(request);
        return Ok();
    }
}
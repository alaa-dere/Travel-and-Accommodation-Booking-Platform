using System.Security.Claims;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Customer")]
public class InvoicesController : ControllerBase
{
    private readonly IGetInvoiceForPdfService _getInvoiceForPdfService;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;

    public InvoicesController(IGetInvoiceForPdfService getInvoiceForPdfService, IInvoicePdfGenerator invoicePdfGenerator)
    {
        _getInvoiceForPdfService = getInvoiceForPdfService;
        _invoicePdfGenerator = invoicePdfGenerator;
    }

    [HttpGet("{invoiceId:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int invoiceId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var invoice = await _getInvoiceForPdfService.GetAsync(invoiceId, userId);
        var pdf = _invoicePdfGenerator.Generate(invoice);

        return File(pdf, "application/pdf", $"invoice-{invoice.InvoiceId}.pdf");
    }
}
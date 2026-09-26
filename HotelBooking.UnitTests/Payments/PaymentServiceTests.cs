using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Payments;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();

        _service = new PaymentService(
            _paymentRepositoryMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentShouldSucceed_ShouldMarkPaymentAsPaid()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = true
        };

        // Act
        var result = await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        Assert.Equal(PaymentStatus.Paid, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentShouldFail_ShouldMarkPaymentAsFailed()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = false
        };

        // Act
        var result = await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        Assert.Equal(PaymentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldCreatePaymentWithInvoiceTotalAmount()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = true
        };

        // Act
        var result = await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        Assert.Equal(invoice.TotalAmount, result.Amount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldAssociatePaymentWithInvoice()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = true
        };

        // Act
        var result = await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        Assert.Same(invoice, result.Invoice);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldAddPaymentToRepository()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = true
        };

        Payment? addedPayment = null;

        _paymentRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(payment =>
                addedPayment = payment);

        // Act
        var result = await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        Assert.NotNull(addedPayment);
        Assert.Same(result, addedPayment);

        _paymentRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<Payment>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentSucceeds_ShouldAddPaidPayment()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = true
        };

        // Act
        await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        _paymentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Payment>(payment =>
                    payment.Amount == invoice.TotalAmount &&
                    payment.Status == PaymentStatus.Paid &&
                    payment.Invoice == invoice)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentFails_ShouldAddFailedPayment()
    {
        // Arrange
        var invoice = CreateInvoice();

        var paymentInformation = new PaymentInformationDto
        {
            ShouldSucceed = false
        };

        // Act
        await _service.ProcessPaymentAsync(
            invoice,
            paymentInformation);

        // Assert
        _paymentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Payment>(payment =>
                    payment.Amount == invoice.TotalAmount &&
                    payment.Status == PaymentStatus.Failed &&
                    payment.Invoice == invoice)),
            Times.Once);
    }

    private static Invoice CreateInvoice()
    {
        return new Invoice(
            userId: 1,
            hotelId: 10,
            totalAmount: 350m)
        {
            InvoiceId = 100
        };
    }
}
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Exceptions;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Services;
using Moq;

namespace BosDAT.Infrastructure.Tests.Services;

public class InvoiceEmailServiceTests
{
    private readonly Mock<IInvoiceService> _mockInvoiceService = new();
    private readonly Mock<IInvoicePdfService> _mockInvoicePdfService = new();
    private readonly Mock<IEmailTemplateRenderer> _mockTemplateRenderer = new();
    private readonly Mock<ISettingsService> _mockSettingsService = new();
    private readonly Mock<IEmailService> _mockEmailService = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepo = new();
    private readonly InvoiceEmailService _service;
    private readonly Guid _invoiceId = Guid.NewGuid();

    public InvoiceEmailServiceTests()
    {
        _mockUow.Setup(u => u.Invoices).Returns(_mockInvoiceRepo.Object);
        _service = new InvoiceEmailService(
            _mockInvoiceService.Object,
            _mockInvoicePdfService.Object,
            _mockTemplateRenderer.Object,
            _mockSettingsService.Object,
            _mockEmailService.Object,
            _mockUow.Object);
    }

    #region PreviewAsync Tests

    [Fact]
    public async Task PreviewAsync_WithValidInvoice_ReturnsPreviewWithRenderedHtml()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();
        SetupInvoiceAndSettings(invoice);
        _mockTemplateRenderer
            .Setup(r => r.RenderFromContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>Rendered HTML</p>");

        // Act
        var result = await _service.PreviewAsync(_invoiceId);

        // Assert
        Assert.Equal("<p>Rendered HTML</p>", result.HtmlBody);
        Assert.Contains("F-2026-001", result.Subject);
        Assert.Equal("student@example.com", result.ToEmail);
    }

    [Fact]
    public async Task PreviewAsync_WithBillingContact_UsesContactEmail()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto(billingContactEmail: "parent@example.com");
        SetupInvoiceAndSettings(invoice);
        _mockTemplateRenderer
            .Setup(r => r.RenderFromContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>HTML</p>");

        // Act
        var result = await _service.PreviewAsync(_invoiceId);

        // Assert
        Assert.Equal("parent@example.com", result.ToEmail);
    }

    [Fact]
    public async Task PreviewAsync_WhenInvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceDto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.PreviewAsync(_invoiceId));
    }

    [Fact]
    public async Task PreviewAsync_WhenNoEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto(studentEmail: null);
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PreviewAsync(_invoiceId));
        Assert.Contains("no email", ex.Message);
    }

    [Fact]
    public async Task PreviewAsync_InterpolatesSubjectPlaceholders()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();
        SetupInvoiceAndSettings(invoice, subjectTemplate: "Factuur {{InvoiceNumber}} - {{SchoolName}}");
        _mockTemplateRenderer
            .Setup(r => r.RenderFromContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>HTML</p>");

        // Act
        var result = await _service.PreviewAsync(_invoiceId);

        // Assert
        Assert.Equal("Factuur F-2026-001 - Test Music School", result.Subject);
    }

    #endregion

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_WithDraftInvoice_TransitionsToSent()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();
        var entity = new Invoice { Id = _invoiceId, InvoiceNumber = "F-2026-001", Status = InvoiceStatus.Draft };
        SetupInvoiceAndSettings(invoice);
        SetupForSend(invoice, entity);

        // Act
        await _service.SendAsync(_invoiceId);

        // Assert
        Assert.Equal(InvoiceStatus.Sent, entity.Status);
        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_QueuesEmailWithPdfAttachment()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();
        var entity = new Invoice { Id = _invoiceId, InvoiceNumber = "F-2026-001", Status = InvoiceStatus.Draft };
        SetupInvoiceAndSettings(invoice);
        SetupForSend(invoice, entity);

        // Act
        await _service.SendAsync(_invoiceId);

        // Assert
        _mockEmailService.Verify(e => e.QueueEmailAsync(
            "student@example.com",
            It.Is<string>(s => s.Contains("F-2026-001")),
            "__rendered__",
            It.IsAny<object>(),
            It.Is<IReadOnlyList<EmailAttachment>>(a => a.Count == 1 && a[0].FileName == "F-2026-001.pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithSentInvoice_DoesNotChangeStatus()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto(status: InvoiceStatus.Sent);
        var entity = new Invoice { Id = _invoiceId, InvoiceNumber = "F-2026-001", Status = InvoiceStatus.Sent };
        SetupInvoiceAndSettings(invoice);
        SetupForSend(invoice, entity);

        // Act
        await _service.SendAsync(_invoiceId);

        // Assert
        Assert.Equal(InvoiceStatus.Sent, entity.Status);
        _mockInvoiceRepo.Verify(r => r.GetByIdAsync(_invoiceId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithCreditInvoice_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto(isCreditInvoice: true);
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendAsync(_invoiceId));
        Assert.Contains("credit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAsync_WhenInvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceDto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SendAsync(_invoiceId));
    }

    [Fact]
    public async Task SendAsync_ReturnsUpdatedInvoice()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();
        var sentInvoice = CreateTestInvoiceDto(status: InvoiceStatus.Sent);
        var entity = new Invoice { Id = _invoiceId, InvoiceNumber = "F-2026-001", Status = InvoiceStatus.Draft };

        SetupInvoiceAndSettings(invoice);
        SetupForSend(invoice, entity);

        // Return the "sent" version on the second GetInvoiceAsync call
        _mockInvoiceService
            .SetupSequence(s => s.GetInvoiceAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice)
            .ReturnsAsync(sentInvoice);

        // Act
        var result = await _service.SendAsync(_invoiceId);

        // Assert
        Assert.Equal(InvoiceStatus.Sent, result.Status);
    }

    #endregion

    #region Helper Methods

    private InvoiceDto CreateTestInvoiceDto(
        InvoiceStatus status = InvoiceStatus.Draft,
        string? studentEmail = "student@example.com",
        string? billingContactEmail = null,
        bool isCreditInvoice = false)
    {
        return new InvoiceDto
        {
            Id = _invoiceId,
            InvoiceNumber = "F-2026-001",
            StudentId = Guid.NewGuid(),
            EnrollmentId = Guid.NewGuid(),
            StudentName = "Jan de Vries",
            StudentEmail = studentEmail,
            IssueDate = new DateOnly(2026, 1, 15),
            DueDate = new DateOnly(2026, 2, 15),
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            Description = "jan26",
            Subtotal = 100m,
            VatAmount = 21m,
            Total = 121m,
            Status = status,
            IsCreditInvoice = isCreditInvoice,
            BillingContact = billingContactEmail != null
                ? new BillingContactDto { Email = billingContactEmail, Name = "Parent" }
                : null,
            Lines = [],
            Payments = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SetupInvoiceAndSettings(InvoiceDto invoice, string subjectTemplate = "Factuur {{InvoiceNumber}} - {{SchoolName}}")
    {
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockSettingsService
            .Setup(s => s.GetByKeyAsync("email_invoice_subject_template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Setting { Key = "email_invoice_subject_template", Value = subjectTemplate });

        _mockSettingsService
            .Setup(s => s.GetByKeyAsync("email_invoice_body_template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Setting { Key = "email_invoice_body_template", Value = "<p>Beste @Model.StudentFirstName</p>" });

        _mockInvoiceService
            .Setup(s => s.GetSchoolBillingInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchoolBillingInfoDto
            {
                Name = "Test Music School",
                Address = "Teststraat 1",
                PostalCode = "1234 AB",
                City = "Amsterdam",
                Phone = "020-1234567",
                Email = "info@test.nl",
                KvkNumber = "12345678",
                BtwNumber = "NL123456789B01",
                Iban = "NL00INGB0001234567",
                VatRate = 21m
            });
    }

    private void SetupForSend(InvoiceDto invoice, Invoice entity)
    {
        _mockTemplateRenderer
            .Setup(r => r.RenderFromContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<p>Rendered</p>");

        _mockInvoicePdfService
            .Setup(s => s.GeneratePdfAsync(invoice, It.IsAny<SchoolBillingInfoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        _mockEmailService
            .Setup(e => e.QueueEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyList<EmailAttachment>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockInvoiceRepo
            .Setup(r => r.GetByIdAsync(_invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockUow
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #endregion
}

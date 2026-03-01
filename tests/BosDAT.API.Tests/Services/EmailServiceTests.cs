using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using Moq;
using Xunit;

namespace BosDAT.API.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IRepository<EmailOutboxMessage>> _outboxRepoMock = new();
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        _uowMock.Setup(u => u.EmailOutboxMessages).Returns(_outboxRepoMock.Object);
        _service = new EmailService(_uowMock.Object);
    }

    [Fact]
    public async Task QueueEmailAsync_CreatesOutboxMessage()
    {
        _outboxRepoMock
            .Setup(r => r.AddAsync(It.IsAny<EmailOutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailOutboxMessage m, CancellationToken _) => m);

        await _service.QueueEmailAsync(
            "user@example.com",
            "Welcome",
            "WelcomeTemplate",
            new { Name = "Test" });

        _outboxRepoMock.Verify(r => r.AddAsync(
            It.Is<EmailOutboxMessage>(m =>
                m.To == "user@example.com" &&
                m.Subject == "Welcome" &&
                m.TemplateName == "WelcomeTemplate" &&
                m.TemplateDataJson.Contains("Test")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task QueueEmailAsync_DoesNotCallSaveChanges()
    {
        _outboxRepoMock
            .Setup(r => r.AddAsync(It.IsAny<EmailOutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailOutboxMessage m, CancellationToken _) => m);

        await _service.QueueEmailAsync("a@b.com", "S", "T", new { });

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

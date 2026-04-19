using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nocturne.API.Controllers.V4.Treatments;
using Nocturne.API.Models.Requests.V4;
using Nocturne.Core.Contracts.V4.Repositories;
using Nocturne.Core.Models.V4;
using Xunit;

namespace Nocturne.API.Tests.Controllers.V4;

[Trait("Category", "Unit")]
public class BolusControllerTests
{
    private readonly Mock<IBolusRepository> _repoMock = new();

    private BolusController CreateController()
    {
        var controller = new BolusController(_repoMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private void SetupCreatePassthrough(Action<Bolus> onCreate)
    {
        _repoMock.As<IV4Repository<Bolus>>()
            .Setup(r => r.CreateAsync(It.IsAny<Bolus>(), It.IsAny<CancellationToken>()))
            .Callback<Bolus, CancellationToken>((b, _) => onCreate(b))
            .ReturnsAsync((Bolus b, CancellationToken _) => b);
    }

    [Fact]
    public async Task Create_PassesThroughCorrelationId()
    {
        var cid = Guid.NewGuid();
        Bolus? captured = null;
        SetupCreatePassthrough(b => captured = b);

        var controller = CreateController();
        var request = new CreateBolusRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            Insulin = 5.0,
            CorrelationId = cid,
        };

        await controller.Create(request);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(cid);
    }

    [Fact]
    public async Task Update_RequestCorrelationIdWins_WhenSupplied()
    {
        var existingCid = Guid.NewGuid();
        var requestCid = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existing = new Bolus
        {
            Id = id,
            Timestamp = DateTime.UtcNow,
            Insulin = 2.0,
            CorrelationId = existingCid,
        };
        Bolus? captured = null;

        _repoMock.As<IV4Repository<Bolus>>()
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock.As<IV4Repository<Bolus>>()
            .Setup(r => r.UpdateAsync(id, It.IsAny<Bolus>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Bolus, CancellationToken>((_, b, _) => captured = b)
            .ReturnsAsync((Guid _, Bolus b, CancellationToken _) => b);

        var controller = CreateController();
        var request = new UpdateBolusRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            Insulin = 3.0,
            CorrelationId = requestCid,
        };

        await controller.Update(id, request);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(requestCid);
    }

    [Fact]
    public async Task Create_WithoutCorrelationId_ServerMintsNonEmptyGuid()
    {
        Bolus? captured = null;
        SetupCreatePassthrough(b => captured = b);

        var controller = CreateController();
        var request = new CreateBolusRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            Insulin = 5.0,
            // CorrelationId intentionally omitted
        };

        await controller.Create(request);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().NotBeNull().And.NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Update_PreservesExistingCorrelationId_WhenRequestOmits()
    {
        var existingCid = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existing = new Bolus
        {
            Id = id,
            Timestamp = DateTime.UtcNow,
            Insulin = 2.0,
            CorrelationId = existingCid,
        };
        Bolus? captured = null;

        _repoMock.As<IV4Repository<Bolus>>()
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock.As<IV4Repository<Bolus>>()
            .Setup(r => r.UpdateAsync(id, It.IsAny<Bolus>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Bolus, CancellationToken>((_, b, _) => captured = b)
            .ReturnsAsync((Guid _, Bolus b, CancellationToken _) => b);

        var controller = CreateController();
        var request = new UpdateBolusRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            Insulin = 3.0,
            // CorrelationId intentionally omitted
        };

        await controller.Update(id, request);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(existingCid);
    }
}

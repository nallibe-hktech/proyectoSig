using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Infrastructure.Persistence;
using SIG.Infrastructure.Services;

namespace SIG.Tests.Unit.Services;

public class DataProcessorServiceTests
{
    private readonly AppDbContext _db = Substitute.For<AppDbContext>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IDepartmentRepository _deptRepo = Substitute.For<IDepartmentRepository>();
    private readonly IAuditLogRepository _auditRepo = Substitute.For<IAuditLogRepository>();
    private readonly ILogger<DataProcessorService> _logger = Substitute.For<ILogger<DataProcessorService>>();

    private DataProcessorService CreateSut() =>
        new(_db, _userRepo, _deptRepo, _auditRepo, _logger);

    [Fact]
    public async Task ProcessAllPendingAsync_RetornaResultadoValido()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.ProcessAllPendingAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Systems.Should().NotBeNull();
        result.TotalProcessed.Should().Be(0);
        result.TotalErrors.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAllPendingAsync_CuandoFalla_RetornaError()
    {
        // Setup: Simulate exception when calling staging repos
        _db.StagingBizneoEmpleados.Returns(x => throw new InvalidOperationException("DB Error"));

        // Act
        var sut = CreateSut();
        var result = await sut.ProcessAllPendingAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Error.Should().Contain("DB Error");
    }
}

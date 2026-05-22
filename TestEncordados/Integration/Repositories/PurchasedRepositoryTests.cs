using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Model;
using BackEncordados.Purchased.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using FilterPurchasedDto = BackEncordados.Purchased.Dto.FilterPurchasedDto;

namespace TestEncordados.Integration.Repositories;

public class PurchasedRepositoryTests
{
    private PedidosDbContext _context = null!;
    private IPuchasedRepository _repository = null!;
    private Mock<ILogger<PurchasedReposirtory>> _loggerMock = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Fresh in-memory database per test for complete isolation
        var options = new DbContextOptionsBuilder<PedidosDbContext>()
            .UseInMemoryDatabase("PurchasedRepositoryTestDb_" + Guid.NewGuid())
            .Options;

        _context = new PedidosDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _loggerMock = new Mock<ILogger<PurchasedReposirtory>>();
        _repository = new PurchasedReposirtory(_context, _loggerMock.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task CreatePurchasedAsync_ValidPedido_ReturnsCreatedPedido()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Comments = "Test Comments",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreatePurchasedAsync(pedido);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(default);
        result.Machine.Should().Be("Test Machine");
        result.Comments.Should().Be("Test Comments");
        result.Price.Should().Be(50.0);
        result.PayStatus.Should().Be(PaymentStatus.PENDING_PAYMENT);
    }

    [Test]
    public async Task FindByIdAsync_ExistingPedido_ReturnsPedido()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Comments = "Test Comments",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        // Act
        var result = await _repository.FindByIdAsync(savedPedido.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(savedPedido.Id);
        result.Machine.Should().Be("Test Machine");
    }

    [Test]
    public async Task FindByIdAsync_NonExistingPedido_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByIdAsync(Ulid.NewUlid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task UpdatePurchasedAsync_ExistingPedido_ReturnsUpdatedPedido()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Original Machine",
            Comments = "Original Comments",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);
        
        // Modify the pedido
        savedPedido.Machine = "Updated Machine";
        savedPedido.Comments = "Updated Comments";
        savedPedido.PayStatus = PaymentStatus.PAID;

        // Act
        var result = await _repository.UpdatePurchasedAsync(savedPedido, savedPedido.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Machine.Should().Be("Updated Machine");
        result.Comments.Should().Be("Updated Comments");
        result.PayStatus.Should().Be(PaymentStatus.PAID);
    }

    [Test]
    public async Task FindAllAsync_WithSearch_ReturnsMatchingResults()
    {
        // Arrange
        var tournamentId = Ulid.NewUlid();
        var pedido1 = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Alpha Machine",
            Comments = "Special comment",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var pedido2 = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Beta Machine",
            Comments = "Regular comment",
            Price = 60.0,
            PayStatus = PaymentStatus.PAID,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreatePurchasedAsync(pedido1);
        await _repository.CreatePurchasedAsync(pedido2);

        // Act
        var filter = new FilterPurchasedDto(
            false, false, null, tournamentId,
            "Alpha", // Search for "Alpha" in Machine or Comments
            0, 10,
            "id", "asc"
        );

        var (items, totalCount) = await _repository.FindAllAsync(filter);

        // Assert
        totalCount.Should().Be(1);
        items.First().Machine.Should().Be("Alpha Machine");
    }

    [Test]
    public async Task CancelPurchasedAsync_ExistingPedido_ReturnsCanceledPedido()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Comments = "Test Comments",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        // Act
        var result = await _repository.CancelPurchasedAsync(savedPedido.Id);

        // Assert
        result.Should().NotBeNull();
        result!.PayStatus.Should().Be(PaymentStatus.CANCELED);
        
        // Also check that lineas were canceled
        foreach (var linea in result.Lineas)
        {
            linea.Status.Should().Be(Status.CANCELED);
        }
    }

    [Test]
    public async Task FindAllAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var userId = Ulid.NewUlid();
        var tournamentId = Ulid.NewUlid();
        
        // Create test data
        var pedido1 = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = userId,
            AssignedTo = Ulid.NewUlid(),
            Machine = "Machine1",
            Comments = "Test comment 1",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
        
        var pedido2 = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = Ulid.NewUlid(), // Different player
            AssignedTo = userId, // Assigned to our user
            Machine = "Machine2",
            Comments = "Test comment 2",
            Price = 75.0,
            PayStatus = PaymentStatus.PAID,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        
        var pedido3 = new Pedidos
        {
            TournamentId = Ulid.NewUlid(), // Different tournament
            PlayerId = userId,
            AssignedTo = Ulid.NewUlid(),
            Machine = "Machine3",
            Comments = "Test comment 3",
            Price = 100.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreatePurchasedAsync(pedido1);
        await _repository.CreatePurchasedAsync(pedido2);
        await _repository.CreatePurchasedAsync(pedido3);

        // Act - Filter by player as user (IsUser = true, no tournament filter)
        var filterByUser = new FilterPurchasedDto(
            false, // IsEncorder
            true,  // IsUser
            userId.ToString(),
            null,  // No tournament filter to test IsUser independently
            string.Empty,  // Search
            0,     // Page
            10,    // Size
            "createdAt", // SortBy
            "desc" // Direction
        );

        var (itemsByUser, totalCountByUser) = await _repository.FindAllAsync(filterByUser);

        // Assert - Should get pedido1 and pedido3 (same user, same tournament)
        totalCountByUser.Should().Be(2);
        itemsByUser.Select(p => p.Id).Should().Contain(new[] { pedido1.Id, pedido3.Id });

        // Act - Filter by assignee (IsEncorder = true)
        var filterByAssignee = new FilterPurchasedDto(
            true,  // IsEncorder
            false, // IsUser
            userId.ToString(),
            tournamentId,
            string.Empty,  // Search
            0,     // Page
            10,    // Size
            "createdAt", // SortBy
            "desc" // Direction
        );

        var (itemsByAssignee, totalCountByAssignee) = await _repository.FindAllAsync(filterByAssignee);

        // Assert - Should get pedido2 (assigned to user)
        totalCountByAssignee.Should().Be(1);
        itemsByAssignee.First().Id.Should().Be(pedido2.Id);
    }

    [Test]
    public async Task ChangeStatusPurchasedAsync_ExistingPedidoPendingToPaid_UpdatesStatus()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Comments = "Test Comments",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        // Act
        var result = await _repository.ChangeStatusPurchasedAsync(savedPedido.Id, "PAID");

        // Assert
        result.Should().NotBeNull();
        result!.PayStatus.Should().Be(PaymentStatus.PAID);
    }

    [Test]
    public async Task ChangeStatusPurchasedAsync_NonExistingPedido_ReturnsNull()
    {
        // Act
        var result = await _repository.ChangeStatusPurchasedAsync(Ulid.NewUlid(), "PAID");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task ChangeStatusPurchasedAsync_InvalidStatusString_KeepsOriginalStatus()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Comments = "Test Comments",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        // Act
        var result = await _repository.ChangeStatusPurchasedAsync(savedPedido.Id, "INVALID_STATUS");

        // Assert - should keep original status
        result.Should().NotBeNull();
        result!.PayStatus.Should().Be(PaymentStatus.PENDING_PAYMENT);
    }

    [Test]
    public async Task FindLineaByIdAsync_ExistingLinea_ReturnsLineaWithPedido()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        var linea = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Wilson Pro Staff",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = true,
            Color = "Negro",
            Status = Status.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedLinea = await _repository.CreateLineaAsync(linea);

        // Act
        var result = await _repository.FindLineaByIdAsync(savedLinea.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(savedLinea.Id);
        result.RaquetModel.Should().Be("Wilson Pro Staff");
        result.Pedido.Should().NotBeNull();
        result.Pedido.Id.Should().Be(savedPedido.Id);
    }

    [Test]
    public async Task FindLineaByIdAsync_NonExistingLinea_ReturnsNull()
    {
        // Act
        var result = await _repository.FindLineaByIdAsync(Ulid.NewUlid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateLineaAsync_ValidLinea_ReturnsCreatedLinea()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        var linea = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Babolat Pure Drive",
            Nudos = 18,
            DateString = DateTime.UtcNow.AddDays(1),
            Logotype = false,
            Color = "Azul",
            Status = Status.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateLineaAsync(linea);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(default);
        result.PedidoId.Should().Be(savedPedido.Id);
        result.RaquetModel.Should().Be("Babolat Pure Drive");
        result.Nudos.Should().Be(18);
        result.Logotype.Should().BeFalse();
        result.Status.Should().Be(Status.PENDING);
    }

    [Test]
    public async Task UpdateLineaAsync_ExistingLinea_UpdatesFields()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        var linea = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Original Model",
            Nudos = 16,
            DateString = DateTime.UtcNow.AddDays(1),
            Logotype = false,
            Color = "Rojo",
            Status = Status.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedLinea = await _repository.CreateLineaAsync(linea);

        var updatedLinea = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Updated Model",
            Nudos = 18,
            DateString = DateTime.UtcNow.AddDays(5),
            Logotype = true,
            Color = "Negro",
            Status = Status.IN_PROGRESS,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.UpdateLineaAsync(updatedLinea, savedLinea.Id);

        // Assert
        result.Should().NotBeNull();
        result!.RaquetModel.Should().Be("Updated Model");
        result.Nudos.Should().Be(18);
        result.Logotype.Should().BeTrue();
        result.Color.Should().Be("Negro");
        result.Status.Should().Be(Status.IN_PROGRESS);
        result.Pedido.Should().NotBeNull();
        result.Pedido.Id.Should().Be(savedPedido.Id);
    }

    [Test]
    public async Task UpdateLineaAsync_NonExistingLinea_ReturnsNull()
    {
        // Arrange
        var linea = new PedidoLinea
        {
            PedidoId = Ulid.NewUlid(),
            RaquetModel = "Test Model",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = false,
            Color = "Blanco",
            Status = Status.PENDING,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.UpdateLineaAsync(linea, Ulid.NewUlid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task ChangeLineaStatusAsync_ExistingLinea_ChangesStatus()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        var linea = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Test Model",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = false,
            Color = "Verde",
            Status = Status.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedLinea = await _repository.CreateLineaAsync(linea);

        // Act
        var result = await _repository.ChangeLineaStatusAsync(savedLinea.Id, Status.COMPLETED);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(Status.COMPLETED);
        result.Pedido.Should().NotBeNull();
        result.Pedido.Id.Should().Be(savedPedido.Id);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_NonExistingLinea_ReturnsNull()
    {
        // Act
        var result = await _repository.ChangeLineaStatusAsync(Ulid.NewUlid(), Status.COMPLETED);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CancelPurchasedAsync_ExistingPedidoWithLineas_CancelsBoth()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        var linea1 = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Model A",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = false,
            Color = "Negro",
            Status = Status.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var linea2 = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "Model B",
            Nudos = 18,
            DateString = DateTime.UtcNow,
            Logotype = true,
            Color = "Rojo",
            Status = Status.IN_PROGRESS,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreateLineaAsync(linea1);
        await _repository.CreateLineaAsync(linea2);

        // Act
        var result = await _repository.CancelPurchasedAsync(savedPedido.Id);

        // Assert
        result.Should().NotBeNull();
        result!.PayStatus.Should().Be(PaymentStatus.CANCELED);
        result.Lineas.Should().HaveCount(2);
        result.Lineas.All(l => l.Status == Status.CANCELED).Should().BeTrue();
    }

    [Test]
    public async Task CancelPurchasedAsync_AlreadyCanceled_ReturnsNull()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Test Machine",
            Price = 50.0,
            PayStatus = PaymentStatus.CANCELED,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        // Act
        var result = await _repository.CancelPurchasedAsync(savedPedido.Id);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CancelPurchasedAsync_NonExistingPedido_ReturnsNull()
    {
        // Act
        var result = await _repository.CancelPurchasedAsync(Ulid.NewUlid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindAllAsync_WithSortByMachineAsc_ReturnsSortedResults()
    {
        // Arrange
        var tournamentId = Ulid.NewUlid();
        var pedidoA = new Pedidos { TournamentId = tournamentId, PlayerId = Ulid.NewUlid(), AssignedTo = Ulid.NewUlid(), Machine = "Alpha", Price = 10.0, PayStatus = PaymentStatus.PENDING_PAYMENT, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var pedidoB = new Pedidos { TournamentId = tournamentId, PlayerId = Ulid.NewUlid(), AssignedTo = Ulid.NewUlid(), Machine = "Beta", Price = 20.0, PayStatus = PaymentStatus.PAID, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        await _repository.CreatePurchasedAsync(pedidoA);
        await _repository.CreatePurchasedAsync(pedidoB);

        // Act
        var filter = new FilterPurchasedDto(false, false, null, tournamentId, string.Empty, 0, 10, "machine", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        // Assert
        items.Select(p => p.Machine).Should().ContainInOrder("Alpha", "Beta");
    }

    [Test]
    public async Task FindAllAsync_WithSortByEncorderDesc_ReturnsSortedResults()
    {
        // Arrange
        var tournamentId = Ulid.NewUlid();
        var userId1 = Ulid.NewUlid();
        var userId2 = Ulid.NewUlid();
        // Create with explicit ordering so desc reverses them
        var pedidoA = new Pedidos { TournamentId = tournamentId, PlayerId = Ulid.NewUlid(), AssignedTo = userId1, Machine = "M1", Price = 10.0, PayStatus = PaymentStatus.PENDING_PAYMENT, CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow };
        var pedidoB = new Pedidos { TournamentId = tournamentId, PlayerId = Ulid.NewUlid(), AssignedTo = userId2, Machine = "M2", Price = 20.0, PayStatus = PaymentStatus.PAID, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        await _repository.CreatePurchasedAsync(pedidoA);
        await _repository.CreatePurchasedAsync(pedidoB);

        // Act
        var filter = new FilterPurchasedDto(false, false, null, tournamentId, string.Empty, 0, 10, "encorder", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        // Assert - descending: larger Ulid first, then smaller
        items.Select(p => p.AssignedTo).Should().BeInDescendingOrder();
    }

    [Test]
    public async Task FindAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var tournamentId = Ulid.NewUlid();
        for (int i = 1; i <= 5; i++)
        {
            var pedido = new Pedidos
            {
                TournamentId = tournamentId,
                PlayerId = Ulid.NewUlid(),
                AssignedTo = Ulid.NewUlid(),
                Machine = $"Machine{i}",
                Price = i * 10.0,
                PayStatus = PaymentStatus.PENDING_PAYMENT,
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                UpdatedAt = DateTime.UtcNow
            };
            await _repository.CreatePurchasedAsync(pedido);
        }

        // Act - Get page 1 (index 1) with size 2
        var filter = new FilterPurchasedDto(false, false, null, tournamentId, string.Empty, 1, 2, "createdAt", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_WithSearchInLineaRaquetModel_ReturnsMatching()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Machine X",
            Comments = "Some comment",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedPedido = await _repository.CreatePurchasedAsync(pedido);

        var linea = new PedidoLinea
        {
            PedidoId = savedPedido.Id,
            RaquetModel = "UniqueSearchModel_X99",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = false,
            Color = "Negro",
            Status = Status.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.CreateLineaAsync(linea);

        // Act - Search by the linea's RaquetModel
        var filter = new FilterPurchasedDto(false, false, null, null, "UniqueSearchModel_X99", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        // Assert
        totalCount.Should().Be(1);
        items.First().Id.Should().Be(savedPedido.Id);
    }

    [Test]
    public async Task SaveChangesAsync_PersistsPendingChanges()
    {
        // Arrange
        var pedido = new Pedidos
        {
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Save Test",
            Comments = "Before save",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Pedidos.Add(pedido);

        // Act
        await _repository.SaveChangesAsync();

        // Assert
        var saved = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == pedido.Id);
        saved.Should().NotBeNull();
    }
}
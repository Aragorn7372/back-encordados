using BackEncordados.Common.Dto;
using FluentAssertions;

namespace TestEncordados.Unit.Common.Dto;

public class PageResponseDtoTests
{
    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_SetsAllProperties()
    {
        var content = new List<string> { "item1", "item2" };
        
        var response = new PageResponseDto<string>(
            Content: content,
            TotalPages: 5,
            TotalElements: 50,
            PageSize: 10,
            PageNumber: 2,
            TotalPageElements: 2,
            SortBy: "name",
            Direction: "asc"
        );

        response.Content.Should().BeSameAs(content);
        response.TotalPages.Should().Be(5);
        response.TotalElements.Should().Be(50);
        response.PageSize.Should().Be(10);
        response.PageNumber.Should().Be(2);
        response.TotalPageElements.Should().Be(2);
        response.SortBy.Should().Be("name");
        response.Direction.Should().Be("asc");
    }

    #endregion

    #region Empty Property Tests

    [Test]
    public void Empty_WhenContentIsEmpty_ReturnsTrue()
    {
        var response = new PageResponseDto<string>(
            Content: [],
            TotalPages: 0,
            TotalElements: 0,
            PageSize: 10,
            PageNumber: 0,
            TotalPageElements: 0,
            SortBy: "name",
            Direction: "asc"
        );

        response.Empty.Should().BeTrue();
    }

    [Test]
    public void Empty_WhenContentHasItems_ReturnsFalse()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 1,
            TotalElements: 1,
            PageSize: 10,
            PageNumber: 0,
            TotalPageElements: 1,
            SortBy: "name",
            Direction: "asc"
        );

        response.Empty.Should().BeFalse();
    }

    #endregion

    #region First Property Tests

    [Test]
    public void First_WhenFirstPage_ReturnsTrue()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 0,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.First.Should().BeTrue();
    }

    [Test]
    public void First_WhenNotFirstPage_ReturnsFalse()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 1,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.First.Should().BeFalse();
    }

    [Test]
    public void First_WhenLastPage_ReturnsFalse()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 2,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.First.Should().BeFalse();
    }

    #endregion

    #region Last Property Tests

    [Test]
    public void Last_WhenLastPage_ReturnsTrue()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 2,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.Last.Should().BeTrue();
    }

    [Test]
    public void Last_WhenNotLastPage_ReturnsFalse()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 1,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.Last.Should().BeFalse();
    }

    [Test]
    public void Last_WhenFirstPage_ReturnsFalse()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 0,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.Last.Should().BeFalse();
    }

    [Test]
    public void Last_WithSinglePage_ReturnsTrue()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 1,
            TotalElements: 1,
            PageSize: 10,
            PageNumber: 0,
            TotalPageElements: 1,
            SortBy: "name",
            Direction: "asc"
        );

        response.Last.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Properties_WithZeroTotalPages_HandlesCorrectly()
    {
        var response = new PageResponseDto<string>(
            Content: [],
            TotalPages: 0,
            TotalElements: 0,
            PageSize: 10,
            PageNumber: 0,
            TotalPageElements: 0,
            SortBy: "name",
            Direction: "asc"
        );

        response.Empty.Should().BeTrue();
        response.First.Should().BeTrue();
        response.Last.Should().BeTrue();
    }

    [Test]
    public void Last_WhenPageNumberEqualsTotalPagesMinusOne_ReturnsTrue()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 10,
            TotalElements: 100,
            PageSize: 10,
            PageNumber: 9,
            TotalPageElements: 10,
            SortBy: "name",
            Direction: "asc"
        );

        response.Last.Should().BeTrue();
    }

    [Test]
    public void Last_WhenPageNumberGreaterThanTotalPages_ReturnsTrue()
    {
        var response = new PageResponseDto<string>(
            Content: ["item1"],
            TotalPages: 3,
            TotalElements: 30,
            PageSize: 10,
            PageNumber: 10,
            TotalPageElements: 0,
            SortBy: "name",
            Direction: "asc"
        );

        response.Last.Should().BeTrue();
    }

    #endregion
}

public class ExportManifestDtoTests
{
    [Test]
    public void DefaultConstructor_SetsDefaultValues()
    {
        var manifest = new ExportManifestDto();

        manifest.Version.Should().Be("1.0");
        manifest.Description.Should().Be("Full database export");
        manifest.Entities.Should().NotBeNull();
        manifest.Entities.Should().BeEmpty();
    }

    [Test]
    public void ExportedAt_ShouldBeCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var manifest = new ExportManifestDto();
        var after = DateTime.UtcNow;

        manifest.ExportedAt.Should().BeOnOrAfter(before);
        manifest.ExportedAt.Should().BeOnOrBefore(after);
    }

    [Test]
    public void WithEntities_SetsEntitiesList()
    {
        var entities = new List<ExportEntityInfo>
        {
            new() { Name = "Users", RecordCount = 100, FileName = "users.json" },
            new() { Name = "Tournaments", RecordCount = 50, FileName = "tournaments.json" }
        };

        var manifest = new ExportManifestDto
        {
            Entities = entities
        };

        manifest.Entities.Should().HaveCount(2);
        manifest.Entities[0].Name.Should().Be("Users");
        manifest.Entities[1].Name.Should().Be("Tournaments");
    }

    [Test]
    public void AddEntity_AddsToEntitiesList()
    {
        var manifest = new ExportManifestDto();
        
        manifest.Entities.Add(new ExportEntityInfo
        {
            Name = "Users",
            RecordCount = 100,
            FileName = "users.json"
        });

        manifest.Entities.Should().HaveCount(1);
        manifest.Entities[0].RecordCount.Should().Be(100);
    }

    [Test]
    public void CanModifyAllProperties()
    {
        var manifest = new ExportManifestDto
        {
            Version = "2.0",
            ExportedAt = DateTime.Now,
            Description = "Partial export",
            Entities = [new ExportEntityInfo { Name = "Test", RecordCount = 1, FileName = "test.json" }]
        };

        manifest.Version.Should().Be("2.0");
        manifest.Description.Should().Be("Partial export");
        manifest.Entities.Should().HaveCount(1);
    }
}

public class ExportEntityInfoTests
{
    [Test]
    public void DefaultConstructor_SetsDefaults()
    {
        var entity = new ExportEntityInfo();

        entity.Name.Should().Be(string.Empty);
        entity.RecordCount.Should().Be(0);
        entity.FileName.Should().Be(string.Empty);
    }

    [Test]
    public void Constructor_SetsAllProperties()
    {
        var entity = new ExportEntityInfo
        {
            Name = "Users",
            RecordCount = 100,
            FileName = "users.json"
        };

        entity.Name.Should().Be("Users");
        entity.RecordCount.Should().Be(100);
        entity.FileName.Should().Be("users.json");
    }

    [Test]
    public void CanModifyProperties()
    {
        var entity = new ExportEntityInfo
        {
            Name = "Initial"
        };

        entity.Name = "Updated";
        entity.RecordCount = 50;
        entity.FileName = "updated.json";

        entity.Name.Should().Be("Updated");
        entity.RecordCount.Should().Be(50);
        entity.FileName.Should().Be("updated.json");
    }
}
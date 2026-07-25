using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class PagedRequestTests
{
    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Page_is_clamped_to_one_or_greater(int? page, int expected)
    {
        Assert.Equal(expected, new PagedRequest(page).Page);
    }

    [Theory]
    [InlineData(null, PagedRequest.DefaultPageSize)]
    [InlineData(0, PagedRequest.DefaultPageSize)]
    [InlineData(-1, PagedRequest.DefaultPageSize)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, PagedRequest.MaxPageSize)]
    [InlineData(10_000, PagedRequest.MaxPageSize)]
    public void PageSize_is_clamped_to_the_maximum(int? pageSize, int expected)
    {
        Assert.Equal(expected, new PagedRequest(pageSize: pageSize).PageSize);
    }

    [Fact]
    public void Offset_is_derived_from_the_clamped_values()
    {
        var request = new PagedRequest(page: 3, pageSize: 25);

        Assert.Equal(50, request.Offset);
    }

    [Theory]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("asc", false)]
    [InlineData(null, false)]
    [InlineData("sideways", false)]
    public void Sort_direction_defaults_to_ascending(string? direction, bool expected)
    {
        var request = new PagedRequest(sortDirection: direction);

        Assert.Equal(expected, request.IsDescending);
    }

    [Fact]
    public void Blank_search_and_sort_become_null()
    {
        var request = new PagedRequest(search: "   ", sortBy: "  ");

        Assert.Null(request.Search);
        Assert.Null(request.SortBy);
    }

    [Fact]
    public void TotalPages_rounds_up()
    {
        var result = new PagedResult<int>([1, 2, 3], page: 1, pageSize: 2, totalItems: 5);

        Assert.Equal(3, result.TotalPages);
    }
}

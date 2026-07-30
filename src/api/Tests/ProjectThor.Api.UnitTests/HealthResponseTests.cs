using ProjectThor.Api.Features.Health;

namespace ProjectThor.Api.UnitTests;

public class HealthResponseTests
{
    [Fact]
    public void HealthResponse_carries_the_status_it_was_created_with()
    {
        var response = new HealthResponse("healthy", DateTimeOffset.UtcNow, DatabaseReachable: true);

        Assert.Equal("healthy", response.Status);
    }
}

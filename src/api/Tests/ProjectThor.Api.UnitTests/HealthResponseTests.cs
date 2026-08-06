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

    [Fact]
    public void Deliberately_failing_test_to_verify_the_ci_gate()
    {
        Assert.Fail("Intentional failure for ci-cd task 7.7 verification; this PR will not be merged.");
    }
}

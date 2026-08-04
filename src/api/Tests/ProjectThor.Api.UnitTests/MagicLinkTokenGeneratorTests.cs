using ProjectThor.Api.Infrastructure.Auth;

namespace ProjectThor.Api.UnitTests;

public class MagicLinkTokenGeneratorTests
{
    [Fact]
    public void Generate_returns_a_hash_matching_the_raw_token()
    {
        var (rawToken, hash) = MagicLinkTokenGenerator.Generate();

        Assert.Equal(hash, MagicLinkTokenGenerator.Hash(rawToken));
    }

    [Fact]
    public void Generate_produces_distinct_tokens_each_call()
    {
        var (firstToken, _) = MagicLinkTokenGenerator.Generate();
        var (secondToken, _) = MagicLinkTokenGenerator.Generate();

        Assert.NotEqual(firstToken, secondToken);
    }

    [Fact]
    public void Hash_of_a_different_token_does_not_match()
    {
        var (_, hash) = MagicLinkTokenGenerator.Generate();

        Assert.NotEqual(hash, MagicLinkTokenGenerator.Hash("a-different-raw-token"));
    }
}

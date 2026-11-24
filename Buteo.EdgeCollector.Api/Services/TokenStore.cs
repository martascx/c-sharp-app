using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Buteo.EdgeCollector.Api.Services;

public class TokenStore
{
    private readonly ConcurrentDictionary<string, int> _tokens = new();

    public string Create(int clientId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _tokens[token] = clientId;
        return token;
    }

    public bool IsValid(string token)
    {
        return _tokens.ContainsKey(token);
    }
}

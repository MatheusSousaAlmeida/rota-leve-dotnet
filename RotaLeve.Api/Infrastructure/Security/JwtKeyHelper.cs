using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RotaLeve.Api.Infrastructure.Security;

public static class JwtKeyHelper
{
    public static SymmetricSecurityKey Create(string secret)
    {
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return new SymmetricSecurityKey(keyBytes);
    }
}

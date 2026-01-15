using System.Security.Cryptography;
using System.Text;

namespace Microondas.API.Security;

public static class HashHelper
{
    // Retorna HEX do SHA-256 (256 bits) — use para armazenar/verificar senha
    public static string Sha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
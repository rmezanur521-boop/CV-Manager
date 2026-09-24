namespace CVPlatform.Infrastructure.Services;

public static class FileContentSniffer
{
    private static readonly (string ContentType, string Extension, byte[] Signature, int Offset)[] Signatures =
    {
        ("image/jpeg", ".jpg", new byte[] { 0xFF, 0xD8, 0xFF }, 0),
        ("image/png", ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, 0),
        ("image/gif", ".gif", new byte[] { 0x47, 0x49, 0x46, 0x38 }, 0),
        ("image/webp", ".webp", new byte[] { 0x57, 0x45, 0x42, 0x50 }, 8)
    };

    public static (string ContentType, string Extension)? Detect(byte[] headerBytes)
    {
        foreach (var (contentType, extension, signature, offset) in Signatures)
        {
            if (headerBytes.Length < offset + signature.Length)
                continue;

            var matches = true;
            for (var i = 0; i < signature.Length; i++)
            {
                if (headerBytes[offset + i] != signature[i])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return (contentType, extension);
        }

        return null;
    }
}
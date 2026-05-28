using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace LibHaru.Internal;

internal sealed class PdfEncryption
{
    private const int IdLength = 16;
    private const int PasswordLength = 32;
    private const int MaxKeyLength = 16;
    private const int PermissionPad = unchecked((int)0xFFFFFFC0);

    private static readonly Encoding Latin1 = Encoding.Latin1;

    private static readonly byte[] PaddingString =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41,
        0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
        0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
    ];

    private byte[] _ownerPassword = PaddingString.ToArray();
    private byte[] _userPassword = PaddingString.ToArray();
    private byte[] _encryptionKey = new byte[MaxKeyLength + 5];
    private readonly HaruError _error;
    private bool _hasEncryptionKey;

    internal PdfEncryption(HaruError error)
    {
        _error = error;
        PermissionValue = PermissionPad
            | (int)Permission.EnablePrint
            | (int)Permission.EnableEditAll
            | (int)Permission.EnableCopy
            | (int)Permission.EnableEdit;
    }

    internal PdfEncryptMode Mode { get; private set; } = PdfEncryptMode.R2;

    internal int KeyLengthBytes { get; private set; } = 5;

    internal int PermissionValue { get; private set; }

    internal byte[] OwnerKey { get; private set; } = new byte[PasswordLength];

    internal byte[] UserKey { get; private set; } = new byte[PasswordLength];

    internal byte[] FileId { get; private set; } = new byte[IdLength];

    internal bool IsPrepared => _hasEncryptionKey;

    internal void SetPassword(string ownerPassword, string? userPassword)
    {
        if (string.IsNullOrEmpty(ownerPassword))
            throw CreateException(HaruStatus.EncryptInvalidPassword, "Owner password cannot be empty.");

        _ownerPassword = PadOrTruncatePassword(ownerPassword);
        _userPassword = PadOrTruncatePassword(userPassword);
        _hasEncryptionKey = false;
    }

    internal void SetPermission(uint permission)
    {
        PermissionValue = unchecked((int)permission);
        _hasEncryptionKey = false;
    }

    internal void SetMode(PdfEncryptMode mode, uint keyLength)
    {
        if (mode == PdfEncryptMode.R2)
        {
            Mode = mode;
            KeyLengthBytes = 5;
            _hasEncryptionKey = false;
            return;
        }

        if (mode != PdfEncryptMode.R3)
            throw CreateException(HaruStatus.InvalidParameter, "Unsupported encryption mode.");

        if (keyLength == 0)
            keyLength = 16;

        if (keyLength is < 5 or > 16)
            throw CreateException(HaruStatus.InvalidEncryptKeyLen, "Revision 3 encryption key length must be between 5 and 16 bytes.");

        Mode = mode;
        KeyLengthBytes = (int)keyLength;
        _hasEncryptionKey = false;
    }

    internal void Prepare(byte[] fileId)
    {
        if (fileId.Length != IdLength)
            throw CreateException(HaruStatus.InvalidEncryptKeyLen, "File identifier must be 16 bytes.");

        FileId = fileId.ToArray();
        OwnerKey = CreateOwnerKey();
        _encryptionKey = CreateEncryptionKey();
        _hasEncryptionKey = true;
        UserKey = CreateUserKey();
    }

    internal byte[] EncryptObjectData(int objectNumber, int generationNumber, ReadOnlySpan<byte> data)
    {
        return CryptObjectData(objectNumber, generationNumber, data);
    }

    internal byte[] DecryptObjectData(int objectNumber, int generationNumber, ReadOnlySpan<byte> data)
    {
        return CryptObjectData(objectNumber, generationNumber, data);
    }

    internal bool ValidateUserPassword(string? password)
    {
        return TryValidateUserPassword(PadOrTruncatePassword(password), out _);
    }

    internal bool ValidateOwnerPassword(string? password)
    {
        return TryValidateOwnerPassword(password, out _);
    }

    internal bool ValidatePassword(string? password)
    {
        return ValidateUserPassword(password) || ValidateOwnerPassword(password);
    }

    internal bool AuthenticateUserPassword(string? password)
    {
        if (!TryValidateUserPassword(PadOrTruncatePassword(password), out var encryptionKey))
            return false;

        _encryptionKey = encryptionKey;
        _hasEncryptionKey = true;
        return true;
    }

    internal bool AuthenticateOwnerPassword(string? password)
    {
        if (!TryValidateOwnerPassword(password, out var encryptionKey))
            return false;

        _encryptionKey = encryptionKey;
        _hasEncryptionKey = true;
        return true;
    }

    internal bool AuthenticatePassword(string? password)
    {
        return AuthenticateUserPassword(password) || AuthenticateOwnerPassword(password);
    }

    internal static bool ValidateDictionary(PdfDictionary? dictionary)
    {
        return dictionary is not null
            && dictionary.ObjectClass == (PdfObjectClass.Dictionary | PdfObjectClass.Encrypt);
    }

    internal static PdfEncryption FromPreparedStandardSecurity(
        HaruError error,
        PdfEncryptMode mode,
        int keyLengthBytes,
        int permissionValue,
        ReadOnlySpan<byte> ownerKey,
        ReadOnlySpan<byte> userKey,
        ReadOnlySpan<byte> fileId)
    {
        var encryption = new PdfEncryption(error);
        encryption.SetPreparedMode(mode, keyLengthBytes);
        encryption.PermissionValue = permissionValue;
        encryption.OwnerKey = CopyFixedLength(ownerKey, PasswordLength, error, "Owner key must be 32 bytes.");
        encryption.UserKey = CopyFixedLength(userKey, PasswordLength, error, "User key must be 32 bytes.");
        encryption.FileId = CopyFixedLength(fileId, IdLength, error, "File identifier must be 16 bytes.");
        encryption._hasEncryptionKey = false;
        Array.Clear(encryption._encryptionKey);
        return encryption;
    }

    internal static byte[] PadOrTruncatePasswordForValidation(string? password)
    {
        return PadOrTruncatePassword(password);
    }

    private byte[] CryptObjectData(int objectNumber, int generationNumber, ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return [];

        if (!_hasEncryptionKey)
            throw CreateException(HaruStatus.InvalidOperation, "Encryption key has not been prepared or authenticated.");

        Span<byte> objectSeed = stackalloc byte[KeyLengthBytes + 5];
        _encryptionKey.AsSpan(0, KeyLengthBytes).CopyTo(objectSeed);
        objectSeed[KeyLengthBytes] = (byte)objectNumber;
        objectSeed[KeyLengthBytes + 1] = (byte)(objectNumber >> 8);
        objectSeed[KeyLengthBytes + 2] = (byte)(objectNumber >> 16);
        objectSeed[KeyLengthBytes + 3] = (byte)generationNumber;
        objectSeed[KeyLengthBytes + 4] = (byte)(generationNumber >> 8);

        var objectDigest = MD5.HashData(objectSeed);
        var objectKeyLength = Math.Min(KeyLengthBytes + 5, MaxKeyLength);
        return Rc4.Crypt(objectDigest.AsSpan(0, objectKeyLength), data);
    }

    internal static byte[] CreateFileId(IReadOnlyDictionary<PdfInfoType, string> infoValues, int objectCount, HaruError error)
    {
        using var md5 = MD5.Create();

        Span<byte> ticks = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(ticks, DateTimeOffset.UtcNow.UtcTicks);
        md5.TransformBlock(ticks.ToArray(), 0, ticks.Length, null, 0);

        foreach (var type in new[]
                 {
                     PdfInfoType.Author,
                     PdfInfoType.Creator,
                     PdfInfoType.Producer,
                     PdfInfoType.Title,
                     PdfInfoType.Subject,
                     PdfInfoType.Keywords
                 })
        {
            if (!infoValues.TryGetValue(type, out var value) || value.Length == 0)
                continue;

            var bytes = Latin1.GetBytes(value);
            md5.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        Span<byte> count = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(count, objectCount);
        md5.TransformFinalBlock(count.ToArray(), 0, count.Length);
        if (md5.Hash is not null)
            return md5.Hash;

        error.RaiseError(HaruStatus.InvalidOperation);
        throw new HaruException(HaruStatus.InvalidOperation, "MD5 did not produce a file identifier.");
    }

    private byte[] CreateOwnerKey()
    {
        var digest = CreateOwnerPasswordDigest(_ownerPassword);

        var ownerKey = Rc4.Crypt(digest.AsSpan(0, KeyLengthBytes), _userPassword);

        if (Mode == PdfEncryptMode.R3)
        {
            for (var i = 1; i <= 19; i++)
            {
                var newKey = new byte[KeyLengthBytes];

                for (var j = 0; j < KeyLengthBytes; j++)
                    newKey[j] = (byte)(digest[j] ^ i);

                ownerKey = Rc4.Crypt(newKey, ownerKey);
            }
        }

        return ownerKey;
    }

    private byte[] CreateEncryptionKey()
    {
        return CreateEncryptionKey(_userPassword);
    }

    private byte[] CreateEncryptionKey(ReadOnlySpan<byte> paddedUserPassword)
    {
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        md5.AppendData(paddedUserPassword);
        md5.AppendData(OwnerKey);

        Span<byte> permissions = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(permissions, PermissionValue);
        md5.AppendData(permissions);
        md5.AppendData(FileId);

        var digest = md5.GetHashAndReset();

        if (Mode == PdfEncryptMode.R3)
        {
            for (var i = 0; i < 50; i++)
                digest = MD5.HashData(digest.AsSpan(0, KeyLengthBytes));
        }

        var key = new byte[MaxKeyLength + 5];
        digest.AsSpan(0, Math.Min(digest.Length, MaxKeyLength)).CopyTo(key);
        return key;
    }

    private byte[] CreateUserKey()
    {
        return CreateUserKey(_encryptionKey);
    }

    private byte[] CreateUserKey(ReadOnlySpan<byte> encryptionKey)
    {
        var userKey = Rc4.Crypt(encryptionKey[..KeyLengthBytes], PaddingString);

        if (Mode != PdfEncryptMode.R3)
            return userKey;

        using var md5 = MD5.Create();
        md5.TransformBlock(PaddingString, 0, PaddingString.Length, null, 0);
        md5.TransformFinalBlock(FileId, 0, FileId.Length);

        var digest = md5.Hash ?? throw CreateException(HaruStatus.InvalidOperation, "MD5 did not produce a user key digest.");
        var digest2 = Rc4.Crypt(encryptionKey[..KeyLengthBytes], digest);

        for (var i = 1; i <= 19; i++)
        {
            var newKey = new byte[KeyLengthBytes];

            for (var j = 0; j < KeyLengthBytes; j++)
                newKey[j] = (byte)(encryptionKey[j] ^ i);

            digest2 = Rc4.Crypt(newKey, digest2);
        }

        userKey = new byte[PasswordLength];
        digest2.AsSpan(0, 16).CopyTo(userKey);
        return userKey;
    }

    private bool TryValidateUserPassword(ReadOnlySpan<byte> paddedUserPassword, out byte[] encryptionKey)
    {
        encryptionKey = CreateEncryptionKey(paddedUserPassword);
        var candidateUserKey = CreateUserKey(encryptionKey);
        var compareLength = Mode == PdfEncryptMode.R3 ? IdLength : PasswordLength;

        return CryptographicOperations.FixedTimeEquals(
            candidateUserKey.AsSpan(0, compareLength),
            UserKey.AsSpan(0, compareLength));
    }

    private bool TryValidateOwnerPassword(string? password, out byte[] encryptionKey)
    {
        var paddedOwnerPassword = PadOrTruncatePassword(password);
        var ownerDigest = CreateOwnerPasswordDigest(paddedOwnerPassword);
        var paddedUserPassword = OwnerKey.ToArray();

        if (Mode == PdfEncryptMode.R3)
        {
            for (var i = 19; i >= 1; i--)
                paddedUserPassword = Rc4.Crypt(CreateXorKey(ownerDigest, KeyLengthBytes, i), paddedUserPassword);
        }

        paddedUserPassword = Rc4.Crypt(ownerDigest.AsSpan(0, KeyLengthBytes), paddedUserPassword);
        return TryValidateUserPassword(paddedUserPassword, out encryptionKey);
    }

    private byte[] CreateOwnerPasswordDigest(ReadOnlySpan<byte> paddedOwnerPassword)
    {
        var digest = MD5.HashData(paddedOwnerPassword);

        if (Mode == PdfEncryptMode.R3)
        {
            for (var i = 0; i < 50; i++)
                digest = MD5.HashData(digest.AsSpan(0, KeyLengthBytes));
        }

        return digest;
    }

    private void SetPreparedMode(PdfEncryptMode mode, int keyLengthBytes)
    {
        if (mode == PdfEncryptMode.R2)
        {
            if (keyLengthBytes != 5)
                throw CreateException(HaruStatus.InvalidEncryptKeyLen, "Revision 2 encryption key length must be 5 bytes.");

            Mode = mode;
            KeyLengthBytes = 5;
            return;
        }

        if (mode != PdfEncryptMode.R3)
            throw CreateException(HaruStatus.InvalidParameter, "Unsupported encryption mode.");

        if (keyLengthBytes is < 5 or > 16)
            throw CreateException(HaruStatus.InvalidEncryptKeyLen, "Revision 3 encryption key length must be between 5 and 16 bytes.");

        Mode = mode;
        KeyLengthBytes = keyLengthBytes;
    }

    private static byte[] CreateXorKey(ReadOnlySpan<byte> key, int keyLength, int xorValue)
    {
        var newKey = new byte[keyLength];

        for (var i = 0; i < newKey.Length; i++)
            newKey[i] = (byte)(key[i] ^ xorValue);

        return newKey;
    }

    private static byte[] CopyFixedLength(ReadOnlySpan<byte> value, int length, HaruError error, string message)
    {
        if (value.Length != length)
        {
            error.RaiseError(HaruStatus.InvalidEncryptKeyLen);
            throw new HaruException(HaruStatus.InvalidEncryptKeyLen, message);
        }

        return value.ToArray();
    }

    private static byte[] PadOrTruncatePassword(string? password)
    {
        password ??= string.Empty;
        var bytes = Latin1.GetBytes(password);
        var result = new byte[PasswordLength];
        var copyLength = Math.Min(bytes.Length, PasswordLength);

        bytes.AsSpan(0, copyLength).CopyTo(result);

        if (copyLength < PasswordLength)
            PaddingString.AsSpan(0, PasswordLength - copyLength).CopyTo(result.AsSpan(copyLength));

        return result;
    }

    private HaruException CreateException(uint status, string message, uint detail = HaruStatus.NoError)
    {
        _error.RaiseError(status, detail);
        return new HaruException(status, detail, message);
    }
}

internal static class Rc4
{
    internal static byte[] Crypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        if (key.Length == 0)
            throw new HaruException(HaruStatus.InvalidEncryptKeyLen, "RC4 key cannot be empty.");

        Span<byte> state = stackalloc byte[256];
        Span<byte> tempKey = stackalloc byte[256];

        for (var i = 0; i < 256; i++)
        {
            state[i] = (byte)i;
            tempKey[i] = key[i % key.Length];
        }

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + state[i] + tempKey[i]) & 0xFF;
            (state[i], state[j]) = (state[j], state[i]);
        }

        var output = new byte[data.Length];
        var idx1 = 0;
        var idx2 = 0;

        for (var i = 0; i < data.Length; i++)
        {
            idx1 = (idx1 + 1) & 0xFF;
            idx2 = (idx2 + state[idx1]) & 0xFF;
            (state[idx1], state[idx2]) = (state[idx2], state[idx1]);

            var t = (state[idx1] + state[idx2]) & 0xFF;
            output[i] = (byte)(data[i] ^ state[t]);
        }

        return output;
    }
}

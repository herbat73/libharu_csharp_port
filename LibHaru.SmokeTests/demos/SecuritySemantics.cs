using System.Text;
using LibHaru;
using LibHaru.Internal;
using static LibHaru.HPdf;

public static class SecuritySemantics
{
    public static void Test()
    {
        EncryptionDictionaryValidationMatchesObjectSubclass();
        StandardSecurityRevision2AuthenticatesAndDecrypts();
        StandardSecurityRevision3AuthenticatesAndDecrypts();
        EncryptOffReplacesSecurityDictionaryWithNull();

        Console.WriteLine("Security semantics smoke passed");
    }

    private static void EncryptionDictionaryValidationMatchesObjectSubclass()
    {
        Require(!PdfEncryption.ValidateDictionary(null), "Null encryption dictionary validated.");

        var plainDictionary = new PdfDictionary();
        Require(!PdfEncryption.ValidateDictionary(plainDictionary), "Plain dictionary validated as an encryption dictionary.");

        var encryptionDictionary = new PdfDictionary { IsEncryptionDictionary = true };
        Require(PdfEncryption.ValidateDictionary(encryptionDictionary), "Encryption dictionary subclass did not validate.");
    }

    private static void StandardSecurityRevision2AuthenticatesAndDecrypts()
    {
        ExerciseStandardSecurity(PdfEncryptMode.R2, 5, "owner", "user");
    }

    private static void StandardSecurityRevision3AuthenticatesAndDecrypts()
    {
        ExerciseStandardSecurity(PdfEncryptMode.R3, 16, "owner", string.Empty, static encryption =>
        {
            encryption.SetPermission((uint)Permission.EnableRead);
            encryption.SetMode(PdfEncryptMode.R3, 16);
        });
    }

    private static void ExerciseStandardSecurity(
        PdfEncryptMode mode,
        int keyLengthBytes,
        string ownerPassword,
        string userPassword,
        Action<PdfEncryption>? configure = null)
    {
        var error = new HaruError();
        var encryption = new PdfEncryption(error);
        encryption.SetPassword(ownerPassword, userPassword);
        configure?.Invoke(encryption);
        encryption.Prepare(Enumerable.Range(0, 16).Select(static i => (byte)(0xA0 + i)).ToArray());

        Require(encryption.IsPrepared, $"{mode} encryption was not marked prepared.");
        Require(encryption.ValidateUserPassword(userPassword), $"{mode} user password did not validate.");
        Require(encryption.ValidateOwnerPassword(ownerPassword), $"{mode} owner password did not validate.");
        Require(encryption.ValidatePassword(userPassword), $"{mode} combined password validation rejected the user password.");
        Require(encryption.ValidatePassword(ownerPassword), $"{mode} combined password validation rejected the owner password.");
        Require(!encryption.ValidateUserPassword("wrong"), $"{mode} accepted an invalid user password.");
        Require(!encryption.ValidateOwnerPassword("wrong"), $"{mode} accepted an invalid owner password.");

        var plain = Encoding.Latin1.GetBytes($"secret payload for {mode}");
        var encrypted = encryption.EncryptObjectData(12, 0, plain);
        Require(!encrypted.SequenceEqual(plain), $"{mode} encrypted object data matched plaintext.");
        Require(encryption.DecryptObjectData(12, 0, encrypted).SequenceEqual(plain), $"{mode} direct decrypt did not round trip.");

        var readWithUserPassword = CreatePreparedCopy(error, encryption, mode, keyLengthBytes);
        Require(!readWithUserPassword.IsPrepared, $"{mode} prepared copy should require authentication before decrypting.");
        var unauthenticatedError = RequireThrows(() => readWithUserPassword.DecryptObjectData(12, 0, encrypted));
        Require(unauthenticatedError.Status == HaruStatus.InvalidOperation, $"{mode} unauthenticated decrypt raised the wrong status.");
        Require(readWithUserPassword.ValidateUserPassword(userPassword), $"{mode} prepared copy rejected the user password.");
        Require(!readWithUserPassword.AuthenticateUserPassword("wrong"), $"{mode} authenticated an invalid user password.");
        Require(readWithUserPassword.AuthenticateUserPassword(userPassword), $"{mode} did not authenticate the user password.");
        Require(
            readWithUserPassword.DecryptObjectData(12, 0, encrypted).SequenceEqual(plain),
            $"{mode} user-authenticated decrypt did not recover plaintext.");

        var readWithOwnerPassword = CreatePreparedCopy(error, encryption, mode, keyLengthBytes);
        Require(readWithOwnerPassword.ValidateOwnerPassword(ownerPassword), $"{mode} prepared copy rejected the owner password.");
        Require(readWithOwnerPassword.AuthenticateOwnerPassword(ownerPassword), $"{mode} did not authenticate the owner password.");
        Require(
            readWithOwnerPassword.DecryptObjectData(12, 0, encrypted).SequenceEqual(plain),
            $"{mode} owner-authenticated decrypt did not recover plaintext.");
    }

    private static PdfEncryption CreatePreparedCopy(HaruError error, PdfEncryption encryption, PdfEncryptMode mode, int keyLengthBytes) =>
        PdfEncryption.FromPreparedStandardSecurity(
            error,
            mode,
            keyLengthBytes,
            encryption.PermissionValue,
            encryption.OwnerKey,
            encryption.UserKey,
            encryption.FileId);

    private static void EncryptOffReplacesSecurityDictionaryWithNull()
    {
        using var pdf = HPDF_New();
        HPDF_SetPassword(pdf, "owner", "user");
        pdf.SetEncryptOff();

        var latin1 = Encoding.Latin1.GetString(pdf.SaveToStream());
        Require(!latin1.Contains("/Encrypt", StringComparison.Ordinal), "Encrypt-off document still had an Encrypt trailer entry.");
        Require(latin1.Contains("null\nendobj", StringComparison.Ordinal), "Encrypt-off did not replace the security dictionary object with null.");
    }

    private static HaruException RequireThrows(Action action)
    {
        try
        {
            action();
        }
        catch (HaruException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected a HaruException.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class CertificateLoaderEncryptionTest
{
    [Theory]
    [InlineData("development.dummy_ecdh_p384-2021June.pfx", false, true)]
    [InlineData("development.dummy_ecdh_p384-2021June_nopass.pfx", false, false)]
    [InlineData("development.dummy_ecdh_p521-2021June.pfx", false, true)]
    [InlineData("development.dummy_ecdh_p521-2021June_nopass.pfx", false, false)]
    [InlineData("development.dummy-rsa-2071June.pfx", false, true)]
    [InlineData("development.dummy-rsa-2071June_nopass.pfx", false, false)]
    [InlineData("development.dummy-rsa4096-2071June.pfx", false, true)]
    [InlineData("development.dummy-rsa4096-2071June_nopass.pfx", false, false)]
#if NET6_0_OR_GREATER
    [InlineData("development.dummy_ecdh_p384-2021June.pem", true, true)]
    [InlineData("development.dummy_ecdh_p384-2021June_noenc.pem", true, false)]
    [InlineData("development.dummy_ecdh_p521-2021June.pem", true, true)]
    [InlineData("development.dummy_ecdh_p521-2021June_noenc.pem", true, false)]
    [InlineData("development.dummy-rsa-2071June.pem", true, true)]
    [InlineData("development.dummy-rsa-2071June_noenc.pem", true, false)]
    [InlineData("development.dummy-rsa4096-2071June.pem", true, true)]
    [InlineData("development.dummy-rsa4096-2071June_noenc.pem", true, false)]
#endif
    public void Pkcs12EncryptionSigningTest(string filename, bool isPem, bool requirePass)
    {
        // Cannot use empty password on MacOS with netcoreapp3.1 and before, only supported custom loader since net5.0
        // https://github.com/dotnet/runtime/issues/23635#issuecomment-334028941
        var filePath = Path.GetFullPath(Path.Combine("Properties", filename));
        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
            KeyPath = isPem ? filePath : null,
            IsPem = isPem,
            Password = requirePass ? filename : null,
        };
        var result = CertificateLoader.LoadCertificate(certificateConfig, "Test", requiredPrivateKey: true);

        Assert.NotNull(result);
        ValidatePrivateKey(result);
    }

    //#if NET6_0_OR_GREATER
    //    [Theory]
    //    [InlineData("development.dummy_ecdh_p384-2021June.pem", "development.dummy_ecdh_p384-2021June.pem")]
    //    [InlineData("development.dummy_ecdh_p384-2021June_noenc.pem", null)]
    //    public void PemEncryptionSigningTest(string filename, string? passphrase)
    //    {
    //        // Cannot use empty password on MacOS with netcoreapp3.1 and before, only supported custom loader since net5.0
    //        // https://github.com/dotnet/runtime/issues/23635#issuecomment-334028941
    //        var filePath = Path.GetFullPath(Path.Combine("Properties", filename));
    //        var certificateConfig = new CertificateConfig { Path = filePath, Password = passphrase, IsPem = true };
    //        var result = CertificateLoader.LoadCertificate(certificateConfig, "Test", requiredPrivateKey: true);

    //        Assert.NotNull(result);
    //        ValidatePrivateKey(result);
    //    }
    //#endif

    private static void ValidatePrivateKey(X509Certificate2 certificate)
    {
        var rsa = certificate.GetRSAPrivateKey();
        var ecdsa = certificate.GetECDsaPrivateKey();
        if (rsa != null)
        {
            RsaEncryptionTest(rsa);
        }
        else if (ecdsa != null)
        {
            ECDsaSigningTest(ecdsa);
        }
        else
        {
            Assert.Fail("Only support rsa or ecdsa");
        }
    }

    private static void RsaEncryptionTest(RSA rsa)
    {
        const string plainText = "Hello, world! こんにちは世界 ഹലോ വേൾഡ് Kαληµε´ρα κο´σµε";

        var cipher = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA512));
        var encBytes = Convert.FromBase64String(cipher);
        var decBytes = rsa.Decrypt(encBytes, RSAEncryptionPadding.OaepSHA512);
        var text = Encoding.UTF8.GetString(decBytes);

        Assert.Equal(plainText, text);
    }

    private static void ECDsaSigningTest(ECDsa ecdsa)
    {
        const string plainText = "Hello, world! こんにちは世界 ഹലോ വേൾഡ് Kαληµε´ρα κο´σµε";

        var message = Encoding.UTF8.GetBytes(plainText);
        var signature = ecdsa.SignData(message, HashAlgorithmName.SHA256);
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
        var result = ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256);

        Assert.True(result);
    }
}

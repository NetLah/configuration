using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class CertificateLoaderTest
{
    [Theory]
    [InlineData("development.dummy_ecdh_p384-2021June.cer")]
    [InlineData("development.dummy_ecdh_p384-2021June.crt")]
    [InlineData("development.dummy_ecdh_p521-2021June.cer")]
    [InlineData("development.dummy_ecdh_p521-2021June.crt")]
    [InlineData("development.dummy-rsa-2071June.cer")]
    [InlineData("development.dummy-rsa-2071June.crt")]
    [InlineData("development.dummy-rsa4096-2071June.cer")]
    [InlineData("development.dummy-rsa4096-2071June.crt")]
    //[InlineData("")]
    public void Pkcs12EncryptionSigningTest(string filename)
    {
        var filePath = Path.GetFullPath(Path.Combine("Properties", filename));
        var certificateConfig = new CertificateConfig { Path = filePath };

        var result = CertificateLoader.LoadCertificate(certificateConfig, "Test", requiredPrivateKey: false);

        Assert.NotNull(result);
        Assert.NotNull(result.Subject);
        Assert.NotNull(result.SubjectName);
    }
}

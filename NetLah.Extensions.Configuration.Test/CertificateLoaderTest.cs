using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace NetLah.Extensions.Configuration.Test
{
    public class CertificateLoaderTest
    {
        [Theory]
        [InlineData("development.dummy_ecdsa_p384-2021June.pfx")]
        [InlineData("development.dummy_ecdsa_p521-2021June.pfx")]
        [InlineData("development.dummy-rsa-2071June.pfx")]
        [InlineData("development.dummy-rsa4096-2071June.pfx")]
        public void ConfigurationConvertTest(string filename)
        {
            var filePath = Path.GetFullPath(Path.Combine("Properties", filename));
            var certConfig = new CertificateConfig { Path = filePath };
            var result = CertificateLoader.LoadCertificate(certConfig, "Test", requiredPrivateKey: true);

            Assert.NotNull(result);

            ValidatePrivateKey(result);
        }

        private static void ValidatePrivateKey(X509Certificate2 cert)
        {
            var rsa = cert.GetRSAPrivateKey();
            var ecdsa = cert.GetECDsaPrivateKey();
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
                Assert.True(false);
            }
        }

        private static void RsaEncryptionTest(RSA rsa)
        {
            const string plainText = "Hello, world! こんにちは世界 ഹലോ വേൾഡ് Kαληµε´ρα κο´σµε";

            var cipher = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.Pkcs1));
            var encBytes = Convert.FromBase64String(cipher);
            var decBytes = rsa.Decrypt(encBytes, RSAEncryptionPadding.Pkcs1);
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
}

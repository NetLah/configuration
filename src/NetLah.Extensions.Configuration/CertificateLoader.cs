using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace NetLah.Extensions.Configuration;

// reference: https://github.com/dotnet/aspnetcore/blob/master/src/Servers/Kestrel/Core/src/CertificateLoader.cs
public static class CertificateLoader
{
    // See http://oid-info.com/get/1.3.6.1.5.5.7.3.1
    // Indicates that a certificate can be used as a SSL server certificate
    public const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    public const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    public static X509Certificate2? LoadCertificate(CertificateConfig? certInfo, string? description, string? oid = null, bool requiredPrivateKey = true)
    {
        if (certInfo == null
            || (certInfo.IsFileCert && certInfo.IsStoreSubject)
            || (certInfo.IsFileCert && certInfo.IsStoreThumbprint)
            || (certInfo.IsStoreThumbprint && certInfo.IsStoreSubject))
        {
            throw new InvalidOperationException($"The source {description} specified multiple certificate sources");
        }

        if (!string.IsNullOrEmpty(certInfo.Path))   // certInfo.IsFileCert
        {
            var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            var keyStorageFlag = !requiredPrivateKey || isMacOs
                ? X509KeyStorageFlags.DefaultKeySet
                : certInfo.KeyStorageFlags is { } keyStorageFlags
                ? (X509KeyStorageFlags)((int)keyStorageFlags & 63)
                : X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;

            X509Certificate2? cert = null;

#if NET9_0_OR_GREATER
            cert = requiredPrivateKey
                ? (certInfo.IsPem
                    ? (string.IsNullOrEmpty(certInfo.Password)
                        ? X509Certificate2.CreateFromPemFile(certInfo.Path, certInfo.KeyPath ?? certInfo.Path)
                        : X509Certificate2.CreateFromEncryptedPemFile(certInfo.Path, certInfo.Password, certInfo.KeyPath ?? certInfo.Path)
                    )
                    : X509CertificateLoader.LoadPkcs12FromFile(certInfo.Path, certInfo.Password, keyStorageFlag)
                )
                : X509CertificateLoader.LoadCertificateFromFile(certInfo.Path);
#else

#if NET6_0_OR_GREATER
            // .NET 5.0 supports X509Certificate2.CreateFromPemFile
            cert = certInfo.IsPem
                ? (requiredPrivateKey
                    ? (string.IsNullOrEmpty(certInfo.Password)
                        ? X509Certificate2.CreateFromPemFile(certInfo.Path, certInfo.KeyPath ?? certInfo.Path)
                        : X509Certificate2.CreateFromEncryptedPemFile(certInfo.Path, certInfo.Password, certInfo.KeyPath ?? certInfo.Path)
                    )
                    : X509Certificate2.CreateFromPemFile(certInfo.Path)
                )
                : new X509Certificate2(certInfo.Path, certInfo.Password, keyStorageFlag);
#else
            cert = new X509Certificate2(certInfo.Path, certInfo.Password, keyStorageFlag);
#endif

            if (requiredPrivateKey && !cert.HasPrivateKey)
            {
                cert.Dispose();
                throw new InvalidOperationException("The certificate doesn't have private key");
            }
#endif

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!cert.HasPrivateKey || !isWindows || !certInfo.Reimport)
            {
                return cert;
            }

#if NET9_0_OR_GREATER
            var result = X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), null);
            cert.Dispose();
#else
            var result = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
            cert.Dispose();
#endif
            return result;
        }

        return certInfo.IsStoreThumbprint || certInfo.IsStoreSubject ? LoadFromStoreCert(certInfo, oid, requiredPrivateKey) : null;
    }

    private static X509Certificate2? LoadFromStoreCert(CertificateConfig certInfo, string? oid, bool requiredPrivateKey)
    {
        var (findType, findValue) = certInfo.IsStoreThumbprint ? (X509FindType.FindByThumbprint, certInfo.Thumbprint) : (X509FindType.FindBySubjectName, certInfo.Subject);
        if (string.IsNullOrEmpty(findValue))
        {
            return null;
        }

        var storeName = string.IsNullOrEmpty(certInfo.Store) ? StoreName.My.ToString() : certInfo.Store;
        var location = certInfo.Location;
        var storeLocation = StoreLocation.CurrentUser;
        if (!string.IsNullOrEmpty(location))
        {
            storeLocation = Enum.Parse<StoreLocation>(location, ignoreCase: true);
        }
        var allowInvalid = certInfo.AllowInvalid ?? true;   // default is allow invalid

        return LoadFromStoreCert(findType, findValue, allowInvalid, storeName, storeLocation, oid, requiredPrivateKey);
    }

    private static X509Certificate2 LoadFromStoreCert(X509FindType findType, string findValue, bool allowInvalid, string storeName,
        StoreLocation storeLocation, string? oid, bool requiredPrivateKey)
    {
        using var store = new X509Store(storeName, storeLocation);
        X509Certificate2Collection? storeCertificates = null;
        X509Certificate2? foundCertificate = null;
        var filterByPrivateKey = requiredPrivateKey ? (Func<X509Certificate2, bool>)(certificate => certificate.HasPrivateKey) : _ => true;
        var filterByOid = string.IsNullOrEmpty(oid) ? (Func<X509Certificate2, bool>)(_ => true) : cert => IsCertificateAllowedFor(cert, oid);

        try
        {
            store.Open(OpenFlags.ReadOnly);
            storeCertificates = store.Certificates;
            var foundCertificates = storeCertificates.Find(findType, findValue, !allowInvalid);
            foundCertificate = foundCertificates
                .OfType<X509Certificate2>()
                .Where(filterByOid)
                .Where(filterByPrivateKey)
                .OrderByDescending(certificate => certificate.NotAfter)
                .FirstOrDefault();

            return foundCertificate ?? throw new InvalidOperationException($"The requested certificate {findValue} could not be found in {storeLocation}/{storeName} with AllowInvalid:{allowInvalid} and oid:{oid}.");
        }
        finally
        {
            DisposeCertificates(storeCertificates, except: foundCertificate);
        }
    }

    internal static bool IsCertificateAllowedFor(X509Certificate2 certificate, string expectedOid)
    {
        /* If the Extended Key Usage extension is included, then we check that the serverAuth usage is included. (http://oid-info.com/get/1.3.6.1.5.5.7.3.1)
         * If the Extended Key Usage extension is not included, then we assume the certificate is allowed for all usages.
         *
         * See also https://blogs.msdn.microsoft.com/kaushal/2012/02/17/client-certificates-vs-server-certificates/
         *
         * From https://tools.ietf.org/html/rfc3280#section-4.2.1.13 "Certificate Extensions: Extended Key Usage"
         *
         * If the (Extended Key Usage) extension is present, then the certificate MUST only be used
         * for one of the purposes indicated.  If multiple purposes are
         * indicated the application need not recognize all purposes indicated,
         * as long as the intended purpose is present.  Certificate using
         * applications MAY require that a particular purpose be indicated in
         * order for the certificate to be acceptable to that application.
         */

        var hasEkuExtension = false;

        foreach (var extension in certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>())
        {
            hasEkuExtension = true;
            foreach (var oid in extension.EnhancedKeyUsages)
            {
                if (string.Equals(oid.Value, expectedOid, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return !hasEkuExtension;
    }

    private static void DisposeCertificates(X509Certificate2Collection? certificates, X509Certificate2? except)
    {
        if (certificates != null)
        {
            foreach (var certificate in certificates)
            {
                if (!certificate.Equals(except))
                {
                    certificate.Dispose();
                }
            }
        }
    }
}

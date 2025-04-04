﻿using System.Security.Cryptography.X509Certificates;

namespace NetLah.Extensions.Configuration;

//#pragma warning disable S125 // Sections of code should not be commented out
// "Certificate": {
//      "Path": "testCert.pfx"
//      "Password": "testPassword"
// }
//#pragma warning restore S125 // Sections of code should not be commented out

public class CertificateConfig
{
    // File
    public bool IsFileCert => !string.IsNullOrEmpty(Path);

    public string? Path { get; set; }

    public string? KeyPath { get; set; }

    public string? Password { get; set; }

    public X509KeyStorageFlags? KeyStorageFlags { get; set; }

    public bool Reimport { get; set; } = true;

    public bool IsPem { get; set; }

    // Cert/Thumbprint on store

    public bool IsStoreThumbprint => !string.IsNullOrEmpty(Thumbprint);

    public string? Thumbprint { get; set; }

    // Cert store

    public bool IsStoreSubject => !string.IsNullOrEmpty(Subject);

    public string? Subject { get; set; }

    public string? Store { get; set; }

    public string? Location { get; set; }

    public bool? AllowInvalid { get; set; }

    public override string? ToString()
    {
        return IsFileCert ? Path : IsStoreThumbprint ? Thumbprint : Subject;
    }
}

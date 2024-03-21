using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace NetLah.Extensions.Configuration.Test;

public class CertificateLoaderMutualTlsTest
{
    [Theory]
    [InlineData("development.dummy_ecdsa_p384-2021June.pfx")]
    // [InlineData("development.dummy_ecdsa_p521-2021June.pfx")]
    [InlineData("development.dummy-rsa-2071June.pfx")]
    [InlineData("development.dummy-rsa4096-2071June.pfx")]
    public void Pkcs12AuthenticationTest(string filename)
    {
        // Cannot use empty password on MacOS with netcoreapp3.1 and before, only supported custom loader since net5.0
        // https://github.com/dotnet/runtime/issues/23635#issuecomment-334028941
        var filePath = Path.GetFullPath(Path.Combine("Properties", filename));
        var certificateConfig = new CertificateConfig { Path = filePath, Password = filename };
        var result = CertificateLoader.LoadCertificate(certificateConfig, "Test", requiredPrivateKey: true);

        Assert.NotNull(result);
        AuthenticateCertificate(result);
    }

    private static void AuthenticateCertificate(X509Certificate2 certificate)
    {
        if (certificate.GetRSAPrivateKey() is { })
        {
            ClientServerAuthenticate(certificate);
        }
        else if (certificate.GetECDsaPrivateKey() is { } ecdsa)
        {
            ClientServerAuthenticate(certificate);
            Assert.True(ecdsa != null);
        }
        else
        {
            Assert.True(false);
        }
    }




    private static async void ClientServerAuthenticate(X509Certificate2 certificate)
    {
        const string plainText = "Hello, world! こんにちは世界 ഹലോ വേൾഡ് Kαληµε´ρα κο´σµε";
        var plainMessage = Encoding.UTF8.GetBytes(plainText);
        var port1 = 19000;
        var port2 = 19999;

        int FindPort(int port1, int port2)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            var flags = new bool[port2 - port1 + 1];
            Array.Fill(flags, true);

            foreach (var tcpListener in tcpListeners)
            {
                if (IPAddress.IsLoopback(tcpListener.Address))
                {
                    for (var port = port1; port <= port2; port++)
                    {
                        if (tcpListener.Port == port)
                        {
                            flags[port - port1] = false;
                        }
                    }
                }
            }

            for (var port = port1; port <= port2; port++)
            {
                if (flags[port - port1])
                {
                    return port;
                }
            }

            return -1;
        }

        var port = FindPort(port1, port2);

        // https://stackoverflow.com/questions/28548326/net-sslstream-with-client-certificate
        // https://code-maze.com/csharp-task-run-vs-task-factory-startnew/

        Assert.InRange(port, port1, port2);

        var clientCompleted = new TaskCompletionSource<int>();
        var serverInitialized = new TaskCompletionSource<int>();
        Exception? clientFault = null;
        Exception? serverFault = null;
        string? clientReceived = null;
        string? serverReceived = null;

        async Task DoServer(int port, CancellationToken token, Task taskWaitClientCompleted)
        {
            var server = new TcpListener(IPAddress.Loopback, port);
            try
            {
                server.Start();
                serverInitialized.SetResult(0);
                using var client = server.AcceptTcpClient();

#if NETCOREAPP3_1
                using var ssltrream = new SslStream(client.GetStream(), false, (s, cm, ch, p) => true);
                ssltrream.AuthenticateAsServer(certificate, true, false);
#else
                using var ssltrream = new SslStream(client.GetStream(), false);
                ssltrream.AuthenticateAsServer(new SslServerAuthenticationOptions
                {
                    ClientCertificateRequired = true,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    AllowRenegotiation = true,
                    EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                    RemoteCertificateValidationCallback = (s, cm, ch, p) => true,
                    ServerCertificate = certificate,
                });
#endif

                var array = new byte[4096];
                var buffer = new Memory<byte>(array);
                var len = await ssltrream.ReadAsync(buffer, token);
                serverReceived = Encoding.UTF8.GetString(buffer[..len].Span);

                await ssltrream.WriteAsync(plainMessage, token);

#if NETCOREAPP3_1 || NET5_0
                taskWaitClientCompleted.Wait((int)TimeSpan.FromMinutes(2).TotalMilliseconds, token);
                await Task.CompletedTask;
#else
                await taskWaitClientCompleted.WaitAsync(TimeSpan.FromMinutes(2), token);
#endif

            }
            catch (Exception ex)
            {
                serverFault = ex;
            }
            finally
            {
                server.Stop();
            }
        }

        async Task DoClient(int port, CancellationToken token, Task taskWaitServerInitialized)
        {
            try
            {
                await taskWaitServerInitialized;
                await Task.Delay(TimeSpan.FromMilliseconds(10), token);
                {
                    using TcpClient client = new();
                    await client.ConnectAsync(IPAddress.Loopback, port);

#if NETCOREAPP3_1
                    using var ssltrream = new SslStream(client.GetStream(), false, (s, cm, ch, p) => true);
                    var certs = new X509CertificateCollection(new[] { certificate });
                    ssltrream.AuthenticateAsClient("localhost", certs, false);
#else
                    using var ssltrream = new SslStream(client.GetStream(), false);
                    ssltrream.AuthenticateAsClient(new SslClientAuthenticationOptions
                    {
                        TargetHost = "localhost",
                        RemoteCertificateValidationCallback = (s, cm, ch, p) => true,
                        LocalCertificateSelectionCallback = (s, h, cc, cs, iss) => certificate,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    });
#endif
                    await ssltrream.WriteAsync(plainMessage, token);

                    var array = new byte[4096];
                    var buffer = new Memory<byte>(array);
                    var len = await ssltrream.ReadAsync(buffer, token);
                    clientReceived = Encoding.UTF8.GetString(buffer[..len].Span);
                }

                clientCompleted.SetResult(0);
            }
            catch (Exception ex)
            {
                clientFault = ex;
            }
        }

        var cts = new CancellationTokenSource();

        var clientWork = Task.Run(() => DoClient(port, cts.Token, serverInitialized.Task));
        var serverWork = Task.Run(() => DoServer(port, cts.Token, clientCompleted.Task));

        Task.WaitAny(new[] { Task.WhenAll(clientWork, serverWork) }, TimeSpan.FromSeconds(20));
        cts.Cancel();

        if (clientFault != null && serverFault != null)
        {
            Assert.Fail($"Client: {clientFault}{Environment.NewLine}Server: {serverFault}");
        }
        else if (clientFault != null)
        {
            Assert.Fail($"Client: {clientFault}");
        }
        else if (serverFault != null)
        {
            Assert.Fail($"Server: {serverFault}");
        }

        Assert.Equal(plainText, clientReceived);
        Assert.Equal(plainText, serverReceived);
    }
}

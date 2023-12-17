#r "nuget: MQTTnet, 4.3.3.952"
#r "nuget: Lestaly, 0.52.0"
#nullable enable
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Lestaly;
using MQTTnet;
using MQTTnet.Client;

var settings = new
{
    BrokerHost = "test.mosquitto.org",

    BrokerPort = 8884,

    CaCert = ThisSource.RelativeFile("./certs/ca.crt"),

    ClientCert = ThisSource.RelativeFile("./certs/client.crt"),

    ClientKey = ThisSource.RelativeFile("./certs/client.key"),

    ClientId = $"test-cid-{Guid.NewGuid()}",

    PublishTopic = "mytest/test/aaa",
};

return await Paved.RunAsync(async () =>
{
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    ConsoleWig.WriteLine("Preparing to connect");

    // Load certs
    // Temporary cert and Export/Reload are to avoid the following errors by Windows : 'Authentication failed because the platform does not support ephemeral keys.'
    using var caCert = X509Certificate2.CreateFromPem(await settings.CaCert.ReadAllTextAsync());
    using var clientCertTemp = X509Certificate2.CreateFromPemFile(settings.ClientCert.FullName, settings.ClientKey.FullName);
    using var clientCert = new X509Certificate2(clientCertTemp.Export(X509ContentType.Pkcs12));

    // TLS options
    var tlsOptions = new MqttClientTlsOptionsBuilder()
        .UseTls()
        .WithTargetHost(settings.BrokerHost)
        .WithTrustChain(new() { caCert, })
        .WithClientCertificates(new() { clientCert, })
        .WithApplicationProtocols(new() { SslApplicationProtocol.Http2, })
        .Build();

    // Client optinos
    var clientOptinos = new MqttClientOptionsBuilder()
        .WithTcpServer(settings.BrokerHost, settings.BrokerPort)
        .WithTlsOptions(tlsOptions)
        .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
        .WithCleanSession(true)
        .WithWillRetain(false)
        .WithClientId(settings.ClientId)
        .Build();

    // Context information
    ConsoleWig.WriteLine("Context information");
    ConsoleWig.WriteLine($"  Broker       : {settings.BrokerHost}:{settings.BrokerPort}");
    ConsoleWig.WriteLine($"  ClientId     : {settings.ClientId}");
    ConsoleWig.WriteLine($"  PublishTopic : {settings.PublishTopic}");

    // Create client
    var factory = new MqttFactory();
    using var client = factory.CreateMqttClient();

    // Connect to broker
    ConsoleWig.WriteLine("Connecting to a broker");
    var connResult = await client.ConnectAsync(clientOptinos);

    // Publish messages. 
    ConsoleWig.WriteLine().WriteLine("Publish the input message.");
    while (true)
    {
        var message = ConsoleWig.Write(">").ReadLine();
        await client.PublishStringAsync(settings.PublishTopic, message, cancellationToken: signal.Token);
    }

});

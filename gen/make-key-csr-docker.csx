#r "nuget: Lestaly, 0.52.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // docker image name
    ImageName = "my-openssl",

    // generate key file
    GenerateKeyFile = ThisSource.RelativeFile("../certs/client.key"),

    // generate csr file
    GenerateCsrFile = ThisSource.RelativeFile("../certs/client.csr"),

    // csr subjects
    CsrSubjects = "/C=JP/ST=Tokyo/O=Test/CN=test.example",
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // input docker file
    using var dockerfile = new StringReader("""
    FROM alpine
    RUN apk add --no-cache openssl
    """);

    // build docker image
    ConsoleWig.WriteLine("Build openssl image");
    await "docker".args("build", "-t", settings.ImageName, "-")
        .silent().input(dockerfile).killby(signal.Token).result().success();

    // generate key
    ConsoleWig.WriteLine("Generate key file");
    await "docker".args(
            "run", "--rm",
            "-v", $"{settings.GenerateKeyFile.Directory!.WithCreate().FullName}:/work/out",
            settings.ImageName,
            "openssl", "genrsa", "-out", $"/work/out/{settings.GenerateKeyFile.Name}"
        )
        .killby(signal.Token).result().success();

    // generate key
    ConsoleWig.WriteLine("Generate csr file");
    var csrDir = settings.GenerateCsrFile.Directory!.WithCreate().FullName;
    await "docker".args(
            "run", "--rm",
            "-v", $"{settings.GenerateKeyFile.FullName}:/work/in/keyfile",
            "-v", $"{settings.GenerateCsrFile.Directory!.WithCreate().FullName}:/work/out",
            settings.ImageName,
            "openssl", "req", "-new", "-utf8", "-out", $"/work/out/{settings.GenerateCsrFile.Name}", "-key", "/work/in/keyfile",
            "-subj", settings.CsrSubjects
        )
        .killby(signal.Token).result().success();
});

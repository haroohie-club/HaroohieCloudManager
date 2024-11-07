using System;
using HaroohieCloudManager;
using Mono.Options;

public class Program
{
    static void Main(string[] args)
    {
        CommandSet commands = new("HaroohieCloudManager")
        {
            new CheckCorsProxyCommand(),
            new UpdateWeblateCommand(),
            new UploadToStorageCommand(),
        };
        commands.Run(args);
    }
}
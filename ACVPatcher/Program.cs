// // See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");
using System;
using System.IO.Compression;
using ACVPatcher;
using CommandLine;
using CommandLine.Text;

class Options
{
    [Option('c', "class", Required = false, HelpText = "Path to the DEX file.")]
    public IEnumerable<string>? ClassPath { get; set; }

    [Option('p', "permission", Required = false, HelpText = "The permission.")]
    public IEnumerable<string>? Permission { get; set; }

    [Option('i', "instrumentation", Required = false, HelpText = "The instrumentation.")]
    public string? Instrumentation { get; set; }

    [Option('r', "receiver", Required = false, HelpText = "The receiver.")]
    public IEnumerable<string>? Receivers { get; set; }

    [Option('a', "apkpath", Required = true, HelpText = "Path to the APK file to patch.")]
    public required string ApkPath { get; set; }

    [Option("silent", Required = false, HelpText = "Silent mode.")]
    public bool Silent { get; set; }

    [Option("jarsigner", Required = false, HelpText = "Use jarsigner signature mode (not recommended).")]
    public bool JarSign { get; set; }

}

class Program
{
    static async Task Main(string[] args)
    {
        // args = "-p android.permission.WRITE_EXTERNAL_STORAGE -i tool.acv.AcvInstrumentation -r tool.acv.AcvReceiver:tool.acv.calculate -a ./base.apk".Split(' ');
        var options = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async options =>
        {
            if (!options.Silent)
            {
                Console.WriteLine($"Class Path: {(options.ClassPath != null ? string.Join(", ", options.ClassPath) : string.Empty)}");
                Console.WriteLine($"Permission: {(options.Permission != null ? string.Join(", ", options.Permission) : string.Empty)}");
                Console.WriteLine($"Instrumentation: {options.Instrumentation}");
                Console.WriteLine($"Receivers: {(options.Receivers != null ? string.Join(", ", options.Receivers) : string.Empty)}");
                Console.WriteLine($"APK Path: {options.ApkPath}");
            }
            var patchingManager = new PatchingManager(options.ApkPath, options.ClassPath, options.Permission, options.Instrumentation, options.Receivers, options.JarSign);
            await patchingManager.Run();
        });
    }
}

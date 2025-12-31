// // See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");
using System;
using System.IO.Compression;
using ACVPatcher;
using CommandLine;
using CommandLine.Text;

class Options
{
    [Option('c', "class", Required = false, HelpText = "Path to the DEX file to rewrite (e.g. classes2.dex).")]
    public IEnumerable<string>? ClassPath { get; set; }

    [Option('p', "permission", Required = false, HelpText = "Adds the permission.")]
    public IEnumerable<string>? Permission { get; set; }

    [Option('i', "instrumentation", Required = false, HelpText = "Adds instrumentation tag.")]
    public string? Instrumentation { get; set; }

    [Option('r', "receiver", Required = false, HelpText = "The receiver.")]
    public IEnumerable<string>? Receivers { get; set; }

    [Option('P', "remove-permission", Required = false, HelpText = "<uses-permission> entry to remove by its name from AndroidManifest.xml.")]
    public IEnumerable<string>? RemovePermissions { get; set; }

    [Option('T', "remove-tag", Required = false, HelpText = "Removes tags by name under the <application> tag in AndroidManifest.xml. Specify the tag and name 'tag:name' (e.g. receiver:com.app.Receiver).")]
    public IEnumerable<string>? RemoveApplicationTags { get; set; }

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
                Console.WriteLine($"DEX Path: {(options.ClassPath != null ? string.Join(", ", options.ClassPath) : string.Empty)}");
                if (options.Permission != null && options.Permission.Any())
                {
                    Console.WriteLine($"Permission: {(options.Permission != null ? string.Join(", ", options.Permission) : string.Empty)}");    
                }
                Console.WriteLine($"Instrumentation: {options.Instrumentation}");
                Console.WriteLine($"Receivers: {(options.Receivers != null ? string.Join(", ", options.Receivers) : string.Empty)}");
                Console.WriteLine($"APK Path: {options.ApkPath}");
                if (options.RemovePermissions != null && options.RemovePermissions.Any())
                {
                    Console.WriteLine($"Remove Permissions: {string.Join(", ", options.RemovePermissions)}");
                }
                if (options.RemoveApplicationTags != null && options.RemoveApplicationTags.Any())
                {
                    Console.WriteLine($"Remove Application Tags: {string.Join(", ", options.RemoveApplicationTags)}");
                }
            }
            var patchingManager = new PatchingManager(options.ApkPath, options.ClassPath, options.Permission, options.Instrumentation, options.Receivers, options.JarSign, options.RemovePermissions, options.RemoveApplicationTags);
            await patchingManager.Run();
        });
    }
}

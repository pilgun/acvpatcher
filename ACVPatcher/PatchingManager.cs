using System;
using System.IO.Compression;
using CommandLine;
using CommandLine.Text;
using QuestPatcher.Axml;
using QuestPatcher.Zip;

namespace ACVPatcher
{
    public class PatchingManager
    {
        private string ApkPath { get; set; }
        private IEnumerable<string>? ClassPath { get; set; }
        private IEnumerable<string>? Permission { get; set; }
        private string? Instrumentation { get; set; }
        private IEnumerable<string>? Receivers { get; set; }

        public PatchingManager(string apkpath, IEnumerable<string>? classpaths, IEnumerable<string>? permissions, string? instrumentationTag, IEnumerable<string>? receivers)
        {
            ApkPath = apkpath;
            ClassPath = classpaths;
            Permission = permissions;
            Instrumentation = instrumentationTag;
            Receivers = receivers;
        }

        public async Task Run()
        {
            using var apkStream = File.Open(ApkPath, FileMode.Open);
            var apk = await ApkZip.OpenAsync(apkStream);

            if (ClassPath != null)
            {
                foreach (var classPath in ClassPath)
                {
                    await AddClassToApk(apk, classPath);
                }
            }
            if (Permission != null || Instrumentation != null || Receivers != null)
            {
                await PatchManifest(apk);
            }

            await apk.DisposeAsync();
        }

        private async Task PatchManifest(ApkZip apk)
        {
            bool modified = false;
            using var ms = new MemoryStream();
            using (var stream = await apk.OpenReaderAsync("AndroidManifest.xml"))
            {
                await stream.CopyToAsync(ms);
            }

            ms.Position = 0;
            var manifest = AxmlLoader.LoadDocument(ms);
            string package = AxmlManager.GetPackage(manifest);
            if (Permission != null)
            {
                var existingPermissions = AxmlManager.GetExistingChildren(manifest, "uses-permission");
                foreach (var permission in Permission)
                {
                    if (existingPermissions.Contains(permission)) { continue; } // Do not add existing permissions
                    AddPermissionToManifest(manifest, permission);
                    modified = true;
                }
            }
            if (Instrumentation != null)
            {
                AddInstrumentationToManifest(manifest, Instrumentation, package);
                modified = true;
            }
            if (Receivers != null)
            {
                var appElement = manifest.Children.Single(child => child.Name == "application");
                // var existingReceivers = AxmlManager.GetExistingChildren(appElement, "receiver");
                var existingReceiverElements = GetExistingReceiverElements(appElement);
                var receiverActions = ParseReceiverActions(Receivers);
                foreach (var receiverAction in receiverActions)
                {
                    var receiverName = receiverAction.Key;
                    List<string> actions = receiverAction.Value;
                    var receiverElement = existingReceiverElements.ContainsKey(receiverName) ? existingReceiverElements[receiverName] : null;
                    if (receiverElement == null)
                    {
                        receiverElement = AddReceiverToManifest(appElement, receiverName);
                        existingReceiverElements[receiverName] = receiverElement;
                        modified = true;
                    }

                    var receiverIntentFilter = receiverElement.Children.Any(ch => ch.Name == "intent-filter") ? receiverElement.Children.Single(ch => ch.Name == "intent-filter") : null;
                    if (receiverIntentFilter == null)
                    {
                        receiverIntentFilter = new AxmlElement("intent-filter");
                        receiverElement.Children.Add(receiverIntentFilter);
                    }
                    List<string?> existingActions = receiverIntentFilter.Children
                        .Where(ch => ch.Name == "action").Select(ch => ch.Attributes.Single(attr => attr.Name == "name")?.Value as string)
                        .ToList();
                    var newActions = actions.Where(action => action != null).Except(existingActions).ToList();

                    foreach (var action in newActions)
                    {
                        AddIntentAction(receiverIntentFilter, action!);
                        modified = true;
                    }
                }
            }
            if (modified)
            {
                ms.SetLength(0);
                ms.Position = 0;
                AxmlSaver.SaveDocument(ms, manifest);
                ms.Position = 0;
                await apk.AddFileAsync("AndroidManifest.xml", ms, CompressionLevel.Optimal);
            }
        }

        private Dictionary<string, AxmlElement> GetExistingReceiverElements(AxmlElement appElement)
        {
            var receiverElements = new Dictionary<string, AxmlElement>();

            foreach (var receiver in appElement.Children)
            {
                if (receiver.Name != "receiver") { continue; }

                var receiverName = receiver.Attributes.Single(attr => attr.Name == "name")?.Value as string;
                if (receiverName != null)
                {
                    receiverElements[receiverName] = receiver;
                }
            }
            return receiverElements;
        }

        private Dictionary<string, List<string>> GetExistingReceiverActions(AxmlElement appElement)
        {
            var receiverActions = new Dictionary<string, List<string>>();

            foreach (var receiver in appElement.Children)
            {
                if (receiver.Name != "receiver") { continue; }

                var receiverName = receiver.Attributes.Single(attr => attr.Name == "name")?.Value as string;
                if (receiverName == null) { continue; }

                var intentFilters = receiver.Children.Where(child => child.Name == "intent-filter").ToList();
                if (intentFilters.Count == 0) { continue; }

                var actions = new List<string>();
                foreach (var intentFilter in intentFilters)
                {
                    foreach (var action in intentFilter.Children)
                    {
                        if (action.Name != "action") { continue; }

                        var actionName = action.Attributes.Single(attr => attr.Name == "name")?.Value as string;
                        if (actionName != null)
                        {
                            actions.Add(actionName);
                        }
                    }
                }
                receiverActions[receiverName] = actions;
            }
            return receiverActions;
        }

        private Dictionary<string, List<string>> ParseReceiverActions(IEnumerable<string> receiverArgs)
        {
            var receiverActions = new Dictionary<string, List<string>>();

            foreach (string receiverArg in receiverArgs)
            {
                // Split the receiverArg string into two separate variables
                var receiverParts = receiverArg.Split(':');
                var receiverClassName = receiverParts[0];
                var actionName = receiverParts[1];

                if (receiverActions.ContainsKey(receiverClassName))
                {
                    receiverActions[receiverClassName].Add(actionName);
                }
                else
                {
                    receiverActions[receiverClassName] = new List<string> { actionName };
                }
            }

            return receiverActions;
        }

        private AxmlElement AddReceiverToManifest(AxmlElement appElement, string receiver)
        {
            AxmlElement receiverElement = new("receiver");
            AxmlManager.AddNameAttribute(receiverElement, receiver);
            AxmlManager.AddExportedAttribute(receiverElement, true);
            AxmlManager.AddEnabledAttribute(receiverElement, true);
            appElement.Children.Add(receiverElement);
            return receiverElement;
        }

        private void AddIntentAction(AxmlElement intentFilterElement, string actionName)
        {
            AxmlElement actionElement = new("action");
            AxmlManager.AddNameAttribute(actionElement, actionName);
            intentFilterElement.Children.Add(actionElement);
        }

        private void AddInstrumentationToManifest(AxmlElement manifest, string instrumentationName, string package)
        {
            AxmlElement instrElement = new("instrumentation");
            AxmlManager.AddNameAttribute(instrElement, instrumentationName);
            AxmlManager.AddTargetPackageAttribute(instrElement, package);
            manifest.Children.Add(instrElement);
        }

        private static void AddPermissionToManifest(AxmlElement manifest, string permission)
        {
            AxmlElement permElement = new("uses-permission");
            AxmlManager.AddNameAttribute(permElement, permission);
            manifest.Children.Add(permElement);
        }

        private async Task AddClassToApk(ApkZip apk, string classPath)
        {
            var fileName = Path.GetFileName(classPath);
            using var dexStream = File.OpenRead(classPath);
            await apk.AddFileAsync(fileName, dexStream, CompressionLevel.Optimal);
        }
    }
}
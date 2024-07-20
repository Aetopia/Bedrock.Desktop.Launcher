using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Xml;
using Windows.Foundation;
using Windows.Management.Deployment;

static class App
{
    static CancellationTokenSource source = null;

    internal static void Cancel() { source?.Cancel(); while (source != null) ; }

    internal static void Install(UpdateIdentity update, ProgressBar progressBar, TextBlock textBlock1, TextBlock textBlock2, Action action)
    {
        var package = Store.PackageManager.FindPackagesForUser(string.Empty, "Bedrock.Desktop.Release_svpbzhw13qwwr").SingleOrDefault();
        if (package is not null && !(update.Version > new Version(package.Id.Version.Major, package.Id.Version.Minor, package.Id.Version.Build, package.Id.Version.Revision))) return;

        IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> operation = default;
        if (package is not null)
        {
            progressBar.Dispatcher.Invoke(() =>
            {
                progressBar.IsIndeterminate = false;
                progressBar.Value = 0;
                textBlock1.Text = "Removing...";
            });
            operation = Store.PackageManager.RemovePackageAsync(package.Id.FullName, RemovalOptions.PreserveApplicationData);
            operation.Progress += (sender, e) => progressBar.Dispatcher.Invoke(() => { if (progressBar.Value != e.percentage) textBlock1.Text = $"Removing {progressBar.Value = e.percentage}%"; });
            operation.AsTask().Wait();
        }

        XmlDocument document = new();
        try
        {
            progressBar.Dispatcher.Invoke(() =>
            {
                progressBar.IsIndeterminate = true;
                textBlock1.Text = "Removing...";
            });

            document.Load("AppxBlockMap.xml");
            foreach (var path in document.GetElementsByTagName("File").Cast<XmlNode>().Select(node => node.Attributes["Name"].InnerText))
            {
                NativeMethods.DeleteFile(path);
                try { Directory.Delete(Path.GetDirectoryName(path), true); } catch { };
            }
        }
        catch { }

        action();

        source = new();
        try
        {
            using var zip = ZipFile.OpenRead(update.Path);
            progressBar.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 0;
                progressBar.Maximum = zip.Entries.Count;
                progressBar.IsIndeterminate = false;
                textBlock1.Text = "Extracting...";
            });


            foreach (var entry in zip.Entries)
            {
                if (!Path.GetExtension(entry.Name).Equals(".p7x", StringComparison.OrdinalIgnoreCase))
                {
                    if (source.IsCancellationRequested) return;
                    try { Directory.CreateDirectory(Path.GetDirectoryName(entry.FullName)); } catch { }
                    entry.ExtractToFile(entry.FullName, true);
                }
                progressBar.Dispatcher.Invoke(() => textBlock2.Text = $"{progressBar.Value += 1} of {progressBar.Maximum}");
            }
        }
        finally { source.Dispose(); source = null; }

        progressBar.Dispatcher.Invoke(() =>
        {
            progressBar.Value = 0;
            progressBar.Maximum = 100;
            textBlock1.Text = "Registering...";
            textBlock2.Text = null;
        });

        Uri manifestUri = new(Path.GetFullPath("AppxManifest.xml"));
        document.Load(manifestUri.LocalPath);

        var identity = document["Package"]["Identity"];
        identity.SetAttribute("Name", "Bedrock.Desktop.Release");
        identity.SetAttribute("Publisher", "CN=Bedrock.Desktop");

        ((XmlElement)document.GetElementsByTagName("uap:VisualElements")[0]).SetAttribute("AppListEntry", "none");

        var child1 = document.GetElementsByTagName("TargetDeviceFamily")[0];
        var child2 = document.GetElementsByTagName("PackageDependency").Cast<XmlNode>().First(node => node.Attributes["Name"]?.InnerText != "Microsoft.Services.Store.Engagement");
        var node = document.GetElementsByTagName("Dependencies")[0];

        node.RemoveAll();
        node.AppendChild(child1);
        node.AppendChild(child2);

        document.Save(manifestUri.LocalPath);

        operation = Store.PackageManager.RegisterPackageAsync(manifestUri, null, DeploymentOptions.ForceApplicationShutdown | DeploymentOptions.DevelopmentMode);
        operation.Progress += (sender, e) => progressBar.Dispatcher.Invoke(() => { if (progressBar.Value != e.percentage) textBlock1.Text = $"Registering {progressBar.Value = e.percentage}%"; });
        operation.AsTask().Wait();
    }
}
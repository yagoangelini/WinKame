// WinKame, a morandi.cloud product. Copyright (C) 2026 Thales de Oliveira Morandi.
// SPDX-License-Identifier: GPL-3.0-or-later. See LICENSE and NOTICE.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace WinKame.Shell;

/// <summary>
/// Per-user (no admin) hooks into Explorer: context menu, Send To and start with Windows.
/// The Microsoft Store (MSIX) build only offers start with Windows, through the package's StartupTask:
/// a packaged app's registry and Send To writes are private to it, so Explorer would never see them.
/// </summary>
internal static partial class ShellIntegration
{
    /// <summary>Same id as the StartupTask in packaging/msix/AppxManifest.xml.</summary>
    private const string StartupTaskId = "WinKameStartup";

    private const string VerbName = "WinKame";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private static string Exe => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "WinKame.exe");

    private static readonly string[] MenuRoots =
    [
        @"Software\Classes\*\shell\" + VerbName,
        @"Software\Classes\Directory\shell\" + VerbName,
    ];

    public static bool ContextMenuInstalled
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(MenuRoots[0] + @"\command");
            return k?.GetValue(null) is string s && s.Contains(Exe, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetContextMenu(bool on)
    {
        foreach (var root in MenuRoots)
        {
            if (on)
            {
                using var k = Registry.CurrentUser.CreateSubKey(root);
                k.SetValue("MUIVerb", T("Abrir no WinKame"));
                k.SetValue("Icon", $"\"{Exe}\",0");
                k.SetValue("MultiSelectModel", "Player");
                using var cmd = k.CreateSubKey("command");
                cmd.SetValue(null, $"\"{Exe}\" --wheel \"%1\"");
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(root, throwOnMissingSubKey: false);
            }
        }
    }

    private static string SendToLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SendTo), "WinKame.lnk");

    public static bool SendToInstalled => File.Exists(SendToLink);

    /// <summary>"Send To" passes every selected file in one command line, which works well for selections.</summary>
    public static void SetSendTo(bool on)
    {
        if (!on)
        {
            if (File.Exists(SendToLink)) File.Delete(SendToLink);
            return;
        }
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell indisponível");
        dynamic shell = Activator.CreateInstance(type)!;
        try
        {
            dynamic link = shell.CreateShortcut(SendToLink);
            link.TargetPath = Exe;
            link.Arguments = "--wheel";
            link.IconLocation = Exe + ",0";
            link.Description = T("Abrir arquivos na roda do WinKame");
            link.Save();
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }

    private const string MailProgId = "WinKame.MailTo";
    private const string CapabilitiesKey = @"Software\WinKame\Capabilities";

    /// <summary>WinKame is offered as an email app (Windows Settings › Default apps). Windows only lets the user pick it.</summary>
    public static bool MailHandlerRegistered
    {
        get
        {
            if (IsPackaged) return true; // declared in the package manifest
            using var k = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{MailProgId}\shell\open\command");
            return k?.GetValue(null) is string s && s.Contains(Exe, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetMailHandler(bool on)
    {
        if (IsPackaged) return;
        if (on)
        {
            using (var k = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{MailProgId}"))
            {
                k.SetValue(null, "URL:MailTo Protocol");
                k.SetValue("URL Protocol", "");
                k.SetValue("FriendlyTypeName", T("WinKame: copiar o endereço de e-mail"));
                using var icon = k.CreateSubKey("DefaultIcon");
                icon.SetValue(null, $"\"{Exe}\",0");
                using var cmd = k.CreateSubKey(@"shell\open\command");
                cmd.SetValue(null, $"\"{Exe}\" --mailto \"%1\"");
            }
            using (var c = Registry.CurrentUser.CreateSubKey(CapabilitiesKey))
            {
                c.SetValue("ApplicationName", "WinKame");
                c.SetValue("ApplicationDescription", T("Copia o endereço dos links de e-mail em vez de abrir um programa de e-mail."));
                using var u = c.CreateSubKey("URLAssociations");
                u.SetValue("mailto", MailProgId);
            }
            using var apps = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications");
            apps.SetValue("WinKame", CapabilitiesKey);
        }
        else
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{MailProgId}", throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree(CapabilitiesKey, throwOnMissingSubKey: false);
            using var apps = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", writable: true);
            apps?.DeleteValue("WinKame", throwOnMissingValue: false);
        }
    }

    /// <summary>Opens Windows Settings at WinKame's default-app page (or the general page in the Store build).</summary>
    public static void OpenDefaultAppsSettings()
    {
        var uri = IsPackaged ? "ms-settings:defaultapps" : "ms-settings:defaultapps?registeredAppUser=WinKame";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
    }

    /// <summary>True when running as the MSIX package (Microsoft Store build).</summary>
    public static bool IsPackaged { get; } = DetectPackage();

    private static bool DetectPackage()
    {
        var length = 0u;
        // ERROR_INSUFFICIENT_BUFFER means there is a package name to return; APPMODEL_ERROR_NO_PACKAGE means none.
        return GetCurrentPackageFullName(ref length, 0) == 122;
    }

    [LibraryImport("kernel32.dll")]
    private static partial int GetCurrentPackageFullName(ref uint packageFullNameLength, nint packageFullName);

    private static StartupTask PackageStartupTask() =>
        Task.Run(async () => await StartupTask.GetAsync(StartupTaskId)).GetAwaiter().GetResult();

    public static bool StartupInstalled
    {
        get
        {
            if (IsPackaged)
            {
                return PackageStartupTask().State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }

            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(VerbName) is string s && s.Contains(Exe, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetStartup(bool on)
    {
        if (IsPackaged)
        {
            var task = PackageStartupTask();
            if (!on)
            {
                task.Disable();
                return;
            }
            var state = Task.Run(async () => await task.RequestEnableAsync()).GetAwaiter().GetResult();
            if (state is not (StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy))
            {
                // Once the user turns it off in Windows, only Windows can turn it back on.
                throw new InvalidOperationException(T("ative o WinKame em Configurações › Aplicativos › Inicialização."));
            }
            return;
        }
        using var k = Registry.CurrentUser.CreateSubKey(RunKey);
        if (on) k.SetValue(VerbName, $"\"{Exe}\" --background");
        else k.DeleteValue(VerbName, throwOnMissingValue: false);
    }
}

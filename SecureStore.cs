using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

namespace Secure_Store
{
    public class Storage
    {
        public static class SecureStore
        {
            private static string BasePath
            {
                get
                {
                    // Cross‑platform per‑session location
                    string? runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
                    if (!string.IsNullOrEmpty(runtimeDir))
                        return runtimeDir; // Linux, auto-clears on logout

                    if (OperatingSystem.IsWindows())
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "XRUIOS_RUNTIME"
                        );

                    // macOS fallback
                    return "/tmp"; // cleared on restart/logout
                }
            }

            private static string GetPath(string key) =>
                Path.Combine(BasePath, $"xr_{key}.dat");

            public static void Set<T>(string key, T value)
            {
                Directory.CreateDirectory(BasePath);

                string path = GetPath(key);
                string json = JsonSerializer.Serialize(value);

                File.WriteAllText(path, json);

                if (OperatingSystem.IsWindows())
                    ApplyWindowsAcl(path);
                else
                    ApplyUnixPermissions(path);
            }

            public static T? Get<T>(string key)
            {
                string path = GetPath(key);
                if (!File.Exists(path))
                    return default;

                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json);
            }

            private static void ApplyWindowsAcl(string path)
            {
                var fileInfo = new FileInfo(path);
                var security = fileInfo.GetAccessControl();

                // Remove inherited permissions
                security.SetAccessRuleProtection(true, false);

                // Current user
                var currentUser = WindowsIdentity.GetCurrent().User!;
                var userRule = new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow
                );
                security.AddAccessRule(userRule);

                fileInfo.SetAccessControl(security);
            }

            private static void ApplyUnixPermissions(string path)
            {
                try
                {
                    var chmod = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/bin/chmod",
                        Arguments = $"600 \"{path}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = System.Diagnostics.Process.Start(chmod);
                    proc?.WaitForExit();
                }
                catch
                {
                    // Fallback: hide file (less secure)
                    File.SetAttributes(path, FileAttributes.Hidden);
                }
            }



        }
    }
}

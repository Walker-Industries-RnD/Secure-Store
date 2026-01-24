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
                    // Use per-session temp directory on all platforms
                    string sessionDir = Path.Combine(Path.GetTempPath(), "SECURE_STORE" + Environment.UserName);
                    Directory.CreateDirectory(sessionDir);
                    return sessionDir;
                }
            }

            private static string GetPath(string key) =>
                Path.Combine(BasePath, $"secstr_{key}.dat");

            private static void EnsureFolder()
            {
                if (!Directory.Exists(BasePath))
                {
                    Directory.CreateDirectory(BasePath);

                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(BasePath);
                            var dirSecurity = dirInfo.GetAccessControl();

                            // Remove inheritance & keep only explicit rules
                            dirSecurity.SetAccessRuleProtection(true, false);

                            // Give full control to current user only
                            var currentUser = WindowsIdentity.GetCurrent().User!;
                            dirSecurity.AddAccessRule(new FileSystemAccessRule(
                                currentUser,
                                FileSystemRights.FullControl,
                                AccessControlType.Allow
                            ));

                            dirInfo.SetAccessControl(dirSecurity);

                            // Hide the folder from casual browsing
                            dirInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                        }
                        catch
                        {
                            // If ACLs fail, fall back silently; still writable
                        }
                    }
                }
            }

            public static void Set<T>(string key, T value)
            {
                EnsureFolder();

                string path = GetPath(key);
                string json = JsonSerializer.Serialize(value);
                File.WriteAllText(path, json);

                if (!OperatingSystem.IsWindows())
                {
                    ApplyUnixPermissions(path);
                }
            }

            public static T? Get<T>(string key)
            {
                string path = GetPath(key);
                if (!File.Exists(path))
                    return default;

                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json);
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
                    // fallback: hide file (less secure)
                    File.SetAttributes(path, FileAttributes.Hidden);
                }
            }
        }
    }
}

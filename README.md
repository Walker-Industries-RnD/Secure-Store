<p align="center"> 
  <img src="https://github.com/Walker-Industries-RnD/Secure-Store/blob/main/docs/assets/secure-store.png" alt="Secure Store" width="80%"/> 
</p>

**A tiny, cross-platform, fully local, per-user encrypted-at-rest secure key/value store that only the current logged-in user can read or write.**

<p align="center">
  Secure Store is the minimalist storage backend used throughout the XRUIOS to safely persist sensitive data (API endpoints, tokens, session keys, etc.) with zero external dependencies and strong filesystem-level isolation. 
  
  It was created during a need to implement cross platform variables that would be secure, not easily tamperable and able to be  easily referenced (Or an alternative to EnvironmentVariableTarget.User)
</p>

<p align="center">
  <strong>Windows • Linux • macOS • 100% Offline • No Registry • No Keychain • Pure .NET 8 </strong>
</p>

<p align="center">
  <a href="https://github.com/Walker-Industries-RnD/Secure-Store"><strong>View on GitHub</strong></a> •
  <a href="https://walkerindustries.xyz">Walker Industries</a> •
  <a href="https://discord.gg/H8h8scsxtH">Discord</a> •
  <a href="https://www.patreon.com/walkerdev">Patreon</a>
</p>


<p align="center">
  <a href="https://walker-industries-rnd.github.io/Secure-Store/" 
     style="font-size: 1.4em; color: #58a6ff; text-decoration: none;">
    <strong> Documentation • Examples • Design </strong>
  </a>
</p>


### How it works

Secure Store writes serialized JSON files into a private runtime directory that is automatically cleaned up on logout/reboot where possible:

| Platform       | Storage Location                                      | Cleanup Behavior                  | Protection Method                     |
|----------------|--------------------------------------------------------|-----------------------------------|---------------------------------------|
| **Linux**      | `$XDG_RUNTIME_DIR` (or `/tmp` fallback)               | Cleared on logout/restart         | `chmod 600` (owner-only)              |
| **Windows**    | `%LocalAppData%\XRUIOS_RUNTIME`                       | Survives reboot, cleared on uninstall | Explicit ACL — only current user      |
| **macOS**      | `/tmp`                                                | Cleared on restart/logout         | `chmod 600` (owner-only)              |

- Files are named `xr_<key>.dat`
- Data is serialized with `System.Text.Json` (UTF-8, no encryption at rest yet — relies on filesystem isolation)
- On Windows: inheritance is disabled and an explicit Allow rule is set for the current user SID only
- On Unix: `chmod 600` is invoked via `/bin/chmod`; if that fails it falls back to marking the file hidden (best-effort)
- Zero trust against other local users or compromised processes running as different accounts

> “If another local user or malware without your exact user context can read it — it’s not secure.”

<div align="center">

| ![WalkerDev](https://github.com/Walker-Industries-RnD/Secure-Store/blob/main/docs/assets/walkerdev.png) | ![Kennaness](https://github.com/Walker-Industries-RnD/Secure-Store/blob/main/docs/assets/kennaness.png) |
|-----------------------------|-----------------------------|
| **Code by WalkerDev**<br>“Loving coding is the same as hating yourself”<br>[Discord](https://discord.gg/H8h8scsxtH) | **Art by Kennaness**<br>“When will I get my isekai?”<br>[Bluesky](https://bsky.app/profile/kennaness.bsky.social) • [ArtStation](https://www.artstation.com/kennaness) |

</div>

<br>

---

## What's In Here

| File / Class              | Description                                                                 |
|---------------------------|-------------------------------------------------------------------------------|
| `Storage.cs`              | The complete implementation (single file, no external dependencies)         |
| `SecureStore` (static)    | Public API — `Set<T>(key, value)` and `Get<T>(key)`                           |
| `ApplyWindowsAcl()`       | Strips inheritance and grants FullControl only to the current user SID      |
| `ApplyUnixPermissions()`  | Calls `/bin/chmod 600` (silent fallback to Hidden attribute if unavailable) |

> Full source is deliberately <150 lines. You can audit it in 30 seconds.

---

## Using Secure Store

Secure Store is stupid simple to use;

```csharp
using Secure_Store;

// Save anything serializable
SecureStore.Set("worker_addr", "http://localhost:5050");
SecureStore.Set("last_session", new SessionData { User = "walker", Expires = DateTime.UtcNow.AddHours(8) });

// Read it back
string? addr = SecureStore.Get<string>("worker_addr");
var session = SecureStore.Get<SessionData>("last_session");

// Works with complex objects too
public record SessionData(string User, DateTime Expires);

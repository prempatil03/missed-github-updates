using System.Runtime.InteropServices;
using System.Text;

namespace MissedGitHubUpdates.Services;

/// <summary>
/// Stores and retrieves the GitHub PAT using the Windows Credential Manager API.
/// The token is encrypted by Windows with the current user's login credentials —
/// it never touches a plain-text file.
///
/// Credential target name: "MissedGitHubUpdates/GitHubPAT"
/// </summary>
public static class CredentialService
{
    private const string CredentialTarget = "MissedGitHubUpdates/GitHubPAT";

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves the PAT to Windows Credential Manager.
    /// Overwrites any existing entry for this target.
    /// </summary>
    public static void SaveToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);

        var credential = new CREDENTIAL
        {
            Type           = CREDENTIAL_TYPE.GENERIC,
            TargetName     = CredentialTarget,
            CredentialBlob = Marshal.StringToCoTaskMemUni(token),
            CredentialBlobSize = (uint)(tokenBytes.Length * 2), // UTF-16 chars × 2 bytes
            Persist        = CREDENTIAL_PERSIST.LOCAL_MACHINE,
            UserName       = "github"
        };

        bool result = CredWrite(ref credential, 0);

        // Free the unmanaged memory we allocated above
        Marshal.FreeCoTaskMem(credential.CredentialBlob);

        if (!result)
            throw new InvalidOperationException(
                $"Failed to save token to Credential Manager. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    /// <summary>
    /// Retrieves the PAT from Windows Credential Manager.
    /// Returns null if no token has been saved yet (first run).
    /// </summary>
    public static string? LoadToken()
    {
        bool found = CredRead(CredentialTarget, CREDENTIAL_TYPE.GENERIC, 0, out IntPtr credPtr);

        if (!found)
            return null;

        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                return null;

            return Marshal.PtrToStringUni(credential.CredentialBlob,
                                          (int)(credential.CredentialBlobSize / 2));
        }
        finally
        {
            CredFree(credPtr);
        }
    }

    /// <summary>
    /// Deletes the stored token from Credential Manager (used if the user clears settings).
    /// </summary>
    public static void DeleteToken()
    {
        CredDelete(CredentialTarget, CREDENTIAL_TYPE.GENERIC, 0);
        // Ignore return value — if it didn't exist, that's fine
    }

    // ── Win32 P/Invoke definitions ────────────────────────────────────────────

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, CREDENTIAL_TYPE type, int reservedFlag,
                                        out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, CREDENTIAL_TYPE type, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);

    // ── Win32 structs and enums ───────────────────────────────────────────────

    private enum CREDENTIAL_TYPE : uint
    {
        GENERIC = 1
    }

    private enum CREDENTIAL_PERSIST : uint
    {
        SESSION      = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE   = 3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint            Flags;
        public CREDENTIAL_TYPE Type;
        public string          TargetName;
        public string          Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint            CredentialBlobSize;
        public IntPtr          CredentialBlob;
        public CREDENTIAL_PERSIST Persist;
        public uint            AttributeCount;
        public IntPtr          Attributes;
        public string          TargetAlias;
        public string          UserName;
    }
}

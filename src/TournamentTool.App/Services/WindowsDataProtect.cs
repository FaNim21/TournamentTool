using System.Security.Cryptography;
using System.Text;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.App.Services;

public class WindowsDataProtect : IDataProtect
{
    public byte[] Protect(string json)
    {
        byte[] raw = Encoding.UTF8.GetBytes(json);
        byte[] encrypted = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);
        return encrypted;
    }

    public string UnProtect(byte[] data)
    {
        byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        string json = Encoding.UTF8.GetString(decrypted);
        //tu sie upewnic czy ten clear nie jest za szybko
        Array.Clear(decrypted, 0, decrypted.Length);
        return json;
    }
}
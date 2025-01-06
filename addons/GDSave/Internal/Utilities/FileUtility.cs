using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;

namespace Chomp.Save.Internal;

public class FileUtility
{
    public static string GetOldestFilePath(string pathOne, string pathTwo) {
        if (!File.Exists(pathOne))
            return pathOne;
        if (!File.Exists(pathTwo))
            return pathTwo;
        DateTime pathOneWriteTime = File.GetLastWriteTime(pathOne);
        DateTime pathTwoWriteTime = File.GetLastWriteTime(pathTwo);
        return pathOneWriteTime < pathTwoWriteTime ? pathOne : pathTwo;
    }

    public static string GetNewestFilePath(string pathOne, string pathTwo) {
        bool pathOneExists = File.Exists(pathOne);
        bool pathTwoExists = File.Exists(pathTwo);
        if (!pathOneExists && !pathTwoExists)
            return "";
        if (pathOneExists && !pathTwoExists)
            return pathOne;
        if (!pathOneExists)
            return pathTwo;
        DateTime pathOneWriteTime = File.GetLastWriteTime(pathOne);
        DateTime pathTwoWriteTime = File.GetLastWriteTime(pathTwo);
        return pathOneWriteTime > pathTwoWriteTime ? pathOne : pathTwo;
    }

    public static void ArchiveFileAsCorrupted(string path) {
        string pathDirectory = Path.GetDirectoryName(path);
        string pathFileName = Path.GetFileName(path);
        string subFolder = Path.Combine(pathDirectory, "Corrupted Saves");
        if (!Directory.Exists(subFolder))
            Directory.CreateDirectory(subFolder);
        var culture = SaveSettings.Instance.UseInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
        string targetPathFileName = new StringBuilder().Append(DateTime.Now.ToString("yyyyMMddHHmmssfff", culture)).Append("_").Append(pathFileName).ToString();
        File.Move(path, Path.Combine(subFolder, targetPathFileName));
        File.Delete(path);
    }

    public static string GetAlternativeFilePath(string path, string extensionName) {
        return new StringBuilder().Append(path).Append(extensionName).ToString();
    }

    public static byte[] Encrypt(byte[] data, string key, string iv) {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        using MemoryStream memoryStream = new();
        using (CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
            cryptoStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }

    public static byte[] Decrypt(byte[] data, string key, string iv) {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        using MemoryStream memoryStream = new();
        using (CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write)) {
            cryptoStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }

    public static void EncryptFile(string path, string key, string iv) {
        byte[] bytes = File.ReadAllBytes(path);
        byte[] encrypted = Encrypt(bytes, key, iv);
        File.WriteAllBytes(path, encrypted);
    }

    public static void DecryptFile(string path, string key, string iv) {
        byte[] bytes = File.ReadAllBytes(path);
        byte[] decrypted = Decrypt(bytes, key, iv);
        File.WriteAllBytes(path, decrypted);
    }

    public static byte[] UnsecureEncrypt(byte[] bytes) => Encrypt(bytes, "1234567890123456", "1234567890123456");
    public static byte[] UnsecureDecrypt(byte[] data) => Decrypt(data, "1234567890123456", "1234567890123456");
    public static void UnsecureEncryptFile(string path) => EncryptFile(path, "1234567890123456", "1234567890123456");
    public static void UnsecureDecryptFile(string path) => DecryptFile(path, "1234567890123456", "1234567890123456");
}

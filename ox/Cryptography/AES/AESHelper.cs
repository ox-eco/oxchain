using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using OX.Cryptography.ECC;

namespace OX.Cryptography.AES
{
    public static class AESHelper
    {
        public static byte[] Encrypt(this byte[] data, OX.Cryptography.ECC.ECPoint shareKey, byte[] salt = default)
        {
            var ks = shareKey.ToString();//.EncodePoint(true).ToHexString();
            Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(ks, salt.IsNotNullAndEmpty() ? salt : new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, });
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = deriver.GetBytes(16);
            aes.Key = deriver.GetBytes(24);
            aes.Key = deriver.GetBytes(32);
            aes.IV = deriver.GetBytes(16);
            ICryptoTransform transform = aes.CreateEncryptor();
            return transform.TransformFinalBlock(data, 0, data.Length);
        }
        public static byte[] Decrypt(this byte[] encryptedData, OX.Cryptography.ECC.ECPoint shareKey, byte[] salt = default)
        {
            var ks = shareKey.EncodePoint(true).ToHexString();
            Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(ks, salt.IsNotNullAndEmpty() ? salt : new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, });
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = deriver.GetBytes(16);
            aes.Key = deriver.GetBytes(24);
            aes.Key = deriver.GetBytes(32);
            aes.IV = deriver.GetBytes(16);
            ICryptoTransform transform = aes.CreateDecryptor();
            return transform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }
    }
}

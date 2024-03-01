using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using OX.Cryptography.ECC;
using Org.BouncyCastle.Math.EC;
using OX.IO;

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
        public static T Encrypt<T>(this ISerializable item, OX.Cryptography.ECC.ECPoint shareKey, byte[] salt = default) where T : AESEncryptData, new()
        {
            return new T()
            {
                Data = item.ToArray().Encrypt(shareKey, salt)
            };
        }
        public static byte[] Decrypt(this byte[] encryptedData, OX.Cryptography.ECC.ECPoint shareKey, byte[] salt = default)
        {
            var ks = shareKey.ToString();
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
        public static T Decrypt<T>(this AESEncryptData data, OX.Cryptography.ECC.ECPoint shareKey, byte[] salt = default) where T : ISerializable, new()
        {
            byte[] bs = data.Data.Decrypt(shareKey, salt);
            if (bs.IsNullOrEmpty()) return default;
            try
            {
                return bs.AsSerializable<T>();
            }
            catch
            {
                return default;
            }
        }
    }
}

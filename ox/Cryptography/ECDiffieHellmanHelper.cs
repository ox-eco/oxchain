using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static OX.Helper;
using ECPoint = OX.Cryptography.ECC.ECPoint;
using OX.IO;
using OX.Cryptography;

namespace OX.Cryptography
{
    public static class ECDiffieHellmanHelper
    {
        private static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static ECPoint DiffieHellman(this KeyPair key, ECPoint pubkey)
        {
            return pubkey * key.PrivateKey;
        }
        public static ECPoint DiffieHellman(this ECPoint pubkey, KeyPair key)
        {
            return pubkey * key.PrivateKey;
        }
        public static byte[] ECDHDeriveKey(KeyPair local, ECPoint remote, byte[] suffix = default)
        {
            ReadOnlySpan<byte> pubkey_local = local.PublicKey.EncodePoint(false);
            ReadOnlySpan<byte> pubkey_remote = remote.EncodePoint(false);
            using ECDiffieHellman ecdh1 = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = local.PrivateKey,
                Q = new System.Security.Cryptography.ECPoint
                {
                    X = pubkey_local[1..][..32].ToArray(),
                    Y = pubkey_local[1..][32..].ToArray()
                }
            });
            using ECDiffieHellman ecdh2 = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new System.Security.Cryptography.ECPoint
                {
                    X = pubkey_remote[1..][..32].ToArray(),
                    Y = pubkey_remote[1..][32..].ToArray()
                }
            });
            IEnumerable<byte> keys = ecdh1.DeriveKeyMaterial(ecdh2.PublicKey);
            if (suffix != default)
            {
                keys = keys.Concat(suffix);
            }
            return Crypto.Default.Hash256(keys.ToArray());
        }
        public static byte[] AES256Encrypt_ForDiffieHellman(this byte[] plainData, byte[] key, byte[] nonce, byte[] associatedData = null)
        {
            if (nonce.Length != 12) throw new ArgumentOutOfRangeException(nameof(nonce));
            var tag = new byte[16];
            var cipherBytes = new byte[plainData.Length];
            if (!IsOSX)
            {
                using var cipher = new AesGcm(key);
                cipher.Encrypt(nonce, plainData, cipherBytes, tag, associatedData);
            }
            else
            {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(
                    new KeyParameter(key),
                    128, //128 = 16 * 8 => (tag size * 8)
                    nonce,
                    associatedData);
                cipher.Init(true, parameters);
                cipherBytes = new byte[cipher.GetOutputSize(plainData.Length)];
                var length = cipher.ProcessBytes(plainData, 0, plainData.Length, cipherBytes, 0);
                cipher.DoFinal(cipherBytes, length);
            }
            return Concat(nonce, cipherBytes, tag);
        }

        public static byte[] AES256Decrypt_ForDiffieHellman(this byte[] encryptedData, byte[] key, byte[] associatedData = null)
        {
            ReadOnlySpan<byte> encrypted = encryptedData;
            var nonce = encrypted[..12];
            var cipherBytes = encrypted[12..^16];
            var tag = encrypted[^16..];
            var decryptedData = new byte[cipherBytes.Length];
            if (!IsOSX)
            {
                using var cipher = new AesGcm(key);
                cipher.Decrypt(nonce, cipherBytes, tag, decryptedData, associatedData);
            }
            else
            {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(
                    new KeyParameter(key),
                    128,  //128 = 16 * 8 => (tag size * 8)
                    nonce.ToArray(),
                    associatedData);
                cipher.Init(false, parameters);
                decryptedData = new byte[cipher.GetOutputSize(cipherBytes.Length)];
                var length = cipher.ProcessBytes(cipherBytes.ToArray(), 0, cipherBytes.Length, decryptedData, 0);
                cipher.DoFinal(decryptedData, length);
            }
            return decryptedData;
        }
        public static T Encrypt<T>(this ISerializable item, KeyPair local, ECPoint remote, byte[] suffix = default) where T : EncryptData, new()
        {
            var key = ECDHDeriveKey(local, remote, suffix);
            Random random = new Random();
            var nonce = new byte[12];
            random.NextBytes(nonce);
            return new T()
            {
                Data = item.ToArray().AES256Encrypt_ForDiffieHellman(key, nonce)
            };
        }
        public static T Encrypt<T>(this ISerializable item, IEnumerable<byte> key, byte[] suffix = default) where T : EncryptData, new()
        {
            var keys = key;
            if (suffix != default)
            {
                keys = keys.Concat(suffix);
            }
            keys = Crypto.Default.Hash256(keys.ToArray());

            Random random = new Random();
            var nonce = new byte[12];
            random.NextBytes(nonce);
            return new T()
            {
                Data = item.ToArray().AES256Encrypt_ForDiffieHellman(keys.ToArray(), nonce)
            };
        }
        public static byte[] Encrypt(this byte[] plainData, KeyPair local, ECPoint remote, byte[] suffix = default)
        {
            var key = ECDHDeriveKey(local, remote, suffix);
            Random random = new Random();
            var nonce = new byte[12];
            random.NextBytes(nonce);

            return plainData.AES256Encrypt_ForDiffieHellman(key, nonce);
        }
        public static byte[] Encrypt(this byte[] plainData, IEnumerable<byte> key, byte[] suffix = default)
        {
            var keys = key;
            if (suffix != default)
            {
                keys = keys.Concat(suffix);
            }
            keys = Crypto.Default.Hash256(keys.ToArray());
            Random random = new Random();
            var nonce = new byte[12];
            random.NextBytes(nonce);

            return plainData.AES256Encrypt_ForDiffieHellman(keys.ToArray(), nonce);
        }
        public static T Decrypt<T>(this EncryptData data, KeyPair local, ECPoint remote, byte[] suffix = default) where T : ISerializable, new()
        {
            try
            {
                var key = ECDHDeriveKey(local, remote, suffix);
                var bs = data.Data.AES256Decrypt_ForDiffieHellman(key);
                if (bs.IsNullOrEmpty()) return default;
                return bs.AsSerializable<T>();
            }
            catch
            {
                return default;
            }
        }
        public static T Decrypt<T>(this EncryptData data, IEnumerable<byte> key, byte[] suffix = default) where T : ISerializable, new()
        {
            try
            {
                var keys = key;
                if (suffix != default)
                {
                    keys = keys.Concat(suffix);
                }
                keys = Crypto.Default.Hash256(keys.ToArray());
                var bs = data.Data.AES256Decrypt_ForDiffieHellman(keys.ToArray());
                if (bs.IsNullOrEmpty()) return default;
                return bs.AsSerializable<T>();
            }
            catch
            {
                return default;
            }
        }
        public static byte[] Decrypt(this byte[] encryptedData, KeyPair local, ECPoint remote, byte[] suffix = default)
        {
            try
            {
                var key = ECDHDeriveKey(local, remote, suffix);
                return encryptedData.AES256Decrypt_ForDiffieHellman(key);
            }
            catch
            {
                return default;
            }
        }
        public static byte[] Decrypt(this byte[] encryptedData, IEnumerable<byte> key, byte[] suffix = default)
        {
            try
            {
                var keys = key;
                if (suffix != default)
                {
                    keys = keys.Concat(suffix);
                }
                keys = Crypto.Default.Hash256(keys.ToArray());
                return encryptedData.AES256Decrypt_ForDiffieHellman(keys.ToArray());
            }
            catch
            {
                return default;
            }
        }
        public static UInt256 CreateRandomKey(out byte[] privateKey)
        {
            privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            //Array.Clear(privateKey, 0, privateKey.Length);
            return new UInt256(Crypto.Default.Hash256(privateKey));
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;

namespace EcoBuilder
{
    // https://stackoverflow.com/questions/15791890/c-sharp-encrypt-php-decrypt-using-rsa/29896339
    public static class Encryption
    {
        static readonly string publicKey = "<RSAKeyValue><Modulus>inCGRpoW93pLfg/zZRhGaKKPLb9XyDreCbNFDFC5Amsr+I4TxDnzWKwE0hWOV/1JvIh4B3qysxANVhCTYWx8UsjpwDQnvHqGfzgOnvTiHPzUDbAV1DOkweS59kAMBSVJSkvkegFk+YFsoYcUjxz8MvJpsd/mHz1iBV6HtAAjNgk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        private static RSACryptoServiceProvider rsaCryptoServiceProvider;
        public static string Encrypt(string inputString)
        {
            if (rsaCryptoServiceProvider == null)
            {
                rsaCryptoServiceProvider = new RSACryptoServiceProvider();
                rsaCryptoServiceProvider.FromXmlString(publicKey);
            }
            // int keySize = dwKeySize / 8;
            int keySize = rsaCryptoServiceProvider.KeySize / 8;
            byte[] bytes = Encoding.Default.GetBytes(inputString);
            // The hash function in use by the .NET RSACryptoServiceProvider here is SHA1
            // int maxLength = ( keySize ) - 2 - ( 2 * SHA1.Create().ComputeHash( rawBytes ).Length );
            int maxLength = keySize - 42;
            int dataLength = bytes.Length;
            int iterations = dataLength / maxLength;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i <= iterations; i++)
            {
                byte[] tempBytes = new byte[(dataLength - maxLength * i > maxLength) ? maxLength : dataLength - maxLength * i];
                Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0, tempBytes.Length);
                byte[] encryptedBytes = rsaCryptoServiceProvider.Encrypt(tempBytes, true);
                // Be aware the RSACryptoServiceProvider reverses the order of encrypted bytes after encryption and before decryption.
                // If you do not require compatibility with Microsoft Cryptographic API (CAPI) and/or other vendors.
                // Comment out the next line and the corresponding one in the DecryptString function.
                Array.Reverse(encryptedBytes);
                // Why convert to base 64?
                // Because it is the largest power-of-two base printable using only ASCII characters
                stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
            }
            return stringBuilder.ToString();
        }
        public static string GenerateXMLParams()
        {
            var rsa = RSA.Create();
            return rsa.ToXmlString(true);
        }
    }
}
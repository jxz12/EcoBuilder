using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder
{
    public class Postman : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMPro.TextMeshProUGUI message;
        [SerializeField] float fadeDuration;

        IEnumerator SendPost(string address, WWWForm form, Action<bool, string> ResponseCallback)
        {
            message.text = "Loading...";
            using (var p = UnityWebRequest.Post(address, form))
            {
                yield return p.SendWebRequest();
                if (p.isNetworkError)
                {
                    message.text = $"Network Error: {p.error}";
                    ResponseCallback?.Invoke(false, p.error);
                    Hide();
                }
                else if (p.isHttpError) // got to server but error occurred there
                {
                    string response;
                    switch (p.responseCode)
                    {
                    case 401: response = "Invalid username or password (401)"; break;
                    case 404: response = "Server URL could not be found (404)"; break;
                    case 409: response = "Username or email already taken (409)"; break;
                    case 412: response = "Request sent too soon (412)"; break;
                    case 500: response = "Internal server error (500)\nPlease try again later"; break;
                    case 503: response = "Could not connect to database (503)"; break;
                    default: response = "Could not connect to server ("+p.responseCode+")"; break;
                    }
                    message.text = $"HTTP Error: {p.error}";
                    ResponseCallback?.Invoke(false, response);
                    Hide();
                }
                else
                {
                    ResponseCallback?.Invoke(true, p.downloadHandler.text);
                    message.text = "Success!";
                    Hide();
                }
            }
        }
        IEnumerator postRoutine = null;
        public void Post(Dictionary<string, string> letter, Action<bool, string> ResponseCallback)
        {
            Assert.IsTrue(letter.ContainsKey("__address__"), "no __address__ given to send to");

            var form = new WWWForm();
            foreach (var line in letter)
            {
                if (line.Key == "__address__") { // reserved for URL
                    continue;
                } else {
                    form.AddField(line.Key, line.Value);
                }
            }
            StartCoroutine(postRoutine = SendPost(letter["__address__"], form, ResponseCallback));
            Show();
        }
        
        public void Show()
        {
            group.blocksRaycasts = true;
            group.interactable = true;
            group.alpha = 1;
            GetComponent<Animator>().enabled = true;
        }
        public void Hide()
        {
            group.interactable = false;
            StartCoroutine(FadeAway(fadeDuration));
        }
        IEnumerator FadeAway(float duration)
        {
            group.interactable = false;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                group.alpha = 1 - ((Time.time-startTime) / duration);
                yield return null;
            }
            group.blocksRaycasts = false;
            group.alpha = 0;
            GetComponent<Animator>().enabled = false;
        }

        //////////////////////
        // encryption stuff //
        //////////////////////
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
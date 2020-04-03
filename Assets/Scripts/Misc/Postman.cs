using UnityEngine;
using UnityEngine.UI;
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
        Queue<WWWForm> sendQueue = new Queue<WWWForm>();
        IEnumerator SendPost(string address, WWWForm form, Action<bool, string> ResponseCallback)
        {
            // message.text = "Loading...";
            sendQueue.Enqueue(form);
            while (sendQueue.Peek() != form) {
                yield return null;
            }

            using (var p = UnityWebRequest.Post(address, form))
            {
                yield return p.SendWebRequest();
                if (p.isNetworkError)
                {
                    // message.text = $"Network Error: {p.error}";
                    ResponseCallback?.Invoke(false, p.error);
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
                    // message.text = $"HTTP Error: {p.error}";
                    ResponseCallback?.Invoke(false, response);
                }
                else
                {
                    ResponseCallback?.Invoke(true, p.downloadHandler.text);
                    // message.text = "Success!";
                }
                print($"text: {p.downloadHandler.text}\nerror: {p.error}");
            }

            Assert.IsTrue(sendQueue.Peek() == form, "sendQueue was tampered with while sending");
            sendQueue.Dequeue();
        }

        
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
            StartCoroutine(SendPost(letter["__address__"], form, ResponseCallback));
            prevForm = form;
            prevAddress = letter["__address__"];
        }

        [SerializeField] Image icon;
        [SerializeField] float spinPeriod;
        [SerializeField] float fadeDuration;
        WWWForm prevForm;
        string prevAddress;

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(SendPost(prevAddress, prevForm, (b,s)=>print($"again: {s}")));
            }
#endif
            if (sendQueue.Count > 0)
            {
                float t = ((Time.time)/spinPeriod) % 1;
                icon.transform.rotation = Quaternion.Euler(0,0,360 * t);
                icon.color = new Color(0,0,0,.8f);
            }
            else
            {
                if (icon.color.a > .01f) {
                    icon.color = new Color(0,0,0, Mathf.Lerp(icon.color.a, 0, 5*Time.deltaTime));
                } else if (icon.color.a > 0) {
                    icon.color = new Color(0,0,0,0);
                }
            }
        }

        //////////////////////
        // encryption stuff //
        //////////////////////
        static readonly string publicKey = "<RSAKeyValue><Modulus>inCGRpoW93pLfg/zZRhGaKKPLb9XyDreCbNFDFC5Amsr+I4TxDnzWKwE0hWOV/1JvIh4B3qysxANVhCTYWx8UsjpwDQnvHqGfzgOnvTiHPzUDbAV1DOkweS59kAMBSVJSkvkegFk+YFsoYcUjxz8MvJpsd/mHz1iBV6HtAAjNgk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        // note: this encryption will not stop someone from decompiling the code and sending fake high scores
        //       but it will keep the data safe locally from someone getting access to their phone.

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
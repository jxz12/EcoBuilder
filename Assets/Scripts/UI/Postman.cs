using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Postman : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Button cancel, retry;
        [SerializeField] TMPro.TextMeshProUGUI message;
        [SerializeField] float fadeDuration;

        IEnumerator SendPost(string address, WWWForm form, Action<bool, string> OnResponse)
        {
            message.text = "Loading...";
            using (var p = UnityWebRequest.Post(address, form))
            {
                yield return p.SendWebRequest();
                if (p.isNetworkError)
                {
                    message.text = p.error;
                    print("TODO: don't call this yet");
                    OnResponse(false, "Could not establish an internet connection");
                    Hide();
                }
                else if (p.isHttpError)
                {
                    string response;
                    switch (p.responseCode)
                    {
                    case 401: response = "Invalid username or password"; break;
                    case 404: response = "Server URL could not be found"; break;
                    case 409: response = "Username already taken"; break;
                    case 503: response = "Could not connect to database"; break;
                    default: response = "Could not connect to server"; break;
                    }
                    message.text = p.error;
                    print("TODO: don't call this yet");
                    OnResponse(false, response);
                    Hide();
                }
                else
                {
                    OnResponse(true, p.downloadHandler.text);
                    message.text = "Success!";
                    Hide();
                }
            }
        }
        IEnumerator postRoutine = null;
        public void Post(Dictionary<string, string> letter, string address, Action<bool, string> OnResponse, bool silentFail=false)
        {
            var form = new WWWForm();
            foreach (var line in letter) {
                form.AddField(line.Key, Encrypt(line.Value));
            }
            print("TODO: silent fail");
            StartCoroutine(postRoutine = SendPost(address, form, OnResponse));
            Show();
        }
        // void CancelPost()
        // {
        //     if (postRoutine == null) {
        //         throw new Exception("no routine running");
        //     }
        //     StopCoroutine(postRoutine);
        //     Hide();
        // }
        
        public void Show()
        {
            group.blocksRaycasts = true;
            group.interactable = true;
            group.alpha = 1;
            print("TODO: nice loading icon");
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
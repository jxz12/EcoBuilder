using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class PrivacyPolicy : MonoBehaviour, IPointerDownHandler
    {

#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void OpenURL();
        public void OnPointerDown(PointerEventData ped)
        {
            OpenURL(DataManager.Instance.ServerURL+"GDPR_Privacy_Notice.html");
        }
#else
        public void OnPointerDown(PointerEventData ped)
        {
            // empty as no javascript needed
        }
        void Start()
        {
            GetComponent<Button>().onClick.AddListener(()=> Application.OpenURL(GameManager.Instance.ServerURL+"GDPR_Privacy_Notice.html"));
        }
#endif
    }
}
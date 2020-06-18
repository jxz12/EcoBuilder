using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.UI
{
    // from https://connect.unity.com/p/updating-your-gui-for-the-iphone-x-and-other-notched-devices
    // and https://assetstore.unity.com/packages/tools/gui/safe-area-helper-130488
    public class SafeArea : MonoBehaviour
    {
        void Start()
        {
            var panel = GetComponent<RectTransform>();
            Assert.IsNotNull(panel);
            var r = GetSafeArea();
            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
        }
    #if !UNITY_EDITOR
        Rect GetSafeArea()
        {
            return Screen.safeArea;
        }
    #else
        public SimDevice device;
        Rect GetSafeArea()
        {
            // normalised safe area
            Rect nsa;
            bool portrait = Screen.height > Screen.width;
            switch (device)
            {
            case SimDevice.iPhoneX:
                nsa = portrait? NSA_iPhoneX[0] : nsa = NSA_iPhoneX[1];
                break;
            case SimDevice.iPhoneXsMax:
                nsa = portrait? NSA_iPhoneXsMax[0] : nsa = NSA_iPhoneXsMax[1];
                break;
            case SimDevice.Pixel3XL_LSL:
                nsa = portrait? NSA_Pixel3XL_LSL[0] : nsa = NSA_Pixel3XL_LSL[1];
                break;
            case SimDevice.Pixel3XL_LSR:
                nsa = portrait? NSA_Pixel3XL_LSR[0] : nsa = NSA_Pixel3XL_LSR[1];
                break;
            default:
                return Screen.safeArea;
            }
            return new Rect(Screen.width*nsa.x, Screen.height*nsa.y, Screen.width*nsa.width, Screen.height*nsa.height);
        }
        public enum SimDevice
        {
            /// <summary>
            /// Don't use a simulated safe area - GUI will be full screen as normal.
            /// </summary>
            None,
            /// <summary>
            /// Simulate the iPhone X and Xs (identical safe areas).
            /// </summary>
            iPhoneX,
            /// <summary>
            /// Simulate the iPhone Xs Max and XR (identical safe areas).
            /// </summary>
            iPhoneXsMax,
            /// <summary>
            /// Simulate the Google Pixel 3 XL using landscape left.
            /// </summary>
            Pixel3XL_LSL,
            /// <summary>
            /// Simulate the Google Pixel 3 XL using landscape right.
            /// </summary>
            Pixel3XL_LSR
        }
        Rect[] NSA_iPhoneX = new Rect[]
        {
            new Rect (0f, 102f / 2436f, 1f, 2202f / 2436f),  // Portrait
            new Rect (132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f)  // Landscape
        };
        Rect[] NSA_iPhoneXsMax = new Rect[]
        {
            new Rect (0f, 102f / 2688f, 1f, 2454f / 2688f),  // Portrait
            new Rect (132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f)  // Landscape
        };
        Rect[] NSA_Pixel3XL_LSL = new Rect[]
        {
            new Rect (0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new Rect (0f, 0f, 2789f / 2960f, 1f)  // Landscape
        };
        Rect[] NSA_Pixel3XL_LSR = new Rect[]
        {
            new Rect (0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new Rect (171f / 2960f, 0f, 2789f / 2960f, 1f)  // Landscape
        };
    #endif
    }
}
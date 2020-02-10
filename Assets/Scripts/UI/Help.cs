using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace EcoBuilder.UI
{
    public class Help : MonoBehaviour
    {
        [SerializeField] VerticalLayoutGroup panelLayout;
        [SerializeField] ContentSizeFitter panelFitter;
        [SerializeField] TMPro.TextMeshProUGUI message;
        [SerializeField] Button hideButton, showButton;
        [SerializeField] Image arrow;

        public event Action OnUserShown;

        RectTransform rt;
        Vector2 targetAnchor, targetPivot;
        Vector2 anchosity, pivosity;
        float width, targetWidth, widthocity;
        Rect canvasRect;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            targetPivot = rt.pivot;
            targetAnchor = rt.anchorMin;
            targetWidth = rt.sizeDelta.x;
            canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect;

            // prevents scene getting dirty, Unity autolayout sucks
            message.transform.parent.GetComponent<ContentSizeFitter>().enabled = true;
        }
        void Start()
        {
            hideButton.onClick.AddListener(Toggle);
            showButton.onClick.AddListener(Toggle);
        }
        // because unity autolayout suckssss with the text box thing
        void ForceUpdateLayout()
        {
            Canvas.ForceUpdateCanvases();
            panelLayout.CalculateLayoutInputVertical();
            panelLayout.SetLayoutVertical();
            panelFitter.SetLayoutVertical();
        }
        public void SetPixelWidth(float pixelWidth, bool damp=true)
        {
            targetWidth = pixelWidth;
            if (!damp) {
                rt.sizeDelta = new Vector2(targetWidth, rt.sizeDelta.y);
            }
            ForceUpdateLayout();
        }
        public void SetAnchorHeight(float normalisedHeight, bool damp=true) // 0-1 range
        {
            targetAnchor = new Vector2(targetAnchor.x, normalisedHeight);
            if (!damp) {
                rt.anchorMin = rt.anchorMin = targetAnchor;
            }
        }
        public void SetPivotHeight(float normalisedHeight, bool damp=true)
        {
            targetPivot.y = normalisedHeight;
            if (!damp) {
                rt.pivot = new Vector2(rt.pivot.x, targetPivot.y);
            }
        }

        public void SetSide(bool left, bool damp=true)
        {
            if (left && targetAnchor.x==0 || !left && targetAnchor.x==1) {
                return;
            }
            if (left)
            {
                targetAnchor = new Vector2(0, targetAnchor.y);
                transform.localScale = new Vector3(-1,1,1);
                message.transform.localScale = new Vector3(-1,1,1);
                hideButton.transform.localScale = new Vector3(-1,1,1);
            }
            else
            {
                targetAnchor = new Vector2(1, targetAnchor.y);
                transform.localScale = new Vector3(1,1,1);
                message.transform.localScale = new Vector3(1,1,1);
                hideButton.transform.localScale = new Vector3(1,1,1);
            }
            if (!damp)
            {
                rt.anchorMin = rt.anchorMax = targetAnchor;
            }
        }
        public bool Showing {
            get { return targetPivot.x==1; }
            set {
                // only toggle if needed
                if (!Showing && value || Showing && !value) {
                    Toggle(); 
                }
            }
        }
        public void Toggle()
        {
            // flips pivot to opposite of current state
            if (targetPivot.x == 1) {
                targetPivot.x = 0;
                arrow.transform.rotation = Quaternion.Euler(0,0,-90);
            } else {
                targetPivot.x = 1;
                arrow.transform.rotation = Quaternion.Euler(0,0,90);
            }
        }

        public string Message {
            get { return message.text; }
            set { message.text = value; ForceUpdateLayout(); }
        }
        public void DelayThenSet(float delay, string delayedMessage)
        {
            StopAllCoroutines();
            StartCoroutine(DelayThenSetRoutine(delay, delayedMessage));
        }
        IEnumerator DelayThenSetRoutine(float delay, string delayedMessage)
        {
            yield return new WaitForSeconds(delay);
            Message = delayedMessage;
        }
        public void DelayThenShow(float delay, string delayedMessage)
        {
            StopAllCoroutines();
            StartCoroutine(DelayThenShowRoutine(delay, delayedMessage));
        }
        IEnumerator DelayThenShowRoutine(float delay, string delayedMessage)
        {
            yield return new WaitForSeconds(delay);
            Message = delayedMessage;
            Showing = true;
        }
        public void ResetPosition()
        {
            SetSide(false);
            SetPivotHeight(1);
            SetAnchorHeight(.85f);
            SetPixelWidth(400);
        }

        void UserShow(bool showing) // to attach to button
        {
            Showing = showing;
            OnUserShown?.Invoke();
        }
        [SerializeField] float smoothTime = .15f;
        void Update()
        {
            rt.pivot = Vector2.SmoothDamp(rt.pivot, targetPivot, ref pivosity, smoothTime);
            rt.anchorMax = rt.anchorMin = Vector2.SmoothDamp(rt.anchorMin, targetAnchor, ref anchosity, smoothTime);

            width = Mathf.SmoothDamp(width, targetWidth, ref widthocity, smoothTime);
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }
    }
}
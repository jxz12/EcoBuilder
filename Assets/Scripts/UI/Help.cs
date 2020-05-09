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

            // enabling here prevents scene getting dirty, Unity autolayout sucks
            panelLayout.enabled = true;
            panelFitter.enabled = true;
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
                width = targetWidth;
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            }
            ForceUpdateLayout();
        }
        public void SetAnchorHeight(float normalisedHeight, bool damp=true) // 0-1 range
        {
            targetAnchor = new Vector2(targetAnchor.x, normalisedHeight);
            if (!damp) {
                rt.anchorMin = rt.anchorMax = targetAnchor;
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
            else
            {
                rt.pivot = new Vector2(1-rt.pivot.x, rt.pivot.y);
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
                arrow.transform.localRotation = Quaternion.Euler(0,180,90);
            } else {
                targetPivot.x = 1;
                arrow.transform.localRotation = Quaternion.Euler(0,0,90);
            }
        }

        // TODO: this is a smelly mess
        public string Message {
            get { return message.text; }
            set { StopAllCoroutines(); message.text = value; ForceUpdateLayout(); }
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
            Showing = false;
            StartCoroutine(DelayThenShowRoutine(delay, delayedMessage!=null? delayedMessage : Message));
        }
        IEnumerator DelayThenShowRoutine(float delay, string delayedMessage)
        {
            yield return new WaitForSeconds(delay);
            Message = delayedMessage;
            Showing = true;
        }
        public void ResetMenuPosition(bool damp=false, bool forceHide=true)
        {
            SetSide(false, damp);
            SetPivotHeight(1, damp);
            SetAnchorHeight(.8f, damp);
            SetPixelWidth(400, damp);

            if (forceHide)
            {
                if (Showing) {
                    Toggle();
                }
                rt.pivot = new Vector2(0, rt.pivot.y);
            }
        }
        public void ResetLevelPosition(bool damp=true)
        {
            SetSide(false, damp);
            SetPivotHeight(1, damp);
            SetAnchorHeight(.88f, damp);
            SetPixelWidth(350, damp);
        }

        void UserShow(bool showing) // to attach to button
        {
            Showing = showing;
            OnUserShown?.Invoke();
        }
        [SerializeField] float smoothTime = .15f;
        void Update()
        {
            // these magnitudes are used as threshold to stop the layout being dirtied if arbitrarily close
            float pivotMag = (rt.pivot - targetPivot).sqrMagnitude;
            if (pivotMag > .000001f) {
                rt.pivot = Vector2.SmoothDamp(rt.pivot, targetPivot, ref pivosity, smoothTime);
            } else if (pivotMag > 0) {
                rt.pivot = targetPivot;
            }
            float anchorMag = (rt.anchorMin - targetAnchor).sqrMagnitude;
            if (anchorMag > .000001f) {
                rt.anchorMax = rt.anchorMin = Vector2.SmoothDamp(rt.anchorMin, targetAnchor, ref anchosity, smoothTime);
            } else if (anchorMag > 0) {
                rt.anchorMax = rt.anchorMin = targetAnchor;
            }
            float widthMag = width-targetWidth;
            widthMag *= widthMag;
            if (widthMag > 1) {
                width = Mathf.SmoothDamp(width, targetWidth, ref widthocity, smoothTime);
            } else if (widthMag > 0) {
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            }
        }
    }
}
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

        public event Action OnUserShown;

        RectTransform rt;
        Vector2 targetPos, targetAnchor;
        Vector2 velocity, anchosity;
        Rect canvasRect;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            targetPos = rt.anchoredPosition;
            targetAnchor = rt.anchorMin;
            canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect;
        }
        void Start()
        {
            hideButton.onClick.AddListener(()=>UserShow(false));
            showButton.onClick.AddListener(()=>UserShow(true));

            StartCoroutine(DelayThenShow(2, true));
        }
        void ForceUpdateLayout()
        {
            Canvas.ForceUpdateCanvases();
            panelLayout.CalculateLayoutInputVertical();
            panelLayout.SetLayoutVertical();
            panelFitter.SetLayoutVertical();
        }
        public void SetText(string toSet)
        {
            message.text = toSet;
            ForceUpdateLayout();
        }
        public void SetDistFromTop(float normalisedHeight, bool damp=true) // 0-1 range
        {
            targetPos.y = canvasRect.height * -normalisedHeight;
            // rt.anchorMin = rt.anchorMax = 
            if (!damp)
            {
                rt.anchoredPosition = targetPos;
            }
        }
        public void SetWidth(float normalisedWidth)
        {
            // float x = isLeft? -canvasRefRes.x*width : canvasRefRes.x*width;
            float prevWidth = rt.sizeDelta.x;
            float newWidth = canvasRect.width * normalisedWidth;
            targetPos.x *= newWidth / prevWidth;
            rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);

            ForceUpdateLayout();
        }
        bool isLeft;
        public void SetSide(bool left, bool damp=true)
        {
            if (left == isLeft)
                return;

            var rt = GetComponent<RectTransform>();
            if (left)
            {
                targetAnchor = new Vector2(0,1);
                transform.localScale = new Vector3(-1,1,1);
                message.transform.localScale = new Vector3(-1,1,1);
                hideButton.transform.localScale = new Vector3(-1,1,1);
            }
            else
            {
                targetAnchor = new Vector2(1,1);
                transform.localScale = new Vector3(1,1,1);
                message.transform.localScale = new Vector3(1,1,1);
                hideButton.transform.localScale = new Vector3(1,1,1);
            }
            targetPos.x = -targetPos.x;
            if (!damp)
            {
                rt.anchoredPosition = targetPos;
                rt.anchorMin = rt.anchorMax = targetAnchor;
            }
            isLeft = left;
        }
        public void Show(bool showing)
        {
            // GetComponent<Animator>().SetBool("Show", showing);
            if (showing)
            {
                targetPos.x = 0;
            }
            else
            {
                targetPos.x = isLeft? -rt.rect.width : rt.rect.width;
            }
        }
        public void DelayThenShow(float seconds, string msg)
        {
            Show(false);
            SetText(GameManager.Instance.PlayedLevel.Details.congratulation);
            StartCoroutine(DelayThenShow(seconds, true));
        }
        void UserShow(bool showing) // to attach to button
        {
            Show(showing);
            OnUserShown?.Invoke();
        }
        void Update()
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref velocity, .15f);
            rt.anchorMax = rt.anchorMin = Vector2.SmoothDamp(rt.anchorMin, targetAnchor, ref anchosity, .15f);
        }
        // public void DelayThenShow(float seconds)
        // {
        //     // GetComponent<Animator>().SetTrigger("Reset");
        //     // GetComponent<Animator>().SetBool("Show", true);
        //     StartCoroutine(DelayThenShow(seconds, true));
        // }
        IEnumerator DelayThenShow(float seconds, bool showing)
        {
            yield return new WaitForSeconds(seconds);
            Show(showing);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace EcoBuilder.UI
{
    public class Help : MonoBehaviour
    {
        [SerializeField] Text text;
        [SerializeField] VerticalLayoutGroup panelLayout;
        [SerializeField] ContentSizeFitter panelFitter;
        [SerializeField] Text message;
        [SerializeField] Button hideButton, showButton;

        RectTransform rt;
        Vector2 targetPos;
        Vector2 velocity;
        Vector2 canvasRefRes;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            targetPos = rt.anchoredPosition;
            canvasRefRes = GetComponentInParent<Canvas>().GetComponent<RectTransform>().sizeDelta;
        }
        void Start()
        {
            hideButton.onClick.AddListener(()=>Show(false));
            showButton.onClick.AddListener(()=>Show(true));

            DelayThenShow(2);
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
            text.text = toSet;
            ForceUpdateLayout();
        }
        public void SetDistFromTop(float height, bool damp=true) // 0-1 range
        {
            targetPos.y = canvasRefRes.y * -height;
            if (!damp)
            {
                rt.anchoredPosition = targetPos;
            }
        }
        public void SetWidth(float width)
        {
            // float x = isLeft? -canvasRefRes.x*width : canvasRefRes.x*width;
            float prevWidth = rt.sizeDelta.x;
            float newWidth = canvasRefRes.x * width;
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
                rt.anchorMin = rt.anchorMax = new Vector2(0,1);
                transform.localScale = new Vector3(-1,1,1);
                message.transform.localScale = new Vector3(-1,1,1);
                hideButton.transform.localScale = new Vector3(-1,1,1);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(1,1);
                transform.localScale = new Vector3(1,1,1);
                message.transform.localScale = new Vector3(1,1,1);
                hideButton.transform.localScale = new Vector3(1,1,1);
            }
            targetPos.x = -targetPos.x;
            if (!damp)
            {
                rt.anchoredPosition = targetPos;
            }
            isLeft = left;
        }
        public void Show(bool showing)
        {
            // GetComponent<Animator>().SetBool("Show", showing);
            if (showing)
                targetPos.x = 0;
            else
                targetPos.x = isLeft? -rt.rect.width : rt.rect.width;
        }
        void FixedUpdate()
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref velocity, .15f);
        }
        public void DelayThenShow(float seconds)
        {
            // GetComponent<Animator>().SetTrigger("Reset");
            // GetComponent<Animator>().SetBool("Show", true);
            StartCoroutine(DelayThenShow(seconds, true));
        }
        IEnumerator DelayThenShow(float seconds, bool showing)
        {
            yield return new WaitForSeconds(seconds);
            Show(showing);
        }
    }
}
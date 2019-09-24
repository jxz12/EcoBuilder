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
        void Start()
        {
            hideButton.onClick.AddListener(()=>Show(false));
            showButton.onClick.AddListener(()=>Show(true));
            rt = GetComponent<RectTransform>();
            targetPos = rt.anchoredPosition;
            canvasRefRes = GetComponentInParent<CanvasScaler>().referenceResolution;
            DelayThenShow();
        }
        public void SetText(string toSet)
        {
            text.text = toSet;
            Canvas.ForceUpdateCanvases();
            panelLayout.CalculateLayoutInputVertical();
            panelLayout.SetLayoutVertical();
            panelFitter.SetLayoutVertical();
        }
        public void SetDistFromTop(float height) // 0-1 range
        {
            targetPos.y = canvasRefRes.y * -height;
        }
        public void SetWidth(float width)
        {
            // float x = isLeft? -canvasRefRes.x*width : canvasRefRes.x*width;
            rt.rect.Set(0, 0, canvasRefRes.x*width, 0);
        }
        bool isLeft;
        public void SetSide(bool left, bool damp=false)
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
        public void DelayThenShow()
        {
            // GetComponent<Animator>().SetTrigger("Reset");
            // GetComponent<Animator>().SetBool("Show", true);
            StartCoroutine(DelayThenShow(2, true));
        }
        IEnumerator DelayThenShow(float seconds, bool showing)
        {
            yield return new WaitForSeconds(seconds);
            Show(showing);
        }
    }
}
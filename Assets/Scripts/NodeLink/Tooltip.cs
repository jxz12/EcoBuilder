using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace EcoBuilder.NodeLink
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] Image tip;
        [SerializeField] Sprite inspectSprite;
        [SerializeField] Sprite linkSprite, addlinkSprite, unlinkSprite, bannedSprite;
        [SerializeField] Sprite trashSprite, notrashSprite;
        [SerializeField] TMPro.TextMeshProUGUI text;

        [SerializeField] float sizeSmoothTime;
        float size=0, sizocity=0;

        Canvas canvas;
        void Start()
        {
            canvas = GetComponent<Canvas>();
        }
        void Update()
        {
            if (visible) {
                size = Mathf.SmoothDamp(size, 1, ref sizocity, sizeSmoothTime);
                canvas.enabled = true;
            } else {
                size = Mathf.SmoothDamp(size, 0, ref sizocity, sizeSmoothTime);
                if (size < 1e-10f) {
                    size = 0; // prevents invalid AABB error
                    canvas.enabled = false;
                }
            }
            tip.transform.localScale = new Vector3(size, size, 1);
        }
        public void SetPos(Vector2 screenPos)
        {
            tip.transform.position = screenPos;
        }
        bool visible = false;
        public void Enable()
        {
            visible = true;
        }
        public void Disable()
        {
            visible = false;
        }
        Sprite Sprite {
            set { tip.enabled = true; tip.sprite = value; text.text = ""; }
        }
        public void ShowInspect()
        {
            Sprite = inspectSprite;
        }
        public void ShowLink()
        {
            Sprite = linkSprite;
        }
        public void ShowUnlink()
        {
            Sprite = unlinkSprite;
        }
        public void ShowAddLink()
        {
            Sprite = addlinkSprite;
        }
        public void ShowBanned()
        {
            Sprite = bannedSprite;
        }
        public void ShowTrash()
        {
            Sprite = trashSprite;
        }
        public void ShowNoTrash()
        {
            Sprite = notrashSprite;
        }

        string Text {
            set { text.text = value; tip.enabled = false; }
        }
        public void ShowText(string message)
        {
            Text = message;
        }
    }
}
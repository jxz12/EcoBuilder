using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
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


        void Update()
        {
            if (visible) {
                size = Mathf.SmoothDamp(size, 1, ref sizocity, sizeSmoothTime);
            } else {
                size = Mathf.SmoothDamp(size, 0, ref sizocity, sizeSmoothTime);
            }
            transform.localScale = new Vector2(size, size);
        }
        public void SetPos(Vector2 screenPos)
        {
            transform.position = screenPos;
        }
        bool visible = false;
        public void Enable()
        {
            if (!visible)
            {
                visible = true;
                transform.localScale = Vector3.zero;
            }
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
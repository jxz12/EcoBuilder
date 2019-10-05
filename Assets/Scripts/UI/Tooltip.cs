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
        [SerializeField] Text text;

		[SerializeField] float scaleLerp;

		void Update()
		{
			if (visible)
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, scaleLerp);
			else
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, scaleLerp);
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
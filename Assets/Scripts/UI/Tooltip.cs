using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Tooltip : MonoBehaviour
	{
        [SerializeField] Image tip;
        [SerializeField] Sprite inspectSprite;
        [SerializeField] Sprite linkSprite, addlinkSprite, unlinkSprite;
		[SerializeField] Sprite noLinkSprite, noAddlinkSprite, noUnlinkSprite;
        [SerializeField] Sprite trashSprite, notrashSprite;

		void FixedUpdate()
		{
			if (visible)
			{
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, .2f);
			}
			else
			{
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, .2f);
			}
			transform.position = Vector3.Lerp(transform.position, target, .5f);
		}
		Vector2 target;
		public void SetPos(Vector2 screenPos, bool noLerp=false)
		{
			target = screenPos;
			if (noLerp)
			{
				transform.position = target;
			}
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
		public void ShowInspect()
		{
			tip.sprite = inspectSprite;
		}
		public void ShowLink()
		{
			tip.sprite = linkSprite;
			// TODO: these colours might be solved in a later version of unity
			// tip.color = Color.black;
		}
		public void ShowNoLink()
		{
			tip.sprite = noLinkSprite;
			// tip.color = Color.black;
		}
		public void ShowAddLink()
		{
			tip.sprite = addlinkSprite;
			tip.color = Color.green;
		}
		public void ShowNoAddLink()
		{
			tip.sprite = noAddlinkSprite;
			// tip.color = Color.green;
		}
		public void ShowUnlink()
		{
			tip.sprite = unlinkSprite;
			// tip.color = Color.red;
		}
		public void ShowNoUnlink()
		{
			tip.sprite = noUnlinkSprite;
			// tip.color = Color.red;
		}
		public void ShowTrash()
		{
			tip.sprite = trashSprite;
			// tip.color = Color.black;
		}
		public void ShowNoTrash()
		{
			tip.sprite = notrashSprite;
			// tip.color = Color.black;
		}
	}
}
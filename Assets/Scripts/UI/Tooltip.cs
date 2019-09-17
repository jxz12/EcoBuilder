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

		[SerializeField] float scaleLerp;

		void FixedUpdate()
		{
			if (visible)
			{
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, scaleLerp);
			}
			else
			{
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, scaleLerp);
			}
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
		public void ShowInspect()
		{
			tip.sprite = inspectSprite;
		}
		public void ShowLink()
		{
			tip.sprite = linkSprite;
		}
		public void ShowUnlink()
		{
			tip.sprite = unlinkSprite;
		}
		public void ShowBanned()
		{
			tip.sprite = bannedSprite;
		}
		public void ShowTrash()
		{
			tip.sprite = trashSprite;
		}
		public void ShowNoTrash()
		{
			tip.sprite = notrashSprite;
		}
	}
}
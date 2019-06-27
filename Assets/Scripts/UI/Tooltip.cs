using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Tooltip : MonoBehaviour
	{
        [SerializeField] Image tip;
        [SerializeField] Sprite linkSprite, addlinkSprite, unlinkSprite;
		[SerializeField] Sprite noLinkSprite, noAddlinkSprite, noUnlinkSprite;
        [SerializeField] Sprite trashSprite, notrashSprite;

		void Update()
		{
			transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, .01f);
		}
		public void SetPos(Vector2 screenPos)
		{
			transform.position = screenPos;
		}
		public void Enable(bool enabled)
		{
			gameObject.SetActive(enabled);
		}
		public void ShowLink()
		{
			tip.sprite = linkSprite;
			// TODO: these colours might be solved in a later version of unity
			// tip.color = Color.black;
			transform.localScale = Vector3.one;
		}
		public void ShowNoLink()
		{
			tip.sprite = noLinkSprite;
			// tip.color = Color.black;
			transform.localScale = Vector3.one;
		}
		public void ShowAddLink()
		{
			tip.sprite = addlinkSprite;
			// tip.color = Color.green;
			transform.localScale = Vector3.one;
		}
		public void ShowNoAddLink()
		{
			tip.sprite = noAddlinkSprite;
			// tip.color = Color.green;
			transform.localScale = Vector3.one;
		}
		public void ShowUnlink()
		{
			tip.sprite = unlinkSprite;
			// tip.color = Color.red;
			transform.localScale = Vector3.one;
		}
		public void ShowNoUnlink()
		{
			tip.sprite = noUnlinkSprite;
			// tip.color = Color.red;
			transform.localScale = Vector3.one;
		}
		public void ShowTrash()
		{
			tip.sprite = trashSprite;
			// tip.color = Color.black;
			transform.localScale = Vector3.zero;
		}
		public void ShowNoTrash()
		{
			tip.sprite = notrashSprite;
			// tip.color = Color.black;
			transform.localScale = Vector3.one;
		}
	}
}
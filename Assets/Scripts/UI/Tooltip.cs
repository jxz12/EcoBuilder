using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Tooltip : MonoBehaviour
	{
        [SerializeField] Image tip;
        [SerializeField] Sprite linkSprite, addlinkSprite, unlinkSprite, nolinkSprite;

		public void SetPos(Vector2 screenPos)
		{
			transform.position = screenPos;
		}
		public void Enable(bool enabled)
		{
			tip.enabled = enabled;
		}
		public void ShowLink()
		{
			tip.sprite = linkSprite;
			// TODO: these colours might be solved in a later version of unity
			// tip.color = Color.black;
		}
		public void ShowAddLink()
		{
			tip.sprite = addlinkSprite;
			// tip.color = Color.green;
		}
		public void ShowUnLink()
		{
			tip.sprite = unlinkSprite;
			// tip.color = Color.red;
		}
		public void ShowNoLink()
		{
			tip.sprite = nolinkSprite;
			// tip.color = Color.black;
		}
	}
}
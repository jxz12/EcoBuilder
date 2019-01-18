using UnityEngine;

namespace EcoBuilder.Nichess
{
	[RequireComponent(typeof(MeshRenderer))]
	public class Marker : MonoBehaviour
	{
		public Color Col {
			set { mr.material.color = new Color(value.r, value.g, value.b, mr.material.color.a); }
		}
		public int Order {
			set { mr.material.renderQueue = defaultRenderQueue - value; }
		}
		public float Size {
			get { return transform.localScale.x; }
			set { transform.localScale = new Vector3(value, value, value); }
		}
		MeshRenderer mr;
		int defaultRenderQueue;
		void Awake()
		{
			mr = GetComponent<MeshRenderer>();
			defaultRenderQueue = mr.material.renderQueue;
		}
		// public void ChangeColor(Color col)
		// {
		// 	Col = col;
		// }
	}
}
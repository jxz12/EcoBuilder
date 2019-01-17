using UnityEngine;

namespace EcoBuilder.Nichess
{
	[RequireComponent(typeof(MeshRenderer))]
	public class Marker : MonoBehaviour
	{
		public Color Col {
			set { mr.material.color = value; }
		}
		public int Layer {
			set { mr.material.renderQueue = value + defaultRenderQueue; }
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
	}
}
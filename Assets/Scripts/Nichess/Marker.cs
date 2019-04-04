using UnityEngine;
using System;

namespace EcoBuilder.Nichess
{
	[RequireComponent(typeof(MeshRenderer))]
	public class Marker : MonoBehaviour
	{
		private Color Col {
			set { mr.material.color = new Color(value.r, value.g, value.b, mr.material.color.a); }
		}
		public int ConIdx {
			get; private set;
		}
		public int RenderOrder {
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
		
		Action<Color> colorAction;
		public void AttachPiece(Piece toAttach)
		{
			ConIdx = toAttach.Idx;
			Col = toAttach.Col;
			colorAction = (c)=> Col = c;
			toAttach.OnColoured += colorAction;
		}
		public void DetachPiece(Piece toDetach)
		{
			// toDetach.OnPosChanged -= colorAction;
			toDetach.OnColoured -= colorAction;
		}
	}
}
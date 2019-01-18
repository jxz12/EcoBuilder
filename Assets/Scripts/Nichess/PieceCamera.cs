using UnityEngine;

namespace EcoBuilder.Nichess
{
	public class PieceCamera : MonoBehaviour
	{
		public void ViewBoard(Board board)
		{
			transform.SetParent(board.transform);
			transform.localPosition = Vector3.up;
		}
		public void ViewPiece(Piece toView)
		{
			// transform.position = toView.transform.position;
			transform.SetParent(toView.transform);
			transform.localPosition = 3*Vector3.up;
		}
		// public void ViewSquare(Square toView)
		// {
		// 	transform.position = toView.transform.position;
		// 	transform.localPosition += Vector3.up * .5f;
		// }
	}
}
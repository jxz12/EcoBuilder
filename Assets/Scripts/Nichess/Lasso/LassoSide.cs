using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CapsuleCollider))]
public class LassoSide : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [SerializeField] LassoCorner cornerAdj1;
    [SerializeField] LassoCorner cornerAdj2;
    [SerializeField] Lasso lasso;

    // this object is kept on the plane floor, and moves in 
    static readonly Plane floor = new Plane(Vector3.up, Vector3.zero);

    public CapsuleCollider Capsule { get; private set; }
    Vector3 moveDirection;

    private void Awake()
    {
        Vector3 lineDirection = transform.TransformVector(Vector3.up).normalized;
        moveDirection = Vector3.Cross(lineDirection, Vector3.up);
        Capsule = GetComponent<CapsuleCollider>();
    }
    public void MatchCorners()
    {
        Vector3 v1 = cornerAdj1.transform.localPosition, v2 = cornerAdj2.transform.localPosition;

        transform.localPosition = (v1 + v2) / 2;
        Capsule.height = (v1 - v2).magnitude;
    }

    Vector3 dragStart, cornerStart1, cornerStart2;
    public void OnBeginDrag(PointerEventData ped)
    {
        //Ray ray = Camera.main.ScreenPointToRay(ped.position);
        //float enter;
        //if (floor.Raycast(ray, out enter))
        //{
        //    dragStart = ray.GetPoint(enter);
        //    cornerStart1 = cornerAdj1.transform.position;
        //    cornerStart2 = cornerAdj2.transform.position;
        //}
        //else
        //    throw new System.Exception("camera facing wrong direction");
    }
    public void OnDrag(PointerEventData ped)
    {
        //Ray ray = Camera.main.ScreenPointToRay(ped.position);
        //float enter;
        //if (floor.Raycast(ray, out enter))
        //{
        //    Vector3 hitPoint = ray.GetPoint(enter);
        //    Vector3 delta = hitPoint - dragStart;
        //    delta = Vector3.Dot(delta, moveDirection) * moveDirection;

        //    cornerAdj1.transform.position = cornerStart1 + delta;
        //    cornerAdj2.transform.position = cornerStart2 + delta;

        //    lasso.MatchAndSnap();
        //}
        //else
        //    throw new System.Exception("camera facing wrong direction");
    }
}
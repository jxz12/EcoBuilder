using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SphereCollider))]
public class LassoCorner : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler//, IPointerEnterHandler
{
    [SerializeField] Lasso lasso;


    public SphereCollider Sphere { get; private set; }
    public Square Location { get; set; }

    private void Awake()
    {
        Sphere = GetComponent<SphereCollider>();
    }
    public void OnDrag(PointerEventData ped)
    {
        Ray ray = Camera.main.ScreenPointToRay(ped.position);
        RaycastHit hit;
        int layerMask = 1 << 8; // should be 'Board' layer
        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            Square hitSquare = hit.transform.GetComponent<Square>();
            if (hitSquare != null && hitSquare != Location)
            {
                Square prevLocation = Location;
                Location = hitSquare;
                lasso.MatchAndSnap(this);
                lasso.MatchNiche(this, prevLocation);
            }
        }
    }
    public void OnBeginDrag(PointerEventData ped)
    {
        lasso.ChangeLayer(2); // 2 should be ignoreraycast
    }
    public void OnEndDrag(PointerEventData ped)
    {
        lasso.ChangeLayer(0); // 2 should be ignoreraycast
    }
}
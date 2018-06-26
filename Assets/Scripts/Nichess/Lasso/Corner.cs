using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SphereCollider))]
public class Corner : MonoBehaviour, IDragHandler
{
    [SerializeField] Corner xMatch;
    [SerializeField] Corner zMatch;
    [SerializeField] Lasso lasso;

    // this object is kept on a plane across the world middle
    static readonly Plane floor = new Plane(Vector3.up, Vector3.zero);

    public SphereCollider Sphere { get; private set; }

    private void Awake()
    {
        Sphere = GetComponent<SphereCollider>();
    }
    public void OnDrag(PointerEventData ped)
    {
        Ray ray = Camera.main.ScreenPointToRay(ped.position);
        float enter;
        if (floor.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            transform.position = hitPoint;
            xMatch.transform.localPosition = new Vector3(transform.localPosition.x, 0, xMatch.transform.localPosition.z);
            zMatch.transform.localPosition = new Vector3(zMatch.transform.localPosition.x, 0, transform.localPosition.z);

            lasso.MatchAndSnap();
        }
    }
}
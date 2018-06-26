using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
public class Lasso : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Corner[] corners = new Corner[4];
    [SerializeField] Side[] sides = new Side[4];

    [SerializeField] float sideWidth=.05f;
    [SerializeField] float cornerRadius=.1f;

    LineRenderer lr;
    public Func<Vector3, Vector3> Snap { get; private set; }
    public Color Col {
        get { return lr.startColor; }
        set { lr.startColor = lr.endColor = value; }
    }
    float defaultAlpha;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        defaultAlpha = lr.material.color.a;
    }
    private void Start()
    {
        foreach (Corner c in corners)
            c.Sphere.radius = cornerRadius;
        foreach (Side s in sides)
            s.Capsule.radius = sideWidth;

        lr.positionCount = 4;
        int size = GameManager.Instance.BoardSize;
        float offset = .5f / size;
        Snap = v => new Vector3(Mathf.Floor(v.x * size) / size + offset, 0, Mathf.Floor(v.z * size) / size + offset);
    }
    public void OnPointerEnter(PointerEventData ped)
    {
        Color c = lr.material.color;
        c.a = 1;
        lr.material.color = c;
    }
    public void OnPointerExit(PointerEventData ped)
    {
        // TODO: this is buggy and doesn't end when the cursor isn't quite over (because of snapping)
        if (ped.dragging == false)
        {
            Color c = lr.material.color;
            c.a = defaultAlpha;
            lr.material.color = c;
        }
    }

    public void MatchAndSnap()
    {
        for (int i = 0; i < 4; i++)
        {
            corners[i].transform.localPosition = Snap(corners[i].transform.localPosition);
            lr.SetPosition(i, corners[i].transform.localPosition);
        }
        foreach (Side s in sides)
            s.MatchCorners();
    }
}

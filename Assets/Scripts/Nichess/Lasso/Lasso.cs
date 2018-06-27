using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
public class Lasso : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Board board;

    [SerializeField] LassoCorner cornerBL;
    [SerializeField] LassoCorner cornerTL;
    [SerializeField] LassoCorner cornerTR;
    [SerializeField] LassoCorner cornerBR;
    [SerializeField] LassoSide sideL;
    [SerializeField] LassoSide sideT;
    [SerializeField] LassoSide sideR;
    [SerializeField] LassoSide sideB;

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
        corners = new LassoCorner[4];
        sides = new LassoSide[4];

        corners[0] = cornerBL;
        corners[1] = cornerTL;
        corners[2] = cornerTR;
        corners[3] = cornerBR;
        sides[0] = sideL;
        sides[1] = sideT;
        sides[2] = sideR;
        sides[3] = sideB;

        foreach (LassoCorner c in corners)
            c.Sphere.radius = cornerRadius;
        foreach (LassoSide s in sides)
            s.Capsule.radius = sideWidth;

        lr = GetComponent<LineRenderer>();
        lr.positionCount = 4;
        defaultAlpha = lr.material.color.a;
    }

    //private Dictionary<LassoCorner, int> cornerIdxs;
    LassoCorner[] corners;
    LassoSide[] sides;
    private void Start()
    {
        //cornerIdxs = new Dictionary<LassoCorner, int>()
        //    { { cornerBL, 0 }, { cornerTL, 1 }, { cornerTR, 2 }, { cornerBR, 3 } };

        //int size = GameManager.Instance.BoardSize;
        //float offset = .5f / size;
        //Snap = v => new Vector3(Mathf.Floor(v.x * size) / size + offset, 0, Mathf.Floor(v.z * size) / size + offset);
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
    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer; // 2 should be ignoreraycast
        foreach (LassoCorner c in corners)
            c.gameObject.layer = layer;
        foreach (LassoSide s in sides)
            s.gameObject.layer = layer;
    }

    private Piece inspected;
    public void Activate(Piece toInspect)
    {
        gameObject.SetActive(true);
        inspected = toInspect;
        cornerBL.Location = inspected.NicheStart;
        cornerTR.Location = inspected.NicheEnd;
        MatchAndSnap(cornerBL);
    }
    public void DeActivate()
    {
        gameObject.SetActive(false);
    }

    private int GetCornerIdx(LassoCorner corner)
    {
        int cornerIdx;
        //for (int i=0; i<4; i++)
        //    if (corners[i] == fixedCorner)
        //        cornerIdx = i;
        if (corner == cornerBL)
            cornerIdx = 0;
        else if (corner == cornerTL)
            cornerIdx = 1;
        else if (corner == cornerTR)
            cornerIdx = 2;
        else if (corner == cornerBR)
            cornerIdx = 3;
        else
            throw new Exception("rogue corner");

        return cornerIdx;
    }

    // TODO: the first draw should be a square area select :)
    // TODO: highlight things that eat it as well as things it eats (maybe only in node-link?)
    public void MatchAndSnap(LassoCorner fixedCorner)
    {
        int cornerIdx = GetCornerIdx(fixedCorner);
        int leftAdj = (cornerIdx + 1) % 4, opposite = (cornerIdx + 2) % 4, rightAdj = (cornerIdx + 3) % 4;

        var newSquares = board.GetAdjacentCornerSquares(corners[cornerIdx].Location, corners[opposite].Location);
        corners[leftAdj].Location = newSquares.Item1;
        corners[rightAdj].Location = newSquares.Item2;


        for (int i = 0; i < 4; i++)
        {
            corners[i].transform.position = corners[i].Location.transform.position;
            lr.SetPosition(i, corners[i].transform.localPosition);
        }
        foreach (LassoSide s in sides)
            s.MatchCorners();
    }

    public void MatchNiche(LassoCorner movedCorner, Square prevSquare)
    {
        int cornerIdx = GetCornerIdx(movedCorner);
        int opposite = (cornerIdx + 2) % 4;

        //var newSquares = board.GetAdjacentCornerSquares(corners[cornerIdx].Location, corners[opposite].Location);
        inspected.NicheStart = corners[cornerIdx].Location;
        inspected.NicheEnd = corners[opposite].Location;
    }
}

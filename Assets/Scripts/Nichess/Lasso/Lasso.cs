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
    //[SerializeField] LassoSide sideL;
    //[SerializeField] LassoSide sideT;
    //[SerializeField] LassoSide sideR;
    //[SerializeField] LassoSide sideB;

    [SerializeField] float sideWidth = .05f;
    [SerializeField] float cornerRadius = .1f;

    LineRenderer lr;
    public Func<Vector3, Vector3> Snap { get; private set; }
    public Color Col {
        get { return lr.startColor; }
        set { lr.startColor = lr.endColor = value; }
    }
    float defaultAlpha;

    //private Dictionary<LassoCorner, int> cornerIdxs;
    LassoCorner[] corners;
    //LassoSide[] sides;
    private void Awake()
    {
        corners = new LassoCorner[4];
        //sides = new LassoSide[4];

        corners[0] = cornerBL;
        corners[1] = cornerTL;
        corners[2] = cornerTR;
        corners[3] = cornerBR;
        //sides[0] = sideL;
        //sides[1] = sideT;
        //sides[2] = sideR;
        //sides[3] = sideB;

        lr = GetComponent<LineRenderer>();
        lr.positionCount = 4;
        defaultAlpha = lr.material.color.a;
    }
    private void Start()
    {
        foreach (LassoCorner c in corners)
            c.Sphere.radius = cornerRadius;
        //foreach (LassoSide s in sides)
        //    s.Capsule.radius = sideWidth;
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
        //if (ped.dragging == false)
        //{
        Color c = lr.material.color;
        c.a = defaultAlpha;
        lr.material.color = c;
        //}
    }
    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        foreach (LassoCorner c in corners)
            c.gameObject.layer = layer;
        //foreach (LassoSide s in sides)
        //    s.gameObject.layer = layer;
    }

    private Piece inspected;
    public void Activate(Piece toInspect)
    {
        gameObject.SetActive(true);
        inspected = toInspect;
        //if (inspected.InitialMode == false)
        //{
        //    corners[0].Location = toInspect.NicheStart;
        //    corners[2].Location = toInspect.NicheEnd;
        //    MatchAndSnap(corners[0]);
        //}
        //else
        //{
        //    corners[0].Location = corners[1].Location = corners[2].Location = corners[3].Location = inspected.NicheStart;
        //}

        DrawCorners();
    }
    void Update()
    {
        //if (inspected.InitialMode == true)
        //{
        //    corners[0].Location = corners[1].Location = corners[2].Location = corners[3].Location = inspected.NicheStart;
        //}
        DrawCorners();
        Col = inspected.Col;
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }


    readonly Dictionary<int, Vector2> clockwiseMap = new Dictionary<int, Vector2>(){
        {0,new Vector2(-.5f, -.5f)}, {1,new Vector2(-.5f, .5f)}, {2,new Vector2(.5f, .5f)}, {3,new Vector2(.5f, -.5f)}
    };
    void DrawCorners()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 newPos = corners[i].Location.transform.TransformPoint(clockwiseMap[i].x, clockwiseMap[i].y, 0); // assuming that the square is of scale 1
            corners[i].transform.position = newPos;
            lr.SetPosition(i, newPos);
        }
        //foreach (LassoSide s in sides)
        //    s.MatchCorners();
    }

    private int GetCornerIdx(LassoCorner corner)
    {
        if (corner == corners[0])
            return 0;
        else if (corner == corners[1])
            return 1;
        else if (corner == corners[2])
            return 2;
        else if (corner == corners[3])
            return 3;
        else
            throw new Exception("rogue corner");
    }



    void SwapCorners(int i, int j)
    {
        LassoCorner temp = corners[i];
        corners[i] = corners[j];
        corners[j] = temp;
    }
    void ReorientCorners()
    {
        // swap corner squares if they're oriented wrong
        if (corners[0].Location.X != corners[1].Location.X || corners[0].Location.Y != corners[3].Location.Y)
        {
            SwapCorners(1, 3);
        }
        if (corners[0].Location.Y > corners[1].Location.Y)
        {
            SwapCorners(0, 1);
            SwapCorners(2, 3);
        }
        if (corners[0].Location.X > corners[3].Location.X)
        {
            SwapCorners(0, 3);
            SwapCorners(1, 2);
        }
    }

    // TODO: highlight things that eat it as well as things it eats (maybe only in node-link?) golden angle!
    public void MatchAndSnap(LassoCorner fixedCorner)
    {
        int cornerIdx = GetCornerIdx(fixedCorner);
        int leftAdj = (cornerIdx + 1) % 4, opposite = (cornerIdx + 2) % 4, rightAdj = (cornerIdx + 3) % 4;

        //var adjacentCornerSquares = board.GetAdjacentCornerSquares(corners[cornerIdx].Location, corners[opposite].Location);
        //corners[leftAdj].Location = adjacentCornerSquares.Item1;
        //corners[rightAdj].Location = adjacentCornerSquares.Item2;

        ReorientCorners();
    }

    public void MatchNiche(LassoCorner movedCorner)
    {
        //int cornerIdx = GetCornerIdx(movedCorner);
        //int opposite = (cornerIdx + 2) % 4;

        //inspected.SetNiche(corners[0].Location, corners[2].Location);
    }
}

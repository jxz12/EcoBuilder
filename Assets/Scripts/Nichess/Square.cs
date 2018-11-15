using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
public class Square : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float defaultAlpha;
    MeshRenderer mr;

    public Color Col {
        get { return mr.material.color; }
        private set { mr.material.color = value; }
    }
    public int X { get; private set; }
    public int Y { get; private set; }

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        // defaultAlpha = mr.material.color.a;
    }

    public void Init(int x, int y, Color c)
    {
        X = x;
        Y = y;
        Col = c;
        name = x + " " + y;
    }
    void Start()
    {
        var c = Col;
        c.a = defaultAlpha;
        Col = c;
    }

    public event Action HoveredEvent;
    public event Action UnhoveredEvent;
    public void OnPointerEnter(PointerEventData ped)
    {
        // print(name);
        var c = Col;
        c.a = 1;
        Col = c;
        HoveredEvent.Invoke(); // otherwise it's a normal hover
    }
    public void OnPointerExit(PointerEventData ped)
    {
        var c = Col;
        c.a = defaultAlpha;
        Col = c;
        UnhoveredEvent.Invoke();
    }
}
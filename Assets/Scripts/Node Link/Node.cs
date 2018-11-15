using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Animator))]
public class Node : MonoBehaviour
{
    public int Idx { get; private set; }

    public Color Col {
        get { return mr.material.GetColor("_Color"); }
        set { mr.material.SetColor("_Color", value); }
    }
    public Vector3 Pos {
        get { return transform.localPosition; }
        set { transform.localPosition = value;}
    }
    public float Size {
        get { return transform.localScale.x; }
        set { transform.localScale = new Vector3(value, value, value); }
    }

    MeshRenderer mr;
    Animator anim;
    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        anim = GetComponent<Animator>();
    }

    //Rigidbody rb;
    public void Init(int idx)
    {
        Idx = idx;
        name = idx.ToString();
    }
    public void Inspect()
    {
        anim.SetTrigger("Inspect");
    }
    public void Uninspect()
    {
        anim.SetTrigger("Uninspect");
    }
}
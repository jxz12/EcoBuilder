using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
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

    MeshRenderer mr;
    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
    }

    //Rigidbody rb;
    public void Init(int idx)
    {
        Idx = idx;
        name = idx.ToString();
    }
    //void Start()
    //{
    //    // check whether initialized?
    //}

    //public float spring = 5f, damping = .9f;
    //void FixedUpdate()
    //{
    //    //transform.localPosition = target;
    //    Vector3 targetGlobal = transform.parent.TransformPoint(Target);
    //    var body = rb;

    //    float distance = Vector3.Distance(targetGlobal, body.position);
    //    Vector3 direction = (targetGlobal - body.position).normalized;
    //    body.velocity += (direction * distance * spring) / body.mass;

    //    // damping
    //    float dampingStrength = damping/(distance+1);
    //    body.velocity *= 1-dampingStrength;
    //}

    //float spring = 10f;
    //float damping = .4f;
    //void TargetJoint()
    //{
    //     float distance = Vector3.Distance(target, transform.localPosition);
    //     Vector3 direction = (target - transform.localPosition).normalized;
    //     rb.velocity += (direction * distance * spring) / rb.mass;

    //     //damping
    //     float dampingValue = 1 - (1 / (distance * damping + 1));
    //     rb.velocity *= dampingValue;
    //}

    //public float Size {
    //    get { return transform.localScale.x; }
    //    set { transform.localScale = new Vector3(value, value, value); }
    //}
}
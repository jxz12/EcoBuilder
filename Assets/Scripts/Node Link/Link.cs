using UnityEngine;

public class Link : MonoBehaviour
{
    [SerializeField] LineRenderer lr;

    public Node Source { get; private set; }
    public Node Target { get; private set; }

    public void Init(Node source, Node target)
    {
        Source = source;
        Target = target;
        name = Source.Idx + " " + Target.Idx;
    }

    private void Update()
    {
        lr.SetPosition(0, Source.transform.position);
        lr.SetPosition(1, Target.transform.position);
        // lr.startColor = Source.Col;
        // lr.endColor = Target.Col;
        lr.endColor = Source.Col;
        lr.startColor = Target.Col;
    }
}
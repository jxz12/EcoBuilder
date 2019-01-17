using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lr;

        public Node Source { get; private set; }
        public Node Target { get; private set; }

        public float Size {
            get { return lr.widthMultiplier; }
            set { lr.widthMultiplier = value; }
        }

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
            lr.startColor = Target.Col;
            lr.endColor = Source.Col;
        }
    }
}
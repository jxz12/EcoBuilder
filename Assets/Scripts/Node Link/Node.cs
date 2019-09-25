using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return shape!=null? shape.GetComponent<MeshRenderer>().material.color : Color.black; }
        }
        public Vector3 StressPos { get; set; }
        public Vector3 FocusPos { get; set; }
        public float Size { get; set; }
        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;

        public Vector3 velocity;  // for public use with Vector3.SmoothDamp

        GameObject shape;

        public void Init(int idx, float size)
        {
            Idx = idx;
            name = idx.ToString();
            StressPos = Random.insideUnitSphere;
            Size = size;
        }

        public void Shape(GameObject shapeObject)
        {
            // drop it in at the point at shapeObject's position, but at z=-1
            StressPos = transform.InverseTransformPoint(new Vector3(shapeObject.transform.position.x, shapeObject.transform.position.y, -1));

            transform.position = shapeObject.transform.position;
            shape = shapeObject;
            shape.transform.SetParent(transform);
            shape.transform.localPosition = Vector3.zero;
            shape.transform.localRotation = Quaternion.identity;
            shape.transform.localScale = Vector3.one;
        }
        public void Outline(int colourIdx=0)
        {
            // var outline = GetComponent<cakeslice.Outline>();
            var outline = shape.GetComponent<cakeslice.Outline>();
            if (outline == null)
            {
                // outline = gameObject.AddComponent<cakeslice.Outline>();
                outline = shape.AddComponent<cakeslice.Outline>();
            }
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            // if (GetComponent<cakeslice.Outline>() != null)
            //     Destroy(GetComponent<cakeslice.Outline>());
            if (shape.GetComponent<cakeslice.Outline>() != null)
                Destroy(shape.GetComponent<cakeslice.Outline>());
        }
        bool flashing = false;
        public void Flash(bool isFlashing)
        {
            flashing = isFlashing;
            GetComponent<Animator>().SetBool("Flashing", isFlashing);
        }
        void Update()
        {
            if ((Time.time*60) % 60 < 30 && flashing)
            {
                // GetComponent<MeshRenderer>().material.color = new Color(1,.01f,.01f,1);
                // GetComponent<MeshRenderer>().enabled = false;
                // shape.SetActive(false);
                // shape.SetActive(false);
                shape.GetComponent<MeshRenderer>().enabled = false;
            }
            else
            {
                // GetComponent<MeshRenderer>().material.color = new Color(.01f,.3f,1,.3f);
                // GetComponent<MeshRenderer>().enabled = true;
                // shape.SetActive(true);
                if (shape != null)
                    shape.GetComponent<MeshRenderer>().enabled = true;
            }
        }
    }
}
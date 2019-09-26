// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// using System.Diagnostics; // TIME TESTING

// namespace EcoBuilder.Archie
// {


//     public class Bmesh : MonoBehaviour
//     {
//         public class skeletal_joint
//         {
//             public Vector3 position;
//             public float size;
//             public List<skeletal_joint> linked;
//             public static void Link(skeletal_joint a, skeletal_joint b)
//             {
//                 a.linked.Add(b);
//                 b.linked.Add(a);
//             }
//             public skeletal_joint(Vector3 position_of_joint = new Vector3(), float size_of_joint = 1)
//             {
//                 position = position_of_joint;
//                 size = size_of_joint;
//                 linked = new List<skeletal_joint>();
//             }

//             public static skeletal_joint String_to_Skeleton(string system)
//             {
//                 // turns an l-system string into a skeleton
//             }
//         }
//     }
// }
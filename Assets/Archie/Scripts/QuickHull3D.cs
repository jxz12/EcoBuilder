using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie
{
    public class QuickHull3D : MonoBehaviour
    {
        public static float Distance_from_Line(Vector3 base_of_line, Vector3 direction_of_line, Vector3 position, bool line_wise = true)
        {
            Vector3 base_to_point = position - base_of_line;
            if ( base_of_line + direction_of_line == position )
            {
                return 0.0f;
            }
            if (direction_of_line.magnitude > 0)
            {
                Vector3 direction = direction_of_line.Normalize();
                float projection_length = Vector3.Dot(base_to_point, direction);
                if (projection_length > direction_of_line.Magnitude() && line_wise)
                {
                    return inf;
                }
                else 
                {
                    double returning = System.Math.Pow(base_to_point.Magnitude(),2) - System.Math.Pow(projection_length,2);
                    if ( returning >= 0 )
                    {
                        return (float)(System.Math.Sqrt(returning));
                    }
                    else
                    {
                        return 0.0f;
                    }
                }
            }
        }
        public static float Distance_from_Plane(Vector3 base_vector, Vector3 directionA, Vector3 directionB, Vector3 position)
        {
            // plane is defined by two non parallel vectors (if they are parallel, this is a problem, return distance from line of them)
            if (Vector3.dot(directionA,directionB)/(directionA.magnitude() * directionB.magnitude()) == 1)
            {
                return Distance_from_Line(base_vector, directionA, position);
            }
            // if the position is coplaner, return 0
            else if (Vector3.Dot(Vector3.Cross(directionA, directionB),position-base_vector) == 0)
            {
                return 0.0f;
            }
            // else, solve, using normal, to find distance
            else
            {
                //find unit normal vector
                Vector3 unit_normal = Vector3.Cross(directionA, directionB);
                float distance = Vector3.Dot(unit_normal, position) - Vector3.Dot(unit_normal, base_vector);
                return distance;
            }

        }

        public static List<Vector3> Clear_Simplex_Area(List<Vector3> points, Vector3[] tetrahedron_corners)
        {
            List<Vector3> exterior_vertices = new List<Vector3>();
            // use plane equation to find if inside or outside tetrahedron
            // or use barycentric coordinates (but difficult without numpy)
            Matrix4x4 T;
            if (T.determinante != 0)
            {
                Matrix4x4 inverse = T.inverse;
                foreach ( Vector3 p in points )
                {
                    if (!Array.Exists(tetrahedron_corners, p))
                    {
                        Vector3 LeftHandSide = p - tetrahedron_corners[0];
                        Vector4 Barycentric = inverse.MultiplyVector(Vector4.LeftHandSide);
                    }

                }
            }
        }

        class face
        {
            public Vector3[] corners;
            public SortedDictionary<float, Vector3> Conflict_List;
            public bool complete;

            public face(Vector3 corner1, Vector3 corner2, Vector3 corner3)
            {
                Conflict_List = new SortedDictionary<float, Vector3>();
                corners = new {corner1, corner2, corner3};
                complete = false;
            }
        }

        public static void Distribute_Conflicts(List<Vector3> points, face[] face_list)
        {
            foreach ( Vector3 p in points )
            {
                SortedList<float,int> distances = new SortedList<float, int>();
                for ( int j = 0 ; j < face_list.Count ; j++ )
                {
                    distances.Add(Distance_from_Plane(face_list[j][0],face_list[j][1] - face_list[j][0], face_list[j][2] - face_list[j][0], p),j);
                }
                face_list[distances.Values[0]].Conflict_List.Add(distances.Keys[0], p);

            }
        }

        public static void it(List<face> face_list)
        {
            List<face> new_face_list = new List<face>();
            foreach ( face face_element in face_list )
            {
                if ( face_element.Conflict_List.Count >= 1 )
                {
                    Vector3 furthest_from_face = face_element.Conflict_List.Values[face_element.Conflict_List.Count - 1];
                    Vector3[]  corners = new Vector3[4];
                    List<Vector3> remaining_conflicts = clear_simplex_area(face_element.Conflict_List.Values, corners);
                    //draw tetrahedron
                    face[] daughter_faces = new face[3];
                    for ( int i = 0 ; i < daughter_faces.Length ; i++ )
                    {
                        daughter_faces[i] = face(face_element[i], face_element[i + 1 % 3], furthest_from_face);
                    }
                    Distribute_Conflicts(remaining_conflicts, daughter_faces);
                    foreach ( face daughter in daughter_faces )
                    {
                        if (daughter.Conflict_List.Count <= 0)
                        {
                            daughter.complete = true;
                        }
                    }
                    new_face_list.Add(daughter_faces[0]);
                    new_face_list.Add(daughter_faces[1]);
                    new_face_list.Add(daughter_faces[2]);
                }
                else
                {
                    face_element.complete = true;
                    new_face_list.Add(face_element);
                }
            }
        }
    }
}
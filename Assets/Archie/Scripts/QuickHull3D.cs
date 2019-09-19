using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
                Vector3 direction = direction_of_line;
                direction.Normalize();
                float projection_length = Vector3.Dot(base_to_point, direction);
                if (projection_length > Vector3.Magnitude(direction_of_line) && line_wise)
                {
                    return float.PositiveInfinity;
                }
                else 
                {
                    double returning = System.Math.Pow(Vector3.Magnitude(base_to_point),2) - System.Math.Pow(projection_length,2);
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
            else
            {
                return float.PositiveInfinity;
            }
        }
        public static float Distance_from_Plane(Vector3 base_vector, Vector3 directionA, Vector3 directionB, Vector3 position)
        {
            // plane is defined by two non parallel vectors (if they are parallel, this is a problem, return distance from line of them)
            if (Vector3.Dot(directionA,directionB)/(directionA.magnitude * directionB.magnitude) == 1)
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

        public static List<Vector3> Clear_Simplex_Volume(Vector3[] points, Vector3[] tetrahedron_corners)
        {
            List<Vector3> exterior_vertices = new List<Vector3>();
            // use plane equation to find if inside or outside tetrahedron
            // or use barycentric coordinates (but difficult without numpy)
            Matrix4x4 T = new Matrix4x4();
            for ( int i = 0 ; i < 4 ; i++ )
            {
                Vector3 tmp = tetrahedron_corners[i] - tetrahedron_corners[3];
                T.SetColumn(i, (Vector4)(tmp));
            }
            if (T.determinant != 0)
            {
                Matrix4x4 inverse = T.inverse;
                foreach ( Vector3 p in points )
                {
                    if (!Array.Exists(tetrahedron_corners, element => element == p))
                    {
                        Vector3 LeftHandSide = p - tetrahedron_corners[3];
                        Vector4 Barycentric = inverse.MultiplyVector((Vector4)(LeftHandSide));
                        if (Barycentric.x > 0 && Barycentric.x < 1 && Barycentric.y > 0 && Barycentric.y < 1 && Barycentric.z > 0 && Barycentric.z < 1 && 1.0f - Barycentric.x - Barycentric.y - Barycentric.z > 0 )
                        {
                            exterior_vertices.Add(p);
                        }
                    }
                }
                return exterior_vertices;
            }
            else
            {
                return new List<Vector3>();
            }
        }

        public class face
        {
            public Vector3[] corners;
            public SortedList<float, Vector3> Conflict_List;
            public bool complete;

            public face(Vector3 corner1, Vector3 corner2, Vector3 corner3)
            {
                Conflict_List = new SortedList<float, Vector3>();
                corners = new Vector3[]{corner1, corner2, corner3};
                complete = false;
            }
        }

        public static void Distribute_Conflicts(Vector3[] points, List<face> face_list)
        {
            foreach ( Vector3 p in points )
            {
                SortedList<float,int> distances = new SortedList<float, int>();
                for ( int j = 0 ; j < face_list.Count ; j++ )
                {
                    distances.Add(Distance_from_Plane(face_list[j].corners[0],face_list[j].corners[1] - face_list[j].corners[0], face_list[j].corners[2] - face_list[j].corners[0], p),j);
                }
                face_list[distances.Values[0]].Conflict_List.Add(distances.Keys[0], p);

            }
        }

        public static List<face> it(List<face> face_list)
        {
            List<face> new_face_list = new List<face>();
            foreach ( face face_element in face_list )
            {
                if ( face_element.Conflict_List.Count >= 1 )
                {
                    Vector3 furthest_from_face = face_element.Conflict_List.Values[face_element.Conflict_List.Count - 1];
                    Vector3[]  corners = new Vector3[] {face_element.corners[0],face_element.corners[1], face_element.corners[2], furthest_from_face};
                    List<Vector3> remaining_conflicts = Clear_Simplex_Volume((new List<Vector3>(face_element.Conflict_List.Values)).ToArray(), corners);
                    //draw tetrahedron
                    List<face> daughter_faces = new List<face>();
                    for ( int i = 0 ; i < 3 ; i++ )
                    {
                        daughter_faces.Add(new face(face_element.corners[i], face_element.corners[i + 1 % 3], furthest_from_face));
                    }
                    Distribute_Conflicts(remaining_conflicts.ToArray(), daughter_faces);
                    foreach ( face daughter in daughter_faces )
                    {
                        if (daughter.Conflict_List.Count <= 0)
                        {
                            daughter.complete = true;
                        }
                        new_face_list.Add(daughter);
                    }
                }
                else
                {
                    face_element.complete = true;
                    new_face_list.Add(face_element);
                }
            }
            return new_face_list;
        }

        public static int[] MakeHull(Vector3[] points)
        {
            // the function returns a triangle list for a mesh
            List<int> triangle_list = new List<int>();
            //find maximal 3D simplex (tetrahedron)
            Vector3[] base_line = new Vector3[]{new Vector3(),new Vector3()};
            Vector3 third = new Vector3();
            foreach ( Vector3 p1 in points)
            {
                foreach ( Vector3 p2 in points)
                {
                    if (( p1 - p2).magnitude > (base_line[0] - base_line[1]).magnitude)
                    {
                        base_line[0] = p1;
                        base_line[1] = p2;
                    }
                }
            }
            Vector3 direction = base_line[1] - base_line[0];
            Vector3 max_distance = base_line[0];
            foreach ( Vector3 p in points )
            {
                float current = Distance_from_Line(base_line[0], direction, max_distance);
                float testing = Distance_from_Line(base_line[0], direction, p);
                if ( testing > current )
                {
                    max_distance = p;
                }
            }
            Vector3[] triangle_corners = new Vector3[]{ base_line[0], base_line[1], max_distance };
            foreach (Vector3 p in points)
            {
                float current = Distance_from_Plane(triangle_corners[0], triangle_corners[1] - triangle_corners[0], triangle_corners[2] - triangle_corners[0], max_distance);
                float testing = Distance_from_Plane(triangle_corners[0], triangle_corners[1] - triangle_corners[0], triangle_corners[2] - triangle_corners[0], p);
                if (testing > current)
                {
                    max_distance = p;
                }
            }
            Vector3[] tetrahedron_corners = new Vector3[]{triangle_corners[0], triangle_corners[1], triangle_corners[2], max_distance};
            //clearsimplex
            var conflict_points = Clear_Simplex_Volume(points, tetrahedron_corners).ToArray();
            // construct faces
            List<face> face_list = new List<face>();
            //...
            face_list.Add(new face(tetrahedron_corners[0],tetrahedron_corners[1],tetrahedron_corners[2]));
            face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[1], tetrahedron_corners[3]));
            face_list.Add(new face(tetrahedron_corners[1], tetrahedron_corners[2], tetrahedron_corners[3]));
            face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[2], tetrahedron_corners[3]));
            //distribute conflicts
            Distribute_Conflicts(conflict_points, face_list);
            //it loop
            bool all_complete = false;
            while ( face_list.Count > 0 )
            {
                face_list = it(face_list);
                all_complete = true;
                var new_face_list = new List<face>();
                foreach ( face face_element in face_list )
                {
                    if (!face_element.complete)
                    {
                        new_face_list.Add(face_element);
                    }
                    else
                    {
                        // add to resultant triangle list
                        foreach ( Vector3 corner in face_element.corners )
                        {
                            triangle_list.Add(System.Array.IndexOf(points, corner));
                        }
                    }
                }
                face_list = new_face_list;
            }
            return triangle_list.ToArray();
        }
    }
}
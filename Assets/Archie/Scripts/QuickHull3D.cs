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
        public static float Distance_from_Plane(Vector3 base_vector, Vector3 directionA, Vector3 directionB, Vector3 position, bool face_wise = true)
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
                // //find unit normal vector
                // Vector3 unit_normal = Vector3.Normalize(Vector3.Cross(directionA, directionB));
                // float distance = Vector3.Dot(unit_normal, position) - Vector3.Dot(unit_normal, base_vector);
                // Vector3 projection_onto_plane = position - unit_normal * distance;
                // // converting 3D projection to a 2D coordinate relative to the defined plane
                // UnityEngine.Debug.Log("Matrix in:");
                // UnityEngine.Debug.Log((directionA));
                // UnityEngine.Debug.Log((Vector4)(directionA));
                // UnityEngine.Debug.Log((directionB));
                // UnityEngine.Debug.Log((Vector4)(directionB));
                // UnityEngine.Debug.Log("project in plane check: " + (Vector3.Dot(Vector3.Cross(directionA, directionB), projection_onto_plane - base_vector) == 0));
                // UnityEngine.Debug.Log("project in plane check: " + (Vector3.Dot(Vector3.Cross(directionA, directionB), (position + unit_normal * distance) - base_vector) == 0));
                // Matrix4x4 M = new Matrix4x4();
                // M.SetColumn(0, (Vector4)(directionA));
                // M.SetColumn(1, (Vector4)(directionB));
                // M.SetColumn(2, (Vector4)(base_vector));
                // M.SetColumn(3, new Vector4(0, 0, 0, 1));
                // Vector4 two_dimensional_coefficents = M.inverse.MultiplyVector((Vector4)(projection_onto_plane)); // x and y values should be coefficents (associated with DirectionA and DirectionB respecitvely), z should be 1, and w should be zero
                // Vector2 projection_in_plane = (Vector2)(two_dimensional_coefficents);
                // UnityEngine.Debug.Log("Matrix problems:");
                // UnityEngine.Debug.Log(two_dimensional_coefficents);
                // UnityEngine.Debug.Log(projection_in_plane);
                // // using barycentric coordinates to find if projection is in triangle defined by base vector and direction vectors
                // // relative to this new coordinate system, the triangle defined by directionA and directionB should be a rightangled triangle, with two size of length 1 and an hypotinous of length root 2
                // float triangle_hypotinous_squared = 2.0f;
                // float distance_from_origin_to_point_squared = (float)System.Math.Pow(projection_in_plane.magnitude, 2);
                // if ( (projection_in_plane.x > 1 || projection_in_plane.x < 0 || projection_in_plane.y > 1 || projection_in_plane.y < 0 || (1 - projection_in_plane.x - projection_in_plane.y) < 0) && face_wise)
                // {
                //     // projection is not in the triangle
                //     return float.PositiveInfinity; 
                // }
                // else
                // {
                //     return distance;
                // }

                // NEW TESTING
                //find unit normal vector
                Vector3 unit_normal = Vector3.Normalize(Vector3.Cross(directionA, directionB));
                // converting 3D projection to a 2D coordinate relative to the defined plane
                // UnityEngine.Debug.Log("Matrix in:");
                // UnityEngine.Debug.Log((directionA));
                // UnityEngine.Debug.Log((Vector4)(directionA));
                // UnityEngine.Debug.Log((directionB));
                // UnityEngine.Debug.Log((Vector4)(directionB));
                // UnityEngine.Debug.Log("project in plane check: " + (Vector3.Dot(Vector3.Cross(directionA, directionB), projection_onto_plane - base_vector) == 0));
                // UnityEngine.Debug.Log("project in plane check: " + (Vector3.Dot(Vector3.Cross(directionA, directionB), (position + unit_normal * distance) - base_vector) == 0));
                Matrix4x4 M = new Matrix4x4();
                M.SetColumn(0, (Vector4)(directionA));
                M.SetColumn(1, (Vector4)(directionB));
                M.SetColumn(2, (Vector4)(unit_normal));
                M.SetColumn(3, new Vector4(0, 0, 0, 1));
                // UnityEngine.Debug.Log(M);
                Vector4 two_dimensional_coefficents = M.inverse.MultiplyVector((Vector4)(position - base_vector)); // x and y values should be coefficents (associated with DirectionA and DirectionB respecitvely), z should be 1, and w should be zero
                Vector2 projection_in_plane = (Vector2)(two_dimensional_coefficents);
                // float distance = (float)(System.Math.Abs(two_dimensional_coefficents.z));
                float distance = two_dimensional_coefficents.z;
                // UnityEngine.Debug.Log("Matrix problems Star:");
                // UnityEngine.Debug.Log(two_dimensional_coefficents);
                // UnityEngine.Debug.Log(projection_in_plane);
                // using barycentric coordinates to find if projection is in triangle defined by base vector and direction vectors
                // relative to this new coordinate system, the triangle defined by directionA and directionB should be a rightangled triangle, with two size of length 1 and an hypotinous of length root 2
                float triangle_hypotinous_squared = 2.0f;
                float distance_from_origin_to_point_squared = (float)System.Math.Pow(projection_in_plane.magnitude, 2);
                if ( (projection_in_plane.x > 1 || projection_in_plane.x < 0 || projection_in_plane.y > 1 || projection_in_plane.y < 0 || (1 - projection_in_plane.x - projection_in_plane.y) < 0) && face_wise)
                {
                    // projection is not in the triangle
                    return float.PositiveInfinity; 
                }
                else
                {
                    return distance;
                }
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
                Vector4 col = (Vector4)(tmp);
                if ( i == 3 )
                {
                    col = col + new Vector4(0, 0, 0, 1);
                }
                T.SetColumn(i, col);
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
                        if (!(Barycentric.x > 0 && Barycentric.x < 1 && Barycentric.y > 0 && Barycentric.y < 1 && Barycentric.z > 0 && Barycentric.z < 1 && 1.0f - Barycentric.x - Barycentric.y - Barycentric.z > 0 ))
                        {
                            bool remaining = true;
                            foreach(Vector3 c1 in tetrahedron_corners)
                            {
                                foreach (Vector3 c2 in tetrahedron_corners)
                                {
                                    if ( c1 != c2 )
                                    {
                                        remaining = remaining && (Distance_from_Line(c1, c2 - c1, p, false) != 0);
                                    }
                                }

                            }
                            if ( remaining )
                            {
                                exterior_vertices.Add(p);
                            }
                        }
                    }
                }
                return exterior_vertices;
            }
            else
            {
                // UnityEngine.Debug.Log("Singular Matrix problem");
                // UnityEngine.Debug.Log(T);
                return new List<Vector3>();
            }
        }

        public static float Area_of_Triangle(Vector3 corner1, Vector3 corner2, Vector3 corner3)
        {
            float base_of_triangle = (corner1 - corner2).magnitude;
            float height_of_triangle = Distance_from_Line(corner1, corner2 - corner1, corner3, true);
            return base_of_triangle * height_of_triangle * 0.5f;
        }

        public class face
        {
            public Vector3[] corners;
            public SortedList<float, Vector3> Conflict_List;
            public bool hidden;
            public bool complete;
            public List<edge> edges;

            public face(Vector3 corner1, Vector3 corner2, Vector3 corner3)
            {
                Conflict_List = new SortedList<float, Vector3>();
                corners = new Vector3[]{corner1, corner2, corner3};
                complete = false;
                hidden = false;
                edges = new List<edge>();
            }

            public void Connect_to_Edge(edge e)
            {
                if (edges.Count < 3)
                {
                    edges.Add(e);
                }
            }

            public bool Is_Visible_from_Point(Vector3 point)
            {
                Vector3 plane_normal = Vector3.Normalize(Vector3.Cross(corners[2]-corners[0],corners[1] - corners[0]));
                Vector3 point_to_plane = Vector3.Normalize(corners[0] - point);
                bool visible = (Vector3.Dot(point_to_plane, plane_normal) > 0);
                return visible;
            }
        }

        public class edge
        {
            public Vector3[] vertices;
            public List<face> connected_faces;
            public edge(Vector3 vertexA, Vector3 vertexB)
            {
                vertices = new Vector3[]{vertexA, vertexB};
                connected_faces = new List<face>();
            }
            public void Connect_to_Face(face f)
            {
                if (connected_faces.Count < 2)
                {
                    connected_faces.Add(f);
                    f.Connect_to_Edge(this);
                }
            }

        }

        // public static void Distribute_Conflicts(Vector3[] points, List<face> face_list)
        // {
        //     foreach ( Vector3 p in points )
        //     {

        //         List<float> distances = new List<float>();
        //         for ( int j = 0 ; j < face_list.Count ; j++ )
        //         {
        //             var distance = System.Math.Abs(Distance_from_Plane(face_list[j].corners[0], face_list[j].corners[1] - face_list[j].corners[0], face_list[j].corners[2] - face_list[j].corners[0], p));
        //             // if ( distance > 0 && distance < float.PositiveInfinity)
        //             // {
        //             distances.Add(distance);
        //             // }
        //         }
        //         UnityEngine.Debug.Log("distances:");
        //         foreach ( float d in distances) {UnityEngine.Debug.Log(d); }
        //         if (distances.Count > 0)
        //         {
        //             face_list[distances.FindIndex(i => i == distances.Min())].Conflict_List.Add(distances.Min(), p);
        //         }
        //     }
        // }

        public static void Distribute_Conflicts(Vector3[] points, List<face> face_list)
        {
            foreach (Vector3 p in points)
            {

                SortedList<float, face> distances = new SortedList<float, face>();
                foreach (face f in face_list)
                {
                    var distance = Distance_from_Plane(f.corners[0], f.corners[1] - f.corners[0], f.corners[2] - f.corners[0], p, false);
                    if ( distance > 0 && distance < float.PositiveInfinity)
                    {
                        distances.Add(distance, f);
                    }
                }
                // UnityEngine.Debug.Log("distances:");
                // UnityEngine.Debug.Log(distances.Count);
                if (distances.Count > 0)
                {
                    distances.Values[0].Conflict_List.Add(distances.Keys[0], p);
                }
            }
        }

        // public static List<face> it(List<face> face_list)
        // {
        //     List<face> new_face_list = new List<face>();
        //     foreach ( face face_element in face_list )
        //     {
        //         if ( face_element.Conflict_List.Count >= 1 )
        //         {
        //             Vector3 furthest_from_face = face_element.Conflict_List.Values[face_element.Conflict_List.Count - 1];
        //             Vector3[]  corners = new Vector3[] {face_element.corners[0],face_element.corners[1], face_element.corners[2], furthest_from_face};
        //             List<Vector3> remaining_conflicts = Clear_Simplex_Volume((new List<Vector3>(face_element.Conflict_List.Values)).ToArray(), corners);
        //             //draw tetrahedron
        //             List<face> daughter_faces = new List<face>();
        //             for ( int i = 0 ; i < 3 ; i++ )
        //             {
        //                 // daughter_faces.Add(new face(face_element.corners[i], furthest_from_face, face_element.corners[(i + 1) % 3]));
        //                 daughter_faces.Add(new face(face_element.corners[(i + 1) % 3], furthest_from_face, face_element.corners[i]));
        //             }
        //             Distribute_Conflicts(remaining_conflicts.ToArray(), daughter_faces);
        //             foreach ( face daughter in daughter_faces )
        //             {
        //                 if (daughter.Conflict_List.Count <= 0)
        //                 {
        //                     daughter.complete = true;
        //                 }
        //                 new_face_list.Add(daughter);
        //             }
        //         }
        //         else
        //         {
        //             UnityEngine.Debug.Log("Empty conflict list");
        //             UnityEngine.Debug.Log(face_element.Conflict_List.Count);
        //             face_element.complete = true;
        //             new_face_list.Add(face_element);
        //         }
        //     }
        //     return new_face_list;
        // }

        public static string Flood_Fill(Vector3 furthest_from_face, face current_face, List<edge> horizon_edge_list, Stack<face> visited_faces)
        {
            string path = "";
            face next_face;
            face last_face;
            if (visited_faces.Count > 0)
            {
                last_face = visited_faces.Peek();
                // Testing if face is a conflict (not visible or already visited)
                // There is some conflict, either the current face has already been visited or it is not visible from the point
                if (visited_faces.Contains(current_face))
                {
                    return "C";
                }
                if (!current_face.Is_Visible_from_Point(furthest_from_face))
                {
                    // Adding the relevant edge to the edge list
                    horizon_edge_list.Add(current_face.edges.Find(element1 => element1.connected_faces.Exists(element2 => element2 == last_face)));
                    UnityEngine.Debug.Log("INTREST: " + horizon_edge_list[horizon_edge_list.Count - 1].connected_faces.Exists(element2 => visited_faces.Contains(element2)));
                    return "H";
                }
            }
            else
            {
                if (!current_face.Is_Visible_from_Point(furthest_from_face))
                {
                    return "H";
                }

            }
            visited_faces.Push(current_face);
            path += 'V';
            // UnityEngine.Debug.Log("[");
            foreach ( edge e in current_face.edges)
            {
                // UnityEngine.Debug.Log("F");
                next_face = e.connected_faces.Find(element => element != current_face);
                path += '[';
                path += Flood_Fill(furthest_from_face, next_face, horizon_edge_list, visited_faces);
                path += ']';
            }
            // UnityEngine.Debug.Log("]");
            return path;
        }


        public static List<face> iterate(List<face> face_list)
        {
            UnityEngine.Debug.Log("I");
            if (face_list.Count <= 0)
            {
                return new List<face>();
            }
            face face_element = face_list[face_list.Count - 1];
            if (face_element.Conflict_List.Count >= 1)
            {
                Vector3 furthest_from_face = face_element.Conflict_List.Values[face_element.Conflict_List.Count - 1];
                // Flood search
                var horizon_edge_list = new List<edge>();
                var current_face = face_element;
                var visited_faces = new Stack<face>();
                string path = "";
                // Use a Flood Fill algorithm to search for horizon edges
                UnityEngine.Debug.Log(visited_faces.Count);
                path = Flood_Fill(furthest_from_face, current_face, horizon_edge_list, visited_faces);
                // Make triangles between furthest point and horizon edges
                List<face> daughter_faces = new List<face>();
                UnityEngine.Debug.Log("priority");
                UnityEngine.Debug.Log(path);
                UnityEngine.Debug.Log(horizon_edge_list.Count);
                UnityEngine.Debug.Log(visited_faces.Count);
                if ( visited_faces.Count == 0 )
                {
                    UnityEngine.Debug.Log("ERROR: current point assigned to wrong face");
                    face_element.hidden = true;
                    return face_list;
                }
                if (horizon_edge_list.Count < 3)
                {
                    UnityEngine.Debug.Log("ERROR: improper flood");
                    face_element.hidden = true;
                    return face_list;
                }

                // Modify horizon edges so they point to new face
                int index = 0;
                foreach (edge e in horizon_edge_list)
                {
                    // adjacent triangles have reversed vertex orders ( for the vertices they share )

                    // List<Vector3> new_face_corners = (from corner in e.connected_faces.Find(element => visited_faces.Contains(element)).corners
                    //                                   where Array.Exists(e.vertices, element => element == corner)
                    //                                   select corner).ToList<Vector3>();
                    List<Vector3> new_face_corners = new List<Vector3>(e.vertices);
                    new_face_corners.Add(furthest_from_face);
                    if ( index == 0 )
                    {
                        new_face_corners.Reverse();
                    }
                    // var old_face = e.connected_faces.Find(element => visited_faces.Contains(element));
                    // var new_face_corners = old_face.corners;
                    // new_face_corners[Array.FindIndex(new_face_corners, element1 => !Array.Exists(e.vertices, element2 => element2 == element1))] = furthest_from_face;
                    // UnityEngine.Debug.Log("MAKING NEW FACE");
                    // foreach( Vector3 v in new_face_corners)
                    // {
                    //     UnityEngine.Debug.Log(v);
                    // }
                    var new_face = new face(new_face_corners[0], new_face_corners[1], new_face_corners[2]);
                    // var new_face = new face(e.vertices[0], e.vertices[1], furthest_from_face);
                    // e.connected_faces[e.connected_faces.FindIndex(element => visited_faces.Contains(element))] = new_face;
                    UnityEngine.Debug.Log("F: " + e.connected_faces.Exists(element => visited_faces.Contains(element)));
                    e.connected_faces.Remove(e.connected_faces.Find(element => visited_faces.Contains(element)));
                    UnityEngine.Debug.Log("P: " + e.connected_faces.Count);
                    e.Connect_to_Face(new_face);
                    daughter_faces.Add(new_face);
                    UnityEngine.Debug.Log("daughter first link count: " + new_face.edges.Count);
                    index += 1;
                }
                // for ( int i = 0 ; i < daughter_faces.Count ; i++ )
                // {
                //     var face_pair = new face[]{daughter_faces[i],daughter_faces[(i+1)%daughter_faces.Count]};
                //     var first_common_point = Array.Find(face_pair[0].corners, element1 => Array.Exists(face_pair[1].corners, element2 => element1 == element2));
                //     var second_common_point = Array.Find(face_pair[0].corners, element1 => Array.Exists(face_pair[1].corners, element2 => element1 == element2 && element2 != first_common_point));
                //     var new_edge = new edge(first_common_point, second_common_point);
                //     foreach ( face f in face_pair)
                //     {
                //         new_edge.Connect_to_Face(f);
                //     }
                // }

                // var face_pairs = from f1 in daughter_faces from f2 in daughter_faces
                // from c1A in f1.corners from c1B in f1.corners from c2A in f2.corners from c2B in f2.corners
                // where f1 != f2 && c1A != c1B && c2A != c2B && c1A == c2A && c1B == c2B
                // select new face[]{f1, f2};

                // UnityEngine.Debug.Log("Attention: " + face_pairs.ToArray().Length);

                // var edge_list = new List<edge>();
                // foreach (face[] pair in face_pairs)
                // {
                //     var first_common_point = Array.Find(pair[0].corners, element1 => Array.Exists(pair[1].corners, element2 => element1 == element2));
                //     var second_common_point = Array.Find(pair[0].corners, element1 => Array.Exists(pair[1].corners, element2 => element1 == element2 && element2 != first_common_point));
                //     var new_edge = new edge(first_common_point, second_common_point);
                //     foreach (face f in pair)
                //     {
                //         new_edge.Connect_to_Face(f);
                //     }
                //     // if (edge_list.Count == 0)
                //     // {
                //     //     edge_list.Add(new_edge);
                //     //     foreach (face f in pair)
                //     //     {
                //     //         new_edge.Connect_to_Face(f);
                //     //     }
                //     // }
                //     // else if (!edge_list.Exists(element => Array.Exists(element.vertices, element2 => element2 == first_common_point) && Array.Exists(element.vertices, element3 => element3 == second_common_point)))
                //     // {
                //     //     edge_list.Add(new_edge);
                //     //     foreach ( face f in pair)
                //     //     {
                //     //         new_edge.Connect_to_Face(f);
                //     //     }
                //     // }
                // }

                var new_edges = new List<edge>();
                foreach (face f1 in daughter_faces)
                {
                    foreach(face f2 in daughter_faces)
                    {
                        if( f1 != f2)
                        {
                            foreach( Vector3 c1A in f1.corners)
                            {
                                foreach (Vector3 c1B in f1.corners)
                                {
                                    if ( c1A != c1B)
                                    {
                                        if (f2.corners.Contains(c1A) && f2.corners.Contains(c1B))
                                        {
                                            bool unique = true;
                                            foreach ( edge e in horizon_edge_list)
                                            {
                                                unique = unique && !(e.vertices.Contains(c1A) && e.vertices.Contains(c1B));
                                            }
                                            foreach (edge e in new_edges)
                                            {
                                                unique = unique && !(e.vertices.Contains(c1A) && e.vertices.Contains(c1B));
                                            }
                                            if (unique)
                                            {
                                                var new_edge = new edge(c1A, c1B);
                                                new_edge.Connect_to_Face(f1);
                                                new_edge.Connect_to_Face(f2);
                                                new_edges.Add(new_edge);
                                            }
                                        }
                                    }

                                }

                            }
                        }
                    }
                }
                UnityEngine.Debug.Log("new edge count: " + new_edges.Count);


                // // Making new edges between new daughter faces
                // var polyhedron_corner_pairs = from f in daughter_faces
                //                          from c1 in f.corners
                //                          from c2 in f.corners
                //                          where c1 != c2 &&
                //                          select new HashSet<Vector3>(c1, c2);


                // List<edge> new_edges = new List<edge>();
                // foreach (edge e in horizon_edge_list)
                // {
                //     foreach (Vector3[] c in polyhedron_corner_pairs)
                //     {
                //         if (Array.Exists(e.vertices, element => element == c[0]) || Array.Exists(e.vertices, element => element == c[1]))
                //         {
                //             new_edges.Add( new edge(c[0], c[1]) );
                //         }

                //     }
                // }
                // foreach (edge e in new_edges)
                // {
                //     daughter_faces.Find(a_face => a_face.corners.Exists(a_corner => a_corner == e.vertices[0]) 
                //     && a_face.corners.Exists(a_corner => a_corner == e.vertices[1]));
                // }

                // List<edge> edge_list = new List<edge>(horizon_edge_list);
                // foreach (Vector3 c1 in polyhedron_corners)
                // {
                //     foreach (Vector3 c2 in polyhedron_corners)
                //     {
                //         if (c1 != c2 && !edge_list.Exists(element => (element.vertices[0] == c2 && element.vertices[1] == c1 ) || (element.vertices[0] == c1 && element.vertices[1] == c2)))
                //         {
                //             edge_list.Add(new edge(c1, c2));
                //         }
                //     }
                // }
                // UnityEngine.Debug.Log("edge length: " + edge_list.Count);
                // foreach (edge e in edge_list)
                // {
                //     foreach (face f in daughter_faces)
                //     {
                //         if (Array.Exists(f.corners, element => element == e.vertices[0]) && Array.Exists(f.corners, element => element == e.vertices[1]) && !horizon_edge_list.Contains(e))
                //         {
                //             e.Connect_to_Face(f);
                //         }
                //     }
                // }

                //Testing
                foreach( face f in daughter_faces)
                {
                    if (f.edges.Count != 3)
                    {
                        UnityEngine.Debug.Log("ERROR: daughter face isn't connected to 3 other faces");
                        UnityEngine.Debug.Log(f.edges.Count);
                    }
                }

                // Compound conflict list of obsolete faces
                face[] hidden_faces = visited_faces.ToArray();
                foreach( face f in hidden_faces)
                {
                    f.hidden = true;
                }
                Vector3[] compound_conflict_list = (from face in hidden_faces from point in face.Conflict_List.Values select point).ToArray();
                UnityEngine.Debug.Log(compound_conflict_list.Length);
                // Remove hidden points
                List<Vector3> remaining_conflicts = new List<Vector3>();
                foreach ( face f in hidden_faces)
                {
                    compound_conflict_list = Clear_Simplex_Volume(compound_conflict_list, new Vector3[]{f.corners[0],f.corners[1] ,f.corners[2] , furthest_from_face} ).ToArray();
                }
                remaining_conflicts = compound_conflict_list.ToList<Vector3>();
                UnityEngine.Debug.Log(remaining_conflicts.Count);
                // Redistribute conflicts
                Distribute_Conflicts(remaining_conflicts.ToArray(), daughter_faces);
                foreach ( face daughter in daughter_faces )
                {
                    if (daughter.Conflict_List.Count <= 0)
                    {
                        daughter.complete = true;
                    }
                    face_list.Add(daughter);
                }
            }
            else
            {
                UnityEngine.Debug.Log("Empty conflict list");
                face_element.complete = true;
            }
            return face_list;
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
            float testing = 0;
            foreach ( Vector3 p in points )
            {
                float current = Distance_from_Line(base_line[0], direction, max_distance, false);
                testing = Distance_from_Line(base_line[0], direction, p, false);
                if ( testing > current )
                {
                    max_distance = p;
                }
            }
            Vector3[] triangle_corners = new Vector3[]{ base_line[0], base_line[1], max_distance };
            // OLD END

            foreach (Vector3 p in points)
            {
                float current = Distance_from_Plane(triangle_corners[0], triangle_corners[1] - triangle_corners[0], triangle_corners[2] - triangle_corners[0], max_distance, false);
                testing = Distance_from_Plane(triangle_corners[0], triangle_corners[1] - triangle_corners[0], triangle_corners[2] - triangle_corners[0], p, false);
                if (System.Math.Abs(testing) > System.Math.Abs(current))
                {
                    max_distance = p;
                }
            }
            Vector3[] tetrahedron_corners = new Vector3[]{triangle_corners[0], triangle_corners[1], triangle_corners[2], max_distance};
            //clearsimplex
            var conflict_points = Clear_Simplex_Volume(points, tetrahedron_corners).ToArray();
            UnityEngine.Debug.Log("number of conflicts: " + conflict_points.Length);
            // construct faces
            List<face> face_list = new List<face>();
            //...
            if (testing >= 0)
            {
                UnityEngine.Debug.Log("Scenario A, test value is: " + testing);
                face_list.Add(new face(tetrahedron_corners[0],tetrahedron_corners[2],tetrahedron_corners[1]));
                face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[1], tetrahedron_corners[3]));
                face_list.Add(new face(tetrahedron_corners[1], tetrahedron_corners[2], tetrahedron_corners[3]));
                face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[3], tetrahedron_corners[2]));
            }
            else
            {
                UnityEngine.Debug.Log("Scenario B, test value is: " + testing);
                face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[1], tetrahedron_corners[2]));
                face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[3], tetrahedron_corners[1]));
                face_list.Add(new face(tetrahedron_corners[1], tetrahedron_corners[3], tetrahedron_corners[2]));
                face_list.Add(new face(tetrahedron_corners[0], tetrahedron_corners[2], tetrahedron_corners[3]));
            }
            List<edge> edge_list = new List<edge>();
            foreach (Vector3 c1 in tetrahedron_corners)
            {
                foreach (Vector3 c2 in tetrahedron_corners)
                {
                    // if ( c1 != c2 && !edge_list.Exists(element => (element.vertices[0] == c2 &&element.vertices[1] == c1)))
                    if (c1 != c2 && !edge_list.Exists(element => (Array.Exists(element.vertices, element1 => element1 == c2) && Array.Exists(element.vertices, element2 => element2 == c1))))
                    {
                        edge_list.Add(new edge(c1, c2));
                    }
                }
            }
            UnityEngine.Debug.Log("edge count: " + edge_list.Count);
            foreach ( edge e in edge_list)
            {
                foreach (face f in face_list)
                {
                    if (Array.Exists(f.corners, element => element == e.vertices[0])   && Array.Exists(f.corners, element => element == e.vertices[1]) )
                    {
                        e.Connect_to_Face(f);
                    }
                }   
            }
            UnityEngine.Debug.Log("done assigning edges");
            // // TESTING!!! Drawing only inital maximal simplex
            // foreach (face face_element in face_list)
            // {
            //     // add to resultant triangle list
            //     foreach (Vector3 corner in face_element.corners)
            //     {
            //         triangle_list.Add(System.Array.IndexOf(points, corner));
            //     }
            // }
            // // TESTING!!!

            // // TESTING!!! Flood Fill of maximal simplex
            // // Flood search
            // // Use a Flood Fill algorithm to search for horizon edges
            // foreach ( face f in face_list)
            // {
            //     var horizon_edge_list = new List<edge>();
            //     var current_face = face_list[0];
            //     var visited_faces = new Stack<face>();
            //     Flood_Fill(new Vector3(10,0,0), f, horizon_edge_list, visited_faces);
            //     UnityEngine.Debug.Log("done flood filling");
            //     UnityEngine.Debug.Log(visited_faces.Count);
            //     UnityEngine.Debug.Log(horizon_edge_list.Count);
            // }
            // // TESTING!!!

            // // TESTING!!! visible face test function
            // foreach ( face f in face_list)
            // {
            //     UnityEngine.Debug.Log("visible: "+( f.Is_Visible_from_Point(new Vector3(10,0,0))));
            //     foreach( Vector3 p in f.corners)
            //     {
            //         UnityEngine.Debug.Log(p);
            //     }
            // }
            // // TESTING!!!

            // // TESTING!!! testing clear simplex
            // var remainers = Clear_Simplex_Volume(points, tetrahedron_corners);
            // foreach ( Vector3 v in remainers)
            // {
            //     UnityEngine.Debug.Log("chek: ");

            //     UnityEngine.Debug.Log(v);
            // }
            // // TESTING!!!

            //distribute conflicts
            Distribute_Conflicts(conflict_points, face_list);
            //it loop
            for( int i = 0 ; i < 100 && face_list.Count > 0 ; i++)
            {
                face_list = iterate(face_list);
                foreach (face face_element in face_list)
                {
                    if (face_element.complete)
                    {
                        // add to resultant triangle list
                        foreach (Vector3 corner in face_element.corners)
                        {
                            triangle_list.Add(System.Array.IndexOf(points, corner));
                        }
                    }
                }
                var new_face_list = from f in face_list where (!f.complete && !f.hidden) select f;
                face_list = new_face_list.ToList<face>();
            }
            return triangle_list.ToArray();
        }
    }
}
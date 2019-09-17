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

            }

        }

        public static List<Vector3> Clear_Simplex_Area()
        {
            // use plane equation to find if inside or outside tetrahedron
            // or use barycentric coordinates (but difficult without numpy)

        }

        class face
        {
            public Vector3[] corners;
            public OrderedDictionary<> Conflict_List;
            public bool complete;

            public face(Vector3 corner1, Vector3 corner2, Vector3 corner3)
            {
                corners = new {corner1, corner2, corner3};
                complete = false;
            }
        }

        public static void Distribute_Conflicts(points, edge_list)
        {
            for ( int i = 0 ; i < points.Count ; i++ )
            {
                List<float> distances = new List<float>();
                for ( int j = 0 ; j < edge_list.Count ; j++ )
                {
                    distances.Add(Distance_from_Plane(edge_list[j][0],edge_list[j][1] - edge_list[j][0], edge_list[j][2] - edge_list[j][0], points[i]));
                }
            }
for p in points:
    distances = []
        for edge in edge_list:
            distances.append(distance_from_line(edge.vertexA, edge.vertexB - edge.vertexA, p))
        edge_list[distances.index(min(distances))].conflict_lists[min(distances)] = p

        }

        public static void it()
        {

        }
    }
}
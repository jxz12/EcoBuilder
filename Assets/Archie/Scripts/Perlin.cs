using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{


    namespace Perlin{

        // to make a perlin texture, one creates a grid of squares
        class corner {
            protected Vector2 position;
            protected Vector2 gradient;

            public Vector2 Position{
                get {
                    return position;
                }
            }

            public Vector2 Gradient{
                get {
                    return gradient;
                }
            }

            public virtual void set_gradient(Dictionary<Vector2,Vector2> point_of_reference) {
                if ( point_of_reference.ContainsKey(position) ) {
                    gradient = point_of_reference[position];
                }
                else {
                    do {
                        gradient  = new Vector2(UnityEngine.Random.Range(-1.0F,1.0F),UnityEngine.Random.Range(-1.0F,1.0F));
                    } while (gradient.magnitude > 1);
                    gradient.Normalize();
                    point_of_reference.Add(position,gradient);
                }
            }

            public corner(Vector2 coordinate) {
                position = coordinate;
            }
        }

        class square {
            public corner[] square_corners;
            private float[] pixel_weight;

            private float ease_curve(float A) { 
                // applies the ease function to a value A
                return (float)( System.Math.Pow(A,5) * 6.0f - 15.0f * System.Math.Pow(A,4) + 10.0f * System.Math.Pow(A,3) );
            }
            private float nlerp(float A, float B, float t) { //non linear interpolation
                return (B - A) * ease_curve(t) + A;
            }

            private static float[] corner_values = new float[4];


            // }
            public float[] process() {
                int x, y;
                for ( int i = 0 ; i < pixel_weight.Length ; i++ ) {
                    x = i % (int)System.Math.Sqrt(pixel_weight.Length);
                    y = i / (int)System.Math.Sqrt(pixel_weight.Length);
                    Vector2 v = new Vector2(x, y);

                    pixel_weight[i] = 0;
                    for ( int j = 0 ; j < 4 ; j++ ) {
                        Vector2 distance = square_corners[j].Position - v;
                        distance.Normalize();
                        corner_values[j] = Vector2.Dot(distance, square_corners[j].Gradient);
                    }
                    // linearly interpolating ( interpolation value is based on x and y ordinate in square )
                    // NB: linear interpolation causes corner discontinuities, it is better to use non linear interpolation
                    float A = (corner_values[1]-corner_values[0]) * (x / (float)System.Math.Sqrt(pixel_weight.Length)) + corner_values[0];
                    float B = (corner_values[3]-corner_values[2]) * (x / (float)System.Math.Sqrt(pixel_weight.Length)) + corner_values[2];
                    pixel_weight[i] = (B-A) * (y / (float)System.Math.Sqrt(pixel_weight.Length)) + A;
                    // instead using non linear interpolation, based on paper by Ken Perlin ( https://mrl.nyu.edu/~perlin/paper445.pdf, but also https://flafla2.github.io/2014/08/09/perlinnoise.html (dates accessed, 30/07/19 and 31/07/19 respectively) )
                    // float A = (corner_values[1]-corner_values[0]) * ease_curve((x / (float)System.Math.Sqrt(pixel_weight.Length))) + corner_values[0];
                    // float B = (corner_values[3]-corner_values[2]) * ease_curve((x / (float)System.Math.Sqrt(pixel_weight.Length))) + corner_values[2];
                    // pixel_weight[i] = (B-A) * ease_curve((y / (float)System.Math.Sqrt(pixel_weight.Length))) + A;

                    // In the end, I switched back to using linear interpolation, it is quicker and Perlin noise is quite slow (too slow)
                }
                return pixel_weight;
            }

            public square(Vector2 bottom_left, int side_length, Dictionary<Vector2,Vector2> point_of_reference) {
                square_corners = new corner[4];
                square_corners[0] = new corner(bottom_left);
                square_corners[1] = new corner(bottom_left + new Vector2(side_length, 0) );
                square_corners[2] = new corner(bottom_left + new Vector2(0, side_length) );
                square_corners[3] = new corner(bottom_left + new Vector2(side_length, side_length) );
                square_corners[0].set_gradient(point_of_reference);
                square_corners[1].set_gradient(point_of_reference);
                square_corners[2].set_gradient(point_of_reference);
                square_corners[3].set_gradient(point_of_reference);

                pixel_weight = new float[side_length * side_length];
            }
        }

        class grid{
            // private Dictionary<Vector2,Vector2> corner_reference;
            private static Dictionary<Vector2, Vector2> corner_reference;

            // private square[] set_of_squares;
            private static square[] set_of_squares;

            public static Color[] tex;

            public static Color[] generate(int texture_size, int f, Color basis, Color weight)
            { // f is some factor which dictates the frequency of the noise
                corner_reference = new Dictionary<Vector2, Vector2>();

                set_of_squares = new square[texture_size * texture_size / (f * f)];

                int length_in_squares = texture_size / f;
                var list_of_results = new List<float[]>();
                for (int i = 0; i < set_of_squares.Length; i++)
                {
                    set_of_squares[i] = new square(new Vector2((i % length_in_squares) * f, (i / length_in_squares) * f), f, corner_reference);
                    list_of_results.Add(set_of_squares[i].process());
                }

                // var reorder = new List<float>();
                tex = new Color[texture_size * texture_size];
                int r = 0;
                int h = -f;
                int c = -1;
                int count = 0;
                for (int i = 0; i <= list_of_results.Count; i++)
                {
                    if (count % (f * length_in_squares) == 0)
                    {
                        c++;
                        h = -f;
                    }
                    if (i % length_in_squares == 0)
                    {
                        h = h + f;
                        i = c * length_in_squares;
                    }
                    if (i < list_of_results.Count)
                    {
                        for (int j = h; j < f + h; j++)
                        {
                            tex[r] = list_of_results[i][j] * weight + basis;
                            r++;
                        }
                    }
                    count++;

                }
                return tex;
            }

            // public grid( int texture_size, int f, Color basis, Color weight ) { // f is some factor which dictates the frequency of the noise
            //     corner_reference = new Dictionary<Vector2,Vector2>();

            //     set_of_squares = new square[texture_size * texture_size / (f * f)];

            //     int length_in_squares =  texture_size / f;
            //     var list_of_results = new List<float[]>();
            //     for ( int i = 0 ; i < set_of_squares.Length ; i++ ) {
            //         set_of_squares[i] = new square(new Vector2((i % length_in_squares) * f, ( i / length_in_squares)*f ), f, corner_reference);
            //         list_of_results.Add(set_of_squares[i].process());
            //     }

            //     // var reorder = new List<float>();
            //     tex = new Color[texture_size * texture_size];
            //     int r = 0;
            //     int h = -f;
            //     int c = -1;
            //     int count = 0;
            //     for ( int i = 0 ; i <= list_of_results.Count ; i++ ) {
            //         if ( count % (f*length_in_squares) == 0 ) {
            //             c++;
            //             h=-f;
            //         }
            //         if ( i % length_in_squares == 0 ) {
            //             h = h + f;
            //             i  = c * length_in_squares;
            //         }
            //         if ( i < list_of_results.Count ) {
            //             for ( int j = h ; j < f + h ; j++ ) {
            //                 tex[r] = list_of_results[i][j] * weight + basis;
            //                 r++;
            //             }
            //         }        
            //         count++;
            //     }


            // }

        }
    }

}
// // animal texture maker
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using System.Linq;

// using System.Diagnostics; // TIME TESTING

// namespace EcoBuilder.Archie
// {
//     public class AnimalTexture
//     {

//         public static Generate(int seed, int size, Vector3 yuv, Vector3[] face_uv_coordiantes, Texture2D face)
//         {
//             // seed random number
//             UnityEngine.Random.InitState(seed);

//             // convert yuv to rgb
//             var yuv_to_rgb = new Matrix4x4();
//             yuv_to_rgb.SetColumn(0, new Vector4
//             (
//                 1,
//                 1,
//                 1,
//                 0
//             ));
//             yuv_to_rgb.SetColumn(1, new Vector4
//             (
//                 0,
//                 -0.39465f,
//                 2.03211f,
//                 0
//             ));
//             yuv_to_rgb.SetColumn(2, new Vector4
//             (
//                 1.13983f,
//                 -0.58060f,
//                 0,
//                 0
//             ));
//             yuv_to_rgb.SetColumn(3, new Vector4
//             (
//                 0,
//                 0,
//                 0,
//                 0
//             ));
//             var rgb_background_colour = new Color(yuv_to_rgb.MultiplyVector((Vector4)(yuv)) + new Vector4(0, 0, 0, 1));
//             // create new texture array
//             var texture = new Texture2D();
//             var colour_values = new Color[size * size];
//             // assuming right face is being used, apply it to texture
//             colour_values = colour_values.Zip(face_array, (main_pixel, face_pixel) => main_pixel + face_pixel).Sum();
//             // filling it with colour   
//             colour_values = (from pixel in colour_values where pixel.a == 0 select rgb_background_colour).ToArray();            
//         }
//     }
// }
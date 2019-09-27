// animal texture maker
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie
{
    public class AnimalTexture : MonoBehaviour
    {
        // [SerializeField] Texture2D[] Face_Textures; // arranged in ascending order of size they represent
        // [SerializeField] Material Base_Animal_Material; // arranged in ascending order of size they represent

        [SerializeField] Texture2D[] EyeTextures, MouthTextures, CheeckTextures, NoseTextures; // arranged in ascending order of size they represent
        [SerializeField] Material Base_Animal_Material; // arranged in ascending order of size they represent

        private Texture2D pick_random(Texture2D[] A)
        {
            return A[UnityEngine.Random.Range(0, A.Length)];
        }

        private Matrix4x4 yuv_to_rgb;
        public void Awake()
        {
            // set up yuv rgb conversion matrix
            yuv_to_rgb = new Matrix4x4();
            yuv_to_rgb.SetColumn(0, new Vector4
            (
                1,
                1,
                1,
                0
            ));
            yuv_to_rgb.SetColumn(1, new Vector4
            (
                0,
                -0.39465f,
                2.03211f,
                0
            ));
            yuv_to_rgb.SetColumn(2, new Vector4
            (
                1.13983f,
                -0.58060f,
                0,
                0
            ));
            yuv_to_rgb.SetColumn(3, new Vector4
            (
                0,
                0,
                0,
                0
            ));
        }

        public Material Generate_and_Apply(int seed, float animal_size, Vector3 yuv)
        {
            // seed random number
            UnityEngine.Random.InitState(seed);

            // convert yuv to rgb
            Color rgb_background_colour = (Vector4)(yuv_to_rgb.MultiplyVector(yuv)) + new Vector4(0,0,0,1); // MultiplyVector takes a Vector3 as its argument

            // // loading face texture
            // var chosen_face = Face_Textures[0];
            // var face_texture_colours = chosen_face.GetPixels();

            // loading face texture
            var face_textures = new List<Texture2D>();
            face_textures.Add(pick_random(EyeTextures));
            face_textures.Add(pick_random(MouthTextures));
            face_textures.Add((UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f) ? pick_random(CheeckTextures) : null);
            face_textures.Add((UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f) ? pick_random(NoseTextures) : null);
            var chosen_face = face_textures[0];
            var face_texture_colours = new Color[chosen_face.width * chosen_face.height];
            foreach (Texture2D t in face_textures)
            {
                if (t != null)
                {
                    var texture_component = t.GetPixels();
                    face_texture_colours = face_texture_colours.Zip(texture_component, (face_pixel, texture_pixel) => face_pixel + texture_pixel).ToArray();
                }
            }

            // size of output texture is same as chosen face texture
            var texture = new Texture2D(chosen_face.width, chosen_face.height);

            // filling empty space with colour, using Alpha blending (face over colour), using Premultiplied Alpha formula
            var background_colour_values = (from pixel in face_texture_colours select rgb_background_colour);
            var colour_values = face_texture_colours.Zip(background_colour_values, (face_pixel, background_pixel) => (face_pixel * face_pixel.a +  background_pixel * background_pixel.a * (1 - face_pixel.a))/(face_pixel.a + background_pixel.a * (1 - face_pixel.a)) ).ToArray();

            // applying  texture
            texture.SetPixels(colour_values);
            texture.Apply();

            var returning_material = new Material(Base_Animal_Material);
            returning_material.SetTexture("_MainTex", texture); // alternative for the one above
            return returning_material;
        }

        // private struct GSpoint
        // {
        //     // A struct storing the relevent concentratoin of substance A and B at a certain 2D coordinate
        //     // based on Gray-Scott equations
        //     public Vector2 position;
        //     public float A;
        //     public float B;
        //     public Matrix4x4 Laplacian;

        //     public GSpoint()
        //     {
        //         Laplacian = new Matrix4x4();
        //         Laplacian.SetColumn(0, new Vector4
        //         (
        //             0.05f,
        //             0.2f,
        //             0.05f,
        //             0
        //         ));
        //         Laplacian.SetColumn(1, new Vector4
        //         (
        //             0.2f,
        //             -1.0f,
        //             0.2f,
        //             0
        //         ));
        //         Laplacian.SetColumn(2, new Vector4
        //         (
        //             0.05f,
        //             0.2f,
        //             0.05f,
        //             0
        //         ));
        //         Laplacian.SetColumn(3, new Vector4
        //         (
        //             0,
        //             0,
        //             0,
        //             0
        //         ));
        //         A = 0.1f;
        //         B = 0;
        //     }

        //     public static List<GSpoint> Calculate_and_Apply_Difference(int width, int height)
        //     {
        //         var cells = from i in Enumerable.Range(0, width * height) select new GSpoint();

        //     }
        // }
    }
}
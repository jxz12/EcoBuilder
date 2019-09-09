using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie
{
    using clamp;
    using Perlin;


    public class generate_texture : MonoBehaviour
    {
        private Texture2D pattern;
        private Color[] colour_values;
        void Awake()
        {
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

        Material target_material;

        public void Refresh(float size = 0, bool first_time = true)
        {
            size = security.clamp(0,1,size);


            // //RAINBOW CODE
            // int width = 128;
            // int height = 128;

            // var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            // texture.filterMode = FilterMode.Point;

            // for ( int i = 0 ; i < width ; i++ ) {
            //     for ( int j = 0 ; j < height ; j++ ) {
            //         texture.SetPixel(j,i,new Color(((float)i)/(float)128,((float)j)/(float)128,0.5F,0));
            //     }
            // }
            // texture.Apply();
            // gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            // PERLIN
            var dim = 64;
            var texture = new Texture2D(dim * 2, dim * 2, TextureFormat.ARGB32, false);
            var map = new Texture2D(128, 128, TextureFormat.ARGB32, false);

            texture.filterMode = FilterMode.Point;

            // layered noise
            Stopwatch layer_generation_time = new Stopwatch(); // TIME TESTING
            layer_generation_time.Start();// TIME TESTING
            // var layers = new grid[(int)System.Math.Log(dim,2)-2];
            var pixels = new Color[((int)System.Math.Log(dim, 2) - 2)][];
            float max_weight = 0;
            float weight = 1;
            // for ( int i = 0 ; i < layers.Length ; i++ ) {
            for (int i = 0; i < pixels.Length; i++)
            {

                weight *= 0.5F + 2.0F * (1.0F - size); //NEW, EXPERIMENTAL
                max_weight += weight;
                pixels[i] = (Perlin.grid.generate(dim,(int)System.Math.Pow(2,i),new Color(0,0,0,0),new Color(weight,weight,weight,0))); // main texture
            }
            UnityEngine.Debug.Log(pixels.Length);
            layer_generation_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("layer generation time: " + layer_generation_time.Elapsed); // TIME TESTING


            Stopwatch texture_folding_time = new Stopwatch(); // TIME TESTING
            texture_folding_time.Start();// TIME TESTING

            // colour_values = new Color[layers[0].tex.Length* 4];
            colour_values = new Color[pixels[0].Length * 4];
            float noise_bias = UnityEngine.Random.Range(0.5F, 0.75F);
            // for ( int i = 0 ; i < layers[0].tex.Length ; i++ ) {
            for (int i = 0; i < pixels[0].Length; i++)
            {
                // Color sum_of_noise = new Color(0.5F,0.5F,0.5F,1);
                // Color sum_of_noise = new Color(0.75F,0.75F,0.75F,1);
                Color sum_of_noise = new Color(noise_bias, noise_bias, noise_bias, 1);
                // for ( int j = 0 ; j < layers.Length ; j++ ) {
                for (int j = 0; j < pixels.Length; j++)
                {

                    // sum_of_noise = sum_of_noise + (0.5F) *layers[j].tex[i]/max_weight;
                    // sum_of_noise = sum_of_noise + (0.25F) * layers[j].tex[i] / max_weight;
                    // sum_of_noise = sum_of_noise + (1 - noise_bias) * layers[j].tex[i] / max_weight;
                    sum_of_noise = sum_of_noise + (1 - noise_bias) * pixels[j][i] / max_weight;
                }

                var x = i % dim;
                var y = i / dim;
                var x_mirror_y = dim * 2 - x - 1;
                var y_mirror_x = dim * 2 - y - 1;
                // texture.SetPixel(x,y,sum_of_noise);
                // texture.SetPixel(x_mirror_y,y,sum_of_noise);
                // texture.SetPixel(x,y_mirror_x,sum_of_noise);
                // texture.SetPixel(x_mirror_y,y_mirror_x,sum_of_noise);

                // colour_values[x + y * dim * 2] = sum_of_noise;
                // colour_values[x_mirror_y + y * dim * 2] = sum_of_noise;
                // colour_values[x + y_mirror_x * dim * 2] = sum_of_noise;
                // colour_values[x_mirror_y + y_mirror_x * dim * 2] = sum_of_noise;
                colour_values[x + y * dim * 2] = Color.white;
                colour_values[x_mirror_y + y * dim * 2] = Color.white;
                colour_values[x + y_mirror_x * dim * 2] = Color.white;
                colour_values[x_mirror_y + y_mirror_x * dim * 2] = Color.white;

            }
            texture.SetPixels(colour_values);
            texture.Apply();

            texture_folding_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("texture folding time: " + texture_folding_time.Elapsed); // TIME TESTING


            if ( first_time )
            {

                Stopwatch normal_time = new Stopwatch(); // TIME TESTING
                normal_time.Start();// TIME TESTING

                colour_values = new Color[128];
                // var o = new grid(128, 8, new Color(0.5F, 0.5F, 0.975F), new Color(0.0125F, 0.0125F, 0.0125F)); // normal map, background is #8080FF
                var o = Perlin.grid.generate(128, 8, new Color(0.5F, 0.5F, 0.975F), new Color(0.0125F, 0.0125F, 0.0125F)); // normal map, background is #8080FF

                map.SetPixels(o);
                map.Apply();

                normal_time.Stop(); // TIME TESTING
                UnityEngine.Debug.Log("normal time: " + normal_time.Elapsed); // TIME TESTING

            }
            if ( first_time )
            {
                target_material = gameObject.GetComponent<Renderer>().material;
            }
            target_material.SetTexture("_MainTex",texture); // alternative for the one above
            pattern = texture;
            if ( first_time )
            {
                target_material.EnableKeyword ("_NORMALMAP");// normal map alteration 
                target_material.SetTexture("_BumpMap",map);// normal map alteration  
            }


        }

        private Matrix4x4 yuv_to_rgb = new Matrix4x4();

        private float u,v;
        public void ColorTexture(float greed = 1, bool first_time = true, bool isProducer = false)
        {
            greed = security.clamp(0,1,greed);
            // greed = greed - 0.5F; // this is because YUV are defined in the range -0.5 to 0.5
            greed = 0.3f + 0.7f * greed;

            if ( first_time )
            {
                u = UnityEngine.Random.Range(-0.5F, 0.5F);
                v = UnityEngine.Random.Range(-0.5F, 0.5F);
                target_material = gameObject.GetComponent<Renderer>().material;
            }

            Vector4 greed_vector = new Vector4 // in YUV format
            (
                greed,        
                u,
                v,
                0
            );
            greed_vector = yuv_to_rgb * greed_vector;
            Color shade_of_greed = new Color(greed_vector.x, greed_vector.y, greed_vector.z);
            // target_material.SetColor("_Color", shade_of_greed); //works fine for standard shader, but for mobile ones we must manually alter the texture
            // //MOBILE SHADING
            Stopwatch colour_time = new Stopwatch(); // TIME TESTING
            colour_time.Start();// TIME TESTING
            Texture2D tex = pattern;
            colour_values = tex.GetPixels();
            for ( int i = 0 ; i < colour_values.Length ; i++ )
            {
                colour_values[i] = colour_values[i] * shade_of_greed;
            }
            
            tex.SetPixels(colour_values);
            tex.Apply();
            target_material.SetTexture("_MainTex", tex); // alternative for the one above
            colour_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("colour time: " + colour_time.Elapsed); // TIME TESTING
            // //MOBILE SHADING
        }

    }
        
}
using System;
using UnityEngine;

namespace EcoBuilder.Archie
{
    [System.Serializable]
    public struct LABColor
    {
        // This script provides a Lab color space in addition to Unity's built in Red/Green/Blue colors.
        // Lab is based on CIE XYZ and is a color-opponent space with L for lightness and a and b for the color-opponent dimensions.
        // Lab color is designed to approximate human vision and so it aspires to perceptual uniformity.
        // The L component closely matches human perception of lightness.
        // Put LABColor.cs in a 'Plugins' folder to ensure that it is accessible to other scripts.
        
        private float L { get; set; }
        private float A { get; set; }
        private float B { get; set; }
    
        // constructor - takes three floats for lightness and color-opponent dimensions
        public LABColor(float l, float a, float b){
            L = l;
            A = a;
            B = b;
        }
    
        // constructor - takes a Color
        public LABColor(Color col){
            LABColor temp = FromColor(col);
            L = temp.L;
            A = temp.A;
            B = temp.B;
        }
    
        // static function for linear interpolation between two LABColors
        public static LABColor Lerp(LABColor a, LABColor b, float t){
            return new LABColor(Mathf.Lerp(a.L, b.L, t), Mathf.Lerp(a.A, b.A, t), Mathf.Lerp(a.B, b.B, t));
        }
    
        // static function for interpolation between two Unity Colors through normalized colorspace
        public static Color Lerp(Color a, Color b, float t){
            return (LABColor.Lerp(LABColor.FromColor(a), LABColor.FromColor(b), t)).ToColor();
        }
    
        // static function for returning the color difference in a normalized colorspace (Delta-E)
        public static float Distance(LABColor a, LABColor b){
            return Mathf.Sqrt(Mathf.Pow((a.L - b.L), 2f) + Mathf.Pow((a.A - b.A), 2f) + Mathf.Pow((a.B - b.B),2f));
        }
    
        // static function for converting from Color to LABColor
        public static LABColor FromColor(Color c){
            float D65x = 0.9505f;
            float D65y = 1.0f;
            float D65z = 1.0890f;
            float rLinear = c.r;
            float gLinear = c.g;
            float bLinear = c.b;
            float r = (rLinear > 0.04045f)? Mathf.Pow((rLinear + 0.055f)/(1f + 0.055f), 2.2f) : (rLinear/12.92f) ;
            float g = (gLinear > 0.04045f)? Mathf.Pow((gLinear + 0.055f)/(1f + 0.055f), 2.2f) : (gLinear/12.92f) ;
            float b = (bLinear > 0.04045f)? Mathf.Pow((bLinear + 0.055f)/(1f + 0.055f), 2.2f) : (bLinear/12.92f) ;
            float x = (r*0.4124f + g*0.3576f + b*0.1805f);
            float y = (r*0.2126f + g*0.7152f + b*0.0722f);
            float z = (r*0.0193f + g*0.1192f + b*0.9505f);
            x = (x>0.9505f)? 0.9505f : ((x<0f)? 0f : x);
            y = (y>1.0f)? 1.0f : ((y<0f)? 0f : y);
            z = (z>1.089f)? 1.089f : ((z<0f)? 0f : z);
            LABColor lab = new LABColor(0f,0f,0f);
            float fx = x/D65x;
            float fy = y/D65y;
            float fz = z/D65z;
            fx = ((fx > 0.008856f)? Mathf.Pow(fx, (1.0f/3.0f)) : (7.787f*fx + 16.0f/116.0f));
            fy = ((fy > 0.008856f)? Mathf.Pow(fy, (1.0f/3.0f)) : (7.787f*fy + 16.0f/116.0f));
            fz = ((fz > 0.008856f)? Mathf.Pow(fz, (1.0f/3.0f)) : (7.787f*fz + 16.0f/116.0f));
            lab.L = 116.0f * fy - 16f;
            lab.A = 500.0f * (fx - fy);
            lab.B = 200.0f * (fy - fz);
            return lab;
        }
    
        // static function for converting from LABColor to Color
        public static Color ToColor(LABColor lab){
            float D65x = 0.9505f;
            float D65y = 1.0f;
            float D65z = 1.0890f;
            float delta = 6.0f/29.0f;
            float fy = (lab.L+16f)/116.0f;
            float fx = fy + (lab.A/500.0f);
            float fz = fy - (lab.B/200.0f);
            float x = (fx > delta)? D65x * (fx*fx*fx) : (fx - 16.0f/116.0f)*3f*(delta*delta)*D65x;
            float y = (fy > delta)? D65y * (fy*fy*fy) : (fy - 16.0f/116.0f)*3f*(delta*delta)*D65y;
            float z = (fz > delta)? D65z * (fz*fz*fz) : (fz - 16.0f/116.0f)*3f*(delta*delta)*D65z;
            float r = x*3.2410f - y*1.5374f - z*0.4986f;
            float g = -x*0.9692f + y*1.8760f - z*0.0416f;
            float b = x*0.0556f - y*0.2040f + z*1.0570f;
            r = (r<=0.0031308f)? 12.92f*r : (1f+0.055f)* Mathf.Pow(r, (1.0f/2.4f)) - 0.055f;
            g = (g<=0.0031308f)? 12.92f*g : (1f+0.055f)* Mathf.Pow(g, (1.0f/2.4f)) - 0.055f;
            b = (b<=0.0031308f)? 12.92f*b : (1f+0.055f)* Mathf.Pow(b, (1.0f/2.4f)) - 0.055f;
            r = (r<0)? 0 : r;
            g = (g<0)? 0 : g;
            b = (b<0)? 0 : b;
            return new Color(r, g, b);
        }
    
        // function for converting an instance of LABColor to Color
        public Color ToColor(){
            return LABColor.ToColor(this);    
        }
    
        // override for string
        public override string ToString(){
            return "L:"+L+" A:"+A+" B:"+B;
        }
    
        // are two LABColors the same?
        public override bool Equals(System.Object obj){
            if(obj==null || GetType()!=obj.GetType()) return false;
            return (this == (LABColor)obj);
        }
    
        // override hashcode for a LABColor
        public override int GetHashCode(){
            return L.GetHashCode() ^ A.GetHashCode() ^ B.GetHashCode();
        }
    
        // Equality operator
        public static bool operator ==(LABColor item1, LABColor item2){
            return (item1.L == item2.L && item1.A == item2.A && item1.B == item2.B);
        }
    
        // Inequality operator
        public static bool operator !=(LABColor item1, LABColor item2){
            return (item1.L != item2.L || item1.A != item2.A || item1.B != item2.B);
        }
    }
    public static class ColorHelper
    {
        public static Color ApplyGamma(Color col, float gamma=1.8f)
        {
            float expo = 1f/gamma;
            float r, g, b;

            r = Mathf.Pow(col.r, expo);
            g = Mathf.Pow(col.g, expo);
            b = Mathf.Pow(col.b, expo);
            
            // if (col.r == 0) r = 0;
            // else            r = Mathf.Pow(col.r, expo);
            // if (col.g == 0) g = 0;
            // else            g = Mathf.Pow(col.g, expo);
            // if (col.b == 0) b = 0;
            // else            b = Mathf.Pow(col.b, expo);

            return new Color(r, g, b);
        }
        public static Color UnapplyGamma(Color col, float gamma=1.8f)
        {
            float expo = gamma;
            float r, g, b;

            r = Mathf.Pow(col.r, expo);
            g = Mathf.Pow(col.g, expo);
            b = Mathf.Pow(col.b, expo);

            return new Color(r, g, b);
        }
        public static Color SetY(Color col, float newY)
        {
            float r = col.r, g = col.g, b = col.b;

            // float y =   0.2126f*r +  0.7152f*g +  0.0722f*b;
            float u = -0.09991f*r - 0.33609f*g +   0.436f*b;
            float v =    0.615f*r - 0.55861f*g - 0.05639f*b;

            return YUVtoRGBtruncated(newY, u, v);
        }
        public static Color SetYGamma(Color col, float newY, float gamma=1.8f)
        {
            col = UnapplyGamma(col, gamma);
            col = SetY(col, newY);
            return ApplyGamma(col);
        }
        public static Color YUVtoRGBtruncated(float y, float u, float v)
        {
            // BT7.09
            float r = y              + 1.28033f*v;
            float g = y - 0.21482f*u - 0.38059f*v;
            float b = y + 2.12798f*u             ;

            if      (r > 1) r = 1;
            else if (r < 0) r = 0;
            if      (g > 1) g = 1;
            else if (g < 0) g = 0;
            if      (b > 1) b = 1;
            else if (b < 0) b = 0;

            return new Color(r, g, b);
        }

        static float HueToRGB(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f/6) return p + (q-p)*6*t;
            if (t < 1f/2) return q;
            if (t < 2f/3) return p + (q-p) * (2f/3-t) * 6;
            return p;
        }
        public static Color HSLToRGB(float h, float s, float l)
        {
            if (s==0)
            {
                return new Color(l, l, l);
            }
            else
            {
                float q = l<.5f? l*(1+s) : l+s-l*s;
                float p = 2 * l - q;

                float r = HueToRGB(p,q,h+1f/3);
                float g = HueToRGB(p,q,h);
                float b = HueToRGB(p,q,h-1f/3);
                return new Color(r, g, b);
            }
        }

        public static Tuple<float,float,float> RGBToHSL(Color rgb)
        {
            float r = rgb.r, g = rgb.g, b = rgb.b;
            float cMax = Mathf.Max(r, g, b);
            float cMin = Mathf.Min(r, g, b);
            float h, s, l;
            h = s = l = (cMax + cMin) / 2f;

            if (cMax == cMin)
            {
                h = s = 0;
            }
            else 
            {
                float delta = cMax - cMin;
                s = l>.5f? delta/(2-cMax-cMin) : delta/(cMax+cMin);
                if (cMax == r)
                    h = (g-b)/delta + (g<b? 6:0);
                else if (cMax == g)
                    h = (b-r)/delta + 2;
                else
                    h = (r-g)/delta + 4;

                h /= 6;
            }

            return Tuple.Create(h,s,l);
        }
        
        public static Color SetLightness(Color col, float newL)
        {
            var HSL = RGBToHSL(col);
            Color newCol = HSLToRGB(HSL.Item1, HSL.Item2, newL);
            return newCol;
        }



        public static Color HSLSquare(float x, float y, float lightness=.5f)
        {
            if (x > 1 || x < 0 || y > 1 || y < 0)
                throw new Exception("x and y coordinates should be between 0 and 1");

            Vector2 square = new Vector2(x - .5f, y - .5f);
            Vector2 circle = 2 * SquareToCircleElliptical(square);
            Vector2 polar = CartesianToPolar(circle);
            Color col = HSLToRGB(polar.y / (2 * Mathf.PI), polar.x, lightness);
            return col;
        }
        public static Color HSVSquare(float x, float y, float value=1f)
        {
            if (x > 1 || x < 0 || y > 1 || y < 0)
                throw new Exception("x and y coordinates should be between 0 and 1");

            Vector2 square = new Vector2(x - .5f, y - .5f);
            Vector2 circle = 2 * SquareToCircleElliptical(square);
            Vector2 polar = CartesianToPolar(circle);
            Color col = Color.HSVToRGB(polar.y / (2 * Mathf.PI), polar.x, value);
            return col;
        }

            
        public static Vector2 SquareToCircleElliptical(Vector2 square)
        {
            float xCircle = square.x * Mathf.Sqrt(1-square.y*square.y/2);
            float yCircle = square.y * Mathf.Sqrt(1-square.y*square.y/2);
            return new Vector2(xCircle, yCircle);
        }
        public static Vector2 SquareToCircleSimpleStretching(Vector2 square)
        {
            float xCircle, yCircle;
            float mag = square.magnitude;
            if (mag == 0)
                return Vector2.zero;
            if (square.x*square.x >= square.y*square.y)
            {
                xCircle = Mathf.Sign(square.x) * ((square.x*square.x)/mag);
                yCircle = Mathf.Sign(square.x) * ((square.x*square.y)/mag);
            }
            else
            {
                xCircle = Mathf.Sign(square.y) * ((square.x*square.y)/mag); // !!! wrong in that paper lol
                yCircle = Mathf.Sign(square.y) * ((square.y*square.y)/mag);
            }
            return new Vector2(xCircle, yCircle);
        }
        public static Vector2 CartesianToPolar(Vector2 cart)
        {
            float mag = Mathf.Sqrt(Mathf.Pow(cart.x,2) + Mathf.Pow(cart.y,2));

            if (cart.x == 0) {
                if (cart.y > 0) {
                    return new Vector2(mag, Mathf.PI/2);
                } else if (cart.y < 0) {
                    return new Vector2(mag, 3*Mathf.PI/2);
                } else {
                    return new Vector2(0,0);
                }
            } else {
                float theta = Mathf.Atan(cart.y/cart.x);
                if (cart.x>0) {
                    if (cart.y<0) {
                        theta += 2*Mathf.PI;
                    }
                } else {
                    theta += Mathf.PI;
                }
                return new Vector2(mag, theta);
            }
        }
    }

}
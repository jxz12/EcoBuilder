using System;
using UnityEngine;

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

        // float y = 0.2126f*r + 0.7152f*g + 0.0722f*b;
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
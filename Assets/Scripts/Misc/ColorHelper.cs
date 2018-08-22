using System;
using UnityEngine;

public static class ColorHelper {
    public static Color HSLToRGB(float h, float s, float l) {
        h *= 360;
        float c = (1 - Mathf.Abs(2*l - 1)) * s;
        float x = c * (1-Mathf.Abs(((h/60)%2) - 1));
        float m = l - c/2;

        Color rgb;
        if (h < 60) {
            rgb = new Color(c,x,0);
        } else if (h < 120) {
            rgb = new Color(x,c,0);
        } else if (h < 180) {
            rgb = new Color(0,c,x);
        } else if (h < 240) {
            rgb = new Color(0,x,c);
        } else if (h < 300) {
            rgb = new Color(x,0,c);
        } else {
            rgb = new Color(c,0,x);
        }
        rgb = new Color(rgb.r+m, rgb.g+m, rgb.b+m);
        return rgb;
    }

    public static Tuple<float,float,float> RGBToHSL (Color rgb) {
        float r = rgb.r, g = rgb.g, b = rgb.b;
        float cMax = Mathf.Max(r, g, b);
        float cMin = Mathf.Min(r, g, b);
        float delta = cMax - cMin;

        float h = 0;
        if (cMax == r) {
            h = 60 * (((g-b)/delta)%6);
        } else if (cMax == g) {
            h = 60 * (((b-r)/delta)+2);
        } else {
            h = 60 * (((r-g)/delta)+4);
        }
        float l = (cMax + cMin) / 2;

        float s = 0;
        if (delta != 0) {
            s = delta/(1-Mathf.Abs(2*l - 1));
        }
        return Tuple.Create(h,s,l);
    }
    
    public static Color SetLightness(Color col, float l) {
        var HSL = RGBToHSL(col);
        // Debug.Log(HSL.Item1 + " " + HSL.Item2 + " " + HSL.Item3);
        return HSLToRGB(HSL.Item1, HSL.Item2, l);
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
        // Debug.Log(square);
        // Debug.Log(circle);
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
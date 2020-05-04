using UnityEngine;
using System.Collections;

namespace EcoBuilder
{
    public static class Tweens
    {
        public static float QuadraticInOut(float t)
        {
            if (t<.5f) {
                return 2*t*t;
            } else {
                return (4-2*t)*t-1;
            }
        }
        public static float CubicInOut(float t)
        {
            if (t < .5f) {
                // t = 2*t*t;
                return 4*t*t*t;
            } else {
                // t = -1 + (4-2*t)*t;
                t -= 1;
                return 4*t*t*t + 1;
            }
        }
        public static float QuadraticOut(float t)
        {
            return -(--t)*t + 1;
        }
        public static float CubicOut(float t)
        {
            return (--t)*t*t+1;
        }
        public static IEnumerator Pivot(RectTransform rt, Vector2 start, Vector2 end, float duration=1)
        {
            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = QuadraticInOut((Time.time-startTime)/duration);
                rt.pivot = Vector2.Lerp(start, end, t);
                yield return null;
            }
            rt.pivot = end;
        }
        // TODO: move other tweens in here too
    }
}
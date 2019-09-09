using System;
namespace EcoBuilder.Archie
{
    namespace clamp {
        public class security
        {
            public static float clamp(float bottom, float top, float A) {
                if ( A < bottom )
                {
                    A = bottom;
                }
                if ( A > top )
                {
                    A = top;
                }
                return A;
            }
        }
    }
}
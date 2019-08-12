using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{
    namespace Distribution {
        public static class normal {
            // Using Box-Muller transform to convert unifrom random numbers to normally distributed random numbers
            // Basic form

            public static float Random(float mean = 0, float standard_distribution = 1, bool positive_only = false) {
                float expectation = mean;
                float variance = (float)System.Math.Pow(standard_distribution, 2);
                bool strictly_postive = positive_only;
                float u1 = UnityEngine.Random.Range(0.0F, 1.0F);
                float u2 = UnityEngine.Random.Range(0.0F, 1.0F);
                // equation based of wikipedia page (https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform), accessed 22/07/2019
                double unit_normal_number = System.Math.Sqrt(-2.0F * System.Math.Log(u1)) * System.Math.Cos(u2 * System.Math.PI);

                if ( strictly_postive ) {
                    unit_normal_number = System.Math.Abs(unit_normal_number);
                }

                // converting unit distribution ( expectation = 0, standard deviation = 1 ) to specified distribution of this normal object
                return (float)( unit_normal_number * System.Math.Sqrt(variance) ) + expectation ;
            }
        }

    }
}
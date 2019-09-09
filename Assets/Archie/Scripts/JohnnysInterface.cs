// interface
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EcoBuilder.Archie
{

    namespace JohnnysInterface {
    public interface ISpeciesGenerator
    {
        GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population = -1);
        void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population = -1); // NOTE this has been edited from the one Johnny sent me
    }
}
}
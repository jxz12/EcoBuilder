using UnityEngine;

namespace EcoBuilder
{
    public abstract class ProceduralMeshGenerator : MonoBehaviour
    {
        public abstract GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population = -1);
        public abstract void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population = -1);
    }
}
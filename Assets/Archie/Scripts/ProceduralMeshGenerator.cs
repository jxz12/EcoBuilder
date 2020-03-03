using UnityEngine;

namespace EcoBuilder.Archie
{
    public abstract class ProceduralMeshGenerator : MonoBehaviour
    {
        public abstract GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed);
        public abstract void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed);

        public abstract void OutlineSpecies(GameObject species, Color colour);
        public abstract void KillSpecies(GameObject species);
        public abstract void RescueSpecies(GameObject species);
    }
}
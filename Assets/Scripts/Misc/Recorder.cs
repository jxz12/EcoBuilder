using System;

namespace EcoBuilder
{
    public class Recorder
    {
        public void RecordSpeciesSpawn(int idx, Action<int> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void RecordSpeciesDespawn(int idx, Action<int> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void RecordSpeciesType(int idx, bool type, Action<int, bool> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void RecordSpeciesSize(int idx, float size, Action<int, float> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void RecordSpeciesGreed(int idx, float greed, Action<int, float> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void RecordInteractionAdded(int res, int con, Action<int, int> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void RecordInteractionRemoved(int res, int con, Action<int, int> Undo)
        {
            UnityEngine.Debug.Log("TODO:");
        }

        // connected to buttons
        public void Undo()
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void Redo()
        {
            UnityEngine.Debug.Log("TODO:");
        }
        public void Record()
        {
            UnityEngine.Debug.Log("TODO:");
        }
    }
}
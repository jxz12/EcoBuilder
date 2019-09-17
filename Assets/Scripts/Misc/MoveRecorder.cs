using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EcoBuilder
{
    public class MoveRecorder : MonoBehaviour
    {
        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;

        class Move
        {
            public Action Undo { get; private set; }
            public Action Redo { get; private set; }
            public Move(Action Undo, Action Redo)
            {
                this.Undo = Undo;
                this.Redo = Redo;
            }
        }

        Stack<Move> undos, redos;
        List<string> record;
        void Start()
        {
            undos = new Stack<Move>();
            redos = new Stack<Move>();
            undoButton.onClick.AddListener(Undo);
            redoButton.onClick.AddListener(Redo);
        }
        void NewMove(Move move)
        {
            undos.Push(move);
            redos.Clear();
            undoButton.interactable = true;
            redoButton.interactable = false;
        }

        public void SpeciesSpawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Despawn(idx), ()=>Respawn(idx)));
        }
        public void SpeciesDespawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Respawn(idx), ()=>Despawn(idx)));
        }
        // public void RecordSpeciesType(int idx, bool type, Action<int, bool> Undo)
        // {
        //     UnityEngine.Debug.Log("TODO:");
        // }
        // // FIXME: for these two, only track moves if the previous is not the same type
        // public void RecordSpeciesSize(int idx, float size, Action<int, float> Undo)
        // {
        //     UnityEngine.Debug.Log("TODO:");
        // }
        // public void RecordSpeciesGreed(int idx, float greed, Action<int, float> Undo)
        // {
        //     UnityEngine.Debug.Log("TODO:");
        // }
        public void InteractionAdded(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Remove(res,con), ()=>Add(res,con)));
        }
        public void InteractionRemoved(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Add(res,con), ()=>Remove(res,con)));
        }

        // connected to buttons
        public void Undo()
        {
            Move toUndo = undos.Pop();
            toUndo.Undo();
            redos.Push(toUndo);

            undoButton.interactable = undos.Count > 0;
            redoButton.interactable = true;
        }
        public void Redo()
        {
            Move toRedo = redos.Pop();
            toRedo.Redo();
            undos.Push(toRedo);

            redoButton.interactable = redos.Count > 0;
            undoButton.interactable = true;
        }
        public void Record()
        {
            GetComponent<Animator>().SetBool("Visible", false);
            UnityEngine.Debug.Log("TODO: write to file");
        }
    }
}
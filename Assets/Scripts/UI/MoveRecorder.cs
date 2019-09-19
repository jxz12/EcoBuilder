using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class MoveRecorder : MonoBehaviour
    {
        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;

        class Move
        {
            public bool Structural { get; private set; }
            public Action Undo { get; private set; }
            public Action Redo { get; private set; }
            public Move(Action Undo, Action Redo, bool structural)
            {
                this.Undo = Undo;
                this.Redo = Redo;
                this.Structural = structural;
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
            print(undos.Count);
        }

        public void SpeciesSpawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Despawn(idx), ()=>Respawn(idx), true));
        }
        public void SpeciesDespawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Respawn(idx), ()=>Despawn(idx), true));
        }
        public void InteractionAdded(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Remove(res,con), ()=>Add(res,con), true));
        }
        public void InteractionRemoved(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Add(res,con), ()=>Remove(res,con), true));
        }

        public void TypeSet(int idx, bool prev, bool current, Action<int, bool> SetType)
        {
            NewMove(new Move(()=>SetType(idx,prev), ()=>SetType(idx,current), false));
        }
        public void SizeSet(int idx, float prev, float current, Action<int, float> SetSize)
        {
            NewMove(new Move(()=>SetSize(idx,prev), ()=>SetSize(idx,current), false));
        }
        public void GreedSet(int idx, float prev, float current, Action<int, float> SetGreed)
        {
            NewMove(new Move(()=>SetGreed(idx,prev), ()=>SetGreed(idx,current), false));
        }

        // connected to buttons
        public void Undo()
        {
            // do at least one undo
            undos.Peek().Undo();
            redos.Push(undos.Pop());

            while (undos.Count>0 && !undos.Peek().Structural) // keep going until last structural change
            {
                undos.Peek().Undo();
                redos.Push(undos.Pop());
            }

            undoButton.interactable = undos.Count > 0;
            redoButton.interactable = true;
        }
        public void Redo()
        {
            // do at least one undo
            redos.Peek().Undo();
            undos.Push(redos.Pop());

            while (redos.Count>0 && !redos.Peek().Structural) // keep going until last structural change
            {
                redos.Peek().Undo();
                undos.Push(redos.Pop());
            }

            redoButton.interactable = redos.Count > 0;
            undoButton.interactable = true;
        }
        public void Record()
        {
            GetComponent<Animator>().SetBool("Visible", false);
            UnityEngine.Debug.Log("TODO: write to file, send to server");
        }
    }
}
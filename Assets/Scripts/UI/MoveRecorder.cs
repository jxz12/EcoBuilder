using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class MoveRecorder : MonoBehaviour
    {
        public event Action<int> OnSpeciesUndone;

        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;

        class Move
        {
            public int idx;
            public bool isStructural;
            public Action Undo;
            public Action Redo;
            public Move(Action Undo, Action Redo, int idx, bool isStructural)
            {
                this.Undo = Undo;
                this.Redo = Redo;
                this.idx = idx;
                this.isStructural = isStructural;
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
            NewMove(new Move(()=>Despawn(idx), ()=>Respawn(idx), idx, true));
        }
        public void SpeciesDespawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Respawn(idx), ()=>Despawn(idx), idx, true));
        }
        public void InteractionAdded(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Remove(res,con), ()=>Add(res,con), int.MinValue, true));
        }
        public void InteractionRemoved(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Add(res,con), ()=>Remove(res,con), int.MinValue, true));
        }

        public void TypeSet(int idx, bool prev, bool current, Action<int, bool> SetType)
        {
            NewMove(new Move(()=>SetType(idx,prev), ()=>SetType(idx,current), idx, false));
        }
        public void SizeSet(int idx, float prev, float current, Action<int, float> SetSize)
        {
            NewMove(new Move(()=>SetSize(idx,prev), ()=>SetSize(idx,current), idx, false));
        }
        public void GreedSet(int idx, float prev, float current, Action<int, float> SetGreed)
        {
            NewMove(new Move(()=>SetGreed(idx,prev), ()=>SetGreed(idx,current), idx, false));
        }

        // connected to buttons
        public void Undo()
        {
            int idx = undos.Peek().idx;
            if (undos.Peek().isStructural)
            {
                // do at least one undo
                undos.Peek().Undo();
                redos.Push(undos.Pop());
            }
            else
            {
                // keep going until last structural change to this species
                while (undos.Count>0 && !undos.Peek().isStructural && undos.Peek().idx==idx)
                {
                    undos.Peek().Undo();
                    redos.Push(undos.Pop());
                }
            }

            undoButton.interactable = undos.Count > 0;
            redoButton.interactable = true;

            if (idx != int.MinValue)
            {
                OnSpeciesUndone.Invoke(idx);
            }
        }
        public void Redo()
        {
            int idx = redos.Peek().idx;
            if (redos.Peek().isStructural)
            {
                // do at least one undo
                redos.Peek().Redo();
                undos.Push(redos.Pop());
            }
            else
            {
                // keep going until last structural change to this species
                while (redos.Count>0 && !redos.Peek().isStructural && redos.Peek().idx==idx)
                {
                    redos.Peek().Redo();
                    undos.Push(redos.Pop());
                }
            }

            redoButton.interactable = redos.Count > 0;
            undoButton.interactable = true;

            if (idx != int.MinValue)
            {
                OnSpeciesUndone.Invoke(idx);
            }
        }
        public void Record()
        {
            GetComponent<Animator>().SetBool("Visible", false);
            UnityEngine.Debug.Log("TODO: write to file, send to server");
        }
    }
}
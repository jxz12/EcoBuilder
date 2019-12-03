using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class MoveRecorder : MonoBehaviour
    {
        public event Action<int> OnSpeciesUndone;
        public event Action<int> OnSpeciesMemoryLeak;

        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;

        class Move
        {
            public int idx;
            public enum Type { Spawn, Despawn, Interaction, Production, Size, Greed };
            public Type type;
            public Action Undo;
            public Action Redo;
            public Move(Action Undo, Action Redo, int idx, Type type)
            {
                this.Undo = Undo;
                this.Redo = Redo;
                this.idx = idx;
                this.type = type;
            }
        }

        Stack<Move> undos, redos;
        List<string> record;
        void Awake()
        {
            undos = new Stack<Move>();
            redos = new Stack<Move>();
            undoButton.onClick.AddListener(Undo);
            redoButton.onClick.AddListener(Redo);
        }
        void NewMove(Move move)
        {
            undos.Push(move);
            foreach (Move redo in redos)
            {
                // spawned species can never be recovered if previously undone
                if (redo.type == Move.Type.Spawn)
                    OnSpeciesMemoryLeak.Invoke(redo.idx);
            }
            redos.Clear();
            undoButton.interactable = true;
            redoButton.interactable = false;
        }

        public void SpeciesSpawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Despawn(idx), ()=>Respawn(idx), idx, Move.Type.Spawn));
        }
        public void SpeciesDespawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            NewMove(new Move(()=>Respawn(idx), ()=>Despawn(idx), idx, Move.Type.Despawn));
        }
        public void InteractionAdded(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Remove(res,con), ()=>Add(res,con), int.MinValue, Move.Type.Interaction));
        }
        public void InteractionRemoved(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            NewMove(new Move(()=>Add(res,con), ()=>Remove(res,con), int.MinValue, Move.Type.Interaction));
        }

        public void ProductionSet(int idx, bool prev, bool current, Action<int, bool> SetType)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Production && undos.Peek().idx == idx))
                undos.Peek().Redo = ()=>SetType(idx,current); // don't record multiple changes
            else
                NewMove(new Move(()=>SetType(idx,prev), ()=>SetType(idx,current), idx, Move.Type.Production));
        }
        public void SizeSet(int idx, float prev, float current, Action<int, float> SetSize)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Size && undos.Peek().idx == idx))
                undos.Peek().Redo = ()=>SetSize(idx,current);
            else
                NewMove(new Move(()=>SetSize(idx,prev), ()=>SetSize(idx,current), idx, Move.Type.Size));
        }
        public void GreedSet(int idx, float prev, float current, Action<int, float> SetGreed)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Greed && undos.Peek().idx == idx))
                undos.Peek().Redo = ()=>SetGreed(idx,current);
            else
                NewMove(new Move(()=>SetGreed(idx,prev), ()=>SetGreed(idx,current), idx, Move.Type.Greed));
        }


        // connected to buttons
        void Undo()
        {
            Move toUndo = undos.Pop();
            toUndo.Undo();
            redos.Push(toUndo);

            undoButton.interactable = undos.Count > 0;
            redoButton.interactable = true;

            if (toUndo.type != Move.Type.Interaction)
                OnSpeciesUndone.Invoke(toUndo.idx);
        }
        void Redo()
        {
            Move toRedo = redos.Pop();
            toRedo.Redo();
            undos.Push(toRedo);

            redoButton.interactable = redos.Count > 0;
            undoButton.interactable = true;

            if (toRedo.type != Move.Type.Interaction)
                OnSpeciesUndone.Invoke(toRedo.idx);
        }
        public void RecordMoves()
        {
            GetComponent<Animator>().SetBool("Visible", false);
            UnityEngine.Debug.Log("TODO: write to file, send to server");
        }
    }
}
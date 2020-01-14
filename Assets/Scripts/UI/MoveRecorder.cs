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
            public int idx { get; private set; }
            public enum Type { None=0, Spawn, Despawn, Link, Unlink, Production, Size, Greed, Undo, Redo }; // uuugh
            public Type type { get; private set; }
            public Action Undo { get; private set; }
            public Action Redo { get; private set; }
            public Move(Action Undo, Action Redo, int idx, Type type)
            {
                this.Undo = Undo;
                this.Redo = Redo;
                this.idx = idx;
                this.type = type;
            }
        }

        Stack<Move> undos, redos;
        List<Tuple<int, int, int, int>> record;
        void Awake()
        {
            undos = new Stack<Move>();
            redos = new Stack<Move>();
            undoButton.onClick.AddListener(Undo);
            redoButton.onClick.AddListener(Redo);

            record = new List<Tuple<int,int,int,int>>();
            record.Add(Tuple.Create((int)Move.Type.None, (int)Time.time, -1, -1));
        }
        void PushMove(Move move)
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
            PushMove(new Move(()=>Despawn(idx), ()=>Respawn(idx), idx, Move.Type.Spawn));
            record.Add(Tuple.Create((int)Move.Type.Spawn, (int)Time.time, idx, -1));
        }
        public void SpeciesDespawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            PushMove(new Move(()=>Respawn(idx), ()=>Despawn(idx), idx, Move.Type.Despawn));
            record.Add(Tuple.Create((int)Move.Type.Despawn, (int)Time.time, idx, -1));
        }
        public void InteractionAdded(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            PushMove(new Move(()=>Remove(res,con), ()=>Add(res,con), int.MinValue, Move.Type.Link));
            record.Add(Tuple.Create((int)Move.Type.Link, (int)Time.time, res, con));
        }
        public void InteractionRemoved(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            PushMove(new Move(()=>Add(res,con), ()=>Remove(res,con), int.MinValue, Move.Type.Unlink));
            record.Add(Tuple.Create((int)Move.Type.Unlink, (int)Time.time, res, con));
        }
        public void ProductionSet(int idx, bool prev, bool current, Action<int, bool> SetType)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Production && undos.Peek().idx == idx))
                undos.Pop(); // don't record multiple changes

            PushMove(new Move(()=>SetType(idx,prev), ()=>SetType(idx,current), idx, Move.Type.Production));
            record.Add(Tuple.Create((int)Move.Type.Production, (int)Time.time, idx, current?1:0));
        }
        public void SizeSet(int idx, float prev, float current, Action<int, float> SetSize)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Size && undos.Peek().idx == idx))
                undos.Pop();

            PushMove(new Move(()=>SetSize(idx,prev), ()=>SetSize(idx,current), idx, Move.Type.Size));
            record.Add(Tuple.Create((int)Move.Type.Size, (int)Time.time*1000, idx, (int)current*1000));
        }
        public void GreedSet(int idx, float prev, float current, Action<int, float> SetGreed)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Greed && undos.Peek().idx == idx))
                undos.Pop();

            PushMove(new Move(()=>SetGreed(idx,prev), ()=>SetGreed(idx,current), idx, Move.Type.Greed));
            record.Add(Tuple.Create((int)Move.Type.Greed, (int)Time.time*1000, idx, (int)current*1000));
        }

        // connected to buttons
        void Undo()
        {
            Move toUndo = undos.Pop();
            toUndo.Undo();
            redos.Push(toUndo);

            undoButton.interactable = undos.Count > 0;
            redoButton.interactable = true;

            if (toUndo.type!=Move.Type.Link && toUndo.type!=Move.Type.Unlink)
                OnSpeciesUndone.Invoke(toUndo.idx); // to focus when needed

            record.Add(Tuple.Create((int)Move.Type.Undo, -1, -1, -1));
        }
        void Redo()
        {
            Move toRedo = redos.Pop();
            toRedo.Redo();
            undos.Push(toRedo);

            redoButton.interactable = redos.Count > 0;
            undoButton.interactable = true;

            if (toRedo.type!=Move.Type.Link && toRedo.type!=Move.Type.Unlink)
                OnSpeciesUndone.Invoke(toRedo.idx);

            record.Add(Tuple.Create((int)Move.Type.Redo, -1, -1, -1));
        }
        public int[,] RecordMoves()
        {
            GetComponent<Animator>().SetBool("Visible", false);

            var array = new int[record.Count,4];
            for (int i=0; i<record.Count; i++)
            {
                array[i,0] = record[i].Item1;
                array[i,1] = record[i].Item2;
                array[i,2] = record[i].Item3;
                array[i,3] = record[i].Item4;
                print(array[i,0]+" "+array[i,1]+" "+array[i,2]+" "+array[i,3]);
            }
            return array;
        }
    }
}
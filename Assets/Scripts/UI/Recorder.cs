using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Recorder : MonoBehaviour
    {
        public event Action<int> OnSpeciesTraitsChanged;
        public event Action<int> OnSpeciesMemoryLeak;
        public event Action OnUndoOrRedo;

        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;

        class Move // NOTE: the game cannot be recreated
        {
            public int idx { get; private set; }
            public enum Type { Spawn, Despawn, Link, Unlink, Size, Greed };
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
        List<string> record;
        void Awake()
        {
            undos = new Stack<Move>();
            redos = new Stack<Move>();
            undoButton.onClick.AddListener(Undo);
            redoButton.onClick.AddListener(Redo);

            record = new List<string>();
            RecordAction("");
            print("TODO: limit the size of the record to something like a million or something");
        }
        void PushMove(Move move)
        {
            undos.Push(move);
            foreach (Move redo in redos)
            {
                // spawned species can never be recovered if previously undone
                if (redo.type == Move.Type.Spawn) {
                    OnSpeciesMemoryLeak.Invoke(redo.idx);
                }
            }
            redos.Clear();
            undoButton.interactable = true;
            redoButton.interactable = false;
        }
        void RecordAction(string action)
        {
            record.Add(((int)(Time.time*10)).ToString() + action);
        }
        void UpdateAction(string action)
        {
            record[record.Count-1] = ((int)(Time.time*10)).ToString() + action;
        }

        public void SpeciesSpawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            PushMove(new Move(()=>Despawn(idx), ()=>Respawn(idx), idx, Move.Type.Spawn));
            RecordAction("+"+idx);
        }
        public void SpeciesDespawn(int idx, Action<int> Respawn, Action<int> Despawn)
        {
            PushMove(new Move(()=>Respawn(idx), ()=>Despawn(idx), idx, Move.Type.Despawn));
            RecordAction("-"+idx);
        }
        public void InteractionAdded(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            PushMove(new Move(()=>Remove(res,con), ()=>Add(res,con), int.MinValue, Move.Type.Link));
            RecordAction("*"+res+","+con);
        }
        public void InteractionRemoved(int res, int con, Action<int,int> Add, Action<int,int> Remove)
        {
            PushMove(new Move(()=>Add(res,con), ()=>Remove(res,con), int.MinValue, Move.Type.Unlink));
            RecordAction("/"+res+","+con);
        }

        // public void ProductionSet(int idx, bool prev, bool current, Action<int, bool> SetType)
        // {
        //     PushMove(new Move(()=>SetType(idx,prev), ()=>SetType(idx,current), idx, Move.Type.Production));
        //     RecordAction("p"+(current?"1":"0"));
        // }

        // the next two only track the latest in a string of actions
        // to prevent the stack growing really big on swipe
        public void SizeSet(int idx, int prev, int current, Action<int, int> SetSize)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Size && undos.Peek().idx == idx))
            {
                var prevMove = undos.Pop();
                PushMove(new Move(prevMove.Undo, ()=>SetSize(idx,current), idx, Move.Type.Size));
                UpdateAction("s"+(int)(current*8));
            }
            else
            {
                PushMove(new Move(()=>SetSize(idx,prev), ()=>SetSize(idx,current), idx, Move.Type.Size));
                RecordAction("s"+(int)(current*8));
            }
        }
        public void GreedSet(int idx, int prev, int current, Action<int, int> SetGreed)
        {
            if (undos.Count > 0 && (undos.Peek().type == Move.Type.Size && undos.Peek().idx == idx))
            {
                var prevMove = undos.Pop();
                PushMove(new Move(prevMove.Undo, ()=>SetGreed(idx,current), idx, Move.Type.Greed));
                UpdateAction("g"+current);
            }
            else
            {
                PushMove(new Move(()=>SetGreed(idx,prev), ()=>SetGreed(idx,current), idx, Move.Type.Greed));
                RecordAction("g"+current);
            }
        }

        // connected to buttons
        void Undo()
        {
            Move toUndo = undos.Pop();
            toUndo.Undo();
            redos.Push(toUndo);

            undoButton.interactable = undos.Count > 0;
            redoButton.interactable = true;

            if (toUndo.type==Move.Type.Size || toUndo.type==Move.Type.Greed)
            {
                OnSpeciesTraitsChanged?.Invoke(toUndo.idx);
            }
            RecordAction("<");
            OnUndoOrRedo?.Invoke();
        }
        void Redo()
        {
            Move toRedo = redos.Pop();
            toRedo.Redo();
            undos.Push(toRedo);

            redoButton.interactable = redos.Count > 0;
            undoButton.interactable = true;

            if (toRedo.type==Move.Type.Size || toRedo.type==Move.Type.Greed)
            {
                OnSpeciesTraitsChanged?.Invoke(toRedo.idx);
            }
            RecordAction(">");
            OnUndoOrRedo?.Invoke();
        }
        public string GetActions()
        {
            return string.Join(";", record);
        }
        public void Finish()
        {
            GetComponent<Animator>().SetBool("Visible", false);
        }
    }
}
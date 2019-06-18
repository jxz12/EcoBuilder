using UnityEngine;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class GameMaster : MonoBehaviour
    {
        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.StatusBar status;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        void Start()
        {
            inspector.OnIncubated +=          ()=> nodelink.MoveLeft();
            inspector.OnUnincubated +=        ()=> nodelink.MoveMiddle();
            inspector.OnSpawned +=         (i,g)=> model.AddSpecies(i);
            inspector.OnSpawned +=         (i,g)=> nodelink.AddNode(i,g);

            inspector.OnProducerSet +=     (i,b)=> model.SetSpeciesIsProducer(i,b);
            inspector.OnProducerSet +=     (i,b)=> nodelink.SetNodeAsSourceOnly(i,b);
            inspector.OnSizeSet +=         (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=        (i,x)=> model.SetSpeciesGreediness(i,x);

            /////////////////////////////////////////////////////////////////////////

            nodelink.OnNodeFocused +=        (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=           ()=> inspector.Uninspect();
            nodelink.OnLinkAdded +=        (i,j)=> model.AddInteraction(i,j);
            nodelink.OnLinkRemoved +=      (i,j)=> model.RemoveInteraction(i,j);
            nodelink.OnDroppedOn +=           ()=> inspector.TrySpawnNew();

            nodelink.OnConstraints +=         ()=> status.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=         ()=> status.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=         ()=> status.DisplayMaxLoop(nodelink.MaxLoop);

            ///////////////////////////////////////////////////////////////////////////

            model.OnEndangered +=            (i)=> nodelink.FlashNode(i);
            model.OnRescued +=               (i)=> nodelink.IdleNode(i);

            // model.OnEquilibrium +=            ()=> status.FillStars(model.Feasible, model.Stable, model.Nonreactive);
            model.OnEquilibrium +=            ()=> nodelink.ResizeNodes(i=> model.GetAbundance(i));
            model.OnEquilibrium +=            ()=> nodelink.ReflowLinks((i,j)=> model.GetFlux(i,j));

            ////////////////////////////////////////////////////////////////////////////

            status.OnGameFinished +=          (x)=> FinishGame(x);
            // status.OnMainMenu +=               ()=> BackToMenu();
            // status.OnReplay +=                 ()=> Replay();

            ////////////////////////////////////////////////////////////////////////////////

            // TODO: move into inspector, with activation on strong-focus
            nodelink.OnNodeRemoved +=        (i)=> inspector.UnspawnSpecies(i);
            nodelink.OnNodeRemoved +=        (i)=> model.RemoveSpecies(i);


            var level = GameManager.Instance.DefaultLevel; // only use for dev
            // var level = GameManager.Instance.ChosenLevel;
            inspector.ConstrainTypes(level.Details.NumProducers, level.Details.NumConsumers);

            status.SlotInLevel(level);
            if (level.Details.MinEdges > -1)
                status.ConstrainNumEdges(level.Details.MinEdges);
            if (level.Details.MinChain > -1)
                status.ConstrainMaxChain(level.Details.MinChain);
            if (level.Details.MinLoop > -1)
                status.ConstrainMaxLoop(level.Details.MinLoop);

        }

        void FinishGame(int numStars)
        {
            GameManager.Instance.SaveLevel(numStars);
        }
    }
}

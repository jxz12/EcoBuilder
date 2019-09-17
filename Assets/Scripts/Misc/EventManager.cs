using UnityEngine;
using System;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class EventManager : MonoBehaviour
    {
        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.StatusBar status;
        [SerializeField] UI.Tutorial tutorial;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;
        [SerializeField] MoveRecorder recorder;

        void Start()
        {
            ///////////////////////////////////
            // hook up events between objects

            inspector.OnIncubated +=   ()=> nodelink.FullUnfocus();
            inspector.OnUnincubated += ()=> print("TODO:");
            inspector.OnSpawned +=    (i)=> nodelink.AddNode(i);
            inspector.OnSpawned +=    (i)=> model.AddSpecies(i);
            inspector.OnDespawned +=  (i)=> nodelink.RemoveNode(i);
            inspector.OnDespawned +=  (i)=> model.RemoveSpecies(i);
            inspector.OnDespawned +=  (i)=> status.RemoveIdx(i);
            ///////////////////////////
            inspector.OnShaped +=   (i,g)=> nodelink.ShapeNode(i,g);
            inspector.OnIsProducerSet += (i,b)=> nodelink.SetIfNodeCanBeTarget(i,!b);
            inspector.OnIsProducerSet += (i,b)=> model.SetSpeciesIsProducer(i,b);
            inspector.OnIsProducerSet += (i,b)=> status.AddType(i,b);
            inspector.OnSizeSet +=       (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=      (i,x)=> model.SetSpeciesInterference(i,x);

            nodelink.OnNodeFocused += (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=    ()=> inspector.Uninspect();
            nodelink.OnEmptyPressed += ()=> inspector.Unincubate();
            nodelink.OnEmptyPressed += ()=> status.ShowHelp(false);
            nodelink.OnLinked +=    (i,j)=> model.AddInteraction(i,j);
            nodelink.OnUnlinked +=  (i,j)=> model.RemoveInteraction(i,j);
            ///////////////////////////
            nodelink.OnConstraints += ()=> status.DisplayDisjoint(nodelink.Disjoint);
            nodelink.OnConstraints += ()=> status.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints += ()=> status.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints += ()=> status.DisplayMaxLoop(nodelink.MaxLoop);

            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);
            model.OnEquilibrium += ()=> nodelink.ResizeNodes(i=> model.GetScaledAbundance(i));
            model.OnEquilibrium += ()=> nodelink.ReflowLinks((i,j)=> model.GetScaledFlux(i,j));
            model.OnEquilibrium += ()=> status.DisplayScore(model.TotalFlux);
            model.OnEquilibrium += ()=> status.DisplayFeastability(model.Feasible, model.Stable);

            status.OnProducersAvailable += (b)=> inspector.SetProducersAvailable(b);
            status.OnConsumersAvailable += (b)=> inspector.SetConsumersAvailable(b);
            status.OnLevelCompleted     +=  ()=> CompleteLevel();

            ///////////////////////////
            // set up data collection

            inspector.OnUserSpawned +=   (i)=> nodelink.FocusNode(i);
            inspector.OnUserSpawned +=   (i)=> recorder.SpeciesSpawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserDespawned += (i)=> recorder.SpeciesDespawn(i, inspector.Respawn, inspector.Despawn);
            nodelink.OnUserLinked +=   (i,j)=> recorder.InteractionAdded(i, j, nodelink.AddLink, nodelink.RemoveLink);
            nodelink.OnUserUnlinked += (i,j)=> recorder.InteractionRemoved(i, j, nodelink.AddLink, nodelink.RemoveLink);




            ///////////////////
            // set up level

            var level = GameManager.Instance.PlayedLevel;
            if (level == null)
            {
                level = GameManager.Instance.GetNewLevel();
                // tutorial.gameObject.SetActive(true);
            }
            
            status.ConstrainFromLevel(level);
            status.AllowUpdateWhen(()=> model.Ready && nodelink.Ready);

            for (int i=0; i<level.Details.numSpecies; i++)
            {
                int newIdx = inspector.SpawnNotIncubated(
                    level.Details.plants[i],
                    level.Details.sizes[i],
                    level.Details.greeds[i],
                    level.Details.randomSeeds[i],
                    level.Details.editables[i]);
                inspector.SetSpeciesRemovable(i, false);
                if (newIdx != i)
                    throw new Exception("inspector not adding indices contiguously");
            }
            for (int ij=0; ij<level.Details.numInteractions; ij++)
            {
                int i = level.Details.resources[ij];
                int j = level.Details.consumers[ij];
                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
            }

        }
        void CompleteLevel()
        {
            inspector.Hide();
            nodelink.Freeze();
            status.Confetti();
            recorder.Record();
            GameManager.Instance.SavePlayedLevel(status.NumStars, model.TotalFlux);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SpeciesManager : MonoBehaviour
{
    /// <summary>
    /// Ensures that SpeciesManager is a singleton
    /// </summary>
    private static SpeciesManager speciesManager;
    public static SpeciesManager Instance {
        get {
            if (speciesManager == null)
            {
                Debug.LogError("No active SpeciesManager");
                speciesManager = new GameObject("Species Manager").AddComponent<SpeciesManager>();
                // might want to remove this for build
            }
            return speciesManager;
        }
    }
    private void Awake()
    {
        if (speciesManager == null)
            speciesManager = this;
        else if (speciesManager != this)
        {
            Debug.LogError("Multiple SpeciesManagers, destroying this one");
            Destroy(gameObject);
            // this means that there can only ever be one GameObject of this type
        }
    }


    /// <summary>
    /// When you want to spawn a species, call these two functions
    /// attach anything you want to listen to these events
    /// </summary>
    [Serializable] public class IntEvent : UnityEvent<int> { }

    [SerializeField] IntEvent SpawnEvent = new IntEvent();
    [SerializeField] IntEvent ExtinctEvent = new IntEvent();

    [SerializeField] Parameterizer param;
    public void SpawnSpecies()
    {
        int idx = ecosystem.AddSpecies();
        param.AddSpecies(idx);

        ecosystem.GrowthVector[idx] = param.GetGrowth(idx);
        ecosystem.InteractionMatrix[idx, idx] = param.GetIntraspecific(idx);

        SpawnEvent.Invoke(idx);
    }
    public void ExtinctSpecies(int idx)
    {
        param.RemoveSpecies(idx);
        ecosystem.RemoveSpecies(idx);

        ExtinctEvent.Invoke(idx);
    }
    //public void InspectSpecies(int idx)
    //{
    //    InspectEvent.Invoke(idx);
    //}

    public bool GetIsBasal(int idx)
    {
        if (idx == 0)
            return true;
        else
            return false;
    }
    public double GetTrophicLevel(int idx)
    {
        return ecosystem.TrophicLevels[idx];
    }
    public double GetAbundance(int idx)
    {
        return ecosystem.EquilibriumAbundances[idx];
    }


    /// <summary>
    /// Mathy stuff goes below
    /// </summary>

    private Stability ecosystem = new Stability();

    async public void Equilibrium()
    {
        print("trophic:\n" + ecosystem.TrophicLevels.ToString(x => x.ToString()));

        await Task.Run(() => ecosystem.Equilibrium());
        print("equilibrium:\n" + ecosystem.EquilibriumAbundances.ToString(x => x.ToString()));
    }
    async public void Community()
    {
        double stability = await Task.Run(() => ecosystem.LocalAsymptoticStability());
        print("stability: " + stability);
    }
    public void AddInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        float interaction = param.GetInteraction(resource, consumer);
        float efficiency = param.GetEfficiency(resource, consumer);
        ecosystem.InteractionMatrix[resource, consumer] = -interaction;
        ecosystem.InteractionMatrix[consumer, resource] = interaction * efficiency;
        print("interaction:\n" + ecosystem.InteractionMatrix.ToString(x => x.ToString()));
    }
    public void RemoveInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");
        ecosystem.InteractionMatrix[resource, consumer] = 0;
        ecosystem.InteractionMatrix[consumer, resource] = 0;
        print("interaction:\n" + ecosystem.InteractionMatrix.ToString(x => x.ToString()));
    }

    private void Update()
    {
        ecosystem.TrophicGaussSeidel();
        //if (Input.GetButtonDown("Cancel"))
        //    UninspectEvent.Invoke();
    }


    ////////////////////////////////////////////////////////////////////////
    // REPLACE THE NICE 'PULSING' WITH EQUILIBRIUM ABUNDANCE CALCULATION! //
    ////////////////////////////////////////////////////////////////////////
}
using UnityEngine;
using UnityEngine.Events;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[RequireComponent(typeof(Parameterizer))]
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
        param = GetComponent<Parameterizer>();
    }


    /// <summary>
    /// When you want to spawn a species, call these two functions
    /// attach anything you want to listen to these events
    /// </summary>
    [Serializable] public class IntEvent : UnityEvent<int> { }

    [SerializeField] IntEvent SpawnEvent = new IntEvent();
    [SerializeField] IntEvent ExtinctEvent = new IntEvent();
    [SerializeField] IntEvent InspectEvent = new IntEvent();
    [SerializeField] UnityEvent UninspectEvent = new UnityEvent();

    public void SpawnSpecies()
    {
        int idx = ecosystem.AddSpecies();
        ecosystem.InteractionMatrix[idx, idx] = -.1;
        ecosystem.GrowthVector[0] = 1;

        SpawnEvent.Invoke(idx);
        InspectEvent.Invoke(idx);
    }
    public void ExtinctSpecies(int idx)
    {
        ecosystem.RemoveSpecies(idx);
        ExtinctEvent.Invoke(idx);
        UninspectEvent.Invoke();
    }
    public void InspectSpecies(int idx)
    {
        InspectEvent.Invoke(idx);
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
    private Parameterizer param;

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
    public void Interaction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        float interaction = param.GetInteraction(resource, consumer);
        float efficiency = param.GetEfficiency(resource, consumer);
        ecosystem.InteractionMatrix[resource, consumer] = -interaction;
        ecosystem.InteractionMatrix[consumer, resource] = interaction * efficiency;
        print("interaction:\n" + ecosystem.InteractionMatrix.ToString(x => x.ToString()));
    }

    private void Update()
    {
        ecosystem.TrophicGaussSeidel();
        if (Input.GetButtonDown("Cancel"))
            UninspectEvent.Invoke();
    }


    ////////////////////////////////////////////////////////////////////////
    // REPLACE THE NICE 'PULSING' WITH EQUILIBRIUM ABUNDANCE CALCULATION! //
    ////////////////////////////////////////////////////////////////////////
}
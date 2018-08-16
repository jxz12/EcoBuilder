using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;

public class EcosystemDriver : MonoBehaviour
{
    [SerializeField] Parameteriser parameteriser;
    [SerializeField] float heartRate = 30;

    public void AddSpecies(int idx)
    {
        model.AddSpecies(idx);
        model.GrowthVector[idx] = parameteriser.GetGrowth(idx);
        model.InteractionMatrix[idx, idx] = parameteriser.GetIntraspecific(idx);
    }
    public void RemoveSpecies(int idx)
    {
        model.RemoveSpecies(idx);
    }

    public void EditInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        double interaction = parameteriser.GetInteraction(resource, consumer);
        double efficiency = parameteriser.GetEfficiency(resource, consumer);
        model.InteractionMatrix[resource, consumer] = -interaction;
        model.InteractionMatrix[consumer, resource] = interaction * efficiency;
        //print("interaction:\n" + model.InteractionMatrix.ToString(x => x.ToString()));
    }
    public void RemoveInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        model.InteractionMatrix[resource, consumer] = 0;
        model.InteractionMatrix[consumer, resource] = 0;
        //print("interaction:\n" + model.InteractionMatrix.ToString(x => x.ToString()));
    }


    /// <summary>
    /// Mathy stuff goes below
    /// </summary>

    private LotkaVolterraLAS model = new LotkaVolterraLAS();

    async void Equilibrium()
    {
        await Task.Run(() => model.Equilibrium());
        print("equilibrium:\n" + model.EquilibriumAbundances.ToString(x => x.ToString()));
    }
    async void LocalStability()
    {
        double stability = await Task.Run(() => model.LocalAsymptoticStability());
        print("stability: " + stability);
    }

    private void Start()
    {
        //StartCoroutine(Pulse(60f / heartRate));
    }
    private void Update()
    {
        model.TrophicGaussSeidel();
    }

    IEnumerator Pulse(float delay)
    {
        while (true)
        {
            Equilibrium();
            yield return new WaitForSeconds(delay);
        }
    }


    ////////////////////////////////////////////////////////////////////////
    // REPLACE THE NICE 'PULSING' WITH EQUILIBRIUM ABUNDANCE CALCULATION! //
    ////////////////////////////////////////////////////////////////////////
}
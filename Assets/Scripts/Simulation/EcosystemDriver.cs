using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Threading.Tasks;

public class EcosystemDriver : MonoBehaviour
{
    [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
    [SerializeField] IntFloatEvent TrophicLevelCalculatedEvent;
    // [Serializable] class FuncIntFloatEvent : UnityEvent<Func<int, float>> { }
    // [SerializeField] FuncIntFloatEvent TrophicLevelEvent;

    [SerializeField] Inspector inspector;
    [SerializeField] float heartRate = 30;

    public void AddSpecies(int idx)
    {
        model.AddSpecies(idx);
        model.GrowthVector[idx] = r_i(idx);
        model.InteractionMatrix[idx, idx] = a_ii(idx);
    }
    public void RemoveSpecies(int idx)
    {
        model.RemoveSpecies(idx);
    }

    public void AddInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        double interaction = a_ij(resource, consumer);
        double efficiency = e_ij(resource, consumer);
        model.InteractionMatrix[resource, consumer] = -interaction;
        model.InteractionMatrix[consumer, resource] = interaction * efficiency;
    }
    public void RemoveInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        model.InteractionMatrix[resource, consumer] = 0;
        model.InteractionMatrix[consumer, resource] = 0;
    }

    private readonly Func<int, double> r_i = i => i == 0 ? .1 : -.4;
    private readonly Func<int, double> a_ii = i => .01;
    private readonly Func<int, int, double> a_ij = (i, j) => .02;
    private readonly Func<int, int, double> e_ij = (i, j) => .5;




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
        foreach (int i in model.TrophicLevels.Indices)
        {
            TrophicLevelCalculatedEvent.Invoke(i, (float)model.TrophicLevels[i]);
        }
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
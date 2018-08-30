using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ECGLine : MonoBehaviour
{
    public float Max { get; private set; }
    public float Min { get; private set; }

    private float tweenSpeed, updateDelay;
    private float target;

    Queue<float> values;
    public void Init(int queueLength, float tweenSpeed, float updateRate, Color col)
    {
        this.tweenSpeed = tweenSpeed;
        updateDelay = 1f / updateRate;
        for (int i=0; i<queueLength; i++) values.Enqueue(0);
    }
    public void Bleep(float newTarget)
    {
        target = newTarget;
    }

    // int width;
    // // float spacing;
    // public float max { get; private set; }
    // Func<float> Getter;
    // Color col;

    // [SerializeField] GameObject lineRendererPrefab;
    // Queue<LineRenderer> lineRenderers;
    // Queue<float> values;

    // public void Init(int width, Color col, Func<float> Getter) {
    //     this.width = width;
    //     // spacing = 1f/(width-1);
    //     this.col = col;
    //     this.Getter = Getter;

    //     values = new Queue<float>();
    //     for (int i=0; i<width; i++) values.Enqueue(0);

    //     lineRenderers = new Queue<LineRenderer>();
    //     eqPrev = eqPrevPrev = false;
    // }
    // LineRenderer MakeNewLine() {
    //     var lr = Instantiate(lineRendererPrefab).GetComponent<LineRenderer>();
    //     lr.transform.SetParent(this.transform, false);
    //     lr.startColor = lr.endColor = this.col;
    //     return lr;
    // }

    // bool eqPrev, eqPrevPrev;
    // public void Poll(Func<float, float> Scale) {
    //     // keep track of previous two values
    //     // DEQUEUE
    //     // if 1|00 then get rid of a line
    //     // ENQUEUE
    //     // if 001| then add a line

    //     float oldValue = values.Dequeue();
    //     float newValue = Getter();
    //     values.Enqueue(newValue);
    //     bool eq = newValue > 0;
    //     if (!eqPrevPrev && !eqPrev && eq) {
    //         LineRenderer newLine = MakeNewLine();
    //         lineRenderers.Enqueue(newLine);
    //         // print("line added!");
    //     }
    //     eqPrevPrev = eqPrev;
    //     eqPrev = eq;

    //     // UPDATE
    //     // if 00: end previous and switch to new line, do nothing until 1 appears

    //     float firstVal = values.Dequeue();
    //     float secondVal = values.Dequeue();
    //     values.Enqueue(firstVal);
    //     values.Enqueue(secondVal);
    //     max = Mathf.Max(firstVal, secondVal);

    //     LineRenderer currentLine = null;
    //     bool midZeros;
    //     if (firstVal<=0 && secondVal<=0) {
    //         if (oldValue > 0) {
    //             LineRenderer toDestroy = lineRenderers.Dequeue();
    //             Destroy(toDestroy.gameObject);
    //             // print("line Destroyed!");
    //             // maybe return if queue is empty somewhere?
    //         }
    //         midZeros = true;
    //     } else {
    //         midZeros = false;
    //         currentLine = lineRenderers.Dequeue();
    //         lineRenderers.Enqueue(currentLine);
    //         currentLine.positionCount = 2;
    //         currentLine.SetPosition(0, new Vector2(0, Scale(firstVal)));
    //         currentLine.SetPosition(1, new Vector2(1, Scale(secondVal)));
    //     }

    //     bool prevNonZero = secondVal > 0;
    //     for (int i=2; i<width; i++) {
    //         float nextVal = values.Dequeue();
    //         values.Enqueue(nextVal);
    //         max = Mathf.Max(max, nextVal);
    //         bool nextNonZero = nextVal > 0;
    //         if (!midZeros) {
    //             if (prevNonZero || nextNonZero) {
    //                 currentLine.positionCount += 1;
    //                 currentLine.SetPosition(currentLine.positionCount-1, new Vector2(i, Scale(nextVal)));
    //             } else {
    //                 midZeros = true;
    //             }
    //         } else {
    //             if (nextVal > 0) {
    //                 currentLine = lineRenderers.Dequeue();
    //                 lineRenderers.Enqueue(currentLine);
    //                 currentLine.positionCount = 2;
    //                 currentLine.SetPosition(0, new Vector2(i-1, Scale(0)));
    //                 currentLine.SetPosition(1, new Vector2(i, Scale(nextVal)));
    //                 midZeros = false;
    //             }
    //         }
    //         prevNonZero = nextNonZero;
    //     }

    // }
}
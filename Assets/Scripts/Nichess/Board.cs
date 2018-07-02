﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Board : MonoBehaviour
{
    [Serializable] public class ColorEvent : UnityEvent<int, Color> { }
    [SerializeField] ColorEvent PieceColorEvent = new ColorEvent();

    [SerializeField] Square squarePrefab;
    [SerializeField] Square dummySquare;
    [SerializeField] Transform spawnTransform;
    Square[,] squares;

    public void Init(int size, Func<float, float, Color> ColorMap)
    {
        Vector3 bottomLeft = new Vector3(-.5f, 0, -.5f); float gap = 1f / size;
        Vector3 offset = new Vector3(gap / 2, 0, gap / 2);
        float squareWidth = (1f / size) * .95f;
        Vector3 scale = new Vector3(squareWidth, squareWidth, squareWidth);

        squares = new Square[size, size];
        //float normWidth = 1f / (size-1);

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                var s = Instantiate(squarePrefab, transform);

                s.transform.localPosition = bottomLeft + new Vector3(i * gap, 0, j * gap) + offset;
                s.transform.localScale = scale;
                s.Init(i, j, ColorMap((float)i / size, (float)j / size));

                squares[i, j] = s;
            }
        }
    }
    public Tuple<Square, Square> GetAdjacentCornerSquares(Square corner1, Square corner2)
    {
        int x1 = corner1.X, x2 = corner2.X, y1 = corner1.Y, y2 = corner2.Y;
        return Tuple.Create(squares[x1, y2], squares[x2, y1]);
    }

    public void PlaceNewPiece(Piece newPiece)
    {
        //newPiece.transform.SetParent(spawnTransform, false); // ???? not working ????
        newPiece.Parent(spawnTransform);
        newPiece.NicheStart = newPiece.NicheEnd = dummySquare;
    }
    public void PieceAdopted(Square newParent, Piece movedPiece)
    {
        Color newCol = newParent.Col;
        newCol.a = 1;
        PieceColorEvent.Invoke(movedPiece.Idx, newCol);
    }

    ////public UnityEvent spin;
    //bool spinning = false;
    //public void Spin90Clockwise() //{
    //    if (spinning == false)
    //    {
    //        spinning = true;
    //        StartCoroutine(Spin(60));
    //    }
    //}
    //IEnumerator Spin(int numFrames)
    //{
    //    Quaternion initial = transform.localRotation;
    //    Quaternion target = initial * Quaternion.Euler(Vector3.up * 90);
    //    for (int i = 0; i < numFrames; i++)
    //    {
    //        transform.localRotation = Quaternion.Slerp(initial, target, (float)i / numFrames);
    //        yield return null;
    //    }
    //    transform.localRotation = target;
    //    spinning = false;
    //}

    //void Start()
    //{
    //    if (squares == null)
    //        throw new Exception("Board not initialised");
    //}

}
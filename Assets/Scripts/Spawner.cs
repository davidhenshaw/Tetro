﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] Tetromino[] tetroPrefabs;
    [SerializeField] int queueSize = 3;
    [SerializeField] Transform[] _symbolPlacements;

    List<Tetromino> _tetroPool = new List<Tetromino>();

    Queue<Tetromino> tetroQueue;
    public Queue<Tetromino> TetroQueue { get => tetroQueue; }

    private void Awake()
    {
        tetroQueue = new Queue<Tetromino>();
    }

    // Start is called before the first frame update
    void Start()
    {
        FillPool();

        //Enqueue [queueSize] number of tetrominoes immediately
        for(int i = 0; i < queueSize; i++)
        {
            EnqueueNewTetromino();
        }
    }

    private void Update()
    {
        RepositionQueuedTetros();
    }

    private void FillPool()
    {
        _tetroPool.AddRange(tetroPrefabs);
    }

    private Tetromino GetTetroFromPool()
    {
        if(_tetroPool.Count <= 0)
        {
            FillPool();
        }

        int random = Random.Range(0, _tetroPool.Count);
        var tetro = _tetroPool[random];

        _tetroPool.RemoveAt(random);

        return tetro;
    }

    private void EnqueueNewTetromino()
    {
        //Get random tetromino
        var poolTetro = GetTetroFromPool();
        
        Tetromino tetro = Instantiate(poolTetro) as Tetromino;

        tetro.gameObject.SetActive(false);
        tetroQueue.Enqueue(tetro);

        RepositionQueuedTetros();
    }

    void RepositionQueuedTetros()
    {
        Tetromino[] tetroArray = tetroQueue.ToArray();

        for(int i = 0; i < tetroArray.Length; i++)
        {
            GameObject obj = tetroArray[i].GetSymbol();
            obj.transform.position = _symbolPlacements[i].position;
        }
    }

    public Tetromino GetNextTetromino()
    {
        if (tetroQueue.Count <= 0)
            return null;

        Tetromino nextTetro = tetroQueue.Dequeue();

        nextTetro.gameObject.SetActive(true);
        nextTetro.DestroySymbol();
        nextTetro.SetSnapToGrid(true);

        EnqueueNewTetromino();

        return nextTetro;
    }
}

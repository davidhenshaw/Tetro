using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetromino : MonoBehaviour
{
    List<Mino> _minos;
    Mino pivotMino;
    Grid _grid;
    BoardController _board;
    [SerializeField] GameObject _symbol;
    GameObject _symbolInstance;

    [SerializeField] ParticleSystem _particleSystem;

    public List<Mino> ChildMinos { get => _minos; }
    Dictionary<Mino, Vector3Int> _origCellPositions;

    private void Awake()
    {
        _minos = new List<Mino>();
        _origCellPositions = new Dictionary<Mino, Vector3Int>();
    }

    private void Start()
    {
        _grid = FindObjectOfType<Grid>();
        _board = FindObjectOfType<BoardController>();

        var minos = GetComponentsInChildren<Mino>();
        _minos.AddRange(minos);

        foreach(Mino m in _minos)
        {
            if (m.IsPivot)
            {
                pivotMino = m;
            }

            _origCellPositions.Add(m, m.CellPos);
        }
    }

    public void Reset()
    {
        foreach(Mino m in _origCellPositions.Keys)
        {
            m.SetCellPosition(_origCellPositions[m]);
        }
    }

    public void SetSnapToGrid(bool value)
    {
        foreach(Mino m in _minos)
        {
            m.snapToGrid = value;
        }
    }

    public int GetMinoHeight(int col)
    {
        foreach(Mino m in _minos)
        {
            if (m.CellPos.x == col)
                return m.CellPos.y;
        }

        return 0;
    }

    public GameObject GetSymbol()
    {
        if(_symbolInstance == null)
            _symbolInstance = Instantiate(_symbol) as GameObject;

        return _symbolInstance;
    }

    public void DestroySymbol()
    {
        if(_symbolInstance)
            Destroy(_symbolInstance);
    }

    public bool MoveLeft()
    {
        //check to see if movement is possible
        foreach (Mino m in _minos)
        {
            if (!m.CanMove(Direction.Left))
                return false;
        }

        //move
        foreach (Mino m in _minos)
        {
            m.MoveLeft();
        }

        return true;
    }

    public bool MoveRight()
    {
        //check to see if movement is possible
        foreach (Mino m in _minos)
        {
            if (!m.CanMove(Direction.Right))
                return false;
        }

        //move
        foreach (Mino m in _minos)
        {
            m.MoveRight();
        }

        return true;
    }

    public bool MoveDown()
    {
        //check to see if movement is possible
        foreach (Mino m in _minos)
        {
            if (!m.CanMove(Direction.Down))
            {
                return false;
            }
        }

        //move
        foreach (Mino m in _minos)
        {
            m.MoveDown();
        }

        return true;
    }

    public void MoveDown(int dist)
    {
        for(int i = 0; i < dist; i++)
        {
            MoveDown();
        }
    }

    public void Explode()
    {
        _board.AddMinos(this);

        foreach(var t in GetComponentsInChildren<Transform>())
        {
            t.parent = _board.transform;
        }

        //TODO: Insert effect that plays
        _particleSystem.Play();

        Destroy(gameObject);
    }

    // Rotating a tetromino is achieved by 
    //      1. Finding the coordinates of each Mino relative to a pivot mino
    //      2. Swapping the x and y values of relative coordinates
    //      3. Negating the new relative y value
    public bool RotateClockwise()
    {
        if (pivotMino == null)
            return false;

        Dictionary<Mino, Vector3Int> nextMinoPos = new Dictionary<Mino, Vector3Int>();

        //Used to reposition tetromino back in bounds if a rotation would put some minos out of bounds
        Vector3Int oobOffset = Vector3Int.zero;

        //Try to find the next positions after rotation
        foreach (Mino m in _minos)
        {
            if (m.IsPivot)
                continue;

            var newCellPos = GetClockwiseRotation(m.CellPos, pivotMino.CellPos);

            nextMinoPos.Add(m, newCellPos);

            //Find Minos that are out of bounds
            if (!_board.IsCellFree(newCellPos))
            {
                // Find the distance between the OutOfBounds Mino to the pivot Mino
                Vector3Int distToPiv = pivotMino.CellPos - newCellPos;
                
                // Check if OOB Mino is now in bounds                
                if(_board.IsCellFree(newCellPos + distToPiv))
                {
                    // Only save the largest distance from the pivot
                    oobOffset = (oobOffset.magnitude < distToPiv.magnitude) ? distToPiv : oobOffset ;
                }
            }
        }

        //If the new position plus the offset is still out of bounds
        //  abort the rotation
        foreach(Mino m in nextMinoPos.Keys)
        {
            if( !_board.IsCellFree(nextMinoPos[m] + oobOffset) )
            {
                return false;
            }
        }

        //Reposition minos to next rotation position
        foreach (Mino m in nextMinoPos.Keys)
        {
            m.SetCellPosition(nextMinoPos[m] + oobOffset);
        }
        // Reposition pivot
        pivotMino.SetCellPosition(pivotMino.CellPos + oobOffset);

        return true;
    }

    //public void RotateClockwise()
    //{
    //    if (pivotMino == null)
    //        return;

    //    Dictionary<Mino, Vector3Int> nextMinoPos = new Dictionary<Mino, Vector3Int>();


    //    foreach (Mino m in _minos)
    //    {
    //        if (m.IsPivot)
    //            continue;

    //        var newPos = GetClockwiseRotation(m.CellPos, pivotMino.CellPos);

    //        nextMinoPos.Add(m, newPos);

    //        //Abort rotation if any new position is occupied/invalid
    //        if (!_board.IsCellFree(newPos))
    //        {
    //            nextMinoPos.Clear();
    //            break;
    //        }
    //    }

    //    foreach(Mino m in nextMinoPos.Keys)
    //    {
    //        m.SetCellPosition(nextMinoPos[m]);
    //    }
    //}

    Vector3Int GetClockwiseRotation(Vector3Int observedPos, Vector3Int pivotPoint)
    {
        var relativePos = observedPos - pivotPoint;

        // Swap x and y values relative to the pivot Mino
        var tempX = relativePos.x;
        var tempY = relativePos.y;

        // Negate the new y value
        var newRelativePos = new Vector3Int(tempY, tempX * -1, observedPos.z);
        var newRawPos = pivotMino.CellPos + newRelativePos;

        return newRawPos;
    }

    public bool RotateCounterClockwise()
    {
        if (pivotMino == null)
            return false;

        Dictionary<Mino, Vector3Int> nextMinoPos = new Dictionary<Mino, Vector3Int>();

        foreach (Mino m in _minos)
        {
            if (m.IsPivot)
                continue;

            Vector3Int observedPos = m.CellPos;
            var relativePos = observedPos - pivotMino.CellPos;

            // Swap x and y values relative to the pivot Mino and negate Y
            var tempX = relativePos.x;
            var tempY = relativePos.y * -1;
            var newRelativePos = new Vector3Int(tempY, tempX, observedPos.z);

            var newPos = pivotMino.CellPos + newRelativePos;

            //TODO: Check to see whether new position is valid

            nextMinoPos.Add(m, newPos);

            //Abort rotation if any new position is occupied/invalid
            if (!_board.IsCellFree(newPos))
                return false;
        }

        foreach (Mino m in nextMinoPos.Keys)
        {
            m.SetCellPosition(nextMinoPos[m]);
        }

        return true;
    }
}

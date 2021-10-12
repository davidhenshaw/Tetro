using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetromino : MonoBehaviour
{
    List<Mino> _minos;
    Mino _pivotMino;
    Grid _grid;
    BoardController _board;
    [SerializeField] GameObject _symbol;
    GameObject _symbolInstance;
    int _rotationState = 0;

    [SerializeField] GameObject _lockEffect;
    [SerializeField] WallKickData _wallKickData;

    public List<Mino> ChildMinos { get => _minos; }
    public int RotationState { get => _rotationState; }
    public TetrominoAction LastAction { get; private set; }
    public Mino PivotMino { get => _pivotMino; }

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
                _pivotMino = m;
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

        LastAction = TetrominoAction.Input;

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

        LastAction = TetrominoAction.Input;

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

        LastAction = TetrominoAction.Gravity;

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
        Instantiate(_lockEffect, _minos[0].transform);

        Destroy(gameObject);
    }

    // Rotating a tetromino is achieved by 
    //      1. Finding the coordinates of each Mino relative to a pivot mino
    //      2. Swapping the x and y values of relative coordinates
    //      3. Negating the new relative y value
    public bool RotateClockwise()
    {
        if (_pivotMino == null)
            return false;

        Vector2Int[] tests = _wallKickData.GetClockWiseTestOffsets(_rotationState);
        Dictionary<Mino, Vector3Int> nextMinoPos = new Dictionary<Mino, Vector3Int>();
        Vector2Int offsetTest = Vector2Int.zero;

        foreach(Vector2Int offset in tests)
        {
            if (CheckClockwiseRotation(offset, out nextMinoPos))
            {
                offsetTest = offset;
                break;
            }
        }

        //If the dictionary is empty, all of the offset tests have failed
        if (nextMinoPos.Count <= 0)
            return false;

        //Reposition minos to next rotation position
        foreach (Mino m in nextMinoPos.Keys)
        {
            m.SetCellPosition(nextMinoPos[m]);
        }

        //Increment rotation state, wrap around to zero if greater than 3
        _rotationState = _rotationState < 3 ? _rotationState + 1 : 0;
        
        //If the successful offset was zero, this means there was no wall kick
        LastAction = (offsetTest == Vector2Int.zero) ? TetrominoAction.Rotate_NoWallKick : TetrominoAction.Rotate_WallKick;

        return true;
    }

    bool CheckClockwiseRotation(Vector2Int offset, out Dictionary<Mino, Vector3Int> nextPositions)
    {
        nextPositions = new Dictionary<Mino, Vector3Int>();
        foreach (Mino m in _minos)
        {
            var newPos = GetClockwiseRotation(m.CellPos, _pivotMino.CellPos);
            newPos += new Vector3Int(offset.x, offset.y, 0);

            nextPositions.Add(m, newPos);

            //Find if mino is out of bounds
            if (!_board.IsCellFree(newPos))
            {
                nextPositions.Clear();
                return false;
            }
        }

        return true;
    }

    bool CheckCounterClockwiseRotation(Vector2Int offset, out Dictionary<Mino, Vector3Int> nextPositions)
    {
        nextPositions = new Dictionary<Mino, Vector3Int>();
        foreach (Mino m in _minos)
        {
            var newPos = GetCounterClockwiseRotation(m.CellPos, _pivotMino.CellPos);
            newPos += new Vector3Int(offset.x, offset.y, 0);

            nextPositions.Add(m, newPos);

            //Find if mino is out of bounds
            if (!_board.IsCellFree(newPos))
            {
                nextPositions.Clear();
                return false;
            }
        }

        return true;
    }

    Vector3Int GetClockwiseRotation(Vector3Int observedPos, Vector3Int pivotPoint)
    {
        var relativePos = observedPos - pivotPoint;

        // Swap x and y values relative to the pivot Mino
        var tempX = relativePos.x;
        var tempY = relativePos.y;

        // Negate the new y value
        var newRelativePos = new Vector3Int(tempY, tempX * -1, observedPos.z);
        var newRawPos = _pivotMino.CellPos + newRelativePos;

        return newRawPos;
    }

    Vector3Int GetCounterClockwiseRotation(Vector3Int observedPos, Vector3Int pivotPoint)
    {
        var relativePos = observedPos - pivotPoint;

        // Swap x and y values relative to the pivot Mino and negate Y
        var tempX = relativePos.x;
        var tempY = relativePos.y * -1;
        var newRelativePos = new Vector3Int(tempY, tempX, observedPos.z);

        var newPos = _pivotMino.CellPos + newRelativePos;

        return newPos;
    }

    public bool RotateCounterClockwise()
    {
        if (_pivotMino == null)
            return false;

        Vector2Int[] tests = _wallKickData.GetCounterClockWiseTestOffsets(_rotationState);
        Dictionary<Mino, Vector3Int> nextMinoPos = new Dictionary<Mino, Vector3Int>();
        Vector2Int offsetTest = Vector2Int.zero;

        foreach (Vector2Int offset in tests)
        {
            if (CheckCounterClockwiseRotation(offset, out nextMinoPos))
            {
                offsetTest = offset;
                break;
            }
        }

        //If the dictionary is empty, all of the offset tests have failed
        if (nextMinoPos.Count <= 0)
            return false;

        //Reposition minos to next rotation position
        foreach (Mino m in nextMinoPos.Keys)
        {
            m.SetCellPosition(nextMinoPos[m]);
        }

        _rotationState = (_rotationState > 0) ? _rotationState - 1 : 3;

        //If the successful offset was zero, this means there was no wall kick
        LastAction = (offsetTest == Vector2Int.zero) ? TetrominoAction.Rotate_NoWallKick : TetrominoAction.Rotate_WallKick;

        return true;
    }
}

public enum TetrominoAction
{
    Rotate_NoWallKick, Rotate_WallKick, Input, Gravity
}

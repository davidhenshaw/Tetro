using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Mino : MonoBehaviour
{
    [SerializeField] Vector3Int _cellPos;
    Tetromino _parentTetro;
    Grid _grid;
    BoardController _board;
    [SerializeField] GameObject _quickDropEffect;

    public bool snapToGrid = true;
    [SerializeField] bool _isPivot = false;

    public Vector3Int CellPos { get => _cellPos;}
    public bool IsPivot { get => _isPivot; }

    private void Start()
    {
        _parentTetro = GetComponentInParent<Tetromino>();
        _grid = FindObjectOfType<Grid>();
        _board = FindObjectOfType<BoardController>();
    }

    void Update()
    {
        if(snapToGrid)
            SnapToGrid();
    }

    void SnapToGrid()
    {
        //Update transform based on cell position
        transform.position = _grid.GetCellCenterLocal(_cellPos) + _grid.transform.position;
    }

    public void PlayQuickDrop()
    {
        Instantiate(_quickDropEffect, transform);
    }

    public void SetCellPosition(Vector3Int newPos)
    {
        _cellPos = newPos;
    }

    public void MoveRight()
    {
        //move right
        _cellPos = new Vector3Int(_cellPos.x + 1, _cellPos.y, _cellPos.z);
    }

    public void MoveLeft()
    {
        //move left
        _cellPos = new Vector3Int(_cellPos.x - 1, _cellPos.y, _cellPos.z);
    }

    public bool MoveDown()
    {
        //if (CanMove(Direction.Down))
        {
            _cellPos = new Vector3Int(_cellPos.x, _cellPos.y - 1, _cellPos.z);
            return true;
        }
        //else
            //return false;
    }

    public void MoveDown(int dist)
    {
        for (int i = 0; i < dist; i++)
        {
            MoveDown();
        }
    }

    public bool CanMove(Direction dir)
    {
        bool canMove = false;

        switch(dir)
        {
            case Direction.Up:
                break;

            case Direction.Down:
                if (_cellPos.y - 1 < 0)
                    canMove = false;

                if (_board.IsCellFree(new Vector3Int(_cellPos.x, _cellPos.y - 1, _cellPos.z)))
                    canMove = true;

                break;

            case Direction.Left:
                if (_cellPos.x - 1 < 0)
                    canMove = false;

                if (_board.IsCellFree(new Vector3Int(_cellPos.x - 1, _cellPos.y, _cellPos.z)))
                    canMove = true;
                break;

            case Direction.Right:
                if (_cellPos.x + 1 > _board.Width)
                    canMove = false;

                if (_board.IsCellFree(new Vector3Int(_cellPos.x + 1, _cellPos.y, _cellPos.z)))
                    canMove = true;
                break;
        }

        return canMove;
    }
}

public enum Direction
{
    Up, Down, Left, Right
}

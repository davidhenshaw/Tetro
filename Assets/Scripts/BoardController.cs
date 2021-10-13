using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action Lost;
    public event Action BoardReset;
    public event Action<BoardAction> BoardAction;
    public event Action TetroMovedDown;
    public event Action<int> HardDropped;

    bool _canBank = true;
    bool isPaused = false;

    Tetromino _currTetro;
    Tetromino _bankedTetro;
    [SerializeField] Mino[] _ghostMinos;
    [SerializeField] Transform _bankedSymbolTransform;

    Dictionary<Vector3Int, Mino> _minoPositions;
    Spawner _spawner;
    Grid _grid;

    [Header("Formatting")]
    [SerializeField] int _width = 10;
    [SerializeField] int _height = 20;
    [SerializeField] int _topMargin = 3;

    [Header("Input")]
    [SerializeField] float inputRepeatTime = 0.1f;
    [SerializeField] PatternDetector quickDropInput;

    float currInputTime = 0;
    private bool _lockMode = false;
    [SerializeField] float _lockDelay = 0.5f;
    float _currLockTime;
    List<int> _emptyRows = new List<int>();

    [Header("Sounds")]
    [SerializeField] AudioClip _blockLocked;
    [SerializeField] AudioClip _blockMoved;
    [SerializeField] AudioClip _blockRotated;
    [SerializeField] AudioClip _quickDrop;
    [SerializeField] AudioClip _storeBank;
    [SerializeField] AudioClip _lineClear;
    AudioSource _audioSource;

    [Header("Effects")]
    [SerializeField] GameObject _lineClearEffect;

    public int Width { get => _width; }
    public int Height { get => _height; }

    private void Awake()
    {
        _grid = FindObjectOfType<Grid>();
        _spawner = FindObjectOfType<Spawner>();
        _minoPositions = new Dictionary<Vector3Int, Mino>();        
    }

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _currTetro = _spawner.GetNextTetromino();
    }

    private void Update()
    {
        if(_currTetro && !isPaused)
        {
            HandleInput();
            RepositionGhostMinos();

            if(_lockMode)
                HandleLockMode();
        }
    }

    public void Reset()
    {
        _lockMode = false;

        BoardReset?.Invoke();
    }

    public void Tick()
    {
        //Note: Tetrominos destroy (Explode) when they can no longer move down
        // its children minos get fixed to the board while the parent tetromino obj is destroyed

        ShiftRowsDown();

        if (_currTetro)//If we currently have a tetromino to control
        {
            if(_currTetro.MoveDown())
            {//Tetromino successfully moved down
                CancelLockMode();
                TetroMovedDown?.Invoke();
            }
            else
            {
                //Start lock timer
                _lockMode = true;
            }
        }
        else //The currTetro tetromino destroyed itself
        {
            if (CheckGameOver())
                return;

            _currTetro = _spawner.GetNextTetromino();

            // restore player's ability to bank
            _canBank = true;

            CancelLockMode();
        }
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }

    void RepositionGhostMinos()
    {
        if (_currTetro != null)
        {
            Mino[] children = _currTetro.ChildMinos.ToArray();

            for (int i = 0; i < children.Length; i++)
            {
                _ghostMinos[i].SetCellPosition(children[i].CellPos);
            }

            HardDropGhostMinos();
        }
        else
        {// Make ghost minos disappear

        }
    }

    void HandleInput()
    {
        bool didMove = false;
        bool didRotate;

        if(Input.GetButtonDown("Right"))
        {
            didMove = _currTetro.MoveRight();
            currInputTime = 0;
        }

        if (Input.GetButton("Right"))
        {
            if (currInputTime > inputRepeatTime)
            {
                didMove = _currTetro.MoveRight();
                currInputTime = 0;
            }
            currInputTime += Time.deltaTime;
        }

        if (Input.GetButtonDown("Left"))
        {
            didMove = _currTetro.MoveLeft();
            currInputTime = 0;
        }

        if (Input.GetButton("Left"))
        {
            if (currInputTime > inputRepeatTime)
            {
                didMove = _currTetro.MoveLeft();

                currInputTime = 0;
            }
            currInputTime += Time.deltaTime;
        }

        if (Input.GetButtonDown("Hard Drop"))
        {
            if (_currTetro != null)
                HardDrop();
        }

        if (Input.GetButtonDown("CW Rotate"))
        {
            didRotate = _currTetro.RotateClockwise();

            if(didRotate)
            {
                _audioSource.PlayOneShot(_blockRotated);
                _currLockTime = 0;
            }
        }

        if (Input.GetButtonDown("CCW Rotate"))
        {
            didRotate = _currTetro.RotateCounterClockwise();

            if (didRotate)
            {
                _audioSource.PlayOneShot(_blockRotated);
                _currLockTime = 0;
            }
        }

        if(Input.GetButtonDown("Hold"))
        {
            if (!_canBank)
                return;

            if(_bankedTetro)
            {
                SwapBankedTetro();
                _audioSource.PlayOneShot(_storeBank);
            }
            else
            {
                _audioSource.PlayOneShot(_storeBank);
                BankCurrentTetro();
            }
        }

        if (didMove)
        {
            _audioSource.PlayOneShot(_blockMoved);
            _currLockTime = 0;
        }
    }

    private void CancelLockMode()
    {
        _lockMode = false;
        _currLockTime = 0;
    }

    private void HandleLockMode()
    {
        _currLockTime += Time.deltaTime;

        if ((_currLockTime >= _lockDelay))
        {
            TetrominoAction tetroAction = _currTetro.LastAction;
            bool t_spin = CheckTSpin(_currTetro);
            _currTetro?.Explode();
            _currTetro = null;

            _emptyRows = ClearLines();

            BoardAction boardAction = new BoardAction(_emptyRows.Count, t_spin, tetroAction);
            BoardAction?.Invoke(boardAction);

            CancelLockMode();
            _audioSource.PlayOneShot(_blockLocked);
        }
    }

    void SkipLockMode()
    {
        _currLockTime = _lockDelay; //max out the lock delay timer so that next tick is immediate
        _lockMode = true;
        HandleLockMode();
    }

    bool CheckTSpin(Tetromino tetro)
    {// Uses 3-Corner T algorithm. Checks whether 3 of the spaces diagonal to the pivot are occupied
        if (!tetro.PivotMino)
            return false;

        var pivot = tetro.PivotMino.CellPos;
        var lastAction = tetro.LastAction;
        int numOccupied = 0;

        Vector3Int[] tests = new Vector3Int[] {
            pivot + new Vector3Int(-1, 1, 0),
            pivot + new Vector3Int(1, 1, 0),
            pivot + new Vector3Int(1, -1, 0),
            pivot + new Vector3Int(-1, -1, 0),
        };

        foreach(Vector3Int pos in tests)
        {
            if (!IsCellFree(pos))
                numOccupied++;
        }

        return (numOccupied >= 3) && 
            (lastAction == TetrominoAction.Rotate_NoWallKick || lastAction == TetrominoAction.Rotate_WallKick);
    }

    void HardDrop()
    {
        if(!isPaused)
            HardDrop(_currTetro);
    }

    void HardDropGhostMinos()
    {
        int shortestDist = _height + _topMargin;
        Vector3Int highest = new Vector3Int(0,0,0);

        foreach (Mino mino in _currTetro.ChildMinos)
        {
            // find highest Mino in the board column
            highest = GetHighestFreeCell(mino.CellPos.x);

            // subtract yPos of board's Mino from that of current Tetromino's lowest Mino in that column 
            int yDiff = mino.CellPos.y - highest.y;

            // Find the shortest yDiff each loop
            shortestDist = shortestDist > yDiff ? yDiff : shortestDist;
        }

        // Move the tetromino down
        foreach(Mino m in _ghostMinos)
        {
            m.MoveDown(shortestDist);
        }

    }

    private void HardDrop(Tetromino t)
    {
        int shortestDist = _height*2;

        foreach(Mino mino in t.ChildMinos)
        {
            // find highest Mino in the board column
            Vector3Int highest = GetHighestFreeCell(mino.CellPos.x);

            // subtract yPos of board's Mino from that of current Tetromino's lowest Mino in that column 
            int yDiff = mino.CellPos.y - highest.y;

            // Find the shortest yDiff each loop
            shortestDist = shortestDist > yDiff ? yDiff : shortestDist;

            // Play quickdrop particle effect
            mino.PlayQuickDrop();
        }

        // Move the tetromino down
        t.MoveDown(shortestDist);

        SkipLockMode();

        HardDropped?.Invoke(shortestDist);
        _audioSource.PlayOneShot(_quickDrop);
    }

    Vector3Int GetHighestFreeCell(int col)
    {
        Vector3Int prevCell = new Vector3Int(col, 0, 0);

        // Start loop from top of board to bottom
        for (int row = _height - 1; row >= 0; row--)
        {
            Vector3Int currCell = new Vector3Int(col, row, 0);

            if (_minoPositions.ContainsKey(currCell))
                return prevCell;
            else
                prevCell = currCell;
        }

        return prevCell;
    }

    void BankCurrentTetro()
    {
        if (_currTetro == null)
            return;

        var symbol = _currTetro.GetSymbol();
        symbol.transform.position = _bankedSymbolTransform.position;

        _currTetro.gameObject.SetActive(false);
        _bankedTetro = _currTetro;

        _currTetro = null;

        // Player can no longer bank until next tetromino is placed
        _canBank = false;
        // If the banked piece is swapped in while lock mode is on, 
        // it could cause the next piece to explode early
        _lockMode = false;
    }

    void SwapBankedTetro()
    {
        // Store the symbol for the tetromino to be banked
        var symbol = _currTetro.GetSymbol();

        // Deactivate current tetromino
        _currTetro.gameObject.SetActive(false);

        // Remove the symbol for the previously banked tetromino
        _bankedTetro.DestroySymbol();

        // Put the symbol for the new banked tetromino up
        symbol.transform.position = _bankedSymbolTransform.position;
        
        // Cache the previously banked tetromino
        var temp = _bankedTetro;
        
        // Swap the tetrominos
        _bankedTetro = _currTetro;
        _currTetro = temp;

        // Activate new tetromino and move it back to the top
        _currTetro.gameObject.SetActive(true);
        _currTetro.Reset();

        // Player can no longer bank until next tetromino
        _canBank = false;
        // If the banked piece is swapped in while lock mode is on, 
        // it could cause the next piece to explode early
        _lockMode = false;
    }

    public void ClearAll()
    {
        foreach(Mino m in _minoPositions.Values)
        {
            Destroy(m.gameObject);
        }

        _bankedTetro?.DestroySymbol();

        _bankedTetro = null;
        _minoPositions.Clear();

        //BoardReset?.Invoke();
    }

    public bool IsCellFree(Vector3Int cell)
    {
        // If cell is out of bounds; cell is not valid
        if (cell.x >= _width || cell.x < 0 || cell.y < 0 || cell.y >= _height + _topMargin)
            return false;

        return !(_minoPositions.ContainsKey(cell));
    }

    private bool CheckGameOver()
    {
        foreach(Vector3Int minoPos in _minoPositions.Keys)
        {
            if(minoPos.y >= _height)
            {
                Lost?.Invoke();
                return true;
            }
        }

        return false;
    }

    public void AddMinos(Tetromino t)
    {
        foreach (Mino m in t.ChildMinos)
        {
            try
            {
                _minoPositions.Add(m.CellPos, m);
            }
            catch(ArgumentException e)
            {
                Debug.LogError("Cannot add mino to board. Position occupied.");
            }
        }
    }

    public void AddMinos(IEnumerable<Mino> minoCollection)
    {
        foreach(Mino m in minoCollection)
        {
            _minoPositions.Add(m.CellPos, m);
        }
    }

    List<int> ClearLines()
    {
        List<int> rowsCleared = new List<int>();
        BoardAction action;

        for(int row = 0; row < _height; row++)
        {
            List<Mino> minosToClear = new List<Mino>();

            for(int col = 0; col < _width; col++)
            {
                Mino mino;
                _minoPositions.TryGetValue(new Vector3Int(col, row, 0), out mino);

                
                if (mino == null)// If one of the cells in a row is empty, go to the next row
                    break;
                else
                    minosToClear.Add(mino);
            }

            //Clear the line if full
            if(minosToClear.Count == _width)
            {
                foreach(Mino m in minosToClear)
                {
                    _minoPositions.Remove(m.CellPos);
                    Destroy(m.gameObject);
                }

                rowsCleared.Add(row);

                SpawnClearEffect(row);
            }
        }
        
        if(rowsCleared.Count > 0)
        {
            _audioSource.PlayOneShot(_lineClear);
        }

        return rowsCleared;
    }

    void SpawnClearEffect(int row)
    {
        var spawnPos = _grid.CellToWorld(new Vector3Int(_width / 2, row, 0)); // Middle of the current row
        Instantiate(_lineClearEffect, spawnPos, Quaternion.identity);
        Instantiate(_lineClearEffect, spawnPos, Quaternion.Euler(0,0,180));
    }

    void ShiftRowsDown()
    {//Traverse emptyRow list backward to drop highest rows first
        for(int i = _emptyRows.Count - 1; i >= 0; i--)
        {
            for(int currRow = _emptyRows[i] + 1; currRow < _height; currRow++)
            {
                ShiftRowDown(currRow);
            }
        }

        _emptyRows.Clear();
    }

    void ShiftRowDown(int row)
    {
        for(int col = 0; col < _width; col++)
        {
            Mino mino;
            _minoPositions.TryGetValue(new Vector3Int(col, row, 0), out mino);

            if(mino)
            {//move the mino down by one row
                _minoPositions.Remove(mino.CellPos);
                mino.MoveDown();
                _minoPositions.Add(mino.CellPos, mino);
            }

        }
    }
}

public class BoardAction
{
    public BoardActionType Type { get; private set; }
    public bool Difficult { get; private set; }
    public int LinesCleared { get; private set; }
    public bool TSpin { get; private set; }

    public BoardAction (int lines, bool t_spin, bool wallKick)
    {
        LinesCleared = lines;
        TSpin = t_spin;
        Type = ClassifyType(lines, t_spin, wallKick);
        Difficult = (t_spin && lines > 0) || (lines >= 4);
    }


    public BoardAction(int lines, bool t_spin, TetrominoAction tetroAction)
    {
        LinesCleared = lines;
        TSpin = t_spin;
        bool wallKick = tetroAction == TetrominoAction.Rotate_WallKick;

        Type = ClassifyType(lines, t_spin, wallKick);
        Difficult = (t_spin && lines > 0) || (lines >= 4);
    }

    private BoardActionType ClassifyType(int lines, bool tSpin, bool wallKick)
    {
        if(tSpin && wallKick)
        {
            switch(lines)
            {
                case 0:
                    return BoardActionType.T_Spin_Mini;
                case 1:
                    return BoardActionType.T_Spin_Mini_Single;
                case 2:
                    return BoardActionType.T_Spin_Mini_Double;
            }
        }

        if (tSpin && !wallKick)
        {
            switch (lines)
            {
                case 0:
                    return BoardActionType.T_Spin;
                case 1:
                    return BoardActionType.T_Spin_Single;
                case 2:
                    return BoardActionType.T_Spin_Double;
                case 3:
                    return BoardActionType.T_Spin_Triple;
            }
        }

        switch(lines)
        {
            case 1:
                return BoardActionType.Single;
            case 2:
                return BoardActionType.Double;
            case 3:
                return BoardActionType.Triple;
            case 4:
                return BoardActionType.Tetris;
        }

        return BoardActionType.Null;
    }
}

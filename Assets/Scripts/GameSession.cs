using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession instance;

    [Header("Board")]
    [SerializeField] BoardController _board;
    public event Action<int> ScoreChanged;
    public event Action<int> LevelUp;
    public event Action Paused;
    public event Action Unpaused;
    public event Action GameReset;
    public event Action<int> Combo;
    public event Action BackToBack;
    public event Action<int> ComboBroke;

    //Scoring
    [Header("Scoring")]    
    [SerializeField] int linesPerLevel = 1;
    [SerializeField] int levelBonus = 40;
    int _totalScore = 0;
    int currLevel = 1;
    int numLinesCleared = 0;
    int _combo = -1;
    int _backToBackCombo = -1;

    float _backToBackMultiplier = 1.5f;
    int softDropMultiplier = 1;
    int hardDropMultiplier = 2;

    Dictionary<BoardActionType, int> _boardActionLUT = new Dictionary<BoardActionType, int> {
        {BoardActionType.Single, 100 }, {BoardActionType.Double, 300}, {BoardActionType.Triple, 500}, {BoardActionType.Tetris, 800},
        {BoardActionType.T_Spin_Mini, 100}, {BoardActionType.T_Spin, 400},
        {BoardActionType.T_Spin_Mini_Single, 200}, {BoardActionType.T_Spin_Single, 800},
        {BoardActionType.T_Spin_Mini_Double, 400}, {BoardActionType.T_Spin_Double, 1200},
        {BoardActionType.T_Spin_Triple, 1600}
    };

    readonly int[] _baseScores = { 40, 100, 300, 1200 };

    //Timers
    [Header("Timing")]
    [SerializeField] float baseTickRate;
    [SerializeField] float fastTickMultiplier;
    [SerializeField] float levelMultiplier = 0.8f;
    [SerializeField] float lineClearDelay = 0.5f;
    float fastTickRate;
    float currLockTimer;
    GameTimer _gameTimer;
    float tickTimer;
    float currTickTime;
    float currDelayTime;

    //State
    bool isGameOver = false;
    bool isPaused = false;
    bool softDropMode = false;
    bool delayMode = false;

    //Sounds
    [Header("Sounds")]
    //[SerializeField] AudioClip s_lineClear;
    [SerializeField] AudioClip s_double;
    [SerializeField] AudioClip s_triple;
    [SerializeField] AudioClip s_tetris;
    [SerializeField] AudioClip s_tSpin;
    [SerializeField] AudioClip s_tSpinMini;
    [SerializeField] AudioClip s_levelUp;
    AudioSource _audioSource;

    public int TotalScore {
        get => _totalScore;
        set {
            _totalScore = value;
            ScoreChanged?.Invoke(value);
        }
    }

    private void Awake()
    {
        _gameTimer = FindObjectOfType<GameTimer>();
        _audioSource = GetComponent<AudioSource>();
        instance = this;
    }

    void Start()
    {
        tickTimer = baseTickRate * Mathf.Pow(levelMultiplier, currLevel);
        fastTickRate = tickTimer * fastTickMultiplier;

        _board.Lost += OnGameOver;
        _board.BoardAction += OnBoardAction;
        _board.HardDropped += OnHardDrop;
        _board.TetroMovedDown += OnTetrominoGravity;
    }

    private void OnTetrominoGravity()
    {
        if(softDropMode)
        {
            TotalScore += softDropMultiplier;
        }
    }

    void Update()
    {
        if(!isGameOver && !isPaused && !delayMode)
            UpdateTickTimer();

        UpdateInput();

        if (delayMode)
            UpdateDelayTimer();
        else
            CancelDelayMode();
    }

    private void CancelDelayMode()
    {
        delayMode = false;
        currDelayTime = 0;
    }

    private void UpdateDelayTimer()
    {
        currDelayTime += Time.deltaTime;

        if(currDelayTime >= lineClearDelay)
        {
            CancelDelayMode();
            SkipToNextTick();
        }
    }

    void UpdateTickTimer()
    {
        currTickTime += Time.deltaTime;

        if (currTickTime >= tickTimer)
        {
            _board.Tick();
            currTickTime = 0;
        }
    }

    public int GetCurrentScore()
    {
        return TotalScore;
    }

    public int GetCurrentLevel()
    {
        return currLevel;
    }

    public void PauseGame()
    {
        _gameTimer.Pause();
        _board.Pause();

        isPaused = true;
        Paused?.Invoke();
    }

    public void UnpauseGame()
    {
        _gameTimer.Unpause();
        _board.Unpause();

        isPaused = false;
        Unpaused?.Invoke();
    }

    private void ResetGame()
    {
        _gameTimer.Reset();
        _board.ClearAll();
        _board.Reset();
        tickTimer = baseTickRate;
        fastTickRate = tickTimer * fastTickMultiplier;
        TotalScore = 0;
        currLevel = 1;
        isGameOver = false;
        GameReset?.Invoke();
    }

    void SkipToNextTick()
    {
        //_board.Tick();
        currTickTime = tickTimer;
    }

    void OnHardDrop(int lines)
    {
        TotalScore += lines * hardDropMultiplier;
        SkipToNextTick();
    }

    void OnGameOver()
    {
        print("Game Over");
        isGameOver = true;

        _gameTimer.Pause();
    }

    void UpdateInput()
    {
        if (Input.GetButtonDown("Soft Drop"))
        {
            softDropMode = true;
            tickTimer = fastTickRate;
        }

        if (Input.GetButtonUp("Soft Drop"))
        {
            softDropMode = false;
            tickTimer = baseTickRate * Mathf.Pow(levelMultiplier, currLevel);
        }

        if(Input.GetButtonDown("Hard Drop") && isGameOver)
        {
            ResetGame();
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                UnpauseGame();
            else
                PauseGame();
        }
    }

    void ComboBreak()
    {
        ComboBroke?.Invoke(_combo);
        _combo = -1;
        _backToBackCombo = -1;
    }
    
    //Is called on each line clear
    void OnBoardAction(BoardAction action)
    {
        if (action.Type == BoardActionType.Null)
        {
            ComboBreak();
            return;
        }

        PlayActionSound(action.Type);

        //Activate line clear delay
        delayMode = true;

        _combo += 1;
        if (action.Difficult)
            _backToBackCombo += 1;
        else
            _backToBackCombo = -1;

        int baseScore = _boardActionLUT[action.Type];

        if (_backToBackCombo > 0)
        {
            BackToBack?.Invoke();
            baseScore = (int)(baseScore * 1.5f);
        }

        int comboScore = 0;
        if(_combo > 0)
        {
            comboScore = _combo * currLevel * 50;
            Combo?.Invoke(_combo);
        }

        int scoreSubtotal = baseScore * currLevel + comboScore;

        TotalScore += scoreSubtotal;

        numLinesCleared += action.LinesCleared;

        if(numLinesCleared >= linesPerLevel)
        {
            IncreaseLevel(numLinesCleared/linesPerLevel);
        }
    }

    void PlayActionSound(BoardActionType type)
    {
        switch(type)
        {
            case BoardActionType.Single:
            case BoardActionType.Double:
                _audioSource.PlayOneShot(s_double);
                break;
            case BoardActionType.Triple:
                _audioSource.PlayOneShot(s_triple);
                break;

            case BoardActionType.Tetris:
                _audioSource.PlayOneShot(s_tetris);
                break;

            case BoardActionType.T_Spin:
            case BoardActionType.T_Spin_Double:
            case BoardActionType.T_Spin_Triple:
                _audioSource.PlayOneShot(s_tSpin);
                break;

            case BoardActionType.T_Spin_Mini:
            case BoardActionType.T_Spin_Mini_Double:
                _audioSource.PlayOneShot(s_tSpinMini);
                break;
        }
    }

    void IncreaseLevel(int numLevels)
    {
        _audioSource.PlayOneShot(s_levelUp);
        numLinesCleared = 0;
        currLevel+= numLevels;
        LevelUp?.Invoke(currLevel);

        // Increase tick rate of board to make it go faster
        //baseTickRate *= levelMultiplier;
        tickTimer = baseTickRate * Mathf.Pow(levelMultiplier, currLevel);
        fastTickRate = tickTimer * fastTickMultiplier;
    }
}

public enum BoardActionType
{
    Single=1, Double=2, Triple=3, Tetris=4, 
    T_Spin_Mini, T_Spin_Mini_Single, T_Spin_Mini_Double, 
    T_Spin, T_Spin_Single, T_Spin_Double,T_Spin_Triple, Null
}

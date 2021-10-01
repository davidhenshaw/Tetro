using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession instance;

    [Header("Board")]
    [SerializeField] BoardController _board;
    public event Action<int,int> ScoreIncreased;
    public event Action<int> LevelUp;
    public event Action Paused;
    public event Action Unpaused;
    public event Action GameReset;

    //Scoring
    [Header("Scoring")]    
    [SerializeField] int linesPerLevel = 1;
    [SerializeField] int levelBonus = 40;
    int currScore;
    int currLevel = 0;
    int numLinesCleared = 0;

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
    bool delayMode = false;

    private void Awake()
    {
        _gameTimer = FindObjectOfType<GameTimer>();
        instance = this;
    }

    void Start()
    {
        tickTimer = baseTickRate * Mathf.Pow(levelMultiplier, currLevel);
        fastTickRate = tickTimer * fastTickMultiplier;

        _board.Lost += OnGameOver;
        _board.LinesCleared += OnLinesCleared;
        _board.QuickDropped += OnQuickDrop;
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
        return currScore;
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
        currScore = 0;
        currLevel = 0;
        isGameOver = false;

        GameReset?.Invoke();
    }

    void SkipToNextTick()
    {
        currTickTime = tickTimer;
    }

    void OnQuickDrop()
    {
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
        if (Input.GetButtonDown("Hurry"))
        {
            tickTimer = fastTickRate;
        }

        if (Input.GetButtonUp("Hurry"))
        {
            tickTimer = baseTickRate * Mathf.Pow(levelMultiplier, currLevel);
        }

        if(Input.GetButtonDown("CW Rotate") && isGameOver)
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

    void OnLinesCleared(int lines)
    {
        if (lines <= 0)
            return;
        //Activate line clear delay
        delayMode = true;

        int baseScore = _baseScores[lines - 1];

        int scoreToAdd = baseScore + currLevel * levelBonus;

        int prevScore = currScore;
        currScore += scoreToAdd;

        ScoreIncreased?.Invoke(prevScore, currScore);

        numLinesCleared += lines;

        if(numLinesCleared >= linesPerLevel)
        {
            IncreaseLevel(numLinesCleared/linesPerLevel);
        }
    }

    void IncreaseLevel(int numLevels)
    {
        numLinesCleared = 0;
        currLevel+= numLevels;
        LevelUp?.Invoke(currLevel);

        // Decrease tick rate of board to make it go faster
        //baseTickRate *= levelMultiplier;
        tickTimer = baseTickRate * Mathf.Pow(levelMultiplier, currLevel);
        fastTickRate = tickTimer * fastTickMultiplier;
    }
}

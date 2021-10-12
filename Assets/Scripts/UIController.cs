using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Overlays")]
    [SerializeField] GameObject _pauseOverlay;

    [Header("Text")]
    [SerializeField] TMP_Text _gameOverText;
    [SerializeField] TMP_Text _timerText;
    [SerializeField] TMP_Text _scoreText;
    [SerializeField] TMP_Text _levelText;

    BoardController _board;
    GameTimer _timer;
    GameSession _session;

    private void Awake()
    {
        _timer = FindObjectOfType<GameTimer>();
        _session = FindObjectOfType<GameSession>();
        _board = FindObjectOfType<BoardController>();        
    }

    // Start is called before the first frame update
    void Start()
    {
        _scoreText.text = _session.GetCurrentScore().ToString();
        _levelText.text = _session.GetCurrentLevel().ToString();

        _session.LevelUp += OnLevelUp;
        _session.ScoreChanged += UpdateScore;
        _session.Paused += OnPause;
        _session.Unpaused += OnUnpause;
        _session.GameReset += () => UpdateScore(0);

        _board.Lost += OnGameOver;
        _board.BoardReset += OnBoardReset;

        _gameOverText.enabled = false;
        _pauseOverlay.SetActive(false);
    }

    private void OnUnpause()
    {
        _pauseOverlay.SetActive(false);
    }

    private void OnPause()
    {
        _pauseOverlay.SetActive(true);
    }

    private void Update()
    {
        _timerText.text = _timer.GetElapsedTimeString();
    }

    void UpdateScore(int newScore)
    {
        _scoreText.text = newScore.ToString();
    }

    void OnGameOver()
    {
        _gameOverText.enabled = true;
    }

    void OnBoardReset()
    {
        _gameOverText.enabled = false;
        UpdateScore(0);
        UpdateLevelText(1);
    }

    void OnLevelUp(int newLevel)
    {
        UpdateLevelText(newLevel);
    }

    void UpdateLevelText(int newLevel)
    {
        _levelText.text = newLevel.ToString();
    }
}

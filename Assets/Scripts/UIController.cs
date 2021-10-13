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

    [Header("Alerts")]
    [SerializeField] Transform _alertSpawn;
    [SerializeField] GameObject _alertPrefab;
    [Space]
    [SerializeField] Transform _comboAlertSpawn;
    [SerializeField] GameObject _comboAlertPrefab;

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
        _session.Combo += OnCombo;
        _session.BackToBack += OnBackToBackCombo;

        _board.Lost += OnGameOver;
        _board.BoardReset += OnBoardReset;
        _board.BoardAction += OnBoardAction;

        _gameOverText.enabled = false;
        _pauseOverlay.SetActive(false);
    }

    private void OnBackToBackCombo()
    {
        //throw new System.NotImplementedException();
    }

    private void OnCombo(int comboNum)
    {
        var popup = Instantiate(_comboAlertPrefab, _comboAlertSpawn);
        TMP_Text tmpLabel = popup.GetComponentsInChildren<TMP_Text>()[0];
        TMP_Text tmpNumber = popup.GetComponentsInChildren<TMP_Text>()[1];

        tmpLabel.text = "Combo";
        tmpNumber.text = $"{comboNum + 1}";
    }

    private void OnBoardAction(BoardAction action)
    {
        if (action.Type == BoardActionType.Null)
        {
            return;
        }
        var popup = Instantiate(_alertPrefab, _alertSpawn);
        TMP_Text tmpText = popup.GetComponentInChildren<TMP_Text>();

        switch(action.Type)
        {
            case BoardActionType.Single:
                tmpText.text = "Single";
                break;
            case BoardActionType.Double:
                tmpText.text = "Double";
                break;
            case BoardActionType.Triple:
                tmpText.text = "Triple";
                break;
            case BoardActionType.Tetris:
                tmpText.text = "Tetris";
                break;
            case BoardActionType.T_Spin:
                tmpText.text = "T-Spin";
                break;
            case BoardActionType.T_Spin_Double:
                tmpText.text = "T-Spin Double";
                break;
            case BoardActionType.T_Spin_Triple:
                tmpText.text = "T-Spin Triple";
                break;
            case BoardActionType.T_Spin_Mini:
                tmpText.text = "T-Spin Mini";
                break;
            case BoardActionType.T_Spin_Mini_Single:
                tmpText.text = "T-Spin Mini Single";
                break;
            case BoardActionType.T_Spin_Mini_Double:
                tmpText.text = "T-Spin Mini Double";
                break;
        }
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

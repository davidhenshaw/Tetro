using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    bool paused;
    float elapsed;

    // Update is called once per frame
    void Update()
    {
        if(!paused)
            elapsed += Time.deltaTime;
    }

    public void Reset()
    {
        elapsed = 0;
        paused = false;
    }

    public void Pause()
    {
        paused = true;
    }

    public void Unpause()
    {
        paused = false;
    }

    public float GetElapsedTime()
    {
        return elapsed;
    }

    public string GetElapsedTimeString()
    {
        int hours = (int) Mathf.Floor(elapsed / 3600f);
        int minutes = (int)Mathf.Floor(elapsed / 60);
        int seconds = (int)Mathf.Floor(elapsed % 60);

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}

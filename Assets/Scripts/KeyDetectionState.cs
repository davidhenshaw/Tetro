using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="DetectHurry")]
public class KeyDetectionState : ScriptableObject, IState
{
    [SerializeField] float timeLimit;
    [SerializeField] KeyCode keyToDetect;
    float timer;
    bool timedOut = false;

    public StateOutput GetStateOutput()
    {
        if (timedOut)
            return StateOutput.Fail;

        if(Input.GetKeyDown(keyToDetect))
        {
            return StateOutput.Pass;
        }

        return StateOutput.None;
    }

    public void Enter()
    {
        Reset();
        timedOut = false;
    }

    public void Exit()
    {
        Reset();
    }

    public void Reset()
    {
        timer = 0;
    }

    private void OnDisable()
    {
        Reset();
    }

    public void Tick(float deltaTime)
    {
        timedOut = false;

        if (! timedOut)
        {
            if (timer >= timeLimit)
            {
                timedOut = true;
            }
            else
            {
                timer += deltaTime;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternDetector : MonoBehaviour
{
    int currStateIndex = 0;
    [SerializeField] KeyDetectionState[] _sequence;

    public event Action PatternDetected;

    private void Start()
    {
        _sequence[0].Enter();
    }

    private void Update()
    {
        IState myState = _sequence[currStateIndex];

        myState.Tick(Time.deltaTime);

        StateOutput output = myState.GetStateOutput();

        switch (output)
        {
            case StateOutput.Pass:
                {
                    if (currStateIndex >= _sequence.Length - 1)
                    {
                        TriggerOnDetect();
                    }
                    else
                    {
                        //Debug.Log("Pass!");
                        int i = currStateIndex + 1;
                        GoToState(i);
                    }
                }
                break;

            case StateOutput.Fail:
                {
                    if (currStateIndex > 0)
                    {
                        GoToState(--currStateIndex);
                    }
                    else
                    {
                        myState.Reset();
                    }

                    //Debug.Log("Sequence Fail", this);
                }
                break;

            case StateOutput.None:
                break;
        }
    }

    void TriggerOnDetect()
    {
        PatternDetected?.Invoke();

        Debug.Log("Pattern Detected!");

        GoToState(0);
    }

    void GoToState(int nextIndex)
    {
        _sequence[nextIndex].Enter();

        currStateIndex = nextIndex;
    }
}

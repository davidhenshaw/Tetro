using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Wall Kick Data")]
public class WallKickData : ScriptableObject
{
    public WallKickState[] states;


    public Vector2Int[] GetClockWiseTestOffsets(int rotationState)
    {
        return states[rotationState].clockwiseTests;
    }

    public Vector2Int[] GetCounterClockWiseTestOffsets(int rotationState)
    {
        return states[rotationState].counterClockwiseTests;
    }
}

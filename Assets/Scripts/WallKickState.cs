using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Wall Kick State")]
public class WallKickState : ScriptableObject
{
    [SerializeField] public Vector2Int[] clockwiseTests;
    [SerializeField] public Vector2Int[] counterClockwiseTests;
}

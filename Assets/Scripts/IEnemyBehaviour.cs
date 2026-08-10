using System;
using UnityEngine;

public interface IEnemyBehaviour
{
    int ScoreValue { get; }
    bool PenalizeOnEscape { get; }
    GameObject GameObject { get; }
    void Initialize(float lifetime);
    event Action<IEnemyBehaviour> OnEnemyEscaped;
}

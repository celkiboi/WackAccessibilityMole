using System;
using UnityEngine;

public interface IEnemyBehaviour
{
    int ScoreValue { get; }
    bool PenalizeOnEscape { get; }
    GameObject GameObject { get; }
    void Initialize(float lifetime);
    void Hit();
    event Action<IEnemyBehaviour> OnEnemyEscaped;
}

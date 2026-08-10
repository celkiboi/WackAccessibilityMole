using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyBehaviour
{
    float TimeExtension { get; }

    GameObject GameObject { get; }
}

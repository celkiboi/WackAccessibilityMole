using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandardEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    float additionalTimeForKill = 2.0f;

    public float TimeExtension { get; private set; }

    public GameObject GameObject => this.gameObject;

    private void OnMouseDown()
    {
        if (!GameManager.isGameFinished)
            GameManager.Instance.KillEnemy(this);
    }

    private void Awake()
    {
        TimeExtension = additionalTimeForKill;
    }
}

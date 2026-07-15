using UnityEngine;

/// <summary>
/// Declares the preferred minigame for a repairable item prefab.
/// Used by customer item randomization before the item instance is spawned.
/// </summary>
[DisallowMultipleComponent]
public class MinigameToPlay : MonoBehaviour
{
    [SerializeField] private MinigameType minigame = MinigameType.Cleaning;
    [SerializeField] private bool overrideRandomTask = true;

    public MinigameType Minigame => minigame;
    public bool OverrideRandomTask => overrideRandomTask;
}

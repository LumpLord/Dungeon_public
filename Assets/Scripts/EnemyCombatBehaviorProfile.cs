using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Combat Profile", menuName = "AI/Combat Behavior Profile")]
public class EnemyCombatBehaviorProfile : ScriptableObject
{
    [SerializeField]
    public List<WeightedCombatState> weightedStates = new();

    public IEnemyCombatState GetStateByName(string name)
    {
        foreach (var ws in weightedStates)
        {
            if (ws.stateComponent != null && ws.stateComponent.GetStateName() == name)
            {
                return ws.GetState();
            }
        }
        return null;
    }
}

[System.Serializable]
public struct WeightedCombatState
{
    [Tooltip("A state component that inherits from EnemyCombatStateBase.")]
    public EnemyCombatStateBase stateComponent;

    [Tooltip("Weight (likelihood) of choosing this state).")]
    public float weight; 

    public IEnemyCombatState GetState()
    {
        return (IEnemyCombatState)stateComponent;
    }
}
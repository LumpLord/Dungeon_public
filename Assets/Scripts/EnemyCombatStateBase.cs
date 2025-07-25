using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyCombatStateBase : ScriptableObject, IEnemyCombatState
{
    public abstract IEnumerator Execute(EnemyCombatController controller);

    public virtual void EnterState(EnemyCombatController controller) { }
    public virtual void ExitState(EnemyCombatController controller) { }
    public virtual bool CanExit(EnemyCombatController controller) => true;
    public virtual bool CanQueueNextState(EnemyCombatController controller) => true;

    public virtual string GetStateName() => name;

    public virtual bool CanExecute(EnemyCombatController controller)
    {
        if (controller == null || !controller.isActiveAndEnabled || controller.GetTarget() == null)
            return false;

        var agent = controller.GetAgent();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        return true;
    }

    public bool CanRunAfter(EnemyCombatStateBase previousState)
    {
        return allowedPreviousStates.Count == 0 || allowedPreviousStates.Contains(previousState);
    }

    [Tooltip("Only allow this state to follow one of the listed states")] 
    public List<EnemyCombatStateBase> allowedPreviousStates = new();
}
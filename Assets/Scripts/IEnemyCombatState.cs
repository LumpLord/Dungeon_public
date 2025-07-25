using System.Collections;

public interface IEnemyCombatState
{
    IEnumerator Execute(EnemyCombatController controller);
    public bool CanExecute(EnemyCombatController controller);
}
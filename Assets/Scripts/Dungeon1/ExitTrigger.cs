// File: Dungeon1/ExitTrigger.cs
// Purpose: Dungeon1 exit trigger.
// Inputs: Player collider entering the exit trigger volume.
// Output/side effects: Notifies GameManager that the level exit was reached.

using UnityEngine;

// Attach to the exit marker trigger collider.
// Reaching the exit completes the current level.
namespace CM3070.Dungeon1
{
    public sealed class ExitTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerInventory>() == null)
            {
                return;
            }

            GameManager.Instance?.NotifyExitReached();
        }
    }
}

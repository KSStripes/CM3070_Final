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

using UnityEngine;

namespace CM3070.Office.Quest
{
    // Attach to a task marker trigger zone to report player interactions to QuestManager.
    [RequireComponent(typeof(Collider))]
    public sealed class TaskMarker : MonoBehaviour
    {
        [SerializeField] private OfficeTaskMarkerId markerId = OfficeTaskMarkerId.None;
        [SerializeField] private string displayName = "Task Marker";
        [SerializeField] private bool triggerOnEnter = true;

        private bool playerInside;
        private QuestInventory currentInventory;

        private void Reset()
        {
            // Task markers should sense the player without blocking movement.
            Collider markerCollider = GetComponent<Collider>();
            markerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            QuestInventory inventory = other.GetComponentInParent<QuestInventory>();
            if (inventory == null) return;

            playerInside = true;
            currentInventory = inventory;

            if (triggerOnEnter)
            {
                NotifyQuestManager(inventory);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            QuestInventory inventory = other.GetComponentInParent<QuestInventory>();
            if (inventory == null || inventory != currentInventory) return;

            playerInside = false;
            currentInventory = null;
        }

        public void Interact()
        {
            // Public hook for future button prompts without changing prefab wiring.
            if (playerInside && currentInventory != null)
            {
                NotifyQuestManager(currentInventory);
            }
        }

        private void NotifyQuestManager(QuestInventory inventory)
        {
            if (markerId == OfficeTaskMarkerId.None) return;

            QuestManager.Instance?.NotifyMarkerReached(markerId, displayName, inventory);
        }
    }
}

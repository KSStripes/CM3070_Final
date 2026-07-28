using UnityEngine;

namespace CM3070.Office.Quest
{
    public enum TaskMarkerVisualState
    {
        Available,
        Unavailable,
        Completed
    }

    // Attach to a task marker trigger zone to report player interactions to QuestManager.
    [RequireComponent(typeof(Collider))]
    public sealed class TaskMarker : MonoBehaviour
    {
        [SerializeField] private OfficeTaskMarkerId markerId = OfficeTaskMarkerId.None;
        [SerializeField] private string displayName = "Task Marker";
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private Renderer[] highlightRenderers;
        [SerializeField] private Material availableMaterial;
        [SerializeField] private Material unavailableMaterial;
        [SerializeField] private Material completedMaterial;

        private bool playerInside;
        private QuestInventory currentInventory;
        private TaskMarkerVisualState visualState = TaskMarkerVisualState.Available;

        private void Reset()
        {
            // Task markers should sense the player without blocking movement.
            Collider markerCollider = GetComponent<Collider>();
            markerCollider.isTrigger = true;
            FindHighlightRenderers();
        }

        private void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                FindHighlightRenderers();
            }

            RefreshVisualState(null);
        }

        private void OnTriggerEnter(Collider other)
        {
            QuestInventory inventory = other.GetComponentInParent<QuestInventory>();
            if (inventory == null) return;

            playerInside = true;
            currentInventory = inventory;
            RefreshVisualState(inventory);

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
            RefreshVisualState(null);
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

            QuestManager questManager = QuestManager.Instance;
            if (questManager != null && !questManager.CanUseMarker(markerId, inventory))
            {
                RefreshVisualState(inventory);
                Debug.Log($"{displayName} is unavailable.");
                return;
            }

            QuestManager.Instance?.NotifyMarkerReached(markerId, displayName, inventory);
            RefreshVisualState(inventory);
        }

        private void RefreshVisualState(QuestInventory inventory)
        {
            QuestManager questManager = QuestManager.Instance;
            if (questManager == null)
            {
                ApplyVisualState(TaskMarkerVisualState.Available);
                return;
            }

            if (questManager.IsMarkerCompleted(markerId))
            {
                ApplyVisualState(TaskMarkerVisualState.Completed);
            }
            else if (questManager.CanUseMarker(markerId, inventory))
            {
                ApplyVisualState(TaskMarkerVisualState.Available);
            }
            else
            {
                ApplyVisualState(TaskMarkerVisualState.Unavailable);
            }
        }

        private void ApplyVisualState(TaskMarkerVisualState nextState)
        {
            visualState = nextState;

            Material stateMaterial = visualState switch
            {
                TaskMarkerVisualState.Unavailable => unavailableMaterial,
                TaskMarkerVisualState.Completed => completedMaterial != null ? completedMaterial : unavailableMaterial,
                _ => availableMaterial
            };

            if (stateMaterial == null) return;

            foreach (Renderer highlightRenderer in highlightRenderers)
            {
                if (highlightRenderer != null)
                {
                    highlightRenderer.sharedMaterial = stateMaterial;
                }
            }
        }

        private void FindHighlightRenderers()
        {
            // Inspector convenience: find the sibling/child glow object used by task prefabs.
            Transform searchRoot = transform.parent != null ? transform.parent : transform;
            Transform highlight = searchRoot.Find("InteractionHighlight");
            highlightRenderers = highlight != null
                ? highlight.GetComponentsInChildren<Renderer>(true)
                : searchRoot.GetComponentsInChildren<Renderer>(true);
        }
    }
}

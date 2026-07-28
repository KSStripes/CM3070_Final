using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Trigger pickup for task-critical office items such as folders and printer paper.
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private QuestItemId itemId = QuestItemId.None;
        [SerializeField] private string displayName = "Quest Item";
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private float rotationSpeed = 45f;

        private void Reset()
        {
            // Keep prefab physics compatible with trigger-based player pickup.
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;

            Rigidbody pickupBody = GetComponent<Rigidbody>();
            pickupBody.isKinematic = true;
            pickupBody.useGravity = false;
        }

        private void Update()
        {
            // Lightweight visibility cue while pickups are still simple prefabs.
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            OfficePlayerInventory inventory = other.GetComponentInParent<OfficePlayerInventory>();
            if (inventory == null || itemId == QuestItemId.None) return;

            inventory.AddQuestItem(itemId, amount);
            QuestManager.Instance?.NotifyItemCollected(itemId, displayName, amount);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}

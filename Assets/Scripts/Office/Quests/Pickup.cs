using CM3070.Dungeon1;
using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Optional coping pickups, separate from required quest items.
    public enum PickupId
    {
        None,
        Coffee,
        Snack,
        Headphones,
        StressBall
    }

    // Trigger pickup for office coping items such as coffee, snacks, and stress relief props.
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupId pickupId = PickupId.None;
        [SerializeField, Min(0)] private int healthRestore = 0;
        [SerializeField, Min(0)] private int maxHealthIncrease = 0;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private float rotationSpeed = 45f;

        private void Reset()
        {
            // Keep pickup prefabs compatible with trigger-based collection.
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;

            Rigidbody pickupBody = GetComponent<Rigidbody>();
            pickupBody.isKinematic = true;
            pickupBody.useGravity = false;
        }

        private void Update()
        {
            // Simple visibility cue for small pickup prefabs.
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (pickupId == PickupId.None) return;

            // QuestInventory identifies the player without depending on tags or layers.
            QuestInventory inventory = other.GetComponentInParent<QuestInventory>();
            if (inventory == null) return;

            HealthSystem health = other.GetComponentInParent<HealthSystem>();
            if (healthRestore > 0 && (health == null || !health.Heal(healthRestore)))
            {
                return;
            }

            // Some coping pickups increase max health instead of restoring current health.
            if (maxHealthIncrease > 0 && health != null)
            {
                health.IncreaseMaxHealth(maxHealthIncrease);
            }

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }

    }
}

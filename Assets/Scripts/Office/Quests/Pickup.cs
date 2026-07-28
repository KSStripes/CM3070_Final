using CM3070.Dungeon1;
using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Prototype IDs for optional coping pickups, separate from required quest items.
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
        [SerializeField] private string displayName = "Pickup";
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
            // Temporary readability cue until final pickup art/animation exists.
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (pickupId == PickupId.None) return;

            // OfficePlayerInventory marks the player without depending on tags or layers.
            OfficePlayerInventory inventory = other.GetComponentInParent<OfficePlayerInventory>();
            if (inventory == null) return;

            HealthSystem health = other.GetComponentInParent<HealthSystem>();
            if (!inventory.AddPickup(pickupId, displayName, healthRestore, health))
            {
                return;
            }

            // Some coping pickups increase max health instead of restoring current health.
            if (maxHealthIncrease > 0 && health != null)
            {
                health.IncreaseMaxHealth(maxHealthIncrease);
            }

            ApplyPrototypeEffect();
            QuestManager.Instance?.NotifyPickupCollected(pickupId, displayName);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyPrototypeEffect()
        {
            // Future emotional stats can branch from these IDs without prefab renaming.
            switch (pickupId)
            {
                case PickupId.Coffee:
                    Debug.Log($"{displayName}: prototype energy boost.");
                    break;
                case PickupId.Snack:
                    Debug.Log($"{displayName}: prototype small recovery.");
                    break;
                case PickupId.Headphones:
                    Debug.Log($"{displayName}: max health increased by {maxHealthIncrease}.");
                    break;
                case PickupId.StressBall:
                    Debug.Log($"{displayName}: max health increased by {maxHealthIncrease}.");
                    break;
            }
        }
    }
}

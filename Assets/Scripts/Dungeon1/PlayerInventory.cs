using UnityEngine;

// Stores player inventory/state gained from pickups.
// GameManager is notified here so later UI can react without loot scripts knowing about UI.
namespace CM3070.Dungeon1
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        public readonly struct InventorySnapshot
        {
            public InventorySnapshot(
                int coinCount,
                int armourCount,
                int weaponCount,
                int attack,
                string armourName,
                string armourType,
                string weaponName,
                string weaponType)
            {
                CoinCount = coinCount;
                ArmourCount = armourCount;
                WeaponCount = weaponCount;
                Attack = attack;
                ArmourName = armourName;
                ArmourType = armourType;
                WeaponName = weaponName;
                WeaponType = weaponType;
            }

            public int CoinCount { get; }
            public int ArmourCount { get; }
            public int WeaponCount { get; }
            public int Attack { get; }
            public string ArmourName { get; }
            public string ArmourType { get; }
            public string WeaponName { get; }
            public string WeaponType { get; }
        }

        public int CoinCount { get; private set; }
        public int ArmourCount { get; private set; }
        public int WeaponCount { get; private set; }
        public int Attack { get; private set; }
        public string ArmourName { get; private set; }
        public string ArmourType { get; private set; }
        public string WeaponName { get; private set; }
        public string WeaponType { get; private set; }

        public InventorySnapshot CaptureSnapshot()
        {
            return new InventorySnapshot(
                CoinCount,
                ArmourCount,
                WeaponCount,
                Attack,
                ArmourName,
                ArmourType,
                WeaponName,
                WeaponType);
        }

        public void ApplySnapshot(InventorySnapshot snapshot)
        {
            CoinCount = snapshot.CoinCount;
            ArmourCount = snapshot.ArmourCount;
            WeaponCount = snapshot.WeaponCount;
            Attack = snapshot.Attack;
            ArmourName = snapshot.ArmourName;
            ArmourType = snapshot.ArmourType;
            WeaponName = snapshot.WeaponName;
            WeaponType = snapshot.WeaponType;

            GameManager.Instance?.NotifyCoinsChanged(CoinCount);
        }

        public void AddCoins(int amount)
        {
            // Clamp negative values so misconfigured pickups cannot remove coins.
            CoinCount += Mathf.Max(0, amount);
            GameManager.Instance?.NotifyCoinsChanged(CoinCount);
        }

        public void AddArmour(LootProperties armour, HealthSystem health)
        {
            if (armour == null || health == null) return;

            ArmourCount++;
            ArmourName = armour.ArmourName;
            ArmourType = armour.ArmourType;
            // Armour currently increases max health as a simple defence placeholder.
            health.IncreaseMaxHealth(armour.DefenseAmount);
            GameManager.Instance?.NotifyArmourCollected(ArmourName, ArmourType, health.MaxHealth);
        }

        public void AddWeapon(LootProperties weapon)
        {
            if (weapon == null) return;

            WeaponCount++;
            WeaponName = weapon.WeaponName;
            WeaponType = weapon.WeaponType;
            // Attack is a simple stat placeholder until a weapon system exists.
            Attack += weapon.AttackAmount;
            GameManager.Instance?.NotifyWeaponCollected(weapon, this);
        }

        public void ResetInventory()
        {
            // Reset all stats at beginning of a new game.
            CoinCount = 0;
            ArmourCount = 0;
            WeaponCount = 0;
            Attack = 0;
            ArmourName = string.Empty;
            ArmourType = string.Empty;
            WeaponName = string.Empty;
            WeaponType = string.Empty;
            GameManager.Instance?.NotifyCoinsChanged(CoinCount);
        }
    }
}

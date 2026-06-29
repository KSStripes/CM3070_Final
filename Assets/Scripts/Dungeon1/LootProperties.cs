using UnityEngine;

// Inspector data for loot prefabs.
// One component covers coin, health, armour, and weapon pickups for this prototype stage.
namespace CM3070.Dungeon1
{
    public enum LootType
    {
        // Determines which branch LootPickup uses.
        Coin, // increases money
        Health, // only picks up when health is not full, increases health
        Weapon, // adds weapon type to inventory, increases attack
        Armour, // adds armour type to inventory, increases max health
    }
    
    public sealed class LootProperties : MonoBehaviour
    {
        // Only the fields relevant to the chosen LootType are used at pickup time.
        [SerializeField] private LootType lootType;
        [SerializeField] private string lootName = "Loot";
        [SerializeField] private int value = 1; // for coins
        [SerializeField] private int healAmount;
        [SerializeField] private string armourName = "Basic Armour";
        [SerializeField] private string armourType = "Light";
        [SerializeField] private int defenseAmount = 10;
        [SerializeField] private string weaponName = "Basic Weapon";
        [SerializeField] private string weaponType = "Melee";
        [SerializeField] private int attackAmount = 1;


        public LootType LootType => lootType;
        public string LootName => lootName;
        public int LootValue => value;
        public int HealAmount => healAmount;
        public string ArmourName => armourName;
        public string ArmourType => armourType;
        public int DefenseAmount => defenseAmount;
        public string WeaponName => weaponName;
        public string WeaponType => weaponType;
        public int AttackAmount => attackAmount;
        
    }

}

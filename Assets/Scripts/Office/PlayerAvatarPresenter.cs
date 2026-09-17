// File: Office/PlayerAvatarPresenter.cs
// Purpose: Avatar visibility switch for the selected player model.
// Inputs: GameManager player choice and male/female avatar GameObjects assigned in the Inspector.
// Output/side effects: Shows the chosen avatar and hides the other at runtime.

using CM3070.Dungeon1;
using UnityEngine;

namespace CM3070.Office
{
    // Enables one of two assigned avatar roots on the player prefab.
    public sealed class PlayerAvatarPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject femaleAvatarRoot;
        [SerializeField] private GameObject maleAvatarRoot;

        public void Apply(PlayerChoice choice)
        {
            // Both avatar prefabs can stay under Player; only the selected one is visible.
            bool useFemale = choice == PlayerChoice.Female;

            if (femaleAvatarRoot != null)
            {
                femaleAvatarRoot.SetActive(useFemale);
            }

            if (maleAvatarRoot != null)
            {
                maleAvatarRoot.SetActive(!useFemale);
            }
        }
    }
}

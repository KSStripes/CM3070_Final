using CM3070.Dungeon1;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Coordinates the first playable office loop: clock in, do tasks, clock out.
    public sealed class QuestManager : MonoBehaviour
    {
        [SerializeField] private bool requireFolderDelivery = true;
        [SerializeField] private bool requirePrinterFix = true;

        public static QuestManager Instance { get; private set; }

        public bool ClockedIn { get; private set; }
        public bool FolderDelivered { get; private set; }
        public bool PrinterFixed { get; private set; }
        public bool ShiftComplete { get; private set; }

        private void Awake()
        {
            // Scene-level singleton mirrors GameManager for simple prototype access.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void NotifyItemCollected(QuestItemId itemId, string displayName, int amount)
        {
            Debug.Log($"QuestManager noted pickup: {displayName} ({itemId}) x{amount}");
        }

        public void NotifyPickupCollected(PickupId pickupId, string displayName)
        {
            Debug.Log($"QuestManager noted coping pickup: {displayName} ({pickupId})");
        }

        public void NotifyMarkerReached(
            OfficeTaskMarkerId markerId,
            string displayName,
            QuestInventory inventory)
        {
            if (inventory == null || ShiftComplete) return;

            switch (markerId)
            {
                case OfficeTaskMarkerId.ClockInTerminal:
                    ClockIn(displayName);
                    break;
                case OfficeTaskMarkerId.DeliveryPoint:
                    TryDeliverFolder(displayName, inventory);
                    break;
                case OfficeTaskMarkerId.Printer:
                    TryFixPrinter(displayName, inventory);
                    break;
                case OfficeTaskMarkerId.ExitTerminal:
                    TryClockOut(displayName);
                    break;
                case OfficeTaskMarkerId.MeetingArea:
                case OfficeTaskMarkerId.BossDesk:
                    Debug.Log($"{displayName} reached. No prototype task is assigned yet.");
                    break;
            }
        }

        public bool CanUseMarker(OfficeTaskMarkerId markerId, QuestInventory inventory)
        {
            if (ShiftComplete) return false;

            return markerId switch
            {
                OfficeTaskMarkerId.ClockInTerminal => !ClockedIn,
                OfficeTaskMarkerId.DeliveryPoint => ClockedIn
                    && !FolderDelivered
                    && inventory != null
                    && inventory.HasItem(QuestItemId.Folder),
                OfficeTaskMarkerId.Printer => ClockedIn
                    && !PrinterFixed
                    && inventory != null
                    && inventory.HasItem(QuestItemId.PrinterPaper),
                OfficeTaskMarkerId.ExitTerminal => ClockedIn && RequiredTasksComplete(),
                _ => false
            };
        }

        public bool IsMarkerCompleted(OfficeTaskMarkerId markerId)
        {
            return markerId switch
            {
                OfficeTaskMarkerId.ClockInTerminal => ClockedIn,
                OfficeTaskMarkerId.DeliveryPoint => FolderDelivered,
                OfficeTaskMarkerId.Printer => PrinterFixed,
                OfficeTaskMarkerId.ExitTerminal => ShiftComplete,
                _ => false
            };
        }

        public void ResetShift()
        {
            ClockedIn = false;
            FolderDelivered = false;
            PrinterFixed = false;
            ShiftComplete = false;
        }

        private void ClockIn(string displayName)
        {
            if (ClockedIn)
            {
                Debug.Log("Already clocked in.");
                return;
            }

            ClockedIn = true;
            Debug.Log($"Clocked in at {displayName}.");
        }

        private void TryDeliverFolder(string displayName, QuestInventory inventory)
        {
            if (!ClockedIn)
            {
                Debug.Log("Clock in before delivering the folder.");
                return;
            }

            if (FolderDelivered)
            {
                Debug.Log("Folder already delivered.");
                return;
            }

            if (!inventory.RemoveItem(QuestItemId.Folder))
            {
                Debug.Log("Delivery needs QuestItem_Folder.");
                return;
            }

            FolderDelivered = true;
            Debug.Log($"Folder delivered at {displayName}.");
        }

        private void TryFixPrinter(string displayName, QuestInventory inventory)
        {
            if (!ClockedIn)
            {
                Debug.Log("Clock in before fixing the printer.");
                return;
            }

            if (PrinterFixed)
            {
                Debug.Log("Printer already fixed.");
                return;
            }

            if (!inventory.RemoveItem(QuestItemId.PrinterPaper))
            {
                Debug.Log("Printer task needs QuestItem_PrinterPaper.");
                return;
            }

            PrinterFixed = true;
            Debug.Log($"Printer fixed at {displayName}.");
        }

        private void TryClockOut(string displayName)
        {
            if (!ClockedIn)
            {
                Debug.Log("Clock in before ending the shift.");
                return;
            }

            if (!RequiredTasksComplete())
            {
                Debug.Log("Shift cannot end yet. Complete required tasks first.");
                return;
            }

            ShiftComplete = true;
            Debug.Log($"Clocked out at {displayName}. Shift complete.");
            GameManager.Instance?.NotifyExitReached();
        }

        private bool RequiredTasksComplete()
        {
            return (!requireFolderDelivery || FolderDelivered)
                && (!requirePrinterFix || PrinterFixed);
        }
    }
}

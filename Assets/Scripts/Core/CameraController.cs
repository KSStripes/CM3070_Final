using CM3070.PCG;
using UnityEngine;

// Positions the overview/minimap camera and the isometric player camera.
namespace CM3070.Dungeon1
{
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera overviewCamera;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private RenderTexture overviewRenderTexture;
        [SerializeField] private Vector3 playerCameraEuler = new(55f, 45f, 0f);
        [SerializeField] private float playerCameraDistance = 7f;
        [SerializeField] private float playerCameraHeight = 14f;
        [SerializeField] private float playerCameraOrthographicSize = 6.5f;
        [SerializeField] private Vector3 playerCameraTargetOffset = Vector3.zero;

        public void EnsureCameras(Transform owner)
        {
            if (overviewCamera == null)
            {
                overviewCamera = FindNamedCamera(owner, "OverviewCamera");
            }

            if (playerCamera == null)
            {
                playerCamera = FindNamedCamera(owner, "PlayerCamera");
            }

            if (overviewCamera != null)
            {
                overviewCamera.orthographic = true;
                overviewCamera.clearFlags = CameraClearFlags.SolidColor;
                overviewCamera.backgroundColor = new Color(0.10f, 0.12f, 0.13f);
                overviewCamera.enabled = true;

                if (overviewRenderTexture != null)
                {
                    // A target texture makes this camera feed the HUD RawImage instead of the main screen.
                    overviewCamera.targetTexture = overviewRenderTexture;
                }
            }

            if (playerCamera != null)
            {
                playerCamera.orthographic = true;
                playerCamera.clearFlags = CameraClearFlags.SolidColor;
                playerCamera.backgroundColor = new Color(0.10f, 0.12f, 0.13f);
                playerCamera.targetTexture = null;
                playerCamera.enabled = true;
            }
        }

        public void PositionOverviewCamera(DungeonLayout layout)
        {
            if (overviewCamera == null || layout == null)
            {
                return;
            }

            float longestSide = Mathf.Max(layout.Width, layout.Height);
            // Size from the largest dungeon side so the whole generated layout is visible.
            overviewCamera.transform.position = GetLayoutCenter(layout) + Vector3.up * longestSide;
            overviewCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            overviewCamera.orthographic = true;
            overviewCamera.orthographicSize = longestSide * 0.55f;
        }

        public void PositionPlayerCamera(Vector3 target)
        {
            if (playerCamera == null)
            {
                return;
            }

            Vector3 focus = target + playerCameraTargetOffset;
            // Fixed isometric-style offset; player input itself remains world-axis based.
            Vector3 offset = new(-playerCameraDistance, playerCameraHeight, -playerCameraDistance);
            playerCamera.transform.position = focus + offset;
            playerCamera.transform.rotation = Quaternion.Euler(playerCameraEuler);
            playerCamera.orthographic = true;
            playerCamera.orthographicSize = playerCameraOrthographicSize;
        }

        private static Camera FindNamedCamera(Transform owner, string cameraName)
        {
            Transform child = owner.Find(cameraName);
            if (child != null && child.TryGetComponent(out Camera childCamera))
            {
                return childCamera;
            }

            GameObject namedObject = GameObject.Find(cameraName);
            return namedObject != null ? namedObject.GetComponent<Camera>() : null;
        }

        private static Vector3 GetLayoutCenter(DungeonLayout layout)
        {
            return new Vector3(layout.Width * 0.5f, 0f, layout.Height * 0.5f);
        }
    }
}

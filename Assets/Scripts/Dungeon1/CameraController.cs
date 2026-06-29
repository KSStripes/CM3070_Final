using CM3070.PCG;
using UnityEngine;

// Owns the Dungeon1 camera setup.
// Keeps the player camera on screen and the overview camera mapped to the HUD texture.
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

        private Camera editorCamera;

        public void EnsureCameras(Transform owner)
        {
            // Prefer assigned cameras, then named children/scene objects, then create missing cameras.
            if (overviewCamera == null)
            {
                overviewCamera = FindNamedCamera(owner, "OverviewCamera");
            }

            if (playerCamera == null)
            {
                playerCamera = FindNamedCamera(owner, "PlayerCamera");
            }

            if (playerCamera == null)
            {
                // Create runtime-safe defaults if the scene was not fully wired in the Inspector.
                GameObject cameraObject = new("PlayerCamera");
                cameraObject.transform.SetParent(owner);
                playerCamera = cameraObject.AddComponent<Camera>();
            }

            if (overviewCamera == null)
            {
                GameObject cameraObject = new("OverviewCamera");
                cameraObject.transform.SetParent(owner);
                overviewCamera = cameraObject.AddComponent<Camera>();
            }

            editorCamera = Camera.main;

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
                else if (Application.isPlaying && overviewCamera.targetTexture == null)
                {
                    Debug.LogWarning("OverviewCamera needs a Target Texture for the HUD map.");
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

            if (Application.isPlaying && editorCamera != null && editorCamera != overviewCamera && editorCamera != playerCamera)
            {
                // Avoid rendering from the editor convenience Main Camera during Play mode.
                editorCamera.enabled = false;
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
            // First search under the controller object, then fall back to the scene.
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

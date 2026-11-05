using System;
using System.Collections;
using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilliardGame.UI
{
    /// <summary>
    /// The 3D lobby showcase (docs/UI_UX.md §2): character with the equipped cue on a turntable (swipe to rotate),
    /// item preview for the inventory, and the crate that opens during a reveal (§8).
    /// </summary>
    public sealed class LobbyStage : MonoBehaviour
    {
        private const float SwipeDegreesPerPixel = 0.35f;
        private const float PreviewSpinSpeed = 18f;

        [SerializeField] private Camera lobbyCamera;
        [SerializeField] private Transform turntable;
        [SerializeField] private GameObject showcase;
        [SerializeField] private GameObject previewCue;
        [SerializeField] private GameObject previewTable;
        [SerializeField] private SkinTarget previewTarget;
        [SerializeField] private Transform crate;
        [SerializeField] private Transform crateLid;
        [SerializeField] private Light crateLight;
        [SerializeField] private Renderer crateSeam;

        private Vector3 cameraHome;
        private bool dragging;
        private bool previewing;
        private Vector2 lastPointer;
        private MaterialPropertyBlock seamBlock;

        public Camera Camera => lobbyCamera;

        private void Awake()
        {
            cameraHome = lobbyCamera.transform.localPosition;
            seamBlock = new MaterialPropertyBlock();
            ShowCharacter();
        }

        public void ShowCharacter()
        {
            previewing = false;
            showcase.SetActive(true);
            previewCue.SetActive(false);
            previewTable.SetActive(false);
            crate.gameObject.SetActive(false);
            turntable.localRotation = Quaternion.identity;
        }

        /// <summary>Inventory preview: the item alone on the turntable, slowly spinning.</summary>
        public void Preview(SkinDefinition skin)
        {
            if (skin == null) return;
            previewing = true;
            showcase.SetActive(false);
            crate.gameObject.SetActive(false);
            previewTarget.Apply(skin);
            previewCue.SetActive(skin.Type == ItemType.Cue);
            previewTable.SetActive(skin.Type == ItemType.Table);
        }

        /// <summary>Drop → glow in the top rarity colour → lid opens. <paramref name="skip"/> jumps to the end.</summary>
        public IEnumerator OpenCrate(Rarity top, Func<bool> skip)
        {
            previewing = false;
            showcase.SetActive(false);
            previewCue.SetActive(false);
            previewTable.SetActive(false);
            crate.gameObject.SetActive(true);
            crateLid.localRotation = Quaternion.identity;
            SetGlow(Color.black, 0f);

            Vector3 rest = Vector3.zero;
            yield return Animate(0.8f, skip, t =>
            {
                float fall = Mathf.Clamp01(t / 0.5f);
                float bounce = t > 0.5f ? Mathf.Sin((t - 0.5f) / 0.3f * Mathf.PI) * 0.06f * (1f - (t - 0.5f) / 0.3f) : 0f;
                crate.localPosition = rest + Vector3.up * ((1f - fall * fall) * 1.4f + Mathf.Max(0f, bounce));
            });
            crate.localPosition = rest;

            Color color = RarityStyle.Color(top);
            bool dramatic = top >= Rarity.Epic;
            yield return Animate(0.6f, skip, t =>
            {
                SetGlow(color, t);
                if (dramatic) lobbyCamera.transform.localPosition = cameraHome + UnityEngine.Random.insideUnitSphere * (0.012f * (1f - t));
            });
            lobbyCamera.transform.localPosition = cameraHome;

            yield return Animate(0.8f, skip, t => crateLid.localRotation = Quaternion.Euler(-110f * Mathf.SmoothStep(0f, 1f, t), 0f, 0f));
            crateLid.localRotation = Quaternion.Euler(-110f, 0f, 0f);
            SetGlow(color, 1f);
        }

        private static IEnumerator Animate(float seconds, Func<bool> skip, Action<float> step)
        {
            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                if (skip()) yield break;
                step(time / seconds);
                yield return null;
            }

            step(1f);
        }

        private void SetGlow(Color color, float amount)
        {
            crateLight.color = color;
            crateLight.intensity = amount * 6f;
            crateSeam.GetPropertyBlock(seamBlock);
            seamBlock.SetColor("_EmissionColor", color * (amount * 6f));
            seamBlock.SetColor("_BaseColor", Color.Lerp(Color.black, color, amount));
            crateSeam.SetPropertyBlock(seamBlock);
        }

        private void Update()
        {
            if (previewing && !dragging) turntable.Rotate(Vector3.up, PreviewSpinSpeed * Time.deltaTime, Space.World);
            Pointer pointer = Pointer.current;
            if (pointer == null || !lobbyCamera.isActiveAndEnabled) return;

            Vector2 position = pointer.position.ReadValue();
            if (pointer.press.wasPressedThisFrame)
            {
                // Only drags that start on the showcase (centre of the screen) rotate; the rails and buttons stay tappable.
                dragging = position.x > Screen.width * 0.3f && position.x < Screen.width * 0.7f && position.y > Screen.height * 0.18f && position.y < Screen.height * 0.85f;
                lastPointer = position;
            }
            else if (!pointer.press.isPressed)
            {
                dragging = false;
            }

            if (!dragging) return;
            turntable.Rotate(Vector3.up, -(position.x - lastPointer.x) * SwipeDegreesPerPixel, Space.World);
            lastPointer = position;
        }
    }
}

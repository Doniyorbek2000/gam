using System;
using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilliardGame.UI
{
    /// <summary>
    /// The player's 3D room (docs/UI_UX.md §4.1): table in the middle, cue rack, trophy shelf, three wall posters, wall
    /// and floor themes. It only draws a <see cref="RoomLayout"/> plus owned items, so a friend's room can be shown later
    /// by passing their layout instead of ours.
    /// </summary>
    public sealed class RoomStage : MonoBehaviour
    {
        private const float DragDegreesPerPixel = 0.12f;
        private const float MaxYaw = 28f;

        [SerializeField] private Camera roomCamera;
        [SerializeField] private Transform orbit;
        [SerializeField] private SkinTarget table;
        [SerializeField] private Renderer[] walls = new Renderer[0];
        [SerializeField] private Renderer floor;
        [SerializeField] private SkinTarget[] rack = new SkinTarget[0];
        [SerializeField] private GameObject[] trophies = new GameObject[0];
        [SerializeField] private Renderer[] posters = new Renderer[0];

        private MaterialPropertyBlock posterBlock;
        private float yaw;
        private bool dragging;
        private Vector2 last;

        public Camera Camera => roomCamera;
        public int TrophySlots => trophies.Length;

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
            roomCamera.enabled = visible;
            if (visible) GameSettings.ApplyCamera(roomCamera);
        }

        /// <summary>Draws <paramref name="layout"/>; cues on the rack are the owned ones, trophies the unlocked achievements.</summary>
        public void Apply(RoomLayout layout, SkinCatalog catalog, Func<string, bool> owns, int trophyCount)
        {
            table.Apply(catalog.Find(layout.tableId));
            SkinDefinition wall = catalog.Find(layout.wallId);
            if (wall != null && wall.Cloth != null)
            {
                foreach (Renderer r in walls) r.sharedMaterial = wall.Cloth;
            }

            SkinDefinition ground = catalog.Find(layout.floorId);
            if (ground != null && ground.Cloth != null) floor.sharedMaterial = ground.Cloth;

            var cues = new List<SkinDefinition>();
            foreach (SkinDefinition skin in catalog.Skins)
            {
                if (skin.Type == ItemType.Cue && owns(skin.Id)) cues.Add(skin);
            }

            for (int i = 0; i < rack.Length; i++)
            {
                bool used = i < cues.Count;
                rack[i].gameObject.SetActive(used);
                if (used) rack[i].Apply(cues[i]);
            }

            for (int i = 0; i < trophies.Length; i++) trophies[i].SetActive(i < trophyCount);

            posterBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < posters.Length; i++)
            {
                SkinDefinition sticker = layout.posters != null && i < layout.posters.Count ? catalog.Find(layout.posters[i]) : null;
                bool show = sticker != null && sticker.Icon != null;
                posters[i].gameObject.SetActive(show);
                if (!show) continue;
                posters[i].GetPropertyBlock(posterBlock);
                posterBlock.SetTexture("_BaseMap", sticker.Icon.texture);
                posters[i].SetPropertyBlock(posterBlock);
            }
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !roomCamera.enabled) return;
            Vector2 position = pointer.position.ReadValue();
            if (pointer.press.wasPressedThisFrame)
            {
                // The middle band rotates the view; header and the edit panel at the bottom stay tappable.
                dragging = position.y > Screen.height * 0.35f && position.y < Screen.height * 0.85f;
                last = position;
            }
            else if (!pointer.press.isPressed)
            {
                dragging = false;
            }

            if (!dragging) return;
            yaw = Mathf.Clamp(yaw + (position.x - last.x) * DragDegreesPerPixel, -MaxYaw, MaxYaw);
            orbit.localRotation = Quaternion.Euler(0f, yaw, 0f);
            last = position;
        }
    }
}

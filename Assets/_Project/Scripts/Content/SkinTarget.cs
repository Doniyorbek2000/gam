using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>
    /// Where skins show up: a cue holder and/or table renderers. Used by the match table and the lobby stage.
    /// A table skin with a prefab (imported model) replaces the built-in table under <see cref="tableHolder"/>;
    /// a material-only skin re-shows the built-in table and recolours its cloth and rails.
    /// </summary>
    public sealed class SkinTarget : MonoBehaviour
    {
        [SerializeField] private Transform cueHolder;
        [SerializeField] private Renderer[] clothRenderers = new Renderer[0];
        [SerializeField] private Renderer[] railRenderers = new Renderer[0];
        [SerializeField] private Transform tableHolder;
        [SerializeField] private GameObject[] builtInTable = new GameObject[0];
        [SerializeField] private Renderer cueSticker;
        [SerializeField] private Renderer[] tableStickers = new Renderer[0];

        private SkinDefinition currentCue;
        private SkinDefinition currentTable;

        public Transform CueHolder => cueHolder;

        public void Apply(SkinDefinition skin)
        {
            if (skin == null) return;
            if (skin.Type == ItemType.Cue) ApplyCue(skin);
            else if (skin.Type == ItemType.Table) ApplyTable(skin);
        }

        /// <summary>Sticker decals on the cue butt and the table rails; null hides a slot. Purely visual.</summary>
        public void ApplyStickers(SkinDefinition onCue, SkinDefinition onTable)
        {
            SetDecal(cueSticker, onCue);
            foreach (Renderer decal in tableStickers) SetDecal(decal, onTable);
        }

        private static MaterialPropertyBlock decalBlock;

        private static void SetDecal(Renderer decal, SkinDefinition sticker)
        {
            if (decal == null) return;
            bool show = sticker != null && sticker.Icon != null;
            decal.gameObject.SetActive(show);
            if (!show) return;
            decalBlock ??= new MaterialPropertyBlock();
            decal.GetPropertyBlock(decalBlock);
            decalBlock.SetTexture("_BaseMap", sticker.Icon.texture);
            decal.SetPropertyBlock(decalBlock);
        }

        private void ApplyCue(SkinDefinition skin)
        {
            if (cueHolder == null || skin.Prefab == null || skin == currentCue) return;
            Clear(cueHolder);
            Instantiate(skin.Prefab, cueHolder, false);
            currentCue = skin;
        }

        private void ApplyTable(SkinDefinition skin)
        {
            if (skin == currentTable) return;
            currentTable = skin;
            bool model = skin.Prefab != null && tableHolder != null;
            if (tableHolder != null) Clear(tableHolder);
            foreach (GameObject part in builtInTable)
            {
                if (part != null) part.SetActive(!model);
            }

            if (model)
            {
                Instantiate(skin.Prefab, tableHolder, false);
                return;
            }

            if (skin.Cloth != null)
            {
                foreach (Renderer r in clothRenderers) r.sharedMaterial = skin.Cloth;
            }

            if (skin.Rail != null)
            {
                foreach (Renderer r in railRenderers) r.sharedMaterial = skin.Rail;
            }
        }

        // Keeps the sticker decal, which lives under the cue holder so it follows the cue.
        private void Clear(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                Transform child = holder.GetChild(i);
                if (cueSticker == null || child != cueSticker.transform) Destroy(child.gameObject);
            }
        }
    }
}

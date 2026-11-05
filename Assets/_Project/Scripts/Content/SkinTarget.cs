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

        private SkinDefinition currentCue;
        private SkinDefinition currentTable;

        public Transform CueHolder => cueHolder;

        public void Apply(SkinDefinition skin)
        {
            if (skin == null) return;
            if (skin.Type == ItemType.Cue) ApplyCue(skin);
            else ApplyTable(skin);
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

        private static void Clear(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) Destroy(holder.GetChild(i).gameObject);
        }
    }
}

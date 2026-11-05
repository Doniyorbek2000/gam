using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>Where skins show up: a cue holder and/or table renderers. Used by the match table and the lobby stage.</summary>
    public sealed class SkinTarget : MonoBehaviour
    {
        [SerializeField] private Transform cueHolder;
        [SerializeField] private Renderer[] clothRenderers = new Renderer[0];
        [SerializeField] private Renderer[] railRenderers = new Renderer[0];

        private SkinDefinition currentCue;

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
            for (int i = cueHolder.childCount - 1; i >= 0; i--) Destroy(cueHolder.GetChild(i).gameObject);
            Instantiate(skin.Prefab, cueHolder, false);
            currentCue = skin;
        }

        private void ApplyTable(SkinDefinition skin)
        {
            if (skin.Cloth != null)
            {
                foreach (Renderer r in clothRenderers) r.sharedMaterial = skin.Cloth;
            }

            if (skin.Rail != null)
            {
                foreach (Renderer r in railRenderers) r.sharedMaterial = skin.Rail;
            }
        }
    }
}

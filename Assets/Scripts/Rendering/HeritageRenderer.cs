using System.Collections.Generic;
using HexCiv.Core;
using UnityEngine;

namespace HexCiv.Render
{
    /// <summary>
    /// 探索済みタイルにある遺産を金色の菱形で示す独立描画。未発見は「史跡」、
    /// 自文明が発見すると実名を表示する。GameBootstrapへ依存せず状態の差し替えにも追従する。
    /// </summary>
    public sealed class HeritageRenderer : MonoBehaviour
    {
        sealed class SiteView
        {
            public GameObject Root;
            public TextMesh Label;
            public MeshRenderer Diamond;
        }

        readonly Dictionary<string, SiteView> views = new Dictionary<string, SiteView>();
        GameState shownState;
        int shownVersion = -1;
        Mesh diamondMesh;
        Material undiscoveredMaterial;
        Material discoveredMaterial;
        Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<HeritageRenderer>() != null) return;
            new GameObject("HeritageSiteRenderer").AddComponent<HeritageRenderer>();
        }

        void Start()
        {
            var mb = new MeshBuilder();
            mb.AddDiamond(Vector3.zero, 0.46f, Color.white);
            diamondMesh = mb.Build(null);
            undiscoveredMaterial = RenderUtil.NewSpriteMaterial();
            undiscoveredMaterial.color = new Color(0.95f, 0.70f, 0.16f, 0.96f);
            discoveredMaterial = RenderUtil.NewSpriteMaterial();
            discoveredMaterial.color = new Color(0.35f, 0.92f, 0.82f, 0.98f);
            font = RenderUtil.JapaneseFont();
        }

        void Update()
        {
            var state = WorldLegacySystem.CurrentState;
            if (state == null) return;
            if (state != shownState || views.Count != state.HeritageSites.Count) Rebuild(state);
            if (shownVersion != state.Version) Refresh(state);
        }

        void Rebuild(GameState state)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            views.Clear();
            shownState = state;
            shownVersion = -1;
            if (state == null) return;

            for (int i = 0; i < state.HeritageSites.Count; i++)
            {
                var placed = state.HeritageSites[i];
                var root = new GameObject("Heritage_" + placed.SiteId);
                root.transform.SetParent(transform, false);
                Vector3 position = placed.Coord.ToWorld();
                position.y = 0.13f;
                root.transform.localPosition = position;

                var diamond = RenderUtil.NewMeshChild(root.transform, "Marker", diamondMesh,
                    undiscoveredMaterial, Vector3.zero, 14);
                var labelObject = new GameObject("Label");
                labelObject.transform.SetParent(root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.02f, 0.58f);
                labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var text = labelObject.AddComponent<TextMesh>();
                text.font = font;
                text.fontSize = 40;
                text.characterSize = 0.035f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = new Color(1f, 0.94f, 0.68f, 1f);
                text.text = "史跡";
                var mr = labelObject.GetComponent<MeshRenderer>();
                mr.sharedMaterial = font.material;
                mr.sortingOrder = 15;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                views[placed.SiteId] = new SiteView
                {
                    Root = root,
                    Label = text,
                    Diamond = diamond,
                };
            }
            Refresh(state);
        }

        void Refresh(GameState state)
        {
            if (state == null) return;
            shownVersion = state.Version;
            var human = state.HumanPlayer;
            for (int i = 0; i < state.HeritageSites.Count; i++)
            {
                var placed = state.HeritageSites[i];
                SiteView view;
                if (!views.TryGetValue(placed.SiteId, out view) || view.Root == null) continue;
                bool known = human == null || human.Explored.Contains(placed.Coord);
                if (view.Root.activeSelf != known) view.Root.SetActive(known);
                if (!known) continue;

                bool ownDiscovery = human == null
                    ? placed.IsDiscovered
                    : placed.DiscoveredByPlayerId == human.Id;
                view.Diamond.sharedMaterial = ownDiscovery ? discoveredMaterial : undiscoveredMaterial;
                var def = placed.Def;
                view.Label.text = ownDiscovery && def != null ? def.NameJa : "史跡";
                view.Label.color = ownDiscovery
                    ? new Color(0.55f, 1f, 0.92f, 1f)
                    : new Color(1f, 0.94f, 0.68f, 1f);
            }
        }

        void OnDestroy()
        {
            if (diamondMesh != null) Destroy(diamondMesh);
            if (undiscoveredMaterial != null) Destroy(undiscoveredMaterial);
            if (discoveredMaterial != null) Destroy(discoveredMaterial);
        }
    }
}

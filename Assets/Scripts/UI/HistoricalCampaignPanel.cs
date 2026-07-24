using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;
using HexCiv.Render;

namespace HexCiv.UI
{
    /// <summary>
    /// ウルク史実キャンペーン専用HUD。通常ゲームのUIManagerへ史実固有の人口縮尺や
    /// 物資欄を混ぜず、独立Canvasとして重ねる。
    /// </summary>
    public sealed class HistoricalCampaignPanel : MonoBehaviour
    {
        HistoricalCampaignSession session;
        Action<string> onAction;
        Action onQuickSave;
        Action onQuickLoad;
        Canvas canvas;
        GameObject detailPanel;
        Text heading;
        Text summary;
        Text advisorTitle;
        Text advisorBody;
        Text detailText;
        Text reportText;
        Button canalButton;
        Button foodButton;
        Button templeButton;
        Button administrationButton;
        bool detailed;
        HistoricalCampaignWorldVisuals worldVisuals;

        public void Init(HistoricalCampaignSession campaignSession,
            Action<string> campaignAction, Action quickSave, Action quickLoad)
        {
            session = campaignSession ?? throw new ArgumentNullException(nameof(campaignSession));
            onAction = campaignAction;
            onQuickSave = quickSave;
            onQuickLoad = quickLoad;
            if (canvas == null) BuildUi();
            if (worldVisuals == null)
            {
                var visuals = new GameObject("HistoricalCampaignWorldVisuals");
                visuals.transform.SetParent(transform, false);
                worldVisuals = visuals.AddComponent<HistoricalCampaignWorldVisuals>();
            }
            worldVisuals.Init(session);
            Refresh();
        }

        public void Refresh()
        {
            if (session == null || heading == null) return;
            var progress = session.Progress;
            heading.text = $"史実キャンペーン　{session.Definition.title.ja}　" +
                $"{session.CurrentIntervalJa}　ターン{session.State.TurnNumber}/{session.Definition.maxTurns}";
            int food = UrukCampaignSystem.TotalFood(progress);
            int irrigated = UrukCampaignSystem.IrrigatedFarmCount(progress);
            summary.text =
                $"人口 {progress.actualPopulation:N0}人（{Signed(progress.lastPopulationChange)}）　" +
                $"食料 {food}　農地 {irrigated}/{UrukCampaignSystem.FarmCount(progress)} 灌漑　" +
                $"運河 {UrukCampaignSystem.CanalCondition(progress)}%　" +
                $"神殿 {progress.templeProgress}%　安定 {progress.stability}";
            advisorTitle.text = "顧問　" + UrukCampaignSystem.TutorialTitleJa(session);
            advisorBody.text = UrukCampaignSystem.TutorialBodyJa(session);
            reportText.text = string.IsNullOrWhiteSpace(progress.lastReportJa)
                ? "" : "前期間: " + progress.lastReportJa;
            detailText.text = BuildDetailText(progress);

            int turn = session.State.TurnNumber;
            canalButton.interactable = !session.State.IsGameOver &&
                progress.lastCanalActionTurn != turn &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") >= 1;
            foodButton.interactable = !session.State.IsGameOver &&
                progress.lastFoodPriorityTurn != turn;
            templeButton.interactable = !session.State.IsGameOver &&
                !progress.templePlanned &&
                UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") >= 4 &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") >= 2;
            administrationButton.interactable = !session.State.IsGameOver &&
                !progress.administrationAdopted &&
                progress.templeProgress >= UrukCampaignSystem.AdministrationUnlockProgress;
            detailPanel.SetActive(detailed);
            worldVisuals?.Refresh();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("HistoricalCampaignCanvas", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 122;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("HistoricalCampaignEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule)).transform.SetParent(transform, false);

            var top = UIStyle.CreatePanel(canvasGo.transform, "CampaignTop",
                new Color(0.035f, 0.055f, 0.09f, 0.94f));
            UIStyle.SetRect(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(1180f, 82f));
            heading = UIStyle.CreateText(top.transform, "Heading", "", 21,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            heading.fontStyle = FontStyle.Bold;
            UIStyle.SetRect(heading.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(-20f, 34f));
            summary = UIStyle.CreateText(top.transform, "Summary", "", 16,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(summary.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-20f, 35f));

            var advisor = UIStyle.CreatePanel(canvasGo.transform, "CampaignAdvisor",
                new Color(0.035f, 0.055f, 0.09f, 0.95f));
            UIStyle.SetRect(advisor, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-18f, 20f), new Vector2(390f, 500f));
            advisorTitle = UIStyle.CreateText(advisor.transform, "AdvisorTitle", "", 20,
                TextAnchor.UpperLeft, UIStyle.Accent);
            advisorTitle.fontStyle = FontStyle.Bold;
            advisorTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(advisorTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-28f, 48f));
            advisorBody = UIStyle.CreateText(advisor.transform, "AdvisorBody", "", 15,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            advisorBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(advisorBody.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-28f, 58f));

            canalButton = CreateActionButton(advisor.transform, "CanalButton",
                "運河を整備（葦1）", 320f,
                () => ApplyAction(UrukCampaignSystem.MaintainCanalAction));
            foodButton = CreateActionButton(advisor.transform, "FoodButton",
                "食料を優先", 268f,
                () => ApplyAction(UrukCampaignSystem.PrioritizeFoodAction));
            templeButton = CreateActionButton(advisor.transform, "TempleButton",
                "神殿区画を着工（粘土4・葦2）", 216f,
                () => ApplyAction(UrukCampaignSystem.PlanTempleAction));
            administrationButton = CreateActionButton(advisor.transform, "AdministrationButton",
                "配給・記録制度を採用（粘土2）", 164f,
                () => ApplyAction(UrukCampaignSystem.AdoptAdministrationAction));

            reportText = UIStyle.CreateText(advisor.transform, "Report", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            reportText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(reportText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 106f), new Vector2(-28f, 48f));

            var detailButton = UIStyle.CreateButton(advisor.transform, "DetailButton",
                "簡潔／詳細", 13, () =>
                {
                    detailed = !detailed;
                    Refresh();
                });
            UIStyle.SetRect(detailButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(112f, 38f));
            var saveButton = UIStyle.CreateButton(advisor.transform, "QuickSaveButton",
                "史実セーブ", 13, () => onQuickSave?.Invoke());
            UIStyle.SetRect(saveButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(136f, 14f), new Vector2(112f, 38f));
            var loadButton = UIStyle.CreateButton(advisor.transform, "QuickLoadButton",
                "史実ロード", 13, () => onQuickLoad?.Invoke());
            UIStyle.SetRect(loadButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(258f, 14f), new Vector2(112f, 38f));

            detailPanel = UIStyle.CreatePanel(canvasGo.transform, "CampaignDetail",
                new Color(0.025f, 0.04f, 0.07f, 0.97f));
            UIStyle.SetRect(detailPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-418f, 20f), new Vector2(470f, 500f));
            detailText = UIStyle.CreateText(detailPanel.transform, "DetailText", "", 14,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.StretchFull(detailText.gameObject, 18f);
            detailPanel.SetActive(false);
        }

        Button CreateActionButton(Transform parent, string name, string label, float y,
            UnityEngine.Events.UnityAction onClick)
        {
            var button = UIStyle.CreateButton(parent, name, label, 14, onClick);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(-28f, 42f));
            return button;
        }

        void ApplyAction(string actionId)
        {
            onAction?.Invoke(actionId);
            Refresh();
        }

        string BuildDetailText(UrukCampaignProgress progress)
        {
            return
                "人口（実人数・推定）\n" +
                $"農耕民 {progress.roles.farmers:N0}　牧畜民 {progress.roles.pastoralists:N0}\n" +
                $"漁民 {progress.roles.fishers:N0}　職人 {progress.roles.artisans:N0}\n" +
                $"神官 {progress.roles.priests:N0}　戦士 {progress.roles.warriors:N0}\n" +
                $"労働者 {progress.roles.laborers:N0}\n\n" +
                "社会的地位\n" +
                $"自由民 {progress.statuses.free:N0}　従属民 {progress.statuses.dependent:N0}\n" +
                $"奴隷 {progress.statuses.enslaved:N0}（開始時0・制度成立後のみ）\n\n" +
                "物資（1=推定物流単位）\n" +
                $"大麦 {Good(progress, "barley")}　エンマー小麦 {Good(progress, "emmer_wheat")}\n" +
                $"魚 {Good(progress, "fish")}　羊・羊毛 {Good(progress, "sheep_wool")}\n" +
                $"葦 {Good(progress, "reeds")}　沖積粘土 {Good(progress, "alluvial_clay")}\n" +
                $"木材 {Good(progress, "timber")}　石材 {Good(progress, "building_stone")}　" +
                $"銅 {Good(progress, "copper")}\n\n" +
                "自動管理\n" +
                $"人口配置 {(progress.populationAutomation ? "ON" : "OFF")}／" +
                $"生産 {(progress.productionAutomation ? "ON" : "OFF")}／" +
                $"交易 {(progress.tradeAutomation ? "ON" : "OFF")}\n\n" +
                "史料表示\n地図・施設配置: 復元推定\n" +
                "人口量: 学術的推定をゲーム用に丸めた値\n" +
                "音響・3Dモデル: 現代制作の暫定復元";
        }

        static int Good(UrukCampaignProgress progress, string id)
        {
            return UrukCampaignSystem.GoodAmount(progress, id);
        }

        static string Signed(int value)
        {
            if (value == 0) return "±0";
            return value > 0 ? "+" + value : value.ToString();
        }
    }

    /// <summary>農地・運河・神殿基壇の軽量な暫定復元ローポリ表示。</summary>
    public sealed class HistoricalCampaignWorldVisuals : MonoBehaviour
    {
        HistoricalCampaignSession session;
        readonly List<GameObject> generated = new List<GameObject>();
        readonly List<Material> materials = new List<Material>();
        int lastCanalCondition = -1;
        int lastTempleProgress = -1;

        public void Init(HistoricalCampaignSession campaignSession)
        {
            session = campaignSession;
            Rebuild();
        }

        public void Refresh()
        {
            if (session == null) return;
            int canal = UrukCampaignSystem.CanalCondition(session.Progress);
            int temple = session.Progress.templeProgress;
            if (canal == lastCanalCondition && temple == lastTempleProgress) return;
            Rebuild();
        }

        void Rebuild()
        {
            ClearGenerated();
            if (session == null) return;
            var progress = session.Progress;
            foreach (var improvement in progress.improvements)
            {
                var coord = HexCoord.FromOffset(improvement.col, improvement.row);
                var tile = session.State.Map.Get(coord);
                if (tile == null) continue;
                float y = RenderUtil.TileVisualHeight(tile) + 0.09f;
                if (improvement.kind == "farm")
                    BuildFarm(coord.ToWorld(), y);
                else if (improvement.kind == "canal")
                    BuildCanal(coord.ToWorld(), y, improvement.condition);
            }
            if (progress.templePlanned)
            {
                var uruk = session.State.HumanPlayer?.Cities.Count > 0
                    ? session.State.HumanPlayer.Cities[0].Coord
                    : HexCoord.FromOffset(16, 10);
                var tile = session.State.Map.Get(uruk);
                BuildTemple(uruk.ToWorld(),
                    (tile != null ? RenderUtil.TileVisualHeight(tile) : 0f) + 0.11f,
                    progress.templeProgress);
            }
            lastCanalCondition = UrukCampaignSystem.CanalCondition(progress);
            lastTempleProgress = progress.templeProgress;
        }

        void BuildFarm(Vector3 center, float y)
        {
            CreateCube("復元推定_農地", new Vector3(center.x, y, center.z),
                new Vector3(0.78f, 0.035f, 0.62f), new Color(0.54f, 0.46f, 0.18f));
            for (int i = -2; i <= 2; i++)
                CreateCube("畝", new Vector3(center.x + i * 0.13f, y + 0.03f, center.z),
                    new Vector3(0.035f, 0.025f, 0.56f), new Color(0.72f, 0.66f, 0.28f));
        }

        void BuildCanal(Vector3 center, float y, int condition)
        {
            float normalized = Mathf.Clamp01(condition / 100f);
            Color water = Color.Lerp(new Color(0.18f, 0.20f, 0.18f),
                new Color(0.15f, 0.52f, 0.68f), normalized);
            CreateCube("復元推定_運河", new Vector3(center.x, y, center.z),
                new Vector3(0.16f, 0.025f, 0.92f), water);
            CreateCube("運河堤防_左", new Vector3(center.x - 0.12f, y + 0.025f, center.z),
                new Vector3(0.07f, 0.06f, 0.92f), new Color(0.52f, 0.38f, 0.18f));
            CreateCube("運河堤防_右", new Vector3(center.x + 0.12f, y + 0.025f, center.z),
                new Vector3(0.07f, 0.06f, 0.92f), new Color(0.52f, 0.38f, 0.18f));
        }

        void BuildTemple(Vector3 center, float y, int progress)
        {
            float height = 0.08f + 0.28f * Mathf.Clamp01(progress / 100f);
            CreateCube("暫定復元_神殿基壇", new Vector3(center.x, y + height * 0.5f, center.z),
                new Vector3(0.56f, height, 0.48f), new Color(0.70f, 0.54f, 0.27f));
            if (progress >= 60)
                CreateCube("暫定復元_神殿上層",
                    new Vector3(center.x, y + height + 0.09f, center.z),
                    new Vector3(0.34f, 0.16f, 0.29f), new Color(0.82f, 0.67f, 0.36f));
        }

        void CreateCube(string objectName, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            materials.Add(material);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            generated.Add(go);
        }

        void ClearGenerated()
        {
            foreach (var go in generated)
                if (go != null) Destroy(go);
            generated.Clear();
            foreach (var material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }

        void OnDestroy()
        {
            ClearGenerated();
        }
    }
}

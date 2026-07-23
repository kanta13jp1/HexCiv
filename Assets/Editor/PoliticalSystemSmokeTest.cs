using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>政治制度の法律効果、支持変化、AI判断、セーブv13互換を検証する。</summary>
public static class PoliticalSystemSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateLawEffects();
            ValidateSupportAndAI();
            ValidateSaveVersion13AndMigration();
            Debug.Log("POLITICAL SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("POLITICAL SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateLawEffects()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        int baseRevenue = AdministrationSystem.Revenue(player);
        int baseExpense = AdministrationSystem.Expenses(player);
        int baseRange = LogisticsSystem.SupplyRange(player);

        player.PoliticalCapital = 100;
        if (!PoliticalSystem.SetLaw(state, player, CivicLaw.LocalAssemblies, true, false) ||
            player.PoliticalCapital != 70 || PoliticalSystem.CultureBonus(player) != 1 ||
            PoliticalSystem.SatisfactionBonus(player) != 4 ||
            AdministrationSystem.Revenue(player) >= baseRevenue)
            throw new Exception("地域民会の費用・文化・満足・税収効果が不正");

        PoliticalSystem.SetLaw(state, player, CivicLaw.MerchantCharters, false, false);
        if (AdministrationSystem.Revenue(player) <= baseRevenue ||
            PoliticalSystem.SatisfactionBonus(player) != -2)
            throw new Exception("商業特許状の収入・満足度効果が不正");

        PoliticalSystem.SetLaw(state, player, CivicLaw.CitizenMilitia, false, false);
        if (LogisticsSystem.SupplyRange(player) != baseRange + 1 ||
            AdministrationSystem.Expenses(player) != baseExpense + player.Cities.Count)
            throw new Exception("市民兵制の補給・維持費効果が不正");

        PoliticalSystem.SetLaw(state, player, CivicLaw.CouncilOfElders, false, false);
        if (PoliticalSystem.SupportForLaw(player, player.ActiveLaw) != player.TraditionalSupport)
            throw new Exception("長老評議会と伝統層の対応が不正");
        Debug.Log("[Politics] 4法律の費用・文化・満足・税収・補給効果 OK");
    }

    static void ValidateSupportAndAI()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        int beforeCapital = player.PoliticalCapital;
        int beforeScholar = player.ScholarSupport;
        PoliticalSystem.AdvancePlayer(state, player);
        if (player.PoliticalCapital <= beforeCapital ||
            Math.Abs(player.ScholarSupport - beforeScholar) > 2 ||
            player.Legitimacy < 0 || player.Legitimacy > 100)
            throw new Exception("政治力・支持・正統性の毎ターン更新が不正");

        Player ai = state.Players[1];
        ai.PoliticalCapital = PoliticalSystem.ChangeLawCost;
        ai.AtWarWith.Add(player.Id);
        PoliticalSystem.AdvancePlayer(state, ai);
        if (ai.ActiveLaw != CivicLaw.CitizenMilitia || ai.PoliticalCapital >= PoliticalSystem.ChangeLawCost)
            throw new Exception("戦時AIが市民兵制を選ばない");
        Debug.Log("[Politics] 支持変化上限・正統性・政治力・AI法律判断 OK");
    }

    static void ValidateSaveVersion13AndMigration()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        player.PoliticalCapital = 87;
        player.Legitimacy = 71;
        player.ActiveLaw = CivicLaw.MerchantCharters;
        player.ScholarSupport = 63;
        player.MerchantSupport = 74;
        player.TraditionalSupport = 41;
        player.MilitarySupport = 58;

        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":17")) throw new Exception("セーブversion 17ではない");
        GameState restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        if (json2 != json3) throw new Exception("政治制度を含むセーブ往復が非決定的");
        Player rp = restored.GetPlayer(player.Id);
        if (rp.PoliticalCapital != 87 || rp.Legitimacy != 71 ||
            rp.ActiveLaw != CivicLaw.MerchantCharters || rp.ScholarSupport != 63 ||
            rp.MerchantSupport != 74 || rp.TraditionalSupport != 41 || rp.MilitarySupport != 58)
            throw new Exception("政治制度値の復元に失敗");

        string old = json1.Replace("\"version\":17", "\"version\":12");
        string[] fields = { "politicalCapital", "legitimacy", "activeLaw", "scholarSupport",
            "merchantSupport", "traditionalSupport", "militarySupport" };
        for (int i = 0; i < fields.Length; i++)
            old = Regex.Replace(old, ",\"" + fields[i] + "\":-?[0-9]+", "");
        Player migrated = SaveLoad.Deserialize(old).GetPlayer(player.Id);
        if (migrated.PoliticalCapital != PoliticalSystem.StartingPoliticalCapital ||
            migrated.Legitimacy != PoliticalSystem.StartingLegitimacy ||
            migrated.ActiveLaw != CivicLaw.CouncilOfElders ||
            migrated.ScholarSupport != PoliticalSystem.StartingSupport ||
            migrated.MerchantSupport != PoliticalSystem.StartingSupport ||
            migrated.TraditionalSupport != PoliticalSystem.StartingSupport ||
            migrated.MilitarySupport != PoliticalSystem.StartingSupport)
            throw new Exception("version 12セーブの政治制度既定値移行に失敗");
        Debug.Log("[Politics] セーブv13決定往復・v12既定値移行 OK");
    }

    static GameState BuildState()
    {
        var state = new GameState
        {
            Config = new GameConfig
            {
                MapWidth = 6, MapHeight = 4, NumPlayers = 2, Seed = 7213,
                MaxTurns = 250, HumanPlayerIndex = 0,
            },
            Map = new HexMap(6, 4),
            Rng = new System.Random(7213),
            TurnNumber = 30,
        };
        var first = new Player
        {
            Id = 0, CivilizationId = "egypt", NameJa = "エジプト", IsHuman = true,
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.yellow, CapitalCityId = 1, Treasury = 120,
        };
        var second = new Player
        {
            Id = 1, CivilizationId = "sumer", NameJa = "シュメール",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red, CapitalCityId = 2,
        };
        var firstCity = new City
        {
            Id = 1, PlayerId = 0, NameJa = "テーベ", Coord = HexCoord.FromOffset(1, 1),
            Population = 4, Farmers = 2, Artisans = 1, Scholars = 1,
            Education = 50, Satisfaction = 65, FoodNeedFulfillment = 100,
            HousingNeedFulfillment = 100, ServiceNeedFulfillment = 70,
            Buildings = new List<string> { "monument", "library" },
        };
        var secondCity = new City
        {
            Id = 2, PlayerId = 1, NameJa = "ウル", Coord = HexCoord.FromOffset(4, 2),
            Population = 3, Farmers = 2, Artisans = 1, Scholars = 0,
            Education = 30, Satisfaction = 60, FoodNeedFulfillment = 100,
            HousingNeedFulfillment = 100, ServiceNeedFulfillment = 60,
            Buildings = new List<string>(),
        };
        first.Cities.Add(firstCity);
        second.Cities.Add(secondCity);
        first.Units.Add(new Unit { Id = 1, PlayerId = 0, DefId = "warrior", Coord = firstCity.Coord });
        second.Units.Add(new Unit { Id = 2, PlayerId = 1, DefId = "warrior", Coord = secondCity.Coord });
        state.Players.Add(first);
        state.Players.Add(second);
        state.Map.Get(firstCity.Coord).City = firstCity;
        state.Map.Get(secondCity.Coord).City = secondCity;
        return state;
    }
}

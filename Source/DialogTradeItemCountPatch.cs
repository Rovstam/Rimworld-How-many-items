using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HowManyItems;

[HarmonyPatch(typeof(TradeUI), nameof(TradeUI.DrawTradeableRow))]
internal static class VanillaTradeRowPatch
{
    private static void Prefix(Tradeable trad)
    {
        ColonyCountTooltipContext.Begin(trad, isColonyRow: true);
    }

    private static void Postfix()
    {
        ColonyCountTooltipContext.End();
    }
}

[HarmonyPatch]
internal static class TradeUIRevisedTradeRowPatch
{
    private const string TradeUIRevisedRowType =
        "TradeUI.TradeUIRework+Harmony_DialogTrade_FillMainRect";

    private static bool Prepare()
    {
        var rowType = AccessTools.TypeByName(TradeUIRevisedRowType);
        return rowType != null && AccessTools.Method(
            rowType,
            "MyDrawTradableRow",
            new[] { typeof(UnityEngine.Rect), typeof(Tradeable), typeof(int), typeof(bool) }) != null;
    }

    private static MethodBase TargetMethod()
    {
        var rowType = AccessTools.TypeByName(TradeUIRevisedRowType);
        return AccessTools.Method(
            rowType,
            "MyDrawTradableRow",
            new[] { typeof(UnityEngine.Rect), typeof(Tradeable), typeof(int), typeof(bool) });
    }

    private static void Prefix(Tradeable trad, bool isOurs)
    {
        ColonyCountTooltipContext.Begin(trad, isOurs);
    }

    private static void Postfix()
    {
        ColonyCountTooltipContext.End();
    }
}

[HarmonyPatch(typeof(TooltipHandler), nameof(TooltipHandler.TipRegionByKey), new[] { typeof(UnityEngine.Rect), typeof(string) })]
internal static class ColonyCountTooltipPatch
{
    private static bool Prefix(UnityEngine.Rect rect, string key)
    {
        if (key != "ColonyCount" || !ColonyCountTooltipContext.Active)
        {
            return true;
        }

        TooltipHandler.TipRegion(rect, ColonyCountTooltipContext.BuildText());
        return false;
    }
}

internal static class ColonyCountTooltipContext
{
    [System.ThreadStatic]
    private static bool active;

    [System.ThreadStatic]
    private static int caravanCount;

    [System.ThreadStatic]
    private static int tileCount;

    [System.ThreadStatic]
    private static bool tileCountTracked;

    [System.ThreadStatic]
    private static bool tileCountAvailable;

    public static bool Active => active;

    public static void Begin(Tradeable trad, bool isColonyRow)
    {
        active = false;
        if (!isColonyRow || !TradeSession.Active || trad?.ThingDef == null)
        {
            return;
        }

        var homeMap = Find.AnyPlayerHomeMap;
        var thingDef = trad.ThingDef;
        caravanCount = trad.CountHeldBy(Transactor.Colony);
        tileCountAvailable = homeMap?.resourceCounter != null;
        tileCountTracked = tileCountAvailable && thingDef.CountAsResource;
        tileCount = tileCountTracked ? homeMap.resourceCounter.GetCount(thingDef) : 0;
        active = true;
    }

    public static string BuildText()
    {
        var text = "ColonyCount".Translate().ToString();
        text += "\n\n" + "HowManyItems_InCaravan".Translate(caravanCount).ToString();
        text += "\n" + (!tileCountAvailable
            ? "HowManyItems_TileCountUnavailable".Translate().ToString()
            : tileCountTracked
                ? "HowManyItems_InTile".Translate(tileCount).ToString()
                : "HowManyItems_TileCountNotTracked".Translate().ToString());
        return text;
    }

    public static void End()
    {
        active = false;
    }
}

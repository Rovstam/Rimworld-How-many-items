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
        ColonyCountTooltipContext.Begin(trad, isColonyRow: null);
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
internal static class TradeCountTooltipPatch
{
    private static bool Prefix(UnityEngine.Rect rect, string key)
    {
        if (!ColonyCountTooltipContext.ShouldAugment(key) ||
            (!Mouse.IsOver(rect) && !DebugViewSettings.drawTooltipEdges))
        {
            return true;
        }

        TooltipHandler.TipRegion(rect, ColonyCountTooltipContext.BuildText(key));
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
    private static bool tileCountAvailable;

    [System.ThreadStatic]
    private static Thing rowThing;

    [System.ThreadStatic]
    private static TransferAsOneMode matchingMode;

    [System.ThreadStatic]
    private static string rowTooltipKey;

    private static TradeDeal cachedDeal;
    private static Map cachedMap;
    private static Thing cachedThing;
    private static int cachedTick = -1;
    private static int cachedTileCount;

    public static bool Active => active;

    public static void Begin(Tradeable trad, bool? isColonyRow)
    {
        active = false;
        if (!TradeSession.Active || trad?.AnyThing == null || trad.AnyThing is Pawn)
        {
            return;
        }

        rowTooltipKey = isColonyRow == true
            ? "ColonyCount"
            : isColonyRow == false
                ? "TraderCount"
                : null;
        rowThing = trad.AnyThing;
        matchingMode = trad.TraderWillTrade
            ? TransferAsOneMode.Normal
            : TransferAsOneMode.InactiveTradeable;
        caravanCount = trad.CountHeldBy(Transactor.Colony);
        tileCountAvailable = Find.AnyPlayerHomeMap != null;
        active = true;
    }

    public static bool ShouldAugment(string key)
    {
        return active &&
            (rowTooltipKey == null || key == rowTooltipKey) &&
            (key == "ColonyCount" || key == "TraderCount");
    }

    public static string BuildText(string tooltipKey)
    {
        var text = tooltipKey.Translate().ToString();
        text += "\n\n" + "HowManyItems_InCaravan".Translate(caravanCount).ToString();
        text += "\n" + (!tileCountAvailable
            ? "HowManyItems_TileCountUnavailable".Translate().ToString()
            : "HowManyItems_InTile".Translate(GetStoredCount()).ToString());
        return text;
    }

    private static int GetStoredCount()
    {
        var map = Find.AnyPlayerHomeMap;
        if (map == null || rowThing == null)
        {
            return 0;
        }

        var deal = TradeSession.deal;
        var tick = Find.TickManager.TicksGame;
        if (object.ReferenceEquals(cachedDeal, deal) &&
            object.ReferenceEquals(cachedMap, map) &&
            object.ReferenceEquals(cachedThing, rowThing) &&
            cachedTick == tick)
        {
            return cachedTileCount;
        }

        tileCount = 0;
        foreach (var group in map.haulDestinationManager.AllGroupsListForReading)
        {
            foreach (var storedThing in group.HeldThings)
            {
                if (storedThing.IsNotFresh() ||
                    (storedThing.SpawnedOrAnyParentSpawned && storedThing.PositionHeld.Fogged(storedThing.MapHeld)))
                {
                    continue;
                }

                if (TransferableUtility.TransferAsOne(rowThing, storedThing, matchingMode))
                {
                    tileCount += storedThing.stackCount;
                }
            }
        }

        cachedDeal = deal;
        cachedMap = map;
        cachedThing = rowThing;
        cachedTick = tick;
        cachedTileCount = tileCount;
        return tileCount;
    }

    public static void End()
    {
        active = false;
    }
}

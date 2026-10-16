using System.Reflection;
using HarmonyLib;
using Verse;

namespace HowManyItems;

public sealed class HowManyItemsMod : Mod
{
    public HowManyItemsMod(ModContentPack content)
        : base(content)
    {
        var harmony = new Harmony("Rovstam.HowManyItems");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

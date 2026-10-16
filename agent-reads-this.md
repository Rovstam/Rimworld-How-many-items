# Agent context: How many items

## Mod goal

This RimWorld 1.7 mod is intended to make item quantities easier to understand in `Dialog_Trade`:

- First feature: augment the existing colony-count tooltip for each tradeable item in `Dialog_Trade`. Keep the visible count and show `In caravan` (the tradeable's `CountHeldBy(Colony)`) and `In tile` (the matching item's count in the first loaded player-home map's `ResourceCounter`). The tile value is informational stored stock, not extra goods the caravan can transfer. If there is no loaded player-home map, say so in the tooltip rather than silently leaving the vanilla tooltip unchanged.
- The first in-game verification target is Components. The implementation remains applicable to other item rows as well.
- Support vanilla `TradeUI.DrawTradeableRow` and assess compatibility with UI replacements individually. Trade UI Revised is one example, not the only possible compatibility target.
- Show the amounts of ingredients missing to complete a recipe in crafting bills, if that feature remains in scope after the trade UI work.
- World-map item displays, caravan inventory UI, and aggregation across multiple tiles are out of scope unless the user changes the scope.

Keep the user-specific design and project status in this file. Shared agents and skills should remain generic and must not contain this mod's details.

## Roadmap and status

Update this checklist as work progresses. Mark a goal **Done** only after its expected outcome has been verified; otherwise leave it in progress or blocked and state the evidence or blocker. Keep completed goals marked Done rather than silently removing them.

1. **Done — Understand the red-marked vanilla resource readout.** Its implementation and count semantics have been traced in the supplied local decompiled source. See “Confirmed resource-readout behavior” below. Some narrow details, such as every caller of `CheckUpdateResource`, have not been exhaustively traced and are not needed to establish the central behavior.
2. **Done — Define the first trade-count meaning and scope.** This feature is specifically in `Dialog_Trade`, not a new world-map/map display. Preserve the visible colony-held count and augment its tooltip: `In caravan` is the tradeable's `CountHeldBy(Transactor.Colony)`; `In tile` is the matching item definition's count in `Find.AnyPlayerHomeMap.resourceCounter`. Tile stock is informational and is not transferred to the caravan. Non-resource items say the tile amount is not tracked; no loaded home map is reported explicitly.
3. **Done — Trace the trade button and trade-dialog row paths.** The world-map caravan's Trade gizmo comes from `CaravanVisitUtility.TradeCommand`; it finds the settlement at the caravan's tile and opens `Dialog_Trade` with that `Settlement` as its `ITrader`. Vanilla draws trade rows through `TradeUI.DrawTradeableRow`; the installed Trade UI Revised 1.7 DLL replaces `Dialog_Trade.FillMainRect` and calls its own `MyDrawTradableRow`. Both renderers issue the `ColonyCount` tooltip for the colony-side count. The prior implementation incorrectly required `TradeSession.trader` to be a `Caravan_TraderTracker`, so it rejected the settlement-backed dialog opened by this Trade button.
4. **Done — Implement and verify the `Dialog_Trade` breakdown for Components.** The row hooks accept the active `TradeSession` regardless of whether its trader is a settlement, caravan, or another trader. They preserve the vanilla count tooltip and add the caravan-held count plus the first loaded player-home map's resource-readout count (or an explicit unavailable/not-tracked message). The user confirmed the in-game result with screenshots: the Components colony-count tooltip displays `In caravan: 28` and `In tile: 352`. This verifies the currently tested Trade UI Revised trade-dialog path on RimWorld 1.7.4980 rev60. The workspace build succeeded with 0 warnings and 0 errors. Vanilla-only trade UI and other item edge cases have not yet been separately playtested.
5. **In progress — Assess third-party trade UI compatibility individually.** Trade UI Revised is verified for the Components tooltip by the user's in-game screenshot. Vanilla-only trade UI and other mods that alter the trade interface still need individual assessment and testing. Do not promise universal support.
6. **Deferred — Investigate crafting-bill missing-ingredient feedback.** Revisit after the initial trade UI behavior and scope are settled.
7. **Deferred — Consider caravan inventory, world-map aggregation, and multi-tile totals.** These are outside the initial scope unless the user requests them.

## Confirmed resource-readout behavior

The red-marked left-side item list is RimWorld's vanilla `ResourceReadout`, a map overlay; it is not the trade window or the bottom inspect pane.

- `UIRoot_Play.UIRootOnGUI()` draws the map interface before the main tabs. `MapInterface.MapInterfaceOnGUI_BeforeMainTabs()` invokes `ResourceReadout.ResourceReadoutOnGUI()` for the current map, subject to the UI's draw-state gates. It is drawn while playing, outside the Layout event, and not while the Menu tab is open. The map interface also applies its screenshot-filter/current-map conditions.
- The readout has a simple list and a categorized/expandable list. `Prefs.ResourceReadoutCategorized` selects the mode; the preference defaults to false. The display uses a narrow upper-left area and a scroll view if its contents exceed the available height.
- Both modes display values supplied by the current map's `ResourceCounter`. `ResourceCounter` builds its resource definitions from `ThingDef.CountAsResource`, ordered by `resourceReadoutPriority`.
- The counter's full recount sums stack counts for things in that map's `haulDestinationManager.AllGroupsListForReading`, unwraps minified things, and applies `ShouldCount` filters. The examined filter excludes things that are not fresh and spawned/parent-spawned things at fogged held positions. Simple mode shows positive counts and definitions configured to always appear. Categorized mode renders category totals and eligible item rows from the same counter.
- This is a stored-resource count for the **current map**, not a universal “everything the colony owns” total. The examined recount is not an aggregation across caravans, other maps, or world tiles. Do not assume this is equivalent to the quantity actually available for a trade.
- The map initializes the counter with a full update and calls its tick method during map ticking. The counter performs a full refresh every 204 game ticks. `CheckUpdateResource(Thing)` can also trigger a full recount for qualifying things; not every call site has been traced.
- The trade interface is a separate UI path. `Dialog_Trade.DoWindowContents()` uses `TradeUI.DrawTradeableRow()` in vanilla; Trade UI Revised replaces the dialog's row-list drawing and uses its own row method. The item breakdown belongs in the `ColonyCount` tooltip in this dialog, not in a separate map/world UI. The user has verified that the Components tooltip works in the Trade UI Revised screen.
- `Dialog_Trade` stores its counterparty as an `ITrader`; the Trade button for a world-map caravan opens the dialog with a `Settlement`, not a `Caravan_TraderTracker`. Never gate this tooltip by requiring a caravan trader. The player's negotiator is in a caravan, and `Tradeable.CountHeldBy(Transactor.Colony)` supplies the current tradeable colony-side count.
- For the tile line, use `Find.AnyPlayerHomeMap.resourceCounter` and the row's `ThingDef` when `CountAsResource` is true. `Find.AnyPlayerHomeMap` returns a loaded player-home map or null; when it is null the updated tooltip explicitly reports that the tile count is unavailable. If multiple player-home maps are loaded, this initial implementation uses the first map returned by the game and does not total all home maps. Counts inherit the vanilla resource counter's scope, filtering, and refresh cadence.

### Source references examined

The local `Source code` tree was used to inspect:

- `RimWorld/ResourceReadout.cs`
- `RimWorld/Listing_ResourceReadout.cs`
- `RimWorld/ResourceCounter.cs`
- `RimWorld/MapInterface.cs`
- `RimWorld/UIRoot_Play.cs`
- `RimWorld/TradeUI.cs`
- `RimWorld/Dialog_Trade.cs`
- `RimWorld/TradeSession.cs`
- `RimWorld.Planet/CaravanVisitUtility.cs`
- `RimWorld.Planet/Settlement.cs`
- `RimWorld.Planet/Caravan_TraderTracker.cs`
- `RimWorld.Planet/CaravanInventoryUtility.cs`
- `Verse/Map.cs`
- `Verse/Prefs.cs` and `Verse/PrefsData.cs`
- `Verse/Listing_Tree.cs`, `Verse/TreeNode.cs`, `Verse/TreeNode_ThingCategory.cs`, and `Verse/ThingCategoryDef.cs`

The installed game was previously checked as RimWorld `1.7.4979 rev109`, but the exact provenance/build match of the supplied decompiled source tree was not independently verified. Confirm its provenance before relying on version-sensitive details. Treat the source tree as read-only and inspect only relevant files.

## Project and tooling status

- This folder is the development workspace for this mod. It contains `HowManyItems.csproj`, a Harmony patch source folder, starter metadata, a placeholder preview, and an empty `Defs/` folder. The current assembly build output is `Assemblies/HowManyItems.dll`; local intermediate build files are in `obj/`.
- `About/About.xml` now uses package ID `Rovstam.HowManyItems` and author `Rovstam`. Its Harmony dependency includes the verified Workshop URL `steam://url/CommunityFilePage/2009463077`, required by RimWorld dependency metadata validation. It is a local, unpublished mod, so no `<publishedFileId>` is set; do not invent one. If it is later published, add only its actual Workshop ID.
- **Runtime-copy status:** The game-local `Mods\How many items` copy was synchronized with workspace metadata, DLL, and English keyed strings after the user authorized that update; SHA-256 hashes matched. The RimSort `Mod testing` instance config now enables `Rovstam.HowManyItems` and no longer enables the old placeholder ID. The latest observed game log confirms Harmony and `Rovstam.HowManyItems` loaded and no longer shows the Harmony dependency warning. After changing source, rebuild and only update the game-local copy when RimWorld is closed; never modify the Workshop copy.
- The installed game was checked as RimWorld `1.7.4980 rev60` (installed `Assembly-CSharp.dll` file version `1.7.9785.1822`). Game references are under `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`; recheck build and file presence before relying on them.
- The .NET 10 SDK `10.0.402` was installed per-user at `C:\Users\PERSONAL\.dotnet`. Recheck `dotnet --info` before building; already-open terminals may not inherit the updated user PATH.
- ILSpy GUI is installed at `C:\Users\PERSONAL\Documents\Programs\ILSpy_windows_selfcontained_11.1.0.9782-x64\ILSpy.exe`. The user-local `ilspycmd` tool was subsequently installed under `C:\Users\PERSONAL\.dotnet\tools`; its first invocation required `DOTNET_ROOT=C:\Users\PERSONAL\.dotnet`. Recheck paths and availability before use. Do not mistake the GUI executable for a CLI.
- Trade UI Revised was previously observed in the user's Workshop at `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2683622537`, including `v1.7\Assemblies\TradeUI.dll`. It is one potential compatibility target. Recheck installed versions and paths when necessary; these files may only be read.
- The project currently references the installed RimWorld `Assembly-CSharp.dll`, `UnityStubs.dll`, and `Waylith.dll`, plus the user's installed Harmony 3.0.1.0 assembly at Workshop ID `2009463077` for compile-time reference only. Build output must remain in this project. Never copy the Harmony assembly from Workshop into the mod.
- Implementation source is in `Source/HowManyItemsMod.cs` and `Source/DialogTradeItemCountPatch.cs`, with English tooltip labels in `Languages/English/Keyed/HowManyItems.xml`. It preserves the visible colony count and augments the `ColonyCount` hover tooltip with `In caravan` and `In tile` values.

## Non-negotiable safety boundary

The user's Workshop directory
`C:\Program Files (x86)\Steam\steamapps\workshop\content\294100`
is SACRED and permanently read-only. Never create, modify, delete, rename, move, overwrite, extract, install, copy, build, publish, or deploy anything into or within that directory. Keep all mod source and build outputs in this workspace or another user-approved safe location. Workshop files needed for research may only be read without changing them.

## Expectations for future agents

- This is RimWorld modding, not Mindustry.
- Load the relevant workspace skills, especially `skill-rimworld-ui-mods`, `skill-rimworld-harmony-patching`, `skill-rimworld-csharp-mods`, `skill-rimworld-ilspy-and-game-source`, `skill-rimworld-1-7-engine-migration`, and `skill-rimworld-testing-and-diagnosis`.
- Verify exact APIs, method signatures, UI behavior, count semantics, and third-party compatibility against matching 1.7 references before implementation; distinguish source-based findings from in-game validation.
- Keep trade UI compatibility broad in intent but empirical in claims: assess mods individually and document tested configurations and known incompatibilities.
- Keep the user informed in plain language. Separate confirmed findings from hypotheses, ask before significant product-scope changes, and explain testing the user must perform.
- Maintain the roadmap above: update status as work proceeds, and mark a goal Done only when its expected outcome is verified. Never claim an in-game result unless it was actually tested in RimWorld.
- Never change the sacred Workshop directory.

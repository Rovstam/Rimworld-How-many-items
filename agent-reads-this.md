# Agent context: How many items

## Mod goal

This RimWorld 1.7 C# / Harmony mod adds inventory context to the **count tooltips in `Dialog_Trade`**. Current behavior and boundaries:

- On existing trade item rows, retain the game's count tooltip and append `In caravan` and `In tile`. This appears when hovering the numeric count area, not the item-name/description tooltip.
- `In caravan` is the row tradeable's `CountHeldBy(Transactor.Colony)`.
- `In tile` is the quantity of matching items stored in slot groups on the first loaded player-home map returned by `Find.AnyPlayerHomeMap`. It is informational, not additional stock available for this caravan trade.
- Do not add trade rows for items that exist only on the map and are absent from the caravan tradeables. Loose items outside storage, other maps, and unloaded maps are not included.
- Matching uses the row exemplar and `TransferableUtility.TransferAsOne`, preserving vanilla trade grouping distinctions such as stuff/quality where applicable. Non-fresh and fog-obscured stored items are excluded.
- Vanilla support hooks `TradeUI.DrawTradeableRow`. Trade UI Revised has a separate row hook. Compatibility with other trade-window replacements is unassessed.
- The mod does not currently render an item list/count panel on the map. Broader map-related ideas remain in the roadmap below as deferred, not cancelled.

Keep the user-specific design and project status in this file. Shared agents and skills should remain generic and must not contain this mod's details.

## Roadmap and status

Update this checklist as work progresses. Mark a goal **Done** only after its expected outcome has been verified; otherwise leave it in progress or blocked and state the evidence or blocker. Keep completed goals marked Done rather than silently removing them.

1. **Done — Understand the vanilla resource readout.** Its implementation and count semantics were traced in the supplied decompiled source. See “Confirmed resource-readout behavior” below.
2. **Done — Define current trade-count semantics and scope.** `In caravan` is the colony count from the tradeable; `In tile` counts matching stored items on the first loaded player-home map. Map-only items do not create trade rows.
3. **Done — Trace trade-button and trade-row paths.** The world-map caravan's Trade command opens `Dialog_Trade` with the settlement at that tile as trader. Vanilla draws rows through `TradeUI.DrawTradeableRow`; Trade UI Revised has its own row method. The mod does not require the trader to be a caravan tracker.
4. **Verified — Components in Trade UI Revised colony count tooltip.** User confirmed an in-game Components tooltip showing `In caravan: 28` and `In tile: 352` on RimWorld 1.7.4980 rev60.
5. **Implemented; broader item verification pending — Count stored non-resource items and distinguish variants.** Uses `TransferableUtility.TransferAsOne` and freshness/fog exclusions. Test non-resource stackables, stuff/quality variants, zero-count rows, and non-stackable equipment.
6. **Implemented; seller-panel test pending — Support Trade UI Revised trader count.** `TraderCount` is handled along with `ColonyCount`, preserving the original keyed tooltip text. Test the Settlement-sold panel in game.
7. **Implemented; current runtime behavior not independently reconfirmed — Support vanilla trade rows.** `VanillaTradeRowPatch` wraps `TradeUI.DrawTradeableRow`. The user previously reported vanilla appeared to work after recopying. A later screenshot showed the cursor over item description, not the numeric count; user recognized the distinction. That screenshot is not evidence of a missing count tooltip.
8. **Deferred — Consider a map-level item display, if still desired.** No such display is implemented: the earlier map/item-display direction was narrowed to `Dialog_Trade`, since the game already lists items on the map. Preserve this as a deferred product decision, not a deleted plan. If revisited, clarify whether it means an overlay/panel, changing the map list, or another map UI feature.
9. **Deferred — Consider caravan inventory UI and aggregation across multiple loaded/home maps.** Out of current scope. Define the included maps and world locations before implementing any aggregation.
10. **Deferred — Investigate missing-ingredient feedback in crafting bills.** Separate possible feature; not currently implemented.

## Confirmed resource-readout behavior

The red-marked left-side item list is RimWorld's vanilla `ResourceReadout`, a map overlay; it is not the trade window or the bottom inspect pane.

- `UIRoot_Play.UIRootOnGUI()` draws the map interface before the main tabs. `MapInterface.MapInterfaceOnGUI_BeforeMainTabs()` invokes `ResourceReadout.ResourceReadoutOnGUI()` for the current map, subject to the UI's draw-state gates. It is drawn while playing, outside the Layout event, and not while the Menu tab is open. The map interface also applies its screenshot-filter/current-map conditions.
- The readout has a simple list and a categorized/expandable list. `Prefs.ResourceReadoutCategorized` selects the mode; the preference defaults to false. The display uses a narrow upper-left area and a scroll view if its contents exceed the available height.
- Both modes display values supplied by the current map's `ResourceCounter`. `ResourceCounter` builds its resource definitions from `ThingDef.CountAsResource`, ordered by `resourceReadoutPriority`.
- The counter's full recount sums stack counts for things in that map's `haulDestinationManager.AllGroupsListForReading`, unwraps minified things, and applies `ShouldCount` filters. The examined filter excludes things that are not fresh and spawned/parent-spawned things at fogged held positions. Simple mode shows positive counts and definitions configured to always appear. Categorized mode renders category totals and eligible item rows from the same counter.
- This is a stored-resource count for the **current map**, not a universal “everything the colony owns” total. The examined recount is not an aggregation across caravans, other maps, or world tiles. Do not assume this is equivalent to the quantity actually available for a trade.
- The map initializes the counter with a full update and calls its tick method during map ticking. The counter performs a full refresh every 204 game ticks. `CheckUpdateResource(Thing)` can also trigger a full recount for qualifying things; not every call site has been traced.
- The trade interface is a separate UI path. Vanilla `Dialog_Trade` uses `TradeUI.DrawTradeableRow()`; Trade UI Revised replaces the dialog's row-list drawing and uses its own row method. In Trade UI Revised, `isOurs: true` is the colony panel (`ColonyCount`) and `isOurs: false` is the trader/Settlement-sold panel (`TraderCount`). The breakdown appears on the numeric count tooltip, not the item-description tooltip. The user verified Components on the Trade UI Revised colony count tooltip. They previously reported vanilla appeared to work after recopying; a later screenshot showed the description tooltip, and the user recognized the distinction. The seller panel still needs in-game confirmation.
- `Dialog_Trade` stores its counterparty as an `ITrader`; the Trade button for a world-map caravan opens the dialog with a `Settlement`, not a `Caravan_TraderTracker`. Never gate this tooltip by requiring a caravan trader. The player's negotiator is in a caravan, and `Tradeable.CountHeldBy(Transactor.Colony)` supplies the current tradeable colony-side count.
- For the tile line, enumerate `Find.AnyPlayerHomeMap.haulDestinationManager.AllGroupsListForReading` and each `SlotGroup.HeldThings`, then sum the stacks that match the row exemplar via `TransferableUtility.TransferAsOne`. This extends beyond `ResourceCounter`, which only includes defs with `CountAsResource`; it can therefore count stackable weapons/apparel/other non-resource items too. Matching uses vanilla trade grouping, including stuff/quality and stack compatibility distinctions. As with the resource readout, skip non-fresh and fog-obscured held items. Loose items outside a storage group are excluded. `Find.AnyPlayerHomeMap` returns a loaded player-home map or null; if several exist, this implementation uses the first map and does not total them. Counts are informational, not additional caravan trade stock.
- Compute stored counts only when an applicable count tooltip is actually hovered. Cache the last computed row count by trade deal, map, row exemplar, and game tick to avoid repeating a storage-wide scan on each repaint. A `Dialog_Trade` forces pause, so contents are expected to remain stable for a trade session; verify cache behavior if that assumption changes.

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
- `RimWorld/TransferableUtility.cs` (`TradeableMatching` / `TransferAsOne`)
- `RimWorld/SlotGroup.cs`
- `RimWorld/HaulDestinationManager.cs`
- `RimWorld.Planet/CaravanVisitUtility.cs`
- `RimWorld.Planet/Settlement.cs`
- `RimWorld.Planet/Caravan_TraderTracker.cs`
- `RimWorld.Planet/CaravanInventoryUtility.cs`
- `Verse/Map.cs`
- `Verse/Prefs.cs` and `Verse/PrefsData.cs`
- `Verse/Listing_Tree.cs`, `Verse/TreeNode.cs`, `Verse/TreeNode_ThingCategory.cs`, and `Verse/ThingCategoryDef.cs`

The supplied decompiled source tree was checked against an earlier 1.7 build; its exact provenance and match to newer game builds have not been independently verified. Confirm the game build and source provenance before relying on version-sensitive details. Treat the source tree as read-only and inspect only relevant files.

## Project and tooling status

- This folder is the development workspace for this mod. It contains `HowManyItems.csproj`, a Harmony patch source folder, starter metadata, a placeholder preview, and an empty `Defs/` folder. The current assembly build output is `Assemblies/HowManyItems.dll`; local intermediate build files are in `obj/`.
- `About/About.xml` now uses package ID `Rovstam.HowManyItems` and author `Rovstam`. Its Harmony dependency includes the verified Workshop URL `steam://url/CommunityFilePage/2009463077`, required by RimWorld dependency metadata validation. It is a local, unpublished mod, so no `<publishedFileId>` is set; do not invent one. If it is later published, add only its actual Workshop ID.
- **Runtime-copy status:** On 2026-10-16 the installed game updated to RimWorld `1.7.4981 rev136` (installed `Assembly-CSharp.dll` file version `1.7.9786.4087`). The workspace project was rebuilt against those installed game assemblies with .NET 10.0.402; build succeeded with 0 warnings/errors. With RimWorld closed, the rebuilt workspace DLL was copied to the game-local `Mods\How many items\Assemblies` folder; both DLLs were verified identical (SHA-256 `E859445915B7B4D7198FB42519952C84C965B24B9D17F277D1FC0F08D0B8F251`). This is a build/deployment record, not independent confirmation that the tooltip displays after the update. Recheck build and loaded copy after future updates. Do not deploy further changes outside the workspace without authorization; never modify the Workshop copy.
- The installed game was checked as RimWorld `1.7.4981 rev136`; game references are under `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`. Recheck build and file presence before relying on them.
- The .NET 10 SDK `10.0.402` was installed per-user at `C:\Users\PERSONAL\.dotnet`. Recheck `dotnet --info` before building; already-open terminals may not inherit the updated user PATH.
- ILSpy GUI is installed at `C:\Users\PERSONAL\Documents\Programs\ILSpy_windows_selfcontained_11.1.0.9782-x64\ILSpy.exe`. The user-local `ilspycmd` tool was subsequently installed under `C:\Users\PERSONAL\.dotnet\tools`; its first invocation required `DOTNET_ROOT=C:\Users\PERSONAL\.dotnet`. Recheck paths and availability before use. Do not mistake the GUI executable for a CLI.
- Trade UI Revised was previously observed in the user's Workshop at `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2683622537`, including `v1.7\Assemblies\TradeUI.dll`. It is one potential compatibility target. Recheck installed versions and paths when necessary; these files may only be read.
- The project currently references the installed RimWorld `Assembly-CSharp.dll`, `UnityStubs.dll`, and `Waylith.dll`, plus the user's installed Harmony 3.0.1.0 assembly at Workshop ID `2009463077` for compile-time reference only. Build output must remain in this project. Never copy the Harmony assembly from Workshop into the mod.
- Implementation source is in `Source/HowManyItemsMod.cs` and `Source/DialogTradeItemCountPatch.cs`; English labels are in `Languages/English/Keyed/HowManyItems.xml`. `HowManyItemsMod` calls Harmony `PatchAll`. The vanilla hook scopes tooltip context around `TradeUI.DrawTradeableRow`. The optional Trade UI Revised hook locates its row method and sets which count tooltip is active (`ColonyCount` or `TraderCount`). A shared prefix on `TooltipHandler.TipRegionByKey(Rect,string)` adds the counts only over a qualifying count area and retains the original keyed tooltip text. Tile inventory is scanned lazily when an applicable tooltip is hovered and cached by deal, map, row exemplar, and game tick. Keep scans out of unhovered per-frame UI drawing.

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

# SPELL RESEARCH SYNTHESIZER

Intended to support JSON files used by [Spell Research JSON Patch](https://www.nexusmods.com/skyrimspecialedition/mods/42381) and [Spell Research - JSON Mysticism Patch](https://www.nexusmods.com/skyrimspecialedition/mods/48515).
Current functionality:
1. Reads the JSON file for the original JSON patch.
2. Reads the JSON file for the Mysticism patch (as well as supporting a new field for added/removed artifacts/ingredients).
3. Updates the tomes
4. Skips tomes depending by level (for all my fellows who disable starting spells).
5. Reads PSC files (tested with a few spell packs from the Spell Research Patch Compendium at [Spell Research Patch Compendium Redux](https://www.nexusmods.com/skyrimspecialedition/mods/61177).
6. Detects and resolves spell/artifact overwrites (last mod in your LO wins).
7. Exports the imported spell/artifact data as JSON for proper importing.
8. Allows binding multiple patches to single ESP, if using specific JSONs for mods.
9. Generates FormListManipulator .ini to bypass Spell Research import procedure, saving setup time in new playthroughs.

Potential functionality (will be implemented if and only if I can be bothered - and figure out how):
1. Automatically generate template files from your load order (that'd still require a great deal of manual untangling, but the hope would be that the user would no longer need to dig for FormID's, at least.

# Updating
If you used the patcher previously, your configuration files may be out of date. The only fix I know of is manually replacing them, or deleting them and re-adding the patcher.

## Options

* Spell Levels
  * Allows specific treatment of each spell level tomes
  * Options:
  * Process: Whether to alter spell tomes for this level to display spell archetypes instead of tteaching you the spell directly. By default, Novice spells are excluded for the sake of players (like myself) that remove starting spells. Will not affect spell tomes that aren't found in any (valid) patches.
  * Font: Font used to fill in the text. Recommended picking from these values, may be possible to use custom fonts:
    * $SkyrimBooks
    * $HandwrittenFont
    * $DaedricFont
    * $DragonFont
    * $DragonFont
    * $FalmerFont
    * $MageScriptFont
  * Use Font Color: Whether to color the words for archetypes in the text. Hex colors, defined in config.json.
  * Use Image: Whether to illustrate the tome's first page according to the first archetype found.
    * Default setup (can be changed in config.json) comes from [Spell Research - Experience Book](https://www.nexusmods.com/skyrimspecialedition/mods/28355). Since I can't package them with my mod and the path in the .bsa is not supported by books, you'll need to manually extract them - the expected location for the .dds files is in "textures/interface/exported/widgets/spellresearchbook/textures/".
  * Novice...Master Experience: How much XP for each of the spell's archetypes you receive on first reading the tome.
  * Skill Required: Minimum skill level in spell's school you need to cause the above to happen.
* Generate FLM Ini: ...Generates an FLM .ini file.
* Json Names
  * Paths to JSON files for specific mods. These patches will take priority over all other patches. Supports JSON files formatted for [Spell Research Json Patch](https://www.nexusmods.com/skyrimspecialedition/mods/42381) or [Spell Research -JSON Mysticism Patch](https://www.nexusmods.com/skyrimspecialedition/mods/48515). Includes the Mysticism patch by default.
* Json Paths
  * Paths to folders to search for other patches. The .JSON file name, without extension, must match the file name of the .ESP (also without extension.) Takes priority over PSC patches. Includes locations for JSON Patch and the Synthesizer itself by default.
* Pscnames
  * List of semicolon-separated pairs of plugin names and paths to .PSC patch files. Default configuration includes all patches from the [Spell Research Patch Compendium](https://www.nexusmods.com/skyrimspecialedition/mods/61177).

# Nine Sols Cutscene Skip

A Nine Sols mod that enables skipping any cutscene or dialogue (that won't break the game), as well as undoing forced slow walks.

This should cover all of the game's longest cutscenes (Heng flashbacks, the prison capture sequence, the Point of no Return scenes, all conversations and phone calls) as well as many smaller ones that are safe to skip (some boss transitions, most of the Pavilion NPC interactions, etc). However, many smaller cutscenes (e.g. some Yi elevator rides) are deliberately not made skippable because trying to skip them would break the game in some way (e.g. leave Yi stuck in a wall).

Whenever the mod detects something it knows how to skip safely (or a 'story walk' it can undo), it will display a notification in the bottom right corner prompting you to skip. The keybinds for actually skipping/undoing can also be changed. If you're new to Nine Sols mods: install the BepinExConfigurationManager mod and press F1 in-game to change settings like that.

As a demonstration, this is what it looks like to skip the Point of no Return cutscenes:

![Demonstration: Skipping the Point of no Return Cutscenes](https://github.com/Ixrec/NineSolsCutsceneSkip/blob/main/ponr_demo.gif?raw=true)

If you find a way to break the game with this mod, please open an issue on this repository.

## Contribution / Development

Should be the same as any other Nine Sols mod. See https://github.com/nine-sols-modding/NineSols-ExampleMod for detailed instructions.

PRs welcome to skip even more stuff, or introduce better ways of skipping things. My goal with this mod was just to skip the things that are easy to skip, then get back to working on my Archipelago Randomizer, so there are many potential improvements if someone feels more passionately about this topic than I did. For example, I have no idea how to skip the ending credits, or the glitch scenes in Ethereal's soulscape, many of the cutscenes on the denylist are probably possible to skip safely if studied in more detail, etc.

## What about Cheat Menu?

The skip feature in Yukikaco's CheatMenu will simply skip anything it can find in the current level with a `.TrySkip()` method. This is sometimes useful for developers, but for normal gameplay this is dangerous and will softlock in dozens of places.

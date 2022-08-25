# Harmonized Oxide Plugin

Hello, and welcome to Harmony. This repo is a template plugin that can load Oxide plugins from within Harmony mods.

## Setup

This project contains the necessary build scripts and boilerplate to publish a Harmony mod with publicized assemblies that are able to reference any private code.

Simply run `update-all-dependencies.bat` and they will be downloaded from SteamDepotDownloader. uMod will be updated automatically from the latest release. Once updated, all relevant DLLs will be ran through the [Assembly Publicizer](https://github.com/CabbageCrow/AssemblyPublicizer). You can change the platform targetting, which can matter if you reference platform-specific DLLs like Steamworks. 

Compiled DLLs will be found in the usual `bin\Linux\net48` folder. Drag this into `HarmonyMods`, and run `harmony.load NAME` if you're adding it while the server is running. Similarly, you can `harmony.unload NAME` to remove all plugins.

## How the hell?

It's really quite simple --- when the Harmony mod is loaded, all plugins are found with reflection and manually loaded through Oxide's RootPluginManager. When it's unloaded, all plugins are given the unload signal. This magic is done in [HarmonizedPluginLoader](https://github.com/RadiumModLoader/HarmonizedOxidePlugin/blob/master/src/HarmonizedOxidePlugin/HarmonizedPluginLoader.cs) if you'd like to have a look.

This completely skips Oxide's compiler step, which means you finally get modern C# 10 features (lang level at least, still on Mono) as well as the benefit of precompiled assemblies and the option to target publicized assemblies. You can fit a hundred plugins into one DLL. You can have multiple plugins with shared libraries bundled alongside them. Go nuts.

Just remember, Oxide (or any other runtime assembly loader) doesn't actually delete old assemblies. Static classes will stay around forever, leading to memory leaks. Every time you reload, a new static class with a new version number is created and initialized. Only *plugin instances* are deleted. Because of this, shared state in static classes needs to be cleared on assembly unload. You can add more calls to HarmonizedPluginLoader.OnUnloaded, or call it from plugin Unload. 

As for "publicized assemblies," it's a Unity trick. Mono will allow you to reference anything as long as your assembly is compiled with AllowUnsafeBlocks, which is on for this repo. You don't need the publicized assemblies on the actual server, just in your dev environment. Mono just doesn't care.

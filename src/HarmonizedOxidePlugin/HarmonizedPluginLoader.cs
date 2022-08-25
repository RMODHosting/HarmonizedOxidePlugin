using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using UnityEngine;

namespace HarmonizedOxidePlugin;
 
/// <summary>
/// This class is the magic that loads all the Oxide plugins in the assembly.
/// It's loaded by the Rust.Harmony backend, and will manually load all plugins contained in the mod.
///
/// This bypasses Oxide's compile step, which means you get modern C# 10 features,
/// and full access to publicized assemblies.
///
/// Plugins contained in a single mod file are able to reference each other directly via plugin instances,
/// as well as any shared libraries in the mod file. Everything will get reloaded.
///
/// Make sure you clean up any static plugin state, since Assemblies are loaded forever.
/// </summary>
public class HarmonizedOxidePluginLoader : IHarmonyModHooks
{
    static readonly List<RustPlugin> Plugins = new ();
    private static bool ServerInitYet = false;
    
    /// <summary>
    /// Needed because Oxide will error if we try to load plugins as early as
    /// Harmony mods. Wait until Oxide init.
    /// </summary>
    [HarmonyPatch(typeof(Bootstrap), nameof(Bootstrap.Init_Tier0))]
    public static class Init_Tier0_Patch
    {
        public static void Postfix()
        {
            ServerInitYet = true;
            LoadPlugins();
        }
    }
    
    /// <summary>
    /// Can be called at runtime via harmony.load,
    /// so load plugins if the server's already started up
    /// </summary> 
    public void OnLoaded(OnHarmonyModLoadedArgs args)
    {
        if (ServerInitYet) 
            LoadPlugins();
    }

    /// <summary>
    /// Triggers unload of all plugins when the Harmony mod is unloaded.
    /// </summary> 
    public void OnUnloaded(OnHarmonyModUnloadedArgs args) => UnloadAllPlugins();


    /// <summary>
    /// Grabs each plugin type, makes an instance of it, and
    /// registers it with Oxide's default plugin loader.
    /// </summary>
    public static void LoadPlugins()
    {
        var plugins = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(RustPlugin).IsAssignableFrom(type))
            .ToList();

        foreach (var pluginType in plugins)
        {
            try
            {
                Interface.Oxide.LogDebug($"[Oxide Harmonizer] Loading {pluginType.Name}");

                object obj = Activator.CreateInstance(pluginType, true);
                if (obj == null)
                    continue;

                RustPlugin plugin = (RustPlugin)obj;
                plugin.Loader = null;
                plugin.Watcher = Interface.Oxide.GetExtension<CSharpExtension>().Watcher;
                plugin.SetPluginInfo(pluginType.Name, $"{pluginType.Name}.cs");

                Plugins.Add(plugin);
                Interface.Oxide.PluginLoaded(plugin);
            }
            catch (Exception e)
            {
                Interface.Oxide.LogException($"Error loading {pluginType.Name}", e);
            } 
        }
    }

    
    public static void UnloadAllPlugins()
    {
        foreach (var plugin in Plugins.ToList())
        {
            UnloadPlugin(plugin);
        }
    }

    /// <summary>
    /// Unloads a specific plugin, useful if you want to
    /// manually remove a problematic plugin without reloading others.
    /// </summary>
    public static void UnloadPlugin(Type type)
    {
        var plugin = Plugins.FirstOrDefault(t => t.GetType() == type);
        if (plugin == null)
        {
            Interface.Oxide.LogError($"Could not find plugin of type {type.Name}");
            return;
        }
        
        UnloadPlugin(plugin);
    }
    
    /// <summary>
    /// Sends unloaded signal to the root plugin manager and removes the plugin from the list.
    /// </summary>
    public static void UnloadPlugin(RustPlugin plugin)
    {
        try
        {
            Interface.Oxide.RootPluginManager.RemovePlugin(plugin);
            
            if (plugin.IsLoaded)
                Interface.Oxide.CallHook("OnPluginUnloaded", plugin);
            
            plugin.IsLoaded = false;
            
            Interface.Oxide.LogInfo("[Oxide Harmonizer] Unloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
        }
        catch (Exception e)
        {
            Interface.Oxide.LogException($"Error unloading {plugin.Name}", e);
        }
        
        Plugins.Remove(plugin);
    }
    
}
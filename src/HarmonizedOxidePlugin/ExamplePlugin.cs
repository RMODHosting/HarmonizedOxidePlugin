using Oxide.Plugins;

namespace HarmonizedOxidePlugin;

public class ExamplePlugin : RustPlugin
{
    void OnServerInitialized()
    {
        Puts("[SkinboxMenu] Harmonizer Works!");
    }

    void Unload()
    {
        Puts("[SkinboxMenu] Unloading!");
    }
}
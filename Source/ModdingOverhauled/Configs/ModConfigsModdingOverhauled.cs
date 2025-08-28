using Game.UI;
using ModdingOverhauled.ConfigModule.UI;
using ModdingOverhauled.UnityExplorerModule;
using UnityEngine.Events;

namespace ModdingOverhauled.Configs
{
    public class ModConfigsModdingOverhauled : ModConfigs
    {
        public override void DoWindowContent()
        {
            Checkbox("Unity Explorer Enabled", Main.Config.IsUnityExplorerLoaded, UnityExplorerManager.ToggleEditor);
        }

        public override void OnSave()
        {
            base.OnSave();
        }
    }
}
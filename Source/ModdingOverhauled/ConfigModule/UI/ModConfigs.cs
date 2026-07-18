using System;
using ModdingOverhauled.Logging;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBeProtected.Global

namespace ModdingOverhauled.ConfigModule.UI
{
    public abstract class ModConfigs : MonoBehaviour
    {
        private void Start() 
        {
            DoWindowContent();
        }

        public virtual void OnSave() 
        {
            Printer.Warn($"Saving {GetType()}");
            try
            {
                ModConfigManager.SaveConfigFromMod(this);
            }
            catch (Exception e)
            {
                Printer.Error($"Error while trying to save {GetType()}\n{e}");
            }
        }
        public abstract void DoWindowContent();

        public void SetActive(bool on)
        {
            gameObject.SetActive(on);
            if(!on)
                OnSave();
        }
    }
}

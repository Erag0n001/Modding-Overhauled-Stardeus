using System;
using Game.UI;
using ModdingOverhauled.Misc;
using UnityEngine;
using UnityEngine.Events;

namespace ModdingOverhauled.ConfigModule.UI
{
    public abstract class ModConfigs : MonoBehaviour, IUIPanel
    {
        private void Start() 
        {
            DoWindowContent();
        }

        public virtual void OnSave() 
        {
            Printer.Warn($"Saving {this.GetType()}");
            try
            {
                ModConfigManager.SaveConfigFromMod(this);
            }
            catch (Exception e)
            {
                Printer.Error($"Error while trying to save {this.GetType()}\n{e}");
            }
        }
        public abstract void DoWindowContent();

        public void SetActive(bool on)
        {
            gameObject.SetActive(on);
            if(!on)
                OnSave();
        }

        public void Checkbox(string text, bool value, UnityAction<bool> onChanged)
        {
            UIBuilder.CreateToggle("UIToggleWidget", transform, 
                text, value, onChanged);
        }

        public void Label(string text)
        {
            UIBuilder.CreateText("UILabelWidget", text, transform);
        }

        public void Slider(float value, UnityAction<float> onChanged)
        {
            UIBuilder.CreateSlider("UISliderWidget", value, onChanged, base.transform);
        }

        public void TextInput(string value, string text, UnityAction<string> onTextChanged)
        {
            var input = UIBuilder.CreateInputField("UITextInputWidget", value, text, base.transform);
            input.onValueChanged.AddListener(onTextChanged);
        }
    }
}

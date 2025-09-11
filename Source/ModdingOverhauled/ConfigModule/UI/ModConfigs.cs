using System;
using Game.UI;
using ModdingOverhauled.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBeProtected.Global

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
        
        public Toggle Checkbox(string text, bool value, UnityAction<bool> onChanged)
        {
            return UIBuilder.CreateToggle("UIToggleWidget", transform, 
                text, value, onChanged);
        }

        public TMP_Text Label(string text)
        {
            return UIBuilder.CreateText("UILabelWidget", text, transform);
        }

        public Slider Slider(float value, UnityAction<float> onChanged)
        {
            return UIBuilder.CreateSlider("UISliderWidget", value, onChanged, transform);
        }

        public TMP_InputField TextInput(string value, string text, UnityAction<string> onTextChanged)
        {
            var input = UIBuilder.CreateInputField("UITextInputWidget", value, text, transform);
            input.onValueChanged.AddListener(onTextChanged);
            return input;
        }
    }
}

﻿using HarmonyLib;
using TB_CameraTweaker.KsHelperLib.Localization;
using TB_CameraTweaker.KsHelperLib.UI.Elements.CheckBox;
using TimberApi.UiBuilderSystem.ElementSystem;
using Timberborn.CameraSystem;

namespace TB_CameraTweaker.Patchers
{
    [HarmonyPatch(typeof(CameraVerticalAngleLimiter), nameof(CameraComponent.LateUpdate))]
    internal class CameraVerticalAngelLimiterPatcher : PatcherBase<bool>
    {
        private static CameraVerticalAngelLimiterPatcher _instance;
        private CheckBoxElement _checkboxVerticalAngelLimiter;

        public CameraVerticalAngelLimiterPatcher() : base("VerticalAngelLimiter") { _instance = this; }

        public static void Prefix(CameraVerticalAngleLimiter __instance) => _instance?.PrefixPatch(__instance);

        public override void UseConfigValue() => ChangeValue(_checkboxVerticalAngelLimiter.Config.Value);

        protected override void AddUIElements(VisualElementBuilder builder) {
            return; // soft disabled this patch
            _checkboxVerticalAngelLimiter.Build(builder);
        }

        protected override void SetupPatcher() {
            SetupConfig();
            _checkboxVerticalAngelLimiter.Config.Updated += () => UseConfigValue();
        }

        private void PrefixPatch(CameraVerticalAngleLimiter instance) {
            return; // soft disabled this patch
            if (IsDirty) {
                instance._minVerticalAngle = NewValue ? 10f : 55f;
                IsDirty = false;
            }
        }

        private void SetupConfig() {
            var cfg = new CheckBoxConfig(
                key: "Vertical Angel Limiter Factor",
                description: "Disable Vertical Angel Limiter",
                def: false,
                labelText: _loc.T(key: $"{LocConfig.LocTag}.menu.VerticalLimiter"),
                footerText: $"{_loc.T($"{LocConfig.LocTag}.single.original")}: {_loc.T($"{LocConfig.LocTag}.single.off")}"
            );
            _checkboxVerticalAngelLimiter = new CheckBoxElement(cfg);
        }
    }
}
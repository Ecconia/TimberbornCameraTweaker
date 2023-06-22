﻿using HarmonyLib;
using TB_CameraTweaker.KsHelperLib.Patches;
using TimberApi.DependencyContainerSystem;
using Timberborn.CameraSystem;

namespace TB_CameraTweaker.Patches
{
    [HarmonyPatch(typeof(CameraComponent), nameof(CameraComponent.LateUpdate))]
    internal class CameraFOVPatcher : PatcherGenericValue<float>
    {
        private static CameraFOVPatcher Instance => _instance ??= DependencyContainer.GetInstance<CameraFOVPatcher>();

        private static CameraFOVPatcher _instance;

        public static void Postfix(CameraComponent __instance) => Instance.PostfixPatch(__instance);

        private void PostfixPatch(CameraComponent instance) {
            if (IsDirty) {
                instance.FieldOfView = NewValue;
                IsDirty = false;
            }
        }
    }
}
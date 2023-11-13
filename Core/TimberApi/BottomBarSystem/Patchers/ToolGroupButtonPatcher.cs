using System.Linq;
using HarmonyLib;
using TimberApi.Common.SingletonSystem;
using TimberApi.DependencyContainerSystem;
using TimberApi.HarmonyPatcherSystem;
using TimberApi.SceneSystem;
using TimberApi.ToolGroupSystem;
using Timberborn.CoreUI;
using Timberborn.Debugging;
using Timberborn.ToolSystem;
using UnityEngine.UIElements;

namespace TimberApi.BottomBarSystem.Patchers
{
    public class ToolGroupButtonPatcher : BaseHarmonyPatcher, ITimberApiLoadableSingleton
    {
        private static ToolGroupService _toolGroupService = null!;
        
        private static BottomBarService _bottomBarService = null!;

        private static DevModeManager _devModeManager = null!;

        private static readonly string ActiveClassName = "button--active";

        public override string UniqueId => "TimberApi.ToolGroupButton";

        public void Load()
        {
            _bottomBarService = DependencyContainer.GetInstance<BottomBarService>();
            _devModeManager = DependencyContainer.GetInstance<DevModeManager>();
            _toolGroupService = DependencyContainer.GetInstance<ToolGroupService>();
        }

        public override void Apply(Harmony harmony)
        {
            
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(ToolGroupButton), nameof(ToolGroupButton.IsVisible)),
                GetHarmonyMethod(nameof(ContainsToolPatch))
            );

            harmony.Patch(
                GetMethodInfo<ToolGroupButton>(nameof(ToolGroupButton.OnToolGroupEntered)),
                GetHarmonyMethod(nameof(OnToolGroupEntered))
            );

            harmony.Patch(
                GetMethodInfo<ToolGroupButton>(nameof(ToolGroupButton.OnToolGroupExited)),
                GetHarmonyMethod(nameof(OnToolGroupExited))
            );
        }
        
        public override bool ShouldApply(SceneEntrypoint? sceneEntrypoint)
        {
            return sceneEntrypoint == SceneEntrypoint.InGame;
        }

        public static bool ContainsToolPatch(ref bool __result, ToolGroup ____toolGroup, ToolGroupButton __instance)
        {
            if (____toolGroup is not IToolGroup apiToolGroup)
            {
                return false;
            }

            var hasActiveTool = __instance._toolButtons.Any<ToolButton>(button => button.ToolEnabled);

            if (hasActiveTool)
            {
                __result = true;
                return false;
            }

            __result = _toolGroupService
                .GetToolGroupButtonByGroupId(apiToolGroup.Id)
                .Any(button => button.IsVisible);

            return false;
        }

        public static bool OnToolGroupEntered(ToolGroupEnteredEvent toolGroupEnteredEvent, ToolGroupButton __instance, ToolGroup ____toolGroup, VisualElement ____toolGroupButtonWrapper)
        {
            if(toolGroupEnteredEvent.ToolGroup is not IToolGroup enteredToolGroup || ____toolGroup is not IToolGroup toolGroup) return false;

            var row = _bottomBarService.GetGroupRow(toolGroup.Id);
            var enteredRow = _bottomBarService.GetGroupRow(enteredToolGroup.Id);

            if(row >= enteredRow && toolGroupEnteredEvent.ToolGroup != ____toolGroup)
            {
                __instance.ToolButtonsElement.ToggleDisplayStyle(false);
                ____toolGroupButtonWrapper.RemoveFromClassList(ActiveClassName);
            }

            if(toolGroupEnteredEvent.ToolGroup != ____toolGroup) return false;

            __instance.ToolButtonsElement.ToggleDisplayStyle(true);
            ____toolGroupButtonWrapper.AddToClassList(ActiveClassName);

            return false;
        }

        public static bool OnToolGroupExited(ToolGroupExitedEvent toolGroupExitedEvent, ToolGroupButton __instance, VisualElement ____toolGroupButtonWrapper)
        {
            if(toolGroupExitedEvent.ToolGroup is not ExitingToolGroup) return false;

            __instance.ToolButtonsElement.ToggleDisplayStyle(false);
            ____toolGroupButtonWrapper.RemoveFromClassList(ActiveClassName);

            return false;
        }
    }
}
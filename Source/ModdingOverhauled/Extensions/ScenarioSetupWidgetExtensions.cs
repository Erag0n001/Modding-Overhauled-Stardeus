using System.Reflection;
using Game.Data;
using Game.Scenarios;
using HarmonyLib;

namespace ModdingOverhauled.Extensions;

public static class ScenarioSetupWidgetExtensions
{
    private static readonly MethodInfo ChangeStorygenMethod =
        AccessTools.Method(typeof(ScenarioSetupWidget), "ChangeStorygen");
    private static readonly MethodInfo ChangeDifficultyMethod = AccessTools.Method(typeof(ScenarioSetupWidget), "ChangeDifficulty");
    private static readonly MethodInfo ChangeCommitmentMethod = AccessTools.Method(typeof(ScenarioSetupWidget), "ChangeCommitment");
    private static readonly MethodInfo DoNextMethod = AccessTools.Method(typeof(ScenarioSetupWidget), "DoNext");
    public static void ChangeStorygen(this ScenarioSetupWidget instance, UDB udb, object v)
    {
        ChangeStorygenMethod.Invoke(instance, [udb, v]);
    }

    public static void ChangeDifficulty(this ScenarioSetupWidget instance, UDB udb, object v)
    {
        ChangeDifficultyMethod.Invoke(instance, [udb, v]);
    }
    
    public static void ChangeCommitment(this ScenarioSetupWidget instance, UDB udb, object v)
    {
        ChangeCommitmentMethod.Invoke(instance, [udb, v]);
    }

    public static void DoNext(this ScenarioSetupWidget instance)
    {
        DoNextMethod.Invoke(instance, null);
    }
}
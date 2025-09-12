using System.Reflection;
using Game.Scenarios;
using Game.Systems.Space;
using HarmonyLib;

namespace ModdingOverhauled.Extensions;

public static class SpaceSetupWidgetExtensions
{
    private static readonly MethodInfo NextMethod = AccessTools.Method(typeof(SpaceSetupWidget), "Next");
    private static readonly FieldInfo SpaceMapVizGetter = AccessTools.Field(typeof(SpaceSetupWidget), "spaceMapViz");
    private static readonly MethodInfo DoNextMethod = AccessTools.Method(typeof(SpaceSetupWidget), "DoNext");
    
    public static void Next(SpaceSetupWidget instance)
    {
        NextMethod.Invoke(instance, null);
    }

    public static SpaceMapViz SpaceMapViz(this SpaceSetupWidget instance)
    {
        return SpaceMapVizGetter.GetValue(instance) as SpaceMapViz;
    }
    
    public static void DoNext(this SpaceSetupWidget instance)
    {
        DoNextMethod.Invoke(instance, null);
    }
}
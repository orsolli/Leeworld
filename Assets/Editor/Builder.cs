using UnityEditor;

namespace Leeworld
{
    public static class Builder
    {
        public static void BuildProject()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Login.unity", "Assets/Scenes/MainMenu.unity", "Assets/Scenes/OpenWorld.unity" },
                target = BuildTarget.WebGL,
                locationPathName = "Build/WebGL",
            };

            BuildPipeline.BuildPlayer(options);
        }
    }
}
using BepInEx;
using BepInEx.IL2CPP;
using System.IO;
using System.Linq;

namespace AvatarList
{
    [BepInPlugin(GUID, "AvatarList", "0.2.0")]
    [BepInDependency(KiraiMod.Core.Plugin.GUID)]
    public class Plugin : BasePlugin
    {
        public const string GUID = "com.github.xKiraiChan.AvatarList";

        public override void Load()
        {
            if (!Directory.Exists("BepInEx/config/AvatarLists"))
                Directory.CreateDirectory("BepInEx/config/AvatarLists");

            AvatarList.OnReady += () =>
            {
                foreach (string file in Directory.EnumerateFiles("BepInEx/config/AvatarLists", "*.list")
                    .Reverse() 
                    .Select(x => Path.GetFileNameWithoutExtension(x))
                ) new AvatarList(file);
            };
        }
    }
}

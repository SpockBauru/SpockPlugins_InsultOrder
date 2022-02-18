using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.ComponentModel;

namespace IO_MirrorResolution
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_BetterMiorror", "IO_MirrorResolution", Version)]
    public class IO_MirrorResolution : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "1.0";

        // User Configurations
        private static ConfigEntry<MirrorRes> Resolutions;

        public IO_MirrorResolution()
        {
            // Config Panel Settings
            Resolutions = Config.Bind("Settings", "Mirror Resolution", MirrorRes.r1024, "Set according your screen resolution. May be heavy on both GPU and vRAM");

            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(IO_MirrorResolution));
        }

        //=======================================Configuration Manager=======================================
        // User can define in which place he wants to play the animation
        private enum MirrorRes
        {
            [Description("8x8: Better disable mirror in settings...")]
            r8 = 8,
            [Description("16x16: But why?")]
            r16 = 16,
            [Description("32x32: Big Blurs")]
            r32 = 32,
            [Description("64x64: Mosaic Glass")]
            r64 = 64,
            [Description("128x128: Potato PC")]
            r128 = 128,
            [Description("256x256: Game Defaults")]
            r256 = 256,
            [Description("512x512: HD resolutions")]
            r512 = 512,
            [Description("1024x1024: FullHD Resolution")]
            r1024 = 1024,
            [Description("2048x2048: 4k Resolution")]
            r2048 = 2048,
            [Description("4096x4096: Are you really sure?")]
            r4096 = 4096
        }

        //======================================= Hooks =======================================
        [HarmonyPrefix, HarmonyPatch(typeof(MirrorReflection), "CreateMirrorObjects")]
        public static void SetMirrorResolution(ref int ___m_TextureSize)
        {
            ___m_TextureSize = (int)Resolutions.Value;
        }
    }
}

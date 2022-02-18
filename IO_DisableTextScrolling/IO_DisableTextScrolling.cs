using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace IO_DisableTextScrolling
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_DisableTextScrolling", "IO_DisableTextScrolling", Version)]
    public class IO_DisableTextScrolling : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "1.1";

        // User Configurations
        private static ConfigEntry<bool> Enabled;

        public IO_DisableTextScrolling()
        {
            // Config Panel Settings
            Enabled = Config.Bind("General", "Enabled", false, "Whether the plugin is enabled");

            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(IO_DisableTextScrolling));
        }

        //===============================================Hooks===============================================
        //Disabling flag for text scrolling
        [HarmonyPostfix, HarmonyPatch(typeof(ADV_Loader), "LineLoad")]
        private static void DisableScrollling(string ___Text, ref int ___m_textPos)
        {
            if (Enabled.Value)
                ___m_textPos = ___Text.Length;
        }
    }
}

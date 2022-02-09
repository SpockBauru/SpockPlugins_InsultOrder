using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace IO_Demosaic
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_demosaic", "IO_Demosaic", Version)]
    public class IO_Demosaic : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "1.0";
        public IO_Demosaic()
        {
            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(IO_Demosaic));
        }

        //===============================================Hooks===============================================

        //Disabling flag for anal mosaic
        [HarmonyPrefix, HarmonyPatch(typeof(MozaicSetUp), "Start")]
        private static void AnalDemosaic()
        {
            ConfigClass.AnalMoza = false;
        }

        // Disable Mosaic Object
        [HarmonyPostfix, HarmonyPatch(typeof(MozaicSetUp), "Update")]
        private static void CharacterDemosaic(Renderer ___MozaObj)
        {
            ___MozaObj.gameObject.SetActive(false);
        }
    }
}
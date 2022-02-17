using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class EffectMgr
    {
        public static bool Enabled;
        public static Configs Users = new Configs();

        internal static Configs BkupOnLoad = new Configs();
        internal static Configs Getval = new Configs();

        [Serializable]
        public class Configs
        {
            public bool UpdateEnabled = false;

            public float BloomAndFlares_bloomIntensity = 1.5f;
            public float BloomAndFlares_bloomThreshold = 0.5f;
            public int BloomAndFlares_bloomBlurIterations = 3;
            public float VignetteAndChromaticAberration_intensity = 0.13f;
            public Color DirectionalLight_color = new Color(0.9f,0.9f,0.9f,1f);
            public float DirectionalLight_shadowStrength = 1f;

            public Configs()
            {
            }

            public bool Save()
            {
                if (!EffectMgr.isActive())
                    return false;

                try
                {
                    var MainCamera = Camera.main.transform;
                    if (!MainCamera)
                        return false;

                    {
                        var comp = bloomAndFlares;
                        this.BloomAndFlares_bloomIntensity = comp.bloomIntensity;
                        this.BloomAndFlares_bloomThreshold = comp.bloomThreshold;
                        this.BloomAndFlares_bloomBlurIterations = comp.bloomBlurIterations;
                    }
                    {
                        var comp = vignette;
                        this.VignetteAndChromaticAberration_intensity = comp.intensity;
                    }
                    {
                        var comp = mainLight;
                        this.DirectionalLight_color = comp.color;
                        this.DirectionalLight_shadowStrength = comp.shadowStrength;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("エフェクト設定の取得に失敗" + e);
                    return false;
                }
                return true;
            }

            public bool Load()
            {
                if (!EffectMgr.isActive())
                    return false;
                try
                {
                    {
                        var comp = bloomAndFlares;
                        comp.bloomIntensity =this.BloomAndFlares_bloomIntensity;
                        comp.bloomThreshold = this.BloomAndFlares_bloomThreshold;
                        comp.bloomBlurIterations = this.BloomAndFlares_bloomBlurIterations;
                    }
                    {
                        var comp = vignette;
                        comp.intensity = this.VignetteAndChromaticAberration_intensity;
                    }
                    {
                        var comp = mainLight;
                        comp.color = this.DirectionalLight_color;
                        comp.shadowStrength = this.DirectionalLight_shadowStrength;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("エフェクトの設定に失敗" + e);
                    return false;
                }
                return true;
            }
        }

        static readonly Dictionary<string, string> trans2Disp = new Dictionary<string, string> 
        {
            { "BloomAndFlares", "ブルーム" },
            { "bloomIntensity", "強度" },
            { "bloomThreshold", "閾値" },
            { "bloomBlurIterations", "ブラー反復" },
            { "VignetteAndChromaticAberration", "ビネット" },
            { "intensity", "強度" },
            { "DirectionalLight", "光源" },
            { "color", "色" },
            { "shadowStrength", "影強さ" },
        };

        public static string TranslateToDisp(string fieldname)
        {
            foreach (var p in trans2Disp)
            {
                fieldname = fieldname.Replace(p.Key, p.Value);
            }

            return fieldname;
        }

        public static bool isActive()
        {
            if (actScene.name == "Title")
                return false;

            var cam = Camera.main;
            return bloomAndFlares && vignette && mainLight;

        }

        static UnityStandardAssets.ImageEffects.BloomAndFlares bloomAndFlares;
        static UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration vignette;
        static Light mainLight;
        static bool init = false;
        static int retry;

        static string prevScnName;

        public static void OnUpdate()
        {
            if (actScene.name != prevScnName)
            {
                prevScnName = actScene.name;
                init = false;
                retry = 50;
            }

            if (actScene.name == "Title")
                return;

            if (!Enabled)
                return;

            if (!init)
            {
                if (retry-- < 0)
                {
                    Debug.LogError("エフェクトマネージャ初期設定に失敗");
                    init = true;
                }

                try
                {
                    // ここのFindはシーン開始時のみなのでキャッシュしない
                    var Camera_Main = GameObject.Find("Camera_Main");//Camera.main.transform;
                    if (!Camera_Main)
                        return;
                    {
                        var go = Camera_Main.transform.Find("Camera_Chara");
                        if (!go) return;

                        bloomAndFlares = go.GetComponent<UnityStandardAssets.ImageEffects.BloomAndFlares>();
                        vignette = go.GetComponent<UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration>();
                    }
                    {
                        var go = Camera_Main.transform.Find("Directional Light");
                        if (!go) return;
                        
                        mainLight = go.GetComponent<Light>(); 
                    }
                }
                catch (Exception e)
                {
                    //Debug.LogError("エフェクトマネージャ初期設定に失敗" + e);
                    return;
                }

                if (isActive())
                {
                    BkupOnLoad.Save();
                    init = true;
                }
            }

            if (!isActive())
                return;

            if (Users.UpdateEnabled)
                Users.Load();

            Getval.Save();
        }

    }
}

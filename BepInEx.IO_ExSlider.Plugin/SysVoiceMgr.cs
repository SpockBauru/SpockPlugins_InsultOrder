using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BepInEx.IO_ExSlider.Plugin
{
    public static class SysVoiceMgr
    {
        public static bool Enabled = false; 
        public static float[] voicePich = new float[2] { 1f, 1f };
        public static string[] lastPlayVoice = new string[2] { "", "" }; // テスト用、とりあえずピッチ変更有効時＆Hシーンのみ

        static string[] headPaths =
        {
            "CH01/CH0001/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head",
            "CH02/CH0002/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head",
        };

        static string headPath2_02 = "CH02/CH0002/HS_kiten_02/bip01_02/bip01 Pelvis_02/bip01 Spine_02/bip01 Spine1_02/bip01 Spine2_02/bip01 Neck_02/bip01 Head_02/HS_HeadScale_02/HS_Head_02";

        public static void OnUpdate(bool forced = false)
        {
            if (!Enabled && forced)
                return;

            if (IO_ExSlider.actScene.name.StartsWith("ADV", StringComparison.Ordinal))
            {
                OnADV();
                return;
            }
            else if (IO_ExSlider.actScene.name == "Custom")
            {
                return;
            }

            if (Free3PVoicePlayer.Inst)
            {
                var vp = Free3PVoicePlayer.Inst;
                if (vp.audioVo && vp.girlCtrl != null && vp.girlCtrl.ini.isGirl())
                    setVoicePitch(vp.audioVo, vp.girlCtrl.ini.id == "Neko" ? 0 : 1);
            }

            for (int i=0; i < headPaths.Length; i++)
            {
                if (!forced && voicePich[i] == 1)
                    continue;

                var s = headPaths[i];
                if (i == 1 && !IO_ExSlider.FlagUsaFH)
                    s = headPath2_02;   // 前戯など

                var obj = Extentions.FindSp(s);
                if (!obj)
                    continue;

                // IC、UC
                if (IO_ExSlider.actScene.name.EndsWith("C", StringComparison.Ordinal))
                {
                    for (int n = 0; n < obj.transform.childCount; n++)
                    {
                        var c = obj.transform.GetChild(n);
                        if (c.name == "SoundUnit_")
                            setVoicePitch(c.gameObject, i);
                    }
                }
                else
                {
                    // FH
                    setVoicePitch(obj, i);
                }

                // 複数存在する可能性があるのでだめぽ
                //var su = obj.transform.FindSp("SoundUnit_");
                //if (su)
                //{
                //    setVoicePitch(su.gameObject, i);
                //}
            }
        }

        static void setVoicePitch(GameObject go, int index)
        {
            var comps = go.GetComponents<AudioSource>();
            if (comps != null)
            {
                foreach (var c in comps)
                {
                    setVoicePitch(c, index);
                }
            }
        }

        static void setVoicePitch(AudioSource c, int index)
        {
            if (c.pitch != voicePich[index])
                c.pitch = voicePich[index];

            if (c.isPlaying && c.volume > 0 && c.clip && c.clip.name.Length > 3)
                lastPlayVoice[index] = c.clip.name;
        }

        static void setVoicePitch(AudioSource c, float val)
        {
            if (c.pitch != val)
                c.pitch = val;
        }


        public static GameObject GetGirlHead(int index)
        {
            int i = index;
            var s = headPaths[i];
            if (i == 1 && !IO_ExSlider.FlagUsaFH)
                s = headPath2_02;   // 前戯など

            return Extentions.FindSp(s);
        }

        public static AudioSource setupGirlAudio(int index)
        {
            var obj = GetGirlHead(index);
            if (!obj)
                return null;

            //var audio = new GameObject("Plugin-VoicePlayer").AddComponent<AudioSource>(); // 音量は別で同期
            //audio.transform.SetParent(obj.transform, false);

            // 混ざらないように一つ親
            var audio = obj.transform.parent.gameObject.AddComponent<AudioSource>(); // 音量は別で同期
            audio.priority = 0;
            audio.dopplerLevel = 0f;
            audio.minDistance = 20f;
            audio.maxDistance = 300f;
            audio.loop = true;

            // 要同期項目
            audio.spatialBlend = ConfigClass.Blend3D;
            if (index == 0)
                audio.volume = Mathf.Max(0.001f, ConfigClass.Neco_out);
            else if (index == 1)
                audio.volume = Mathf.Max(0.001f, ConfigClass.Tomoe_out);

            return audio; // 音量は別で同期

            //// 音量調整のためのユニットを呼ぶ
            //var res = Resources.Load<GameObject>("SoundUnit");
            //var su = GameObject.Instantiate(res);
            //su.name = su.name.Replace(" (Clone)", " (Plg)");
            //su.transform.SetParent(obj.transform, false);

            //return su.GetComponent<AudioSource>();

            // IC、UC
            var su = obj.transform.FindSp("SoundUnit_");
            if (su)
                return su.GetComponent<AudioSource>();

            // FH
            return obj.GetComponent<AudioSource>() ?? obj.AddComponent<AudioSource>();
        }

        public static void OnADV()
        {
            var adv = GameObject.Find("MainSystem").GetComponent<ADV_Loader>();
            if (!adv)
                return;

            var audiVoice = adv.Voice;
            
            if (!audiVoice.isPlaying || audiVoice.clip == null || !audiVoice.clip.name.StartsWith("CH0", StringComparison.OrdinalIgnoreCase))
            {
                setVoicePitch(audiVoice, 1f);
            }
            else
            {
                var sid = audiVoice.clip.name.Substring(2, 2);
                if (sid == "01") // "CH01"
                {
                    setVoicePitch(audiVoice, voicePich[0]);
                }
                else if (sid == "02") // "CH02"
                {
                    setVoicePitch(audiVoice, voicePich[1]);
                }
                else
                    setVoicePitch(audiVoice, 1f);
            }


            audiVoice = adv.VoiceLoop;

            if (!audiVoice.isPlaying || audiVoice.clip == null || audiVoice.clip.name.Length < "LoopCV_00".Length)
            {
                setVoicePitch(audiVoice, 1f);
            }
            else
            {
                var name = audiVoice.clip.name.Substring(0, "LoopCV_00".Length);

                if (name.EndsWith("00", StringComparison.Ordinal))
                {
                    setVoicePitch(audiVoice, voicePich[0]);
                }
                else if (name.EndsWith("01", StringComparison.Ordinal))
                {
                    setVoicePitch(audiVoice, voicePich[1]);
                }
                else
                    setVoicePitch(audiVoice, 1f);
            }
        }
    }
}

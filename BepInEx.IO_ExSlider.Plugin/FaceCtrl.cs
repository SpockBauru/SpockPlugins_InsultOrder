using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class FaceCtrl
    {
        GirlCtrl girlCtrl;
        public bool enabled = true; // 女性キャラは常にONのこと
        FaceMotion faceMotion;
        Animator animator;
        public Animator getAnimator => animator;
        public Dictionary<GameObject, bool> activateItems = new Dictionary<GameObject, bool>();

        public FaceCtrl(GirlCtrl girlCtrl)
        {
            this.girlCtrl = girlCtrl;

            if (girlCtrl.ini.isNotGirl())
            {
                enabled = false;
                return;
            }
        }

        public bool isReady()
        {
            return animator && faceMotion;
        }

        public void Init()
        {
            animator = girlCtrl.FindModel().GetComponent<Animator>();
            faceMotion = girlCtrl.ini.rootObj.GetComponent<FaceMotion>();
        }

        public string[] EyeNames
        {
            get => faceMotion.GetNonPublicField<string[]>("EY01");
        }

        public string[] MouthNames1
        {
            get => faceMotion.GetNonPublicField<string[]>("MO01_0");
        }

        public string[] MouthNames2
        {
            get => faceMotion.GetNonPublicField<string[]>("MO01_1");
        }

        private void setAnime(string name, int layer)
        {
            this.animator.CrossFade(name, faceMotion.FadeSpeed, layer);
        }

        private string getAnime(int layer)
        {
            var hash = this.animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            string[] list = null;
            switch (layer)
            {
                case 1:
                    list = EyeNames;
                    break;

                case 2:
                    list = MouthNames1;
                    break;

                case 3:
                    list = MouthNames2;
                    break;

            }

            return list.FirstOrDefault(x => Animator.StringToHash(x).Equals(hash));
        }

        public string Eye
        {
            set { setAnime(value, 1); }
            get {
                    return getAnime(1);
                }
        }

        public string Mouth1
        {
            set { setAnime(value, 2); }
            get
            {
                return getAnime(2);
            }
        }

        public string Mouth2
        {
            set { setAnime(value, 3); }
            get
            {
                return getAnime(3);
            }
        }

        public float Cheek
        {
            set { faceMotion.FaceMats.ForEach(x => x.SetFloat("_CheekAlpha", value)); }
            get
            {
                return faceMotion.FaceMats.First(x => x.HasProperty("_CheekAlpha")).GetFloat("_CheekAlpha");
            }
        }

        public bool ToothOpen
        {
            set
            {
                this.animator.SetBool("Tooth", value);
                GameClass.ToothOpen = value;
            }
            get
            {
                return this.animator.GetBool("Tooth");
            }
        }


        static readonly TransformData offsetVibe = new TransformData(new Vector3(0, 0.55f, 0), Quaternion.Euler(new Vector3(-90f, 0, 0)), Vector3.zero);
        static readonly TransformData offsetAna = new TransformData(new Vector3(0, 0, -0.7f), Quaternion.identity, Vector3.zero);

        public void OnUpdate()
        {
            if (activateItems.Count > 0)
            {
                // v0.92
                //activateItems.ForEach(x =>
                activateItems.ForEach( (x, y) =>
                {
                    //if (x) x.SetActive(true);
                    if (x) x.SetActive(y);
                    
                    if (x && x.name == "IT02_Gyagbole" && !GameClass.Gyagbole)
                    {
                        if (animator.layerCount > 7)
                        {
                            if (y)
                                this.animator.SetLayerWeight(7, 100f);
                            else
                                this.animator.SetLayerWeight(7, 0f);
                        }
                    }
                    if (!y) return;

                    if (x && (actScene.name == "IC" || actScene.name == "Custom"))
                    {
                        Transform bone_ana = null;

                        if (girlCtrl.ini.id == "Neko")
                            bone_ana = girlCtrl.FindBone().transform.FindSp("bip01/bip01 Pelvis/HS01_anaru");
                        else
                            bone_ana = girlCtrl.FindBone().transform.FindSp("bip01_02/bip01 Pelvis_02/HS01_anaru_02");

                        // バイブ位置補正
                        if (x.name == "IT10_baibu")
                        {
                            var skmr = x.GetComponent<SkinnedMeshRenderer>();

                            var root = skmr.rootBone;

                            if (!root.name.StartsWith("HS", StringComparison.Ordinal))
                            {
                                root = root.parent;
                            }
                            //if (Vector3.Distance(root.position, bone_pus.position) > 0.10f
                            //    && Vector3.Distance(root.position, bone_ana.position) > 0.10f)
                            //
                            {
                                root.rotation = bone_ana.rotation * offsetVibe.rotation;

                                var offset = (skmr.rootBone.position - root.position);
                                root.position = bone_ana.position - offset + root.rotation * (offsetVibe.position + new Vector3(UnityEngine.Random.Range(-0.015f, 0.015f), UnityEngine.Random.Range(0, 0.1f), UnityEngine.Random.Range(-0.015f, 0.015f)));
                            }
                        }
                        if (x.name == "IT13_anapa")
                        {
                            var skmr = x.GetComponent<SkinnedMeshRenderer>();
                            var root = skmr.rootBone;
                            if (!root.name.StartsWith("HS", StringComparison.Ordinal))
                            {
                                root = root.parent;
                            }
                            //if (Vector3.Distance(root.position, bone_ana.position) > 0.3f)
                            {
                                root.rotation = bone_ana.rotation * offsetAna.rotation;
                                root.position = bone_ana.position + root.rotation * (offsetAna.position + new Vector3(UnityEngine.Random.Range(-0.01f, 0.01f), UnityEngine.Random.Range(-0.01f, 0.01f), UnityEngine.Random.Range(-0.02f, 0.05f)));
                            }
                        }
                    }

                });
            }


            if (!enabled)
                return;

            if (!animator || !faceMotion)
                Init();
        }

        public void OnNewScene()
        {
            activateItems.Clear();
        }
    }
}

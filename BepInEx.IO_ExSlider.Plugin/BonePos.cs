using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using OhMyGizmo2;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using System.Text.RegularExpressions;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class BonePos
    {
        //public static OhMyGizmo gizmoGirlPos;

        public static void gizmoGirlPosInit()
        {
            //if (!gizmoGirlPos)
            //{
            //    gizmoGirlPos = OhMyGizmo.AddGizmo(null, "PlgGirlPos");
            //    gizmoGirlPos.modeRot = true;
            //    gizmoGirlPos.modePos = true;
            //    gizmoGirlPos.visible = false;
            //    gizmoGirlPos.sizeRot = 4f;
            //    gizmoGirlPos.sizeHandle = 10f;
            //    gizmoGirlPos.threthold = 20;
            //    gizmoGirlPos.undoDrag = true;
            //}
        }

        public string rootObjPath = "CH01/CH0001/HS_kiten"; // 仮

        //初期値
        public readonly List<StrV3Pair> girl_bPositions0 = new List<StrV3Pair>()
        {
        };

        public Dictionary<string, Vector3> dicNPositions = new Dictionary<string, Vector3>();

        public Transform transform
        {
            get
            {
                var go = Extentions.FindSp(rootObjPath);
                return go ? go.transform : null;
            }
        }

        GirlCtrl girlCtrl;
        TargetIni.TargetDics targetDics;

        //保存用
        public Edits edits;

        [Serializable]
        public class Edits
        {
            BonePos _bonePos;
            public BonePos getControl() { return _bonePos; }
            public void setControl(BonePos value) { _bonePos = value; }

            public List<StrV3Pair> girl_bPos = new List<StrV3Pair>();

            // 位置調整
            //public Vector3 girl_dpos = new Vector3(0, 0, 0);
            //public Vector3 girl_drot = new Vector3(0, 0, 0);

            public Edits()
            {
            }
            public Edits(BonePos bonePos)
            {
                this._bonePos = bonePos;
            }

            //public void Load(Edits edits)
            //{
            //    this.girl_bScales = edits.girl_bScales;
            //    this.girl_dpos = edits.girl_dpos;
            //    this.girl_drot = edits.girl_drot;
            //}

            public void setupPos()
            {
                var go = Extentions.FindSp(this._bonePos.rootObjPath);
                Transform gtr = null;
                if (go)
                {
                    gtr = go.transform;

                    // ボーン指定があれば一度リセット
                    _bonePos.resetBoneLocpos();
                }

                var tmp = girl_bPos;
                girl_bPos = new List<StrV3Pair>();
                foreach (var v in _bonePos.girl_bPositions0)
                {
                    var hit = tmp.FirstOrDefault(x => x.Key == v.Key);
                    //if (hit.Equals(default(StrV3Pair)))
                    if (hit == null)
                    {
                        girl_bPos.Add(new StrV3Pair(string.Empty, v.Value));
                    }
                    else
                    {
                        girl_bPos.Add(new StrV3Pair(v.Key, hit.Value));

                        _bonePos.dicNPositions[v.Key] = v.Value;
                        if (gtr)
                        {
                            var tr = gtr.FindSp(v.Key, true);
                            if (tr)
                                _bonePos.dicNPositions[v.Key] = tr.localPosition;
                        }
                    }
                }

                // ユーザーがiniで追加した分は後で足す
                foreach (var v in tmp)
                {
                    if (string.IsNullOrEmpty(v.Key))
                        continue;
                    var hit = _bonePos.girl_bPositions0.FirstOrDefault(x => x.Key == v.Key);
                    //if (hit.Equals(default(StrV3Pair)))
                    if (hit == null)
                    {
                        girl_bPos.Add(new StrV3Pair(v.Key, v.Value));

                        // このシーンにないボーンなのでデフォ値は持たない
                        _bonePos.girl_bPositions0.Add(new StrV3Pair(v.Key, v.Value));
                    }
                }
            }
        }

        public BonePos(GirlCtrl girlCtrl, string rootObjPath)
        {
            this.girlCtrl = girlCtrl;
            this.targetDics = girlCtrl.ini.dics;

            gizmoGirlPosInit();
            Init(rootObjPath);
        }

        public void Init(string rootObjPath)
        {
            this.rootObjPath = rootObjPath;
            this.edits = new Edits(this);

        }

        public void OnUpdate()
        {
            gizmoGirlPosInit();
        }

        public static void setBoneLocpos(Transform gtr, string bone, Vector3 pos)
        {
            var tr = gtr.FindSp(bone);
            if (tr)
                tr.localPosition = pos;
        }

        // ボーンリスト作成
        void boneDicInit(GameObject ggo)
        {
            loop("", ggo.transform);
            
            foreach (var tgt in girlCtrl.ini.dics.extraFindSpTarget)
            {
                var s = tgt;
                bool addself = true;
                if (s.EndsWith("/", StringComparison.Ordinal))
                {
                    addself = false;
                    s = s.Substring(0, s.Length - 1);
                }

                var get = ggo.transform.FindSp(s, true);
                if (get)
                {
                    if (addself)
                        add2dic(s, get);
                    else
                        add2dic_loop(s, get);
                }
            }

            // セットアップ
            edits.setupPos();

            void add2dic(string path, Transform tr)
            {
                if (girl_bPositions0.Any(x => x.Key == path))
                    return;

                girl_bPositions0.Add(new StrV3Pair(path, tr.localPosition));
                if (this.edits.girl_bPos.Count < girl_bPositions0.Count)
                    this.edits.girl_bPos.Add(new StrV3Pair("", tr.localPosition));
            }
            void add2dic_loop(string path, Transform get)
            {
                string tmp;
                foreach (Transform c in get)
                {
                    add2dic(tmp = $"{path}/{c.name}", c);
                    add2dic_loop(tmp, c);
                }
            }

            void loop(string path, Transform tr)
            {
                //for (int i = 0; i < tr.childCount; i++)
                foreach (Transform tr2 in tr)
                {
                    //var tr2 = tr.GetChild(i);

                    //if (tr2.name.StartsWith("bip01 L", StringComparison.Ordinal))
                    //if (this.targetDics.bpsIgnoreObjNamesRE.Any(x => tr2.name.Contains(x)))
                    if (this.targetDics.bpsIgnoreObjNamesRE.Any(x => CacheRegex.IsMatch(tr2.name, x)))
                            continue;

                    var tr2name = this.RevTransNameUsaBonesFH(tr2.name);

                    var newpath = $"{path}/{tr2name}";
                    if (path == "")
                        newpath = $"{tr2name}";
                    
                    //Extentions.RevTransNameUsaBonesFH(ref newpath);

                    //if (!tr2.name.StartsWith("bip", StringComparison.Ordinal))
                    //continue;
                    //if (this.targetDics.bpsValidObjNamesSW.Any(x => x.Value.Any(y => tr2.name.StartsWith(y, StringComparison.Ordinal))))
                    //if (this.targetDics.bpsValidObjNamesSW.Any(x => x.Value.Any(y => CacheRegex.IsMatch(tr2.name, "^" + y))))
                    if (this.targetDics.checkBPosObjNameValid(tr2.name))
                    {
                        girl_bPositions0.Add(new StrV3Pair(newpath, tr2.localPosition));
                        if (this.edits.girl_bPos.Count < girl_bPositions0.Count)
                            this.edits.girl_bPos.Add(new StrV3Pair("", tr2.localPosition));

                        //Debug.Log($"{newpath}  {tr2.localScale}  {tr2.lossyScale}");
                    }
                    loop(newpath, tr2);
                }
            }
        }

        public string RevTransNameUsaBonesFH(string name)
        {
            if (IO_ExSlider.FlagUsaFH && girlCtrl.ini.id == "Usa")
            {
                if (name.StartsWith("BP01_aniEar", StringComparison.Ordinal))
                {
                    if (!name.EndsWith("_02", StringComparison.Ordinal))
                        name = name + "_01";
                    return name;
                }

                if (name.StartsWith("BP01_tail", StringComparison.Ordinal))
                {
                    return name;
                }

                //Console.Write(name);
                if (!name.EndsWith("(Clone)", StringComparison.Ordinal)
                    && !name.StartsWith("Particle", StringComparison.Ordinal)) // Particle
                    name = name + "_02";
            }
            return name;
        }


        public void OnLateUpdate()
        {
            var ggo = Extentions.FindSp(rootObjPath);
            if (!ggo)
                return;

            if (girl_bPositions0.Count <= 0)
            {
                boneDicInit(ggo);
            }

            ProcBonePos(ggo, this.edits);
        }

        public static void ProcBonePos(GameObject ggo, BonePos.Edits edits)
        {
            var dic = edits.girl_bPos;

            for (int i = 0; i < dic.Count; i++)
            {
                var v = dic[i];

                if (string.IsNullOrEmpty(v.Key))
                    continue;

                var tr = ggo.transform.FindSp(v.Key);
                if (!tr)
                    continue;

                tr.localPosition = v.Value;
            }
        }

        // ボーンスケールリセット
        public void ResetBonePos()
        {
            var go = Extentions.FindSp(rootObjPath);

            foreach (var s in dicNPositions)
            {
                setBoneLocpos(go.transform, s.Key, s.Value);
            }
            dicNPositions.Clear();

            this.girl_bPositions0.Clear();
            this.edits.girl_bPos.Clear();
        }

        //public void ResetPosRot()
        //{
        //    this.edits.girl_dpos = Vector3.zero;
        //    this.edits.girl_drot = Vector3.zero;
        //}

        // リセット（内部用)
        private void resetBoneLocpos()
        {
            var go = Extentions.FindSp(rootObjPath);

            foreach (var s in dicNPositions)
            {
                setBoneLocpos(go.transform, s.Key, s.Value);
            }
            dicNPositions.Clear();
        }

        public void ResetBoneLocposTemp()
        {
            var go = Extentions.FindSp(rootObjPath);

            foreach (var s in dicNPositions)
            {
                setBoneLocpos(go.transform, s.Key, s.Value);
            }
        }

        public void LoadEdits(Edits edits)
        {
            this.edits.setControl(null);

            this.edits = edits;
            this.edits.setControl(this);
            this.edits.setupPos();
        }
    }

    // 実行順として揺れボーン問題回避のため体形調整はLateUpdateの最後にやるのでパーティクルの位置がずれるから修正
    // 　→ 揺れ処理前に体形調整できるようになったが、Update～LateUpdate間のUnity側アニメ処理との干渉を避けるためやっぱり必須
    // １フレーム分のずれは許容
    public class FixParticlesPos
    {
        // 設定項目
        public float particlesScale 
        { get => girlCtrl.boneScales.edits.fixParticlesScale; set => girlCtrl.boneScales.edits.fixParticlesScale = value; }

        public GirlCtrl girlCtrl;
        ParticleSystem[] particles;

        public ParticleSystem[] GetParticles => particles;

        Dictionary<Transform, Offset> posdata = new Dictionary<Transform, Offset>();
        Dictionary<Transform, Offset> posdata2 = new Dictionary<Transform, Offset>();
        Dictionary<Transform, Vector3> scales = new Dictionary<Transform, Vector3>();


        public void OnNewScene()
        {
            //return;
            posdata.Clear();
            posdata2.Clear();
            scales.Clear();

            if (!girlCtrl.FindModel())
                return;

            //particles = girlCtrl.FindBone().GetComponentsInChildren<ParticleSystem>();
            particles = girlCtrl.FindModel().GetComponentsInChildren<ParticleSystem>();
        }

        // プラグイン無効処理用
        public void OnDisabled()
        {
            if (particles == null)
                return;

            // 復元処理
            LateUpdate_3();

            foreach (var p in posdata)
            {
                if (!p.Key)
                    continue;

                var lla = p.Key.parent.GetComponent<LiquidLoolAt>();
                if (lla)
                {
                    // 母乳のみのはず
                    lla.enabled = true;
                }
            }
        }

        GameObject gotemp;
        Dictionary<Transform, Transform> parents = new Dictionary<Transform, Transform>();
        //public FixParticlesPosComp fixParticlesPosComp;

        // 色々やってみた結果、パーティクルはアニメ処理時にボーンヒエラルキー中にあると
        // 体形カスタムに対応できないっぽいという結論に
        // （ミルクだけGetVertexで後付けしてる理由と同じかも？）
        // 試作3
        public void Update_3pre()
        {
            //Debug.Log(girlCtrl.ini.id + " OnUpdate " + Time.frameCount);

            if (particles == null)
                return;

            foreach (var p in posdata)
            {
                if (!p.Key)
                    continue;

                var lla = p.Key.parent.GetComponent<LiquidLoolAt>();
                if (lla)
                {
                    // 母乳のみのはず
                    // コンポーネントを無効にして手動更新
                    miLLAUpdata.Invoke(lla, null);
                    lla.enabled = false;
                }
            }
        }

        public void Update_3post()
        {
            //Debug.Log(girlCtrl.ini.id + " OnUpdate " + Time.frameCount);

            // アニメーターと同時くらいで再生されるパーティクル向け
            if (particles == null)
                return;

            if (parents.Count > 0)
            {
                // エラーなどで復元されてない
                LateUpdate_3();
            }

            posdata2.Clear();
            scales.Clear();
            foreach (var p in particles)
            {
                scales[p.transform] = p.transform.localScale;
                posdata2[p.transform] = new Offset(p.transform.localPosition, p.transform.localEulerAngles);
            }

            foreach (var p in posdata)
            {
                if (!p.Key)
                    continue;

                if (!gotemp)
                {
                    gotemp = new GameObject("plgParticlesHolder");
                    gotemp.transform.localScale = (Vector3.one * 0.1f); // ローカルスケールしかみてないっぽい
                }

                //var lla = p.Key.parent.GetComponent<LiquidLoolAt>();
                //if (!lla)
                if (!p.Key.parent.GetComponent<ParticleSystem>()) // Pool等を除外
                {
                    // ボーンアニメに位置が影響されるオブジェクトをヒエラルキーから一時的に外す
                    parents[p.Key] = p.Key.parent;
                    p.Key.SetParent(gotemp.transform, false);
                }

                p.Key.position = p.Value.dpos;
                p.Key.rotation = Quaternion.Euler(p.Value.drot);

                // アニメーション処理時だけスケーリング
                p.Key.localScale *= particlesScale;
            }
        }
        System.Reflection.MethodInfo miLLAUpdata = typeof(LiquidLoolAt).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // 試作3
        public void LateUpdate_3()
        {
            if (particles == null)
                return;

            // 親戻す
            foreach (var p in parents)
            {
                if (!p.Key)
                    continue;

                p.Key.SetParent(p.Value, false);
            }
            parents.Clear();

            // 位置・サイズ戻す
            foreach (var p in posdata2)
            {
                if (!p.Key)
                    continue;

                p.Key.localPosition = p.Value.dpos;
                p.Key.localEulerAngles = p.Value.drot;
            }
            foreach (var p in scales)
            {
                if (!p.Key)
                    continue;

                p.Key.localScale = p.Value;
            }
        }

        // 試作3
        public void LateLateUpdate_3()
        {
            // やることなくなった
            if (particles == null)
                return;
        }


        // 位置関係の処理がすべて終わった後に呼びたい
        // 雰囲気的にOnRenderObjectだが、シーン中カメラ台数分呼ばれて無駄なので疑似
        public void OnRender()
        {
            if (particles == null)
                return;

            posdata.Clear();
            foreach(var p in particles)
            {
                posdata[p.transform] = new Offset(p.transform.position, p.transform.eulerAngles);
            }

            //posdata2.Clear();
            //foreach (var p in particles)
            //{
            //    posdata2[p.transform] = new Offset(p.transform.localPosition, p.transform.localEulerAngles);
            //}

            //// プラグインで変更された位置関係のオフセット量を記録 試作1～2用
            //foreach (var p in particles)
            //{
            //    offsets[p.transform] -= p.transform.position;
            //    offsetsRot[p.transform] -= p.transform.eulerAngles;
            //}
            //Debug.Log(girlCtrl.ini.id + " OnRender " + Time.frameCount);
        }

        public FixParticlesPos(GirlCtrl girlCtrl)
        {
            this.girlCtrl = girlCtrl;
        }


#if LEGACY
        [DisallowMultipleComponent]
        public class FixParticlesPosComp : MonoBehaviour
        {
            public FixParticlesPos fixParticlesPos;

            public static FixParticlesPosComp Add(GameObject tgt, FixParticlesPos fix)
            {
                var comp = tgt.AddComponent<FixParticlesPosComp>();
                comp.fixParticlesPos = fix;
                return comp;
            }

            void Update()
            {
                fixParticlesPos.Update_3();

                // スケーリング戻す
                fixParticlesPos.girlCtrl.boneScales.ResetBoneTemp();
            }
        }

        
        public void Update_3()
        {
            //Debug.Log(girlCtrl.ini.id + " OnUpdate " + Time.frameCount);

            // アニメーターと同時くらいで再生されるパーティクル向け
            if (particles == null)
                return;

            posdata2.Clear();
            foreach (var p in particles)
            {
                posdata2[p.transform] = new Offset(p.transform.localPosition, p.transform.localEulerAngles);
            }

            foreach (var p in posdata)
            {
                if (!p.Key)
                    continue;

                if (!gotemp)
                    gotemp = new GameObject("plgParticlesHolder");

                var lla = p.Key.parent.GetComponent<LiquidLoolAt>();
                if (!lla)
                {
                    // ボーンアニメに影響されるオブジェクトをヒエラルキーから一時的に外す
                    parents[p.Key] = p.Key.parent;
                    p.Key.SetParent(gotemp.transform, true);
                }
                else
                {
                    // 母乳のみのはず
                    // コンポーネントを無効にして手動更新
                    miLLAUpdata.Invoke(lla, null);
                    lla.enabled = false;
                    posdata2[p.Key] = new Offset(p.Key.localPosition, p.Key.localEulerAngles);
                }

                p.Key.position = p.Value.dpos;
                p.Key.rotation = Quaternion.Euler(p.Value.drot);
            }
        }

        Dictionary<Transform, Vector3> offsets = new Dictionary<Transform, Vector3>();
        Dictionary<Transform, Vector3> offsetsRot = new Dictionary<Transform, Vector3>();

        public void PreOffset()
        {
            offsets.Clear();
            foreach (var p in particles)
            {
                offsets[p.transform] = p.transform.position;
            }
        }

        public void Update()
        {
            //Debug.Log(girlCtrl.ini.id + " OnUpdate " + Time.frameCount);

            // アニメーターと同時くらいで再生されるパーティクル向け
            if (particles == null)
                return;

            // 本体の位置や角度、スケールなどがリセットされるのでオフセットで補正
            foreach (var p in offsets)
            {
                if (p.Key)
                    p.Key.position = p.Key.position - p.Value;
            }
            foreach (var p in offsetsRot)
            {
                if (p.Key)
                    p.Key.eulerAngles = p.Key.eulerAngles - p.Value;
            }
        }

        // 試作2
        public void LateUpdate_2()
        {
            if (particles == null)
                return;

            //// 尿、酔い用のオフセット取得用に位置戻す
            //foreach (var p in posdata2)
            //{
            //    if (!p.Key)
            //        continue;

            //    p.Key.localPosition = p.Value.dpos;
            //    p.Key.localEulerAngles = p.Value.drot;
            //}

            //// オフセット基準取得
            //offsets.Clear();
            //offsetsRot.Clear();
            //foreach (var p in particles)
            //{
            //    offsets[p.transform] = p.transform.position;
            //    offsetsRot[p.transform] = p.transform.eulerAngles;
            //}

            // 母乳用に角度補正
            foreach (var p in posdata)
            {
                if (!p.Key)
                    continue;

                p.Key.position = p.Value.dpos;
                p.Key.rotation = Quaternion.Euler(p.Value.drot);
            }
        }

        // 試作2
        public void LateLateUpdate_2()
        {
            if (particles == null)
                return;

            // パーティクルの処理が終わったはずなのでローカル角度戻す
            foreach (var p in posdata2)
            {
                if (!p.Key)
                    continue;
                p.Key.localPosition = p.Value.dpos;
                p.Key.localEulerAngles = p.Value.drot;
            }

            // オフセット基準取得
            offsets.Clear();
            offsetsRot.Clear();
            foreach (var p in particles)
            {
                offsets[p.transform] = p.transform.position;
                offsetsRot[p.transform] = p.transform.eulerAngles;
            }
        }

        // 試作1
        public void LateUpdate()
        {
            if (particles == null)
                return;

            foreach (var p in posdata)
            {
                if (!p.Key)
                    continue;

                p.Key.position = p.Value.dpos;
                p.Key.rotation = Quaternion.Euler(p.Value.drot);
            }
        }

        // 試作1
        public void LateLateUpdate()
        {
            if (particles == null)
                return;

            foreach (var p in posdata2)
            {
                if (!p.Key)
                    continue;

                p.Key.localPosition = p.Value.dpos;
                p.Key.localEulerAngles = p.Value.drot;
            }

            offsets.Clear();
            foreach (var p in particles)
            {
                offsets[p.transform] = p.transform.position;
            }

            //Debug.Log(girlCtrl.ini.id + " OnLateLate " + Time.frameCount);
        }
#endif

    }
}

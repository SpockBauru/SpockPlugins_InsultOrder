
using HarmonyLib;
using OhMyGizmo2;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
//using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider.MyXML;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.PlgIK;
using System.Text;
using DamienG.Security.Cryptography;
using System.Xml.Serialization;
using System.Collections;

namespace BepInEx.IO_ExSlider.Plugin
{
    public partial class IO_ExSlider : BaseUnityPlugin
    {
        public class SaveGirlExPresets
        {
            public string charID = "";
            public BoneScales.Edits boneScales = new BoneScales.Edits();
            public SaveShapeKeys saveShapeKeys = new SaveShapeKeys();
            public SaveMaterials saveMateProps = new SaveMaterials();

            public SaveGirlExPresets()
            {
            }

            public SaveGirlExPresets(GirlCtrl girlCtrl)
            {
                Save(girlCtrl);
            }

            public void Save(GirlCtrl girlCtrl)
            {
                this.charID = girlCtrl.ini.id;
                this.boneScales = girlCtrl.boneScales.edits;

                if (girlCtrl.shapeKeys != null)
                {
                    this.saveShapeKeys = new SaveShapeKeys(girlCtrl.shapeKeys.edits);
                }
                else
                    this.saveShapeKeys = new SaveShapeKeys();

                if (girlCtrl.mateProp != null)
                    this.saveMateProps = new SaveMaterials(girlCtrl.mateProp);
                else
                    this.saveMateProps = new SaveMaterials();
            }

            public void Load(GirlCtrl girlCtrl)
            {
                if (girlCtrl.ini.id != this.charID)
                {
                    Debug.LogError("ExプリセットのキャラクターIDが一致しません");
                    return;
                }

                if (this.boneScales != null && girlCtrl.boneScales != null)
                    girlCtrl.boneScales.LoadEdits(this.boneScales);

                if (this.saveShapeKeys != null && girlCtrl.shapeKeys != null)
                    this.saveShapeKeys.Load(girlCtrl.shapeKeys);

                if (this.saveMateProps != null && girlCtrl.mateProp != null)
                    this.saveMateProps.Load(ref girlCtrl.mateProp);
            }
        }


        public class SaveGirlCtrl
        {
            public SaveLookAt lookAtData;
            public IKConfig ikConfig;
            public KeyValuePair<string, SavePlgIK>[] ikPresetByMotions;

            public SaveGirlCtrl()
            {
                lookAtData = new SaveLookAt();
                ikConfig = new IKConfig();
                ikPresetByMotions = new KeyValuePair<string, SavePlgIK>[0];
            }

            public SaveGirlCtrl(GirlCtrl ctrl)
            {
                Save(ctrl);
            }

            public void Save(GirlCtrl ctrl)
            {
                lookAtData = new SaveLookAt(ctrl.plgIK);
                ikConfig = ctrl.plgIK.ikConfig;
                ikPresetByMotions = ctrl.plgIK.saveListIkPresetByMotions;
            }

            public void Load(GirlCtrl ctrl)
            {
                lookAtData.Load(ctrl.plgIK);
                ctrl.plgIK.ikConfig = ikConfig;
                ctrl.plgIK.saveListIkPresetByMotions = ikPresetByMotions;
            }
        }

        public class SaveShapeKeys
        {
            public KeyValuePair<string, KeyValuePair<int, float>[]>[] blendData;
            public bool useLegacyCtrl = false;
            public KeyValuePair<string, float>[] blendDataNew;

            public SaveShapeKeys()
            {
                blendData = new KeyValuePair<string, KeyValuePair<int, float>[]>[0];
                blendDataNew = new KeyValuePair<string, float>[0];
                useLegacyCtrl = false;
            }

            public SaveShapeKeys(ShapeKeys.Edits edits)
            {
                var dic = edits.dicShapeKeys;

                blendData = new KeyValuePair<string, KeyValuePair<int, float>[]>[dic.Count];
                for (int i = 0; i < dic.Count; i++)
                {
                    blendData[i] = new KeyValuePair<string, KeyValuePair<int, float>[]>(dic.ToArray()[i].Key, new KeyValuePair<int, float>[dic.ToArray()[i].Value.Count]);
                    var tmp = dic.ToArray()[i].Value.ToArray();
                    for (int j = 0; j < dic.ToArray()[i].Value.Count; j++)
                    {
                        blendData[i].Value[j] = new KeyValuePair<int, float>(tmp[j].Key, tmp[j].Value);
                    }
                }

                blendDataNew = edits.dicNameAndValues.ToArray();
                
                this.useLegacyCtrl = edits.useLegacyCtrl;
            }

            public void Load(ShapeKeys tgt)
            {
                tgt.RestoreAllShapes();
                tgt.OnNewSceneLoaded();

                //if (tgt.edits.dicShapeKeys != null)
                    //tgt.ResetOldDicShapes();

                tgt.edits.dicShapeKeys = this.toDic(tgt);
                tgt.edits.useLegacyCtrl = this.useLegacyCtrl;
                tgt.edits.dicNameAndValues = this.blendDataNew.ToDictionary(x => x.Key, x => x.Value);
            }

            private Dictionary<string, Dictionary<int, float>> toDic(ShapeKeys shapeKeys)
            {
                Dictionary<string, Dictionary<int, float>> dic = new Dictionary<string, Dictionary<int, float>>();
                foreach (var v in shapeKeys.dicShapeKeysMap)
                {
                    dic.Add(v.Key, new Dictionary<int, float>());
                }
                foreach (var v in blendData)
                {
                    dic[v.Key] = new Dictionary<int, float>();
                    foreach (var k in v.Value)
                    {
                        dic[v.Key].Add(k.Key, k.Value);
                    }
                }
                return dic;
            }
        }

        public class SaveDicStrV3
        {
            public KeyValuePair<string, Vector3>[] data;

            public SaveDicStrV3()
            {

                data = new KeyValuePair<string, Vector3>[0];
            }

            public SaveDicStrV3(Dictionary<string, Vector3> dic)
            {
                Save(dic);
            }

            public void Save(Dictionary<string, Vector3> dic)
            {
                data = dic.ToArray();
            }

            public void Load(ref Dictionary<string, Vector3> tgt)
            {
                tgt = data.ToDictionary(n => n.Key, n => n.Value);
            }

            public Dictionary<string, Vector3> toDic()
            {
                Dictionary<string, Vector3> dic = new Dictionary<string, Vector3>();

                foreach (var v in data)
                {
                    dic[v.Key] = v.Value;
                }
                return dic;
            }
        }

        public class SaveMaterials
        {
            public SaveDicStrT<MateProp.MateData> data;
            public bool mSYNC_CHEEK_BLEND = true;

            public SaveMaterials()
            {
                data = new SaveDicStrT<MateProp.MateData>();
            }

            public SaveMaterials(MateProp mateProp)
            {
                data = new SaveDicStrT<MateProp.MateData>();

                if (mateProp != null)
                {
                    Save(mateProp);
                }
            }

            public void Save(MateProp mateProp)
            {
                // 書込みONのみ保存する
                mateProp.dicMate = mateProp.dicMate.Where(x => x.Value.write).ToDictionary(x => x.Key, x => x.Value);
                data = new SaveDicStrT<MateProp.MateData>(mateProp.dicMate);
                
                this.mSYNC_CHEEK_BLEND = mateProp.mSYNC_CHEEK_BLEND;
            }

            public void Load(ref MateProp mateProp)
            {
                mateProp.mSYNC_CHEEK_BLEND = this.mSYNC_CHEEK_BLEND;

                mateProp.ResetMateAll();

                data.Load(ref mateProp.dicMate);
                mateProp.SetupMateDic();
                mateProp.UpdateAll(true);
                //mateProp.GetMateAll();
            }
        }

        public class SaveDicStrT<T>
        {
            public KeyValuePair<string, T>[] data;

            public SaveDicStrT()
            {

                data = new KeyValuePair<string, T>[0];
            }

            public SaveDicStrT(Dictionary<string, T> dic)
            {
                Save(dic);
            }

            public void Save(Dictionary<string, T> dic)
            {
                data = dic.ToArray();
            }

            public void Load(ref Dictionary<string, T> tgt)
            {
                tgt = data.ToDictionary(n => n.Key, n => n.Value);
            }

            public Dictionary<string, T> toDic()
            {
                Dictionary<string, T> dic = new Dictionary<string, T>();

                foreach (var v in data)
                {
                    dic[v.Key] = v.Value;
                }
                return dic;
            }
        }

    }

    public class SaveDataUtil
    {
        const string CHUNK_TYPE_EXPRESET = "exSp";


        // 元プリセットPNGの作成を待つコルーチン作成用
        public static void StartCreateExPresetPNG(string pngPath, IO_ExSlider.SaveGirlExPresets save)
        {
            IO_ExSlider._Instance.StartCoroutine(SaveExPngCoroutine(pngPath, save));
        }

        static IEnumerator SaveExPngCoroutine(string pngPath, IO_ExSlider.SaveGirlExPresets save)
        {
            yield return new WaitForEndOfFrame();
            yield return null;
            CreateExPresetPNG(pngPath, save);
        }

        // プリセットPNGにデータを追加
        public static void CreateExPresetPNG(string pngPath, IO_ExSlider.SaveGirlExPresets save)
        {
            try
            {
                Console.WriteLine("ExPNGデータ作成");
                var sw = new StringWriter();
                var xml = new XmlSerializer(typeof(IO_ExSlider.SaveGirlExPresets));
                xml.Serialize(sw, save);

                var data = ConvCompBase64(sw.ToString());
                var chunk = CreatePngChunk(CHUNK_TYPE_EXPRESET, Encoding.ASCII.GetBytes(data));

                var pngData = File.ReadAllBytes(pngPath);
                if (InsertPngChunk(ref pngData, "IHDR", chunk))
                {
                    //string pngPathEx = pngPath.Substring(0, pngPath.Length - 4) + ".ExPreset.png";
                    string pngPathEx = getExPngPath(pngPath);
                    Console.WriteLine("ExPNG保存: " + pngPathEx);
                    File.WriteAllBytes(pngPathEx, pngData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return;
        }

        public static string getExPngPath(string pngPath)
        {
            var dir = Path.Combine(Path.GetDirectoryName(pngPath), "ExPreset");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var pngPathEx = Path.Combine(dir, Path.GetFileNameWithoutExtension(pngPath) + ".ExPreset.png");
            return pngPathEx;
        }

        // プリセットPNGからデータ取得
        public static bool ReadExPresetPNG(string pngPath, out IO_ExSlider.SaveGirlExPresets save, bool modeDD)
        {
            save = null;

            try
            {
                string pngPathEx = pngPath;
                if (!modeDD)
                {
                    //pngPathEx = pngPath.Substring(0, pngPath.Length - 4) + ".ExPreset.png";
                    pngPathEx = getExPngPath(pngPath);
                    Console.WriteLine("ExPNGロード: " + pngPathEx + " " + File.Exists(pngPathEx));

                    if (!File.Exists(pngPathEx))
                        pngPathEx = pngPath;
                }
                var pngData = File.ReadAllBytes(pngPathEx);

                Console.WriteLine("チャンクの検索: " + CHUNK_TYPE_EXPRESET);
                var chunk = GetPngChunk(pngData, CHUNK_TYPE_EXPRESET);
                if (chunk == null || chunk.data == null)
                    return false;

                Console.WriteLine("解凍＆デコード");
                var xmldata = DeconvCompBase64(Encoding.ASCII.GetString(chunk.data));

                Console.WriteLine("デシリアライズ");
                var xml = new XmlSerializer(typeof(IO_ExSlider.SaveGirlExPresets));
                save = xml.Deserialize(new StringReader(xmldata)) as IO_ExSlider.SaveGirlExPresets;
                return true;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            return false;
        }

        // 文字列を圧縮済みのBASE64に変換
        public static string ConvCompBase64(string str)
        {
            byte[] src = Encoding.UTF8.GetBytes(str);
            MemoryStream dest = new MemoryStream();
            using (DeflateStream df = new DeflateStream(dest, CompressionMode.Compress))
            {
                df.Write(src, 0, src.Length);
            }

            return System.Convert.ToBase64String(dest.ToArray(), Base64FormattingOptions.None);
        }

        // 文字列を圧縮済みのBASE64から逆変換
        public static string DeconvCompBase64(string str)
        {
            byte[] src = System.Convert.FromBase64String(str);
            MemoryStream ms = new MemoryStream(src);
            MemoryStream dest = new MemoryStream();
            byte[] buff = new byte[256];
            using (DeflateStream df = new DeflateStream(ms, CompressionMode.Decompress))
            {
                // CopyToは使えない？
                int num;
                while ((num = df.Read(buff, 0, buff.Length)) > 0)
                {
                    dest.Write(buff, 0, num);
                }
            }

            return Encoding.UTF8.GetString(dest.ToArray());
        }

        public class PngChunk
        {
            public int datalen = -1;
            public string type = null;
            public int nextChunk = -1;
            public byte[] data = null;
            public byte[] crc32 = null;

            public PngChunk(byte[] png, int offset)
            {
                if (png.Length < (12 + offset))
                    return;

                this.datalen = png[offset + 0] << 24 | png[offset + 1] << 16
                    | png[offset + 2] << 8 | png[offset + 3];
                //this.datalen = BitConverter.ToInt32(png, offset); // リトルエンディアンで面倒

                this.nextChunk = offset + this.datalen + 12;
                
                byte[] type = new byte[0];
                memcpy(ref type, png, offset + 4, 4);

                this.type = Encoding.ASCII.GetString(type);
                //Console.WriteLine(this.type);

                this.data = new byte[0];
                this.crc32 = new byte[0];

                memcpy(ref this.data, png, offset + 8, datalen);
                memcpy(ref this.crc32, png, offset + 8 + datalen, 4);
            }

            public static readonly byte[] SIGNATURE = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }; 
        }

        public static IEnumerator<PngChunk> ReadPngChunk(byte[] png)
        {
            if (!memcmp(png, PngChunk.SIGNATURE, PngChunk.SIGNATURE.Length))
            {
                Console.WriteLine("ERROR:  PNGシグネチャが一致しません");
                yield break;
            }

            int offset = PngChunk.SIGNATURE.Length;
            while (true)
            {
                Console.WriteLine($"{offset} / {png.Length}");
                var chunk = new PngChunk(png, offset);

                if (string.IsNullOrEmpty(chunk.type) || chunk.type == "IEND" || chunk.type == "IDAT") // IDAT前に追加するので
                {
                    yield break;
                }
                offset = chunk.nextChunk;
                yield return chunk;
            }
        }

        public static PngChunk GetPngChunk(byte[] png, string type)
        {
            using (var ie = ReadPngChunk(png))
            {
                while (ie.MoveNext())
                {
                    var c = ie.Current;

                    Console.WriteLine("Chunk: " + c.type);
                    if (c.type == type)
                    {
                        return c;
                    }
                }
            }
            return null;
        }
        public static bool InsertPngChunk(ref byte[] png, string prevChunk, byte[] chunk)
        {
            using (var ie = ReadPngChunk(png))
            {
                while(ie.MoveNext())
                {
                    var c = ie.Current;

                    Console.WriteLine("Chunk: " + c.type);
                    if (c.type == prevChunk)
                    {
                        var bs = new byte[0];
                        memcpy(ref bs, png, 0, c.nextChunk);
                        memcpy(ref bs, chunk, 0, chunk.Length);
                        memcpy(ref bs, png, c.nextChunk, png.Length - c.nextChunk);

                        Console.WriteLine("チャンクを追加");
                        png = bs;
                        return true;
                    }
                }
            }
            return false;
        }

        public static byte[] CreatePngChunk(string type, byte[] data)
        {
            List<byte> chunk = new List<byte>();

            chunk.AddRange(Encoding.ASCII.GetBytes(type));
            if (chunk.Count != 4)
                return null;

            chunk.AddRange(data);

            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            var typeAndData = chunk.ToArray();

            chunk.Clear();
            if (BitConverter.IsLittleEndian)
                chunk.AddRange(BitConverter.GetBytes(data.Length).Reverse());
            else
                chunk.AddRange(BitConverter.GetBytes(data.Length));
            chunk.AddRange(typeAndData);
            chunk.AddRange(crc32.ComputeHash(typeAndData));

            Console.WriteLine("チャンクを作成");

            return chunk.ToArray();
        }

        public static bool memcpy(ref byte[] a, byte[] b, int offset, int len)
        {
            if (b.Length < (offset + len))
                return false;

            var list = a.ToList();
            for (int i = offset; i < (offset + len); i++)
            {
                 list.Add(b[i]);
            }
            a = list.ToArray();

            return true;
        }

        public static bool memcmp(byte[] a, byte[] b, int len)
        {
            if (a.Length < len || b.Length < len)
                return false;

            for(int i = 0; i < len; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityAssetAuditor
{
    public class AssetAuditor
    {
        public static string ProxyModelPath;
        public static string ProxyAudioPath;
        public static string ProxyTexturePath;
        
        static AssetAuditor()
        {
            
            ProxyModelPath = "Assets/Editor/AssetAuditor/Models/DefaultAvatar.fbx";
            ProxyAudioPath = "Assets/Editor/AssetAuditor/Audio/DefaultAudio.wav";
            ProxyTexturePath = "Assets/Editor/AssetAuditor/Texture/DefaultTexture.jpg";
        }
        private static List<string> foundAssets;

        public enum WildCardMatchType
        {
            NameContains,
            Regex
        }

        public enum AssetType
        {
            Texture,
            Model,
            Audio,
            Folder
        }


        [Serializable]
        public struct AssetRule
        {
            public string RuleName;
            public WildCardMatchType WildCardMatchType;
            public string WildCard;
            public string AssetGuid;
            public AssetType assetType;
            public bool SelectiveMode;
            public List<string> SelectiveProperties;
        }

        public static Type TypeFromAssetType(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.Texture:
                    return typeof(Texture);
                case AssetType.Model:
                    return typeof(GameObject);
                case AssetType.Audio:
                    return typeof(AudioClip);
                case AssetType.Folder:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException("assetType", assetType, null);
            }
        }

        public static string[] GetAffectedAssets(AssetRule assetRule)
        {
            foundAssets = new List<string>();

            switch (assetRule.WildCardMatchType)
            {
                case WildCardMatchType.NameContains:
                    DoNameContainsSearch(foundAssets, assetRule);
                    break;
                case WildCardMatchType.Regex:
                    DoRegexNameSearch(foundAssets, assetRule);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return foundAssets.ToArray();
        }


        private static void DoRegexNameSearch(List<string> foundAssets, AssetRule assetRule)
        {
            foreach (var file in Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (file.Contains(".meta") || file.Contains("/Editor/AssetAuditor/ProxyAssets")) continue;

                if (!Regex.IsMatch(file, assetRule.WildCard)) continue;

                var mainAssetTypeAtPath =
                    AssetDatabase.GetMainAssetTypeAtPath(file.Substring(Application.dataPath.Length - 6));

                var typeFromAssetType = TypeFromAssetType(assetRule.assetType);

                if (!IsSameOrBaseClass(mainAssetTypeAtPath, typeFromAssetType)) continue;

                foundAssets.Add(file);
            }
        }

        public static void DoNameContainsSearch(List<string> foundAssets, AssetRule assetRule)
        {
            foreach (var file in Directory.GetFiles(Application.dataPath, "*" + assetRule.WildCard + "*",
                SearchOption.AllDirectories))
            {

                if (file.Contains(".meta") || file.Contains("/Editor/AssetAuditor/ProxyAssets")) continue;

                var mainAssetTypeAtPath =
                    AssetDatabase.GetMainAssetTypeAtPath(file.Substring(Application.dataPath.Length - 6));

                var typeFromAssetType = TypeFromAssetType(assetRule.assetType);

                if (!IsSameOrBaseClass(mainAssetTypeAtPath, typeFromAssetType)) continue;

                foundAssets.Add(file);
            }
        }

        public static bool CompareSerializedProperty(SerializedProperty foundAssetSp, SerializedProperty assetRuleSp)
        {

            if (foundAssetSp.propertyPath == "m_FileIDToRecycleName") return true; // the file ids will always be different so we should skip over this. 
            
            switch (foundAssetSp.propertyType)
            {
                case SerializedPropertyType.Generic: // this eventually goes down through the data until we get a useable value to compare 

                    var foundAssetsSPCopy = foundAssetSp.Copy();
                    var assetRuleSPCopy = assetRuleSp.Copy();

                    // we must get the next sibling SerializedProperties to know when to stop the comparison
                    var nextSiblingAssetSP = foundAssetSp.Copy ();
                    var nextSiblingRuleSP = assetRuleSp.Copy ();
                    nextSiblingAssetSP.NextVisible (false);
                    nextSiblingRuleSP.NextVisible (false);

                    bool asset, found;
                    
                    do
                    {
                        if (assetRuleSPCopy.propertyType != foundAssetsSPCopy.propertyType)
                        {
                            return false; // mistmatch in types different serialisation
                        }
                        if (assetRuleSPCopy.propertyType != SerializedPropertyType.Generic)
                        {
                            if (!CompareSerializedProperty(foundAssetsSPCopy, assetRuleSPCopy))
                            {
                                Debug.Log(foundAssetsSPCopy.propertyPath + foundAssetsSPCopy.propertyType);
                                return false;
                            }
                        }
                        asset = foundAssetsSPCopy.NextVisible(true);
                        found = assetRuleSPCopy.NextVisible(true);
                    } while (found && asset &&
                        !SerializedProperty.EqualContents (foundAssetsSPCopy, nextSiblingAssetSP) &&
                        !SerializedProperty.EqualContents (assetRuleSPCopy, nextSiblingRuleSP));

                    return true;
                case SerializedPropertyType.Integer:
                    return foundAssetSp.intValue == assetRuleSp.intValue;
                case SerializedPropertyType.Boolean:
                    return foundAssetSp.boolValue == assetRuleSp.boolValue;
                case SerializedPropertyType.Float:
                    return foundAssetSp.floatValue == assetRuleSp.floatValue;
                case SerializedPropertyType.String:
                    return foundAssetSp.stringValue == assetRuleSp.stringValue;
                case SerializedPropertyType.Color:
                    return foundAssetSp.colorValue == assetRuleSp.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return true;// this is weird on models imports and needs a solution as the exposed transforms reference the model itself
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    return foundAssetSp.enumValueIndex == assetRuleSp.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return foundAssetSp.vector2Value == assetRuleSp.vector2Value;
                case SerializedPropertyType.Vector3:
                    return foundAssetSp.vector3Value == assetRuleSp.vector3Value;
                case SerializedPropertyType.Vector4:
                    return foundAssetSp.vector4Value == assetRuleSp.vector4Value;
                case SerializedPropertyType.Rect:
                    return foundAssetSp.rectValue == assetRuleSp.rectValue;
                case SerializedPropertyType.ArraySize:
                    if (foundAssetSp.isArray && assetRuleSp.isArray)
                    {
                        Debug.Log(foundAssetSp.arraySize + assetRuleSp.arraySize);
                        return foundAssetSp.arraySize == assetRuleSp.arraySize;
                    }
                    else
                    {
                        return foundAssetSp.intValue == assetRuleSp.intValue;
                    }
                case SerializedPropertyType.Character:
                    
                    break;
                case SerializedPropertyType.AnimationCurve:
                    return foundAssetSp.animationCurveValue == assetRuleSp.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return foundAssetSp.boundsValue == assetRuleSp.boundsValue;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    return foundAssetSp.quaternionValue == assetRuleSp.quaternionValue;
                case SerializedPropertyType.ExposedReference:
                    return foundAssetSp.exposedReferenceValue == assetRuleSp.exposedReferenceValue;
#if UNITY_2017_1_OR_NEWER
                case SerializedPropertyType.FixedBufferSize:
                    break;
#endif
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return false;
        }


        private static bool IsSameOrBaseClass(Type b, Type a)
        {
            if (b == null || a == null) return false;

            return b.IsSubclassOf(a) || b == a;
        }

        public static bool CompareSerializedObject(SerializedObject rule, SerializedObject asset)
        {
            var ruleIter = rule.GetIterator();
            var assetIter = asset.GetIterator();
            assetIter.NextVisible(true);
            ruleIter.NextVisible(true);

            do
            {
                if (!CompareSerializedProperty(ruleIter, assetIter))
                {
                    Debug.Log(ruleIter.propertyPath + ruleIter.propertyType);
                    return false;
                }         

                ruleIter.NextVisible(false);
            } while (assetIter.NextVisible(false));

            return true;
        }

        public static bool HaveEqualProperties<T>(T rhs, T lhs)
        {
            if (rhs != null && lhs != null)
            {
                Type type = typeof(T);
                foreach (System.Reflection.PropertyInfo pi in type.GetProperties(
                    System.Reflection.BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    object rhsValue = type.GetProperty(pi.Name).GetValue(rhs, null);
                    object lhsValue = type.GetProperty(pi.Name).GetValue(lhs, null);
                    if (!rhsValue.Equals(lhsValue))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        public static string GetPropertyNameFromDisplayName(SerializedObject so, string displayName)
        {
            var iter = so.GetIterator();

            iter.NextVisible(true);
            
            do
            {
                if (iter.displayName == displayName) return iter.name;
            }
            while (iter.NextVisible(false)) ;

            return null;
        }
        
        public static string[] GetPropertyNames(SerializedObject so)
        {
            var soIter = so.GetIterator();

            List<string> propNames = new List<string>();

            soIter.NextVisible(true);
            do
            {
                propNames.Add(soIter.displayName);
            } while (soIter.NextVisible(false));

            return propNames.ToArray();
        }

        public static void CreateProxyAudio(AssetRule newRule , ref string currentAsset)
        {
            string newAssetPath = "Assets/Editor/AssetAuditor/ProxyAssets/" + newRule.RuleName + ".wav";
            AssetDatabase.CopyAsset(ProxyAudioPath, newAssetPath);

            AssetDatabase.ImportAsset(newAssetPath);

            WriteUserData(newAssetPath , newRule, ref currentAsset);
        }

        public static void CreateProxyModel(AssetRule newRule, ref string currentAsset)
        {
            string newAssetPath = "Assets/Editor/AssetAuditor/ProxyAssets/" + newRule.RuleName + ".fbx";
            AssetDatabase.CopyAsset(ProxyModelPath, newAssetPath);

            AssetDatabase.ImportAsset(newAssetPath);
            WriteUserData(newAssetPath , newRule, ref currentAsset);
        }

        public static void CreateProxyTexture(AssetRule newRule, ref string currentAsset)
        {    
            string newAssetPath = "Assets/Editor/AssetAuditor/ProxyAssets/" + newRule.RuleName + ".jpg";
            AssetDatabase.CopyAsset(ProxyTexturePath, newAssetPath);

            AssetDatabase.ImportAsset(newAssetPath);
            WriteUserData(newAssetPath , newRule , ref currentAsset);
        }

        public static void WriteUserData(string path, AssetAuditor.AssetRule assetRule, ref string currentAsset)
        {
            var assetImporter = AssetImporter.GetAtPath(path);
            assetRule.AssetGuid = AssetDatabase.AssetPathToGUID(assetImporter.assetPath);

            assetImporter.userData = JsonUtility.ToJson(assetRule);

            EditorUtility.SetDirty(assetImporter);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.SaveAssets();

            currentAsset = assetRule.AssetGuid;
        }
        
        public static void WriteUserData(string path , AssetAuditor.AssetRule assetRule)
        {
            var assetImporter = AssetImporter.GetAtPath(path);
            assetRule.AssetGuid = AssetDatabase.AssetPathToGUID(assetImporter.assetPath);

            assetImporter.userData = JsonUtility.ToJson(assetRule);

            EditorUtility.SetDirty(assetImporter);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.SaveAssets();
        }

        public static bool RuleExists(AssetAuditor.AssetRule assetRule)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor/AssetAuditor/ProxyAssets"))
                AssetDatabase.CreateFolder("Assets/Editor/AssetAuditor", "ProxyAssets");
            
            foreach (var asset in AssetDatabase.FindAssets("", new[] {"Assets/Editor/AssetAuditor/ProxyAssets"}))
            {
                var guidToAssetPath = AssetDatabase.GUIDToAssetPath(asset);
                var assetImporter = AssetImporter.GetAtPath(guidToAssetPath);

                AssetAuditor.AssetRule ar = new AssetAuditor.AssetRule();
                ar = JsonUtility.FromJson<AssetAuditor.AssetRule>(assetImporter.userData);
                if (ar.RuleName == assetRule.RuleName && ar.WildCard == assetRule.WildCard &&
                    ar.WildCardMatchType == assetRule.WildCardMatchType) return true;
            }
            return false;
        }
    }
}
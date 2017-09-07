using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Collections;

namespace UnityAssetAuditor
{

    public delegate void OnQueueComplete();
    
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
            EditorApplication.update += TickEnumerator;
            enumerableQueue = new Queue<IEnumerable<float>>();
        }

        private static void TickEnumerator()
        {
            if (currentEnumerable == null)
            {
                return;
            }
            if (!currentEnumerator.MoveNext())
            {
                currentEnumerable = null;
                currentEnumerator = null;

                if (enumerableQueue.Count == 0)
                {
                    if (queueComplete != null) queueComplete.Invoke();
                }
                else
                {
                    currentEnumerable = enumerableQueue.Dequeue();
                    currentEnumerator = currentEnumerable.GetEnumerator();
                }
            }
        }

        public static float GetProgress()
        {
            return currentEnumerator != null ? currentEnumerator.Current : 0f;
        }

        public static IEnumerable<float> currentEnumerable;

        public static IEnumerator<float> currentEnumerator;

        public static Queue<IEnumerable<float>> enumerableQueue;

        public static event OnQueueComplete queueComplete;
        
        public static void AddEnumerator(IEnumerable<float> enumerator)
        {
            enumerableQueue.Enqueue(enumerator);

            if (currentEnumerable == null)
            {
                currentEnumerable = enumerableQueue.Dequeue();
                currentEnumerator = currentEnumerable.GetEnumerator();
            }
        }

        public static void ClearQueue()
        {
            enumerableQueue.Clear();

            currentEnumerable = null;
            currentEnumerator = null;
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

        public static string[] GetAffectedAssets()
        {
            return foundAssets.ToArray();
        }

        public static void UpdateAffectedAssets(AssetRule assetRule)
        {
            foundAssets = new List<string>();

            switch (assetRule.WildCardMatchType)
            {
                case WildCardMatchType.NameContains:
                    ClearQueue();
                    AddEnumerator(DoNameContainsSearch(foundAssets, assetRule));
                    break;
                case WildCardMatchType.Regex:
                    ClearQueue();
                    AddEnumerator(DoRegexNameSearch(foundAssets, assetRule));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private static IEnumerable<float> DoRegexNameSearch(List<string> foundAssets, AssetRule assetRule)
        {
            var strings = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);

            float progress = 0f;
            float total = strings.Length;
            
            foreach (var file in strings)
            {
                if (file.Contains(".meta") || file.Contains("/Editor/AssetAuditor/ProxyAssets"))
                {
                    progress += 1;
                    yield return progress/total;
                    continue;
                }

                if (!Regex.IsMatch(file, assetRule.WildCard))
                {
                    progress += 1;
                    yield return progress/total;
                    continue;
                }

                var mainAssetTypeAtPath =
                    AssetDatabase.GetMainAssetTypeAtPath(file.Substring(Application.dataPath.Length - 6));

                var typeFromAssetType = TypeFromAssetType(assetRule.assetType);

                if (!IsSameOrBaseClass(mainAssetTypeAtPath, typeFromAssetType))
                {
                    progress += 1;
                    yield return progress/total;
                    continue;
                }

                foundAssets.Add(file);
                progress += 1;
                yield return progress/total;
            }

            AssetAuditor.foundAssets = foundAssets;
        }

        public static IEnumerable<float> DoNameContainsSearch(List<string> foundAssets, AssetRule assetRule)
        {
            var strings = Directory.GetFiles(Application.dataPath, "*" + assetRule.WildCard + "*",
                SearchOption.AllDirectories);
            
            float progress = 0f;
            float total = strings.Length;            
            
            foreach (var file in strings)
            {

                if (file.Contains(".meta") || file.Contains("/Editor/AssetAuditor/ProxyAssets"))
                {
                    progress += 1f;
                    yield return progress / total;
                    continue;
                }

                var mainAssetTypeAtPath =
                    AssetDatabase.GetMainAssetTypeAtPath(file.Substring(Application.dataPath.Length - 6));

                var typeFromAssetType = TypeFromAssetType(assetRule.assetType);

                if (!IsSameOrBaseClass(mainAssetTypeAtPath, typeFromAssetType))
                {
                    progress += 1f;
                    yield return progress / total;
                    continue;
                }

                foundAssets.Add(file);
                progress += 1f;
                yield return progress / total;
            }
            
            AssetAuditor.foundAssets = foundAssets;   
        }


        public static IEnumerable<float> GatherAssetRules(List<AssetRule> _assetRules , List<string> _assetRuleNames )
        {
            int progress = 0;
            var foundAssets = AssetDatabase.FindAssets("", new[] {"Assets/Editor/AssetAuditor/ProxyAssets"});
            // get all assets in the proxyassets folder
            foreach (var asset in foundAssets)
            {
                var guidToAssetPath = AssetDatabase.GUIDToAssetPath(asset);
                var assetImporter = AssetImporter.GetAtPath(guidToAssetPath);
                AssetRule ar = new AssetRule();
                ar = JsonUtility.FromJson<AssetRule>(assetImporter.userData);
                _assetRules.Add(ar);

                progress++;
                yield return progress / (float)foundAssets.Length;
            }
            
            _assetRuleNames.Clear();
            foreach (var assetRule in _assetRules)
            {
                _assetRuleNames.Add(assetRule.RuleName);
            }

            yield return 1f;
        }


        public static IEnumerable<float> GatherData(AssetRule assetRule , List<AssetAuditTreeElement> elements, int selectedSelective)
        {
            int id = -1;
            
            // get the affected assets 
           var affectedAssets = GetAffectedAssets();
            
            if(affectedAssets == null) Debug.Log(" null affected assets");

            // build the directory tree from the affected assets
            elements.Add(new AssetAuditTreeElement("Root", "", -1, 0, false, false, AssetType.Folder ));

            // early out if there are no affected assets
            if (affectedAssets.Length == 0)
            {
                Debug.Log("no affected assets ");
                if (queueComplete != null) queueComplete.Invoke();
                yield break;
            }

            AssetAuditTreeElement assetsFolder = new AssetAuditTreeElement("Assets", "Assets", 0, id++, false, false,
                AssetType.Folder );
            // add the project root "Assets" folder
            elements.Add(assetsFolder);

            if (assetsFolder.children == null) assetsFolder.children = new List<TreeElement>();
            // search the next level down directories

            var dirs = Directory.GetDirectories(Application.dataPath);
            
            float progress = 0f;
            float total = dirs.Length;
            
            foreach (string t in dirs)
            {
                // check if the directory actually contais any of the assets we want to show
                foreach (var affectedAsset in affectedAssets)
                {
                    float innerProgress = 0f;
                    if (affectedAsset.Contains(t))
                    {
                        AddChildrenRecursive(assetsFolder, t, ref id, elements, affectedAssets , assetRule, selectedSelective);
                        break;
                    }
                    innerProgress += 1f;

                    yield return ((innerProgress / affectedAssets.Length) / total) + (progress / total);
                }

                CheckAffectedAssets(elements, t, assetsFolder.depth, ref id , affectedAssets , assetRule, selectedSelective);

                progress += 1f;
                yield return progress / total;
            }

            CheckAffectedAssets(elements, Application.dataPath, assetsFolder.depth, ref id, affectedAssets , assetRule, selectedSelective);
        }
        
        
        private static void AddChildrenRecursive(AssetAuditTreeElement parent, string dir, ref int id,
            List<AssetAuditTreeElement> _elements  , string[] affectedAssets , AssetRule assetRule, int selectedSelective)
        {
            var child = new AssetAuditTreeElement(new DirectoryInfo(dir).Name, "", parent.depth + 1, id++, false, false,
                AssetAuditor.AssetType.Folder);

            _elements.Add(child);

            var dirs = Directory.GetDirectories(dir);
            foreach (string t in dirs)
            {
                if (affectedAssets.Any(affectedAsset => affectedAsset.Contains(t)))
                {
                    AddChildrenRecursive(child, t, ref id, _elements, affectedAssets, assetRule, selectedSelective);
                }

                CheckAffectedAssets(_elements, t, child.depth + 1, ref id , affectedAssets , assetRule, selectedSelective);
            }
        }
        
        
         private static void CheckAffectedAssets(List<AssetAuditTreeElement> elements, string searchDirectory, int depth,
            ref int id , string[] affectedAssets , AssetRule assetRule, int selectedSelective)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                searchDirectory = searchDirectory.Replace('/', '\\');

            foreach (var affectedAsset in affectedAssets)
            {
                if (new DirectoryInfo(affectedAsset).Parent.FullName != searchDirectory) continue;
                
                SerializedObject assetImporterSO = null;
                SerializedObject ruleImporterSO = null;

                if (AssetAuditor.TypeFromAssetType(assetRule.assetType) == typeof(Texture))
                {
                    var assetimporter =
                        AssetImporter.GetAtPath(affectedAsset.Substring(Application.dataPath.Length - 6)) as
                            TextureImporter;

                    // this may happen (e.g. render texture)
                    if (assetimporter == null)
                        continue;

                    var ruleimporter =
                        AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid)) as
                            TextureImporter;

                    if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                        continue; // this shouldnt happen but is a nice failsafe

                    assetImporterSO = new SerializedObject(assetimporter);
                    ruleImporterSO = new SerializedObject(ruleimporter);
                }
                    
                if (AssetAuditor.TypeFromAssetType(assetRule.assetType) == typeof(GameObject))
                {
                    var assetimporter =
                        AssetImporter.GetAtPath(affectedAsset.Substring(Application.dataPath.Length - 6)) as
                            ModelImporter;
                    var ruleimporter =
                        AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid)) as
                            ModelImporter;

                    if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                        continue; // this shouldnt happen but is a nice failsafe

                    assetImporterSO = new SerializedObject(assetimporter);
                    ruleImporterSO = new SerializedObject(ruleimporter);
                }

                if (AssetAuditor.TypeFromAssetType(assetRule.assetType) == typeof(AudioClip))
                {
                    var assetimporter =
                        AssetImporter.GetAtPath(affectedAsset.Substring(Application.dataPath.Length - 6)) as
                            AudioImporter;
                    var ruleimporter =
                        AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid)) as
                            AudioImporter;

                    if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                        continue; // this shouldnt happen but is a nice failsafe
                        
                    assetImporterSO = new SerializedObject(assetimporter);
                    ruleImporterSO = new SerializedObject(ruleimporter);
                }

                if (assetImporterSO == null || ruleImporterSO == null) continue; // TODO log message here
                    
                if (!assetRule.SelectiveMode)
                {
                    bool equal = AssetAuditor.CompareSerializedObject(assetImporterSO, ruleImporterSO);

                    elements.Add(new AssetAuditTreeElement(Path.GetFileName(affectedAsset), affectedAsset,
                        depth + 1, id++, true, equal, assetRule.assetType));
                }
                else
                {
                    string property = assetRule.SelectiveProperties[selectedSelective];

                    var realname = GetPropertyNameFromDisplayName(assetImporterSO, property);

                    var foundAssetSP = assetImporterSO.FindProperty(realname);

                    var assetRuleSP = ruleImporterSO.FindProperty(realname);

                    if (CompareSerializedProperty(foundAssetSP, assetRuleSP))
                    {
                        elements.Add(new AssetAuditTreeElement(Path.GetFileName(affectedAsset),
                            affectedAsset,
                            depth + 1, id++, true, true, assetRule.assetType));
                    }
                    else
                    {
                        elements.Add(new AssetAuditTreeElement(Path.GetFileName(affectedAsset),
                            affectedAsset,
                            depth + 1, id++, true, false, assetRule.assetType));
                    }
                }
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
                 //   Debug.Log(ruleIter.propertyPath + ruleIter.propertyType);
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

        public static void WriteUserData(string path, AssetRule assetRule, ref string currentAsset)
        {
            var assetImporter = AssetImporter.GetAtPath(path);
            assetRule.AssetGuid = AssetDatabase.AssetPathToGUID(assetImporter.assetPath);

            assetImporter.userData = JsonUtility.ToJson(assetRule);

            EditorUtility.SetDirty(assetImporter);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.SaveAssets();

            currentAsset = assetRule.AssetGuid;
        }
        
        public static void WriteUserData(string path , AssetRule assetRule)
        {
            var assetImporter = AssetImporter.GetAtPath(path);
            assetRule.AssetGuid = AssetDatabase.AssetPathToGUID(assetImporter.assetPath);

            assetImporter.userData = JsonUtility.ToJson(assetRule);

            EditorUtility.SetDirty(assetImporter);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.SaveAssets();
        }

        public static bool RuleExists(AssetRule assetRule)
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
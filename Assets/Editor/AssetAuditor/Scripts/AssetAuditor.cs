using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Collections;

namespace UnityAssetAuditor
{

    public delegate void OnQueueComplete();
    
    public class AssetAuditor
    {
        public static IEnumerable<float> currentEnumerable;
        public static IEnumerator<float> currentEnumerator;
        public static Queue<IEnumerable<float>> enumerableQueue;
        public static event OnQueueComplete queueComplete;      
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
        
        
        static AssetAuditor()
        {
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
            string type = "";
            switch (assetRule.assetType)
            {
                case AssetType.Texture:
                    type = "Texture";
                    break;
                case AssetType.Model:
                    type = "GameObject";
                    break;
                case AssetType.Audio:
                    type = "AudioClip";
                    break;
                case AssetType.Folder:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            foreach (var asset in AssetDatabase.FindAssets("t:" + type))
            {
                string guidToAssetPath = AssetDatabase.GUIDToAssetPath(asset);

                if (guidToAssetPath.Contains(AssetAuditorPreferences.ProxyAssetsDirectory)) continue;

                if (Regex.IsMatch(guidToAssetPath, assetRule.WildCard))
                {
                    foundAssets.Add(guidToAssetPath);
                }
            }

            AssetAuditor.foundAssets = foundAssets;
            yield return 1f;
        }


        public static IEnumerable<float> DoNameContainsSearch(List<string> foundAssets, AssetRule assetRule)
        {
            string type = "";
            switch (assetRule.assetType)
            {
                case AssetType.Texture:
                    type = "Texture";
                    break;
                case AssetType.Model:
                    type = "GameObject";
                    break;
                case AssetType.Audio:
                    type = "AudioClip";
                    break;
                case AssetType.Folder:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            foreach (string findAsset in AssetDatabase.FindAssets("t:" + type + " " + assetRule.WildCard))
            {
                string guidToAssetPath = AssetDatabase.GUIDToAssetPath(findAsset);
                if(guidToAssetPath.Contains(AssetAuditorPreferences.ProxyAssetsDirectory))continue;
                foundAssets.Add(guidToAssetPath);
            }

            AssetAuditor.foundAssets = foundAssets;
            yield return 1f;
        }


        public static IEnumerable<float> GatherAssetRules(List<AssetRule> _assetRules , List<string> _assetRuleNames )
        {
            int progress = 0;
            string[] foundAssets = AssetDatabase.FindAssets("", new[] {AssetAuditorPreferences.ProxyAssetsDirectory});
            // get all assets in the proxyassets folder
            foreach (string asset in foundAssets)
            {
                string guidToAssetPath = AssetDatabase.GUIDToAssetPath(asset);
                AssetImporter assetImporter = AssetImporter.GetAtPath(guidToAssetPath);
                AssetRule ar = new AssetRule();
                ar = JsonUtility.FromJson<AssetRule>(assetImporter.userData);
                _assetRules.Add(ar);

                progress++;
                yield return progress / (float)foundAssets.Length;
            }
            
            _assetRuleNames.Clear();
            foreach (AssetRule assetRule in _assetRules)
            {
                _assetRuleNames.Add(assetRule.RuleName);
            }

            yield return 1f;
        }


        public static IEnumerable<float> GatherData(AssetRule assetRule , List<AssetAuditTreeElement> elements, int selectedSelective)
        {
            int id = -1;
            
            // get the affected assets 
           string[] affectedAssets = GetAffectedAssets();
            
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
            float progress = 0f;
            foreach (var affectedAsset in affectedAssets)
            {
                // split the path 
                string path = affectedAsset.Substring(7);
                var strings = path.Split(new[]{'/'}, StringSplitOptions.None);
                string projectPath = "Assets";
                // the first entries have lower depth
                for(int i = 0 ; i < strings.Length ; i++)
                {
                    projectPath += "/" + strings[i];
                    
                    // the last element is the asset itself
                    if (i == strings.Length-1)
                    {
                       var result = CheckAffectedAsset(affectedAsset, assetRule, selectedSelective);
                       var element =  new AssetAuditTreeElement(strings[i], projectPath, i + 1, id + 1, true, result,
                            assetRule.assetType); 
                        
                        elements.Add(element);
                        id++;
                    }
                    else if (!elements.Exists(element => element.name == strings[i] && element.projectPath == projectPath))
                    {
                        var assetAuditTreeElement = new AssetAuditTreeElement(strings[i], projectPath, i+1 , id+1, false, false, AssetType.Folder);
                        elements.Add(assetAuditTreeElement);
                        id++;
                    }
                }
                progress += 1f;
                yield return progress / affectedAssets.Length;
            }
        }
 

        private static bool CheckAffectedAsset(string affectedAsset, AssetRule assetRule, int selectedSelective)
        {
            SerializedObject assetImporterSO = null;
            SerializedObject ruleImporterSO = null;

            if (TypeFromAssetType(assetRule.assetType) == typeof(Texture))
            {
                TextureImporter assetimporter =
                    AssetImporter.GetAtPath(affectedAsset) as
                        TextureImporter;

                // this may happen (e.g. render texture)
                if (assetimporter == null)
                    return false;

                TextureImporter ruleimporter =
                    AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid)) as
                        TextureImporter;

                if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                    return false; // this shouldnt happen but is a nice failsafe

                assetImporterSO = new SerializedObject(assetimporter);
                ruleImporterSO = new SerializedObject(ruleimporter);
            }

            if (TypeFromAssetType(assetRule.assetType) == typeof(GameObject))
            {
                ModelImporter assetimporter =
                    AssetImporter.GetAtPath(affectedAsset) as
                        ModelImporter;
                ModelImporter ruleimporter =
                    AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid)) as
                        ModelImporter;

                if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                    return false; // this shouldnt happen but is a nice failsafe

                assetImporterSO = new SerializedObject(assetimporter);
                ruleImporterSO = new SerializedObject(ruleimporter);
            }

            if (TypeFromAssetType(assetRule.assetType) == typeof(AudioClip))
            {
                AudioImporter assetimporter =
                    AssetImporter.GetAtPath(affectedAsset) as
                        AudioImporter;
                AudioImporter ruleimporter =
                    AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid)) as
                        AudioImporter;

                if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                    return false; // this shouldnt happen but is a nice failsafe

                assetImporterSO = new SerializedObject(assetimporter);
                ruleImporterSO = new SerializedObject(ruleimporter);
            }

            if (assetImporterSO == null || ruleImporterSO == null) return false; // TODO log message here

            if (!assetRule.SelectiveMode || assetRule.SelectiveProperties.Count <= 0)
            {
                return CompareSerializedObject(assetImporterSO, ruleImporterSO);
            }
            string property = assetRule.SelectiveProperties[selectedSelective];

            string realname = GetPropertyNameFromDisplayName(assetImporterSO, property);

            SerializedProperty foundAssetSP = assetImporterSO.FindProperty(realname);

            SerializedProperty assetRuleSP = ruleImporterSO.FindProperty(realname);

            return CompareSerializedProperty(foundAssetSP, assetRuleSP);
        }

        
        public static bool CompareSerializedProperty(SerializedProperty foundAssetSp, SerializedProperty assetRuleSp)
        {

            if (foundAssetSp.propertyPath == "m_FileIDToRecycleName" || foundAssetSp.propertyPath == "m_UserData") return true; // the file ids will always be different so we should skip over this. The user data is where the asset rule info is stored so we dont want to check that
            
            switch (foundAssetSp.propertyType)
            {
                case SerializedPropertyType.Generic: // this eventually goes down through the data until we get a useable value to compare 

                    SerializedProperty foundAssetsSPCopy = foundAssetSp.Copy();
                    SerializedProperty assetRuleSPCopy = assetRuleSp.Copy();

                    // we must get the next sibling SerializedProperties to know when to stop the comparison
                    SerializedProperty nextSiblingAssetSP = foundAssetSp.Copy ();
                    SerializedProperty nextSiblingRuleSP = assetRuleSp.Copy ();
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


        public static bool CompareSerializedObject(SerializedObject rule, SerializedObject asset)
        {
            SerializedProperty ruleIter = rule.GetIterator();
            SerializedProperty assetIter = asset.GetIterator();
            assetIter.NextVisible(true);
            ruleIter.NextVisible(true);

            do
            {
                if (!CompareSerializedProperty(ruleIter, assetIter))
                {
                    Debug.Log(" failied property " + ruleIter.propertyPath + "  " + assetIter.displayName);
                    return false;
                }         

                ruleIter.NextVisible(false);
            } while (assetIter.NextVisible(false));

            return true;
        }
        
        
        public static IEnumerable<float> FixAll(AssetAuditTreeView treeView , AssetRule assetRule)
        {
            List<AssetAuditTreeElement> list = new List<AssetAuditTreeElement>();
            TreeElementUtility.TreeToList(treeView.treeModel.root, list);

            float progress = 0f;
            
            foreach (AssetAuditTreeElement assetAuditTreeElement in list)
            {
                if (assetAuditTreeElement.isAsset && !assetAuditTreeElement.conforms)
                    FixRule(assetAuditTreeElement, assetRule);

                progress += 1f;
                yield return progress / list.Count;
            }
        }

        
        public static void FixRule(AssetAuditTreeElement data , AssetRule assetRule)
        {

            string ruleAssetPath = AssetDatabase.GUIDToAssetPath(assetRule.AssetGuid);
            string affectedAssetPath = data.projectPath;

            switch (data.assetType)
            {
                case AssetType.Texture:
                
                    TextureImporter ruleTexImporter = AssetImporter.GetAtPath(ruleAssetPath) as TextureImporter;
                    TextureImporter affectedAssetTexImporter = AssetImporter.GetAtPath(affectedAssetPath) as TextureImporter;

                    if (assetRule.SelectiveMode)
                    {
                        SerializedObject ruleImporterSO = new SerializedObject(ruleTexImporter);
                        SerializedObject affectedAssetImporterSO = new SerializedObject(affectedAssetTexImporter);
                        CopySelectiveProperties(affectedAssetImporterSO, ruleImporterSO, assetRule);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(ruleTexImporter, affectedAssetTexImporter);
                    }
                    affectedAssetTexImporter.userData = "";
                    affectedAssetTexImporter.SaveAndReimport();
                
                    break;

                case AssetType.Model:
                    
                    ModelImporter ruleModelImporter = AssetImporter.GetAtPath(ruleAssetPath) as ModelImporter;
                    ModelImporter affectedAssetModelImporter = AssetImporter.GetAtPath(affectedAssetPath) as ModelImporter;

                    if (assetRule.SelectiveMode)
                    {
                        SerializedObject ruleImporterSO = new SerializedObject(ruleModelImporter);
                        SerializedObject affectedAssetImporterSO = new SerializedObject(affectedAssetModelImporter);
                        CopySelectiveProperties(affectedAssetImporterSO, ruleImporterSO, assetRule);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(ruleModelImporter, affectedAssetModelImporter);
                    }
                    affectedAssetModelImporter.userData = "";
                    affectedAssetModelImporter.SaveAndReimport();
                    break;
                    
                case AssetType.Audio:
                    
                    AudioImporter ruleAudioImporter = AssetImporter.GetAtPath(ruleAssetPath) as AudioImporter;
                    AudioImporter affectedAssetAudioImporter = AssetImporter.GetAtPath(affectedAssetPath) as AudioImporter;

                    if (assetRule.SelectiveMode)
                    {
                        SerializedObject ruleImporterSO = new SerializedObject(ruleAudioImporter);
                        SerializedObject affectedAssetImporterSO = new SerializedObject(affectedAssetAudioImporter);
                        CopySelectiveProperties(affectedAssetImporterSO, ruleImporterSO, assetRule);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(ruleAudioImporter, affectedAssetAudioImporter);
                    }
                    affectedAssetAudioImporter.userData = "";
                    affectedAssetAudioImporter.SaveAndReimport();
                    break;
                case AssetType.Folder:
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }

            data.conforms = true;
        }
        
        
        private static void CopySelectiveProperties(SerializedObject affectedAssetImporterSO, SerializedObject ruleImporterSO, AssetRule assetRule)
        {
            foreach (string property in assetRule.SelectiveProperties)
            {
                string realname = GetPropertyNameFromDisplayName(affectedAssetImporterSO, property);

                SerializedProperty assetRuleSP = ruleImporterSO.FindProperty(realname);

                affectedAssetImporterSO.CopyFromSerializedProperty(assetRuleSP);

                bool applyModifiedProperties = affectedAssetImporterSO.ApplyModifiedProperties();

                if (!applyModifiedProperties) Debug.Log(" copy failed ");
            }
        }

        
        public static bool HaveEqualProperties<T>(T rhs, T lhs)
        {
            if (rhs != null && lhs != null)
            {
                Type type = typeof(T);
                foreach (PropertyInfo pi in type.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
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
            SerializedProperty iter = so.GetIterator();

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
            SerializedProperty soIter = so.GetIterator();

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
            string audioProxy = AssetAuditorPreferences.ProxyAudioPath;
            string ext = audioProxy.Substring( audioProxy.LastIndexOf( '.' ) );
            string newAssetPath = AssetAuditorPreferences.ProxyAssetsDirectory + Path.DirectorySeparatorChar + newRule.RuleName + ext;
            if( !AssetDatabase.CopyAsset( audioProxy, newAssetPath ) )
            {
                Debug.LogWarning( "Failed to copy proxy asset from " + audioProxy );
                return;
            }
            
            AssetDatabase.ImportAsset(newAssetPath);
            WriteUserData(newAssetPath , newRule, ref currentAsset);
        }

        public static void CreateProxyModel(AssetRule newRule, ref string currentAsset)
        {
            string modelProxy = AssetAuditorPreferences.ProxyModelPath;
            string ext = modelProxy.Substring( modelProxy.LastIndexOf( '.' ) );
            string newAssetPath = AssetAuditorPreferences.ProxyAssetsDirectory + Path.DirectorySeparatorChar + newRule.RuleName + ext;
            if( !AssetDatabase.CopyAsset( modelProxy, newAssetPath ) )
            {
                Debug.LogWarning( "Failed to copy proxy asset from " + modelProxy );
                return;
            }

            AssetDatabase.ImportAsset(newAssetPath);
            WriteUserData(newAssetPath , newRule, ref currentAsset);
        }
        
        public static void CreateProxyTexture(AssetRule newRule, ref string currentAsset)
        {
            string textureProxy = AssetAuditorPreferences.ProxyTexturePath;
            string ext = textureProxy.Substring( textureProxy.LastIndexOf( '.' ) );
            string newAssetPath = AssetAuditorPreferences.ProxyAssetsDirectory + Path.DirectorySeparatorChar + newRule.RuleName + ext;
            if( !AssetDatabase.CopyAsset( textureProxy, newAssetPath ) )
            {
                Debug.LogWarning( "Failed to copy proxy asset from " + textureProxy );
                return;
            }

            AssetDatabase.ImportAsset(newAssetPath);
            WriteUserData(newAssetPath , newRule , ref currentAsset);
        }

        
        public static void WriteUserData(string path, AssetRule assetRule, ref string currentAsset)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            assetRule.AssetGuid = AssetDatabase.AssetPathToGUID(assetImporter.assetPath);

            assetImporter.userData = JsonUtility.ToJson(assetRule);

            EditorUtility.SetDirty(assetImporter);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.SaveAssets();

            currentAsset = assetRule.AssetGuid;
        }
        
        
        public static void WriteUserData(string path , AssetRule assetRule)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            assetRule.AssetGuid = AssetDatabase.AssetPathToGUID(assetImporter.assetPath);

            assetImporter.userData = JsonUtility.ToJson(assetRule);

            EditorUtility.SetDirty(assetImporter);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.SaveAssets();
        }

        
        public static bool RuleExists(AssetRule assetRule)
        {
            if (!AssetDatabase.IsValidFolder(AssetAuditorPreferences.ProxyAssetsDirectory))
            {
                string folder = AssetAuditorPreferences.ProxyAssetsDirectory.Split(Path.DirectorySeparatorChar).Last();

                string dir = AssetAuditorPreferences.ProxyAssetsDirectory.Substring(0,
                    AssetAuditorPreferences.ProxyAssetsDirectory.Length - folder.Length);
               
                AssetDatabase.CreateFolder(dir, folder);
            }
                 
            foreach (string asset in AssetDatabase.FindAssets("", new[] {AssetAuditorPreferences.ProxyAssetsDirectory}))
            {
                string guidToAssetPath = AssetDatabase.GUIDToAssetPath(asset);
                AssetImporter assetImporter = AssetImporter.GetAtPath(guidToAssetPath);

                AssetRule ar = new AssetRule();
                ar = JsonUtility.FromJson<AssetRule>(assetImporter.userData);
                if (ar.RuleName == assetRule.RuleName && ar.WildCard == assetRule.WildCard &&
                    ar.WildCardMatchType == assetRule.WildCardMatchType) return true;
            }
            return false;
        }
        
        
    }
}
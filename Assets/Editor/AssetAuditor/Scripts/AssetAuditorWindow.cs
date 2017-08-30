using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityAssetAuditor
{
    class AssetAuditorWindow : EditorWindow
    {
        [NonSerialized] bool m_Initialized;

        [SerializeField]
        static TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading

        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

        static AssetAuditTreeView m_TreeView;
        private static List<AssetAuditor.AssetRule> assetRules;
        private List<string> assetRuleNames;
        private static int selected = 0;
        private string[] affectedAssets;
        private Action<AssetAuditTreeElement> act;

        [MenuItem("Asset Auditing/Auditor View")]
        public static AssetAuditorWindow GetWindow()
        {
            var window = GetWindow<AssetAuditorWindow>();
            window.titleContent = new GUIContent("Audit");

            return window;
        }


        Rect multiColumnTreeViewRect
        {
            get { return new Rect(20, 90, position.width - 40, position.height - 60); }
        }

        Rect toolbarRect
        {
            get { return new Rect(20f, 70f, position.width - 40f, 20f); }
        }

        Rect bottomToolbarRect
        {
            get { return new Rect(20f, position.height - 18f, position.width - 40f, 16f); }
        }

        Rect ruleSelectRect
        {
            get { return new Rect(20f, 10f, position.width - 40, 15); }
        }

        Rect wildCardDisplayRect
        {
            get { return new Rect(20, 30, position.width - 40f, 15f); }
        }

        Rect SelectivePropRect
        {
            get { return new Rect(20, 50, position.width - 40f, 15f); }
        }

        public AssetAuditTreeView treeView
        {
            get { return m_TreeView; }
        }

        void InitIfNeeded()
        {
            if (!m_Initialized)
            {
                act = FixRule;
                assetRuleNames = new List<string>();
                GatherAssetRules();
                // Check if it already exists (deserialized from window layout file or scriptable object)
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                var headerState = AssetAuditTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                m_MultiColumnHeaderState = headerState;


                var multiColumnHeader = new MultiColumnHeader(headerState);
                var data = GetData();
                if (data == null) return;
                var treeModel = new TreeModel<AssetAuditTreeElement>(data);
                m_TreeView = new AssetAuditTreeView(m_TreeViewState, multiColumnHeader, treeModel, act);

                m_Initialized = true;
            }
        }

        IList<AssetAuditTreeElement> GetData()
        {
            int id = -1;
            if (assetRules == null)
            {
                GatherAssetRules();
            }

            List<AssetAuditTreeElement> elements = new List<AssetAuditTreeElement>();


            // check to see if there any rules
            if (assetRules.Count == 0) return null;


            // get the affected assets
            affectedAssets = AssetAuditor.GetAffectedAssets(assetRules[selected]);

            // build the directory tree from the affected assets
            elements.Add(new AssetAuditTreeElement("Root", "", -1, 0, false, false, AssetAuditor.AssetType.Folder ));

            // early out if there are no affected assets
            if (affectedAssets.Length == 0)
            {
                return elements;
            }

            AssetAuditTreeElement assetsFolder = new AssetAuditTreeElement("Assets", "Assets", 0, id++, false, false,
                AssetAuditor.AssetType.Folder );
            // add the project root "Assets" folder
            elements.Add(assetsFolder);

            if (assetsFolder.children == null) assetsFolder.children = new List<TreeElement>();
            // search the next level down directories

            var dirs = Directory.GetDirectories(Application.dataPath);

            foreach (string t in dirs)
            {
                // check if the directory actually contais any of the assets we want to show
                foreach (var affectedAsset in affectedAssets)
                {
                    if (affectedAsset.Contains(t))
                    {
                        AddChildrenRecursive(assetsFolder, t, ref id, elements);
                        break;
                    }
                }

                CheckAffectedAssets(elements, t, assetsFolder.depth, ref id);
            }

            CheckAffectedAssets(elements, Application.dataPath, assetsFolder.depth, ref id);

            return elements;
        }

        private void CheckAffectedAssets(List<AssetAuditTreeElement> elements, string searchDirectory, int depth,
            ref int id)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                searchDirectory = searchDirectory.Replace('/', '\\');

            foreach (var affectedAsset in affectedAssets)
            {
                if (new DirectoryInfo(affectedAsset).Parent.FullName != searchDirectory) continue;
                
                SerializedObject assetImporterSO = null;
                SerializedObject ruleImporterSO = null;

                if (AssetAuditor.TypeFromAssetType(assetRules[selected].assetType) == typeof(Texture))
                {
                    var assetimporter =
                        AssetImporter.GetAtPath(affectedAsset.Substring(Application.dataPath.Length - 6)) as
                            TextureImporter;
                    var ruleimporter =
                        AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRules[selected].AssetGuid)) as
                            TextureImporter;

                    if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                        continue; // this shouldnt happen but is a nice failsafe

                    assetImporterSO = new SerializedObject(assetimporter);
                    ruleImporterSO = new SerializedObject(ruleimporter);
                }
                    
                if (AssetAuditor.TypeFromAssetType(assetRules[selected].assetType) == typeof(GameObject))
                {
                    var assetimporter =
                        AssetImporter.GetAtPath(affectedAsset.Substring(Application.dataPath.Length - 6)) as
                            ModelImporter;
                    var ruleimporter =
                        AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRules[selected].AssetGuid)) as
                            ModelImporter;

                    if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                        continue; // this shouldnt happen but is a nice failsafe

                    assetImporterSO = new SerializedObject(assetimporter);
                    ruleImporterSO = new SerializedObject(ruleimporter);
                }

                if (AssetAuditor.TypeFromAssetType(assetRules[selected].assetType) == typeof(AudioClip))
                {
                    var assetimporter =
                        AssetImporter.GetAtPath(affectedAsset.Substring(Application.dataPath.Length - 6)) as
                            AudioImporter;
                    var ruleimporter =
                        AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assetRules[selected].AssetGuid)) as
                            AudioImporter;

                    if (assetimporter.GetInstanceID() == ruleimporter.GetInstanceID())
                        continue; // this shouldnt happen but is a nice failsafe
                        
                    assetImporterSO = new SerializedObject(assetimporter);
                    ruleImporterSO = new SerializedObject(ruleimporter);
                }

                if (assetImporterSO == null || ruleImporterSO == null) continue; // TODO log message here
                    
                if (!assetRules[selected].SelectiveMode)
                {
                    bool equal = AssetAuditor.CompareSerializedObject(assetImporterSO, ruleImporterSO);

                    elements.Add(new AssetAuditTreeElement(Path.GetFileName(affectedAsset), affectedAsset,
                        depth + 1, id++, true, equal, assetRules[selected].assetType));
                }
                else
                {
                    foreach (string property in assetRules[selected].SelectiveProperties)
                    {
                        var realname = AssetAuditor.GetPropertyNameFromDisplayName(assetImporterSO, property);

                        var foundAssetSP = assetImporterSO.FindProperty(realname);

                        var assetRuleSP = ruleImporterSO.FindProperty(realname);

                        if (AssetAuditor.CompareSerializedProperty(foundAssetSP, assetRuleSP))
                        {
                            elements.Add(new AssetAuditTreeElement(Path.GetFileName(affectedAsset),
                                affectedAsset,
                                depth + 1, id++, true, true, assetRules[selected].assetType));
                        }
                        else
                        {
                            elements.Add(new AssetAuditTreeElement(Path.GetFileName(affectedAsset),
                                affectedAsset,
                                depth + 1, id++, true, false, assetRules[selected].assetType));
                        }
                    }
                }
            }
        }


        private void AddChildrenRecursive(AssetAuditTreeElement parent, string dir, ref int id,
            List<AssetAuditTreeElement>
                _elements)
        {
            var child = new AssetAuditTreeElement(new DirectoryInfo(dir).Name, "", parent.depth + 1, id++, false, false,
                AssetAuditor.AssetType.Folder);

            _elements.Add(child);

            var dirs = Directory.GetDirectories(dir);
            foreach (string t in dirs)
            {
                if (affectedAssets.Any(affectedAsset => affectedAsset.Contains(t)))
                {
                    AddChildrenRecursive(child, t, ref id, _elements);
                }

                CheckAffectedAssets(_elements, t, child.depth + 1, ref id);
            }
        }


        void OnSelectionChange()
        {
            if (!m_Initialized)
                return;
            m_TreeView.treeModel.SetData(GetData());
            m_TreeView.Reload();
        }

        private void OnFocus()
        {
            if (m_Initialized)
            {
                GatherAssetRules();
                m_TreeView.treeModel.SetData(GetData());
                m_TreeView.Reload();
            }
        }

        void OnGUI()
        {
            if (!m_Initialized)
            {
                InitIfNeeded();
                GUILayout.Label(" no asset rules have been found in the project");
                return;
            }
            DoRuleSelectionGUI();
            SearchBar(toolbarRect);
            DoTreeView(multiColumnTreeViewRect);
            BottomToolBar(bottomToolbarRect);
        }

        private void DoRuleSelectionGUI()
        {
            EditorGUI.BeginChangeCheck();
            selected = EditorGUI.Popup(ruleSelectRect, "Rule Name", selected, assetRuleNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                m_TreeView.treeModel.SetData(GetData());
                m_TreeView.Reload();
            }

            // make wildcard editable and update selection from it
            // tODO doesnt update the wildcard that has been saved
            if (assetRules != null && selected != -1 && !string.IsNullOrEmpty(assetRules[selected].WildCard))
            {
                AssetAuditor.AssetRule ar = assetRules[selected];
                EditorGUI.BeginChangeCheck();
                ar.WildCard = EditorGUI.TextField(wildCardDisplayRect, "WildCard ", ar.WildCard);
                if (EditorGUI.EndChangeCheck())
                {
                    assetRules[selected] = ar;
                    m_TreeView.treeModel.SetData(GetData());
                    m_TreeView.Reload();
                    AssetAuditor.WriteUserData(AssetDatabase.GUIDToAssetPath(ar.AssetGuid), ar);
                }
                
                if(ar.SelectiveProperties != null && ar.SelectiveProperties.Count > 0)
                EditorGUI.IntPopup(SelectivePropRect, "Selective Properties", 0, ar.SelectiveProperties.ToArray(),
                    new int[]{0});
            }
        }

        private void GatherAssetRules()
        {
            // clear current list
            assetRules = new List<AssetAuditor.AssetRule>();

            // get all assets in the proxyassets folder
            foreach (var asset in AssetDatabase.FindAssets("", new[] {"Assets/Editor/AssetAuditor/ProxyAssets"}))
            {
                var guidToAssetPath = AssetDatabase.GUIDToAssetPath(asset);
                var assetImporter = AssetImporter.GetAtPath(guidToAssetPath);
                AssetAuditor.AssetRule ar = new AssetAuditor.AssetRule();
                ar = JsonUtility.FromJson<AssetAuditor.AssetRule>(assetImporter.userData);
                assetRules.Add(ar);
            }
            assetRuleNames.Clear();
            foreach (var assetRule in assetRules)
            {
                assetRuleNames.Add(assetRule.RuleName);
            }
        }

        void SearchBar(Rect rect)
        {
            treeView.searchString = SearchField.OnGUI(rect, treeView.searchString);
        }

        void DoTreeView(Rect rect)
        {
            m_TreeView.OnGUI(rect);
        }

        void BottomToolBar(Rect rect)
        {
            var style = "miniButton";
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width / 3, rect.height), "Expand All", style))
            {
                treeView.ExpandAll();
            }
            if (GUI.Button(new Rect(rect.x + rect.width / 3, rect.y, rect.width / 3, rect.height), "Collapse All",
                style))
            {
                treeView.CollapseAll();
            }
            if (GUI.Button(new Rect(rect.x + ((rect.width / 3) * 2), rect.y, rect.width / 3, rect.height), "Fix All",
                style))
            {
                FixAll();
            }
        }

        private void FixAll()
        {
            List<AssetAuditTreeElement> list = new List<AssetAuditTreeElement>();
            TreeElementUtility.TreeToList(m_TreeView.treeModel.root, list);
            foreach (var assetAuditTreeElement in list)
            {
                if (assetAuditTreeElement.isAsset && !assetAuditTreeElement.conforms)
                    FixRule(assetAuditTreeElement);
            }
        }

        public void FixRule(AssetAuditTreeElement data)
        {

            string ruleAssetPath = AssetDatabase.GUIDToAssetPath(assetRules[selected].AssetGuid);
            string affectedAssetPath = data.projectPath.Substring(Application.dataPath.Length - 6);

            switch (data.assetType)
            {
                case AssetAuditor.AssetType.Texture:
                
                    var ruleTexImporter = AssetImporter.GetAtPath(ruleAssetPath) as TextureImporter;
                    var affectedAssetTexImporter = AssetImporter.GetAtPath(affectedAssetPath) as TextureImporter;

                    if (assetRules[selected].SelectiveMode)
                    {
                        var ruleImporterSO = new SerializedObject(ruleTexImporter);
                        var affectedAssetImporterSO = new SerializedObject(affectedAssetTexImporter);
                        CopySelectiveProperties(affectedAssetImporterSO, ruleImporterSO);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(ruleTexImporter, affectedAssetTexImporter);
                    }
                    affectedAssetTexImporter.SaveAndReimport();
                
                    break;

                case AssetAuditor.AssetType.Model:
                    
                    var ruleModelImporter = AssetImporter.GetAtPath(ruleAssetPath) as ModelImporter;
                    var affectedAssetModelImporter = AssetImporter.GetAtPath(affectedAssetPath) as ModelImporter;

                    if (assetRules[selected].SelectiveMode)
                    {
                        var ruleImporterSO = new SerializedObject(ruleModelImporter);
                        var affectedAssetImporterSO = new SerializedObject(affectedAssetModelImporter);
                        CopySelectiveProperties(affectedAssetImporterSO, ruleImporterSO);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(ruleModelImporter, affectedAssetModelImporter);
                    }
                    affectedAssetModelImporter.SaveAndReimport();
                    break;
                    
                case AssetAuditor.AssetType.Audio:
                    
                    var ruleAudioImporter = AssetImporter.GetAtPath(ruleAssetPath) as AudioImporter;
                    var affectedAssetAudioImporter = AssetImporter.GetAtPath(affectedAssetPath) as AudioImporter;

                    if (assetRules[selected].SelectiveMode)
                    {
                        var ruleImporterSO = new SerializedObject(ruleAudioImporter);
                        var affectedAssetImporterSO = new SerializedObject(affectedAssetAudioImporter);
                        CopySelectiveProperties(affectedAssetImporterSO, ruleImporterSO);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(ruleAudioImporter, affectedAssetAudioImporter);
                    }
                    affectedAssetAudioImporter.SaveAndReimport();
                    break;
                case AssetAuditor.AssetType.Folder:
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            var headerState = AssetAuditTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
            
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
            
            m_MultiColumnHeaderState = headerState;
            var multiColumnHeader = new MultiColumnHeader(headerState);
            var treeModel = new TreeModel<AssetAuditTreeElement>(GetData());
            m_TreeView = new AssetAuditTreeView(m_TreeViewState, multiColumnHeader, treeModel, act);
        }

        private static void CopySelectiveProperties(SerializedObject affectedAssetImporterSO, SerializedObject ruleImporterSO)
        {
            foreach (string property in assetRules[selected].SelectiveProperties)
            {
                var realname = AssetAuditor.GetPropertyNameFromDisplayName(affectedAssetImporterSO, property);

                var assetRuleSP = ruleImporterSO.FindProperty(realname);

                affectedAssetImporterSO.CopyFromSerializedProperty(assetRuleSP);

                var applyModifiedProperties = affectedAssetImporterSO.ApplyModifiedProperties();

                if (!applyModifiedProperties) Debug.Log(" copy failed ");
            }
        }
    }

    internal static class SearchField
    {
        static class Styles
        {
            public static GUIStyle searchField = "SearchTextField";
            public static GUIStyle searchFieldCancelButton = "SearchCancelButton";
            public static GUIStyle searchFieldCancelButtonEmpty = "SearchCancelButtonEmpty";
        }

        public static string OnGUI(Rect position, string text)
        {
            // Search field 
            Rect textRect = position;
            textRect.width -= 15;
            text = EditorGUI.TextField(textRect, GUIContent.none, text, Styles.searchField);

            // Cancel button
            Rect buttonRect = position;
            buttonRect.x += position.width - 15;
            buttonRect.width = 15;
            if (GUI.Button(buttonRect, GUIContent.none,
                    text != "" ? Styles.searchFieldCancelButton : Styles.searchFieldCancelButtonEmpty) && text != "")
            {
                text = "";
                GUIUtility.keyboardControl = 0;
            }
            return text;
        }
    }
}
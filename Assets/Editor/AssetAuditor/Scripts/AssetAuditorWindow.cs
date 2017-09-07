using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityAssetAuditor
{

    public delegate void OnGatherAssetRulesComplete();

    public delegate void OnGatherDataComplete();
    
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
        private static int selectedSelective = 0;
        private string[] affectedAssets;
        private Action<AssetAuditTreeElement> act;

        private OnGatherAssetRulesComplete onGatherAssetRulesComplete;
        private OnGatherDataComplete onGatherDataComplete;
        private List<AssetAuditTreeElement> elements;
        private bool gatherAssetsComplete;
        private bool gatherDataComplete;
        private List<AssetAuditTreeElement> tempElements;


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
        
        Rect progressBarRect
        {
            get { return new Rect(20f, position.height - 36f, position.width - 40f, 16f); }
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
                
                
                if(!gatherAssetsComplete) GatherAssetRules();

                if(!gatherDataComplete && gatherAssetsComplete)
                    GatherData();

                if (gatherAssetsComplete && gatherDataComplete) m_Initialized = true;
            }
        }


        void GatherAssetRules()
        {
            gatherAssetsComplete = false;
            AssetAuditor.queueComplete += OnGatherAssetRulesComplete;
                
            assetRules = new List<AssetAuditor.AssetRule>();
            assetRuleNames = new List<string>();
                
                
            AssetAuditor.ClearQueue();
            AssetAuditor.AddEnumerator(AssetAuditor.GatherAssetRules(assetRules,assetRuleNames));   
        }
        
        private void OnGatherAssetRulesComplete()
        {
            AssetAuditor.queueComplete -= OnGatherAssetRulesComplete;
            gatherAssetsComplete = true;
            GatherData();
        }


        void GatherData()
        {
            gatherDataComplete = false;
            AssetAuditor.queueComplete += OnGatherDataComplete;
            elements = new List<AssetAuditTreeElement>();
            
            AssetAuditor.ClearQueue();
            AssetAuditor.UpdateAffectedAssets(assetRules[selected]);
            AssetAuditor.AddEnumerator(AssetAuditor.GatherData(assetRules[selected],elements,selectedSelective));  
        }
        
        private void OnGatherDataComplete()
        {
            AssetAuditor.queueComplete -= OnGatherDataComplete;

           // elements = tempElements;

            if (m_Initialized)
            {
                // Check if it already exists (deserialized from window layout file or scriptable object)
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                m_TreeView.treeModel.SetData(elements);
                m_TreeView.Reload();
            }

            gatherDataComplete = true;
        }


        void OnSelectionChange()
        {
       //     if (!m_Initialized)
       //         return;
       //     m_TreeView.treeModel.SetData(GetData());
       //     m_TreeView.Reload();
        }

        private void OnFocus()
        {
            // this doesn't seem to be needed
            /*if (m_Initialized)
            {
                GatherAssetRules();
                m_TreeView.treeModel.SetData(GetData());
                m_TreeView.Reload();
            }*/
        }

        void OnGUI()
        {
            if (!m_Initialized)
            {
                InitIfNeeded();
                if (gatherAssetsComplete && gatherDataComplete)
                {
                    

                    // Check if it already exists (deserialized from window layout file or scriptable object)
                   if (m_TreeViewState == null)
                        m_TreeViewState = new TreeViewState();

                    var headerState =
                        AssetAuditTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
                    if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                        MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                    m_MultiColumnHeaderState = headerState;

                    var multiColumnHeader = new MultiColumnHeader(headerState); 
                    Debug.Log(elements.Count);
                    var treeModel = new TreeModel<AssetAuditTreeElement>(elements); 
                    m_TreeView = new AssetAuditTreeView(m_TreeViewState, multiColumnHeader, treeModel, act); 
                    GUILayout.Label(" no asset rules have been found in the project");

                    m_Initialized = true;
                }
                return;
            }
            DoRuleSelectionGUI();
            SearchBar(toolbarRect);
            DoTreeView(multiColumnTreeViewRect);
            DoProgressBar(progressBarRect);
            BottomToolBar(bottomToolbarRect);
        }

        private void DoProgressBar(Rect rect)
        {
            var progress = AssetAuditor.GetProgress();
            EditorGUI.ProgressBar(progressBarRect , progress, " Search Progress " + progress.ToString("0.00%"));
        }

        private void DoRuleSelectionGUI()
        {   
            EditorGUI.BeginChangeCheck();
            selected = EditorGUI.Popup(ruleSelectRect, "Rule Name", selected, assetRuleNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                selectedSelective = 0;
                GatherData();
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
                    GatherData();
                    AssetAuditor.WriteUserData(AssetDatabase.GUIDToAssetPath(ar.AssetGuid), ar);
                }
                
                if (ar.SelectiveProperties != null && ar.SelectiveProperties.Count > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    selectedSelective = EditorGUI.Popup(SelectivePropRect, "Selective Properties", selectedSelective, ar.SelectiveProperties.ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        GatherData();
                    }
                }
            }
        }



        void SearchBar(Rect rect)
        {
            if(treeView != null)
            treeView.searchString = SearchField.OnGUI(rect, treeView.searchString);
        }

        void DoTreeView(Rect rect)
        {
            if(m_TreeView != null)
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
            // todo move fix rule to asset auditor class
       //     var treeModel = new TreeModel<AssetAuditTreeElement>(GetData());
       //     m_TreeView = new AssetAuditTreeView(m_TreeViewState, multiColumnHeader, treeModel, act);
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
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
        [SerializeField]
        static TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading

        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

        static AssetAuditTreeView m_TreeView;
        private static List<AssetAuditor.AssetRule> assetRules;
        private List<string> assetRuleNames;
        private static int selected = 0;
        private static int selectedSelective = 0;
        private static bool editSelective;
        private static int editSelectiveProp = 0;
        private string[] affectedAssets;
        private Action<AssetAuditTreeElement> act;

        private OnGatherAssetRulesComplete onGatherAssetRulesComplete;
        private OnGatherDataComplete onGatherDataComplete;
        private List<AssetAuditTreeElement> elements;
        private bool gatherAssetsComplete;
        private bool gatherDataComplete;
        private List<AssetAuditTreeElement> tempElements;

        private enum State
        {
            Uninitialized,
            GatheringRules,
            GatheringData,
            Initialized,
            NoAssetRules
        }

        [NonSerialized]
        private State state = State.Uninitialized;


        [MenuItem("Asset Auditing/Auditor View")]
        public static AssetAuditorWindow GetWindow()
        {
            var window = GetWindow<AssetAuditorWindow>();
            window.titleContent = new GUIContent("Audit");

            return window;
        }


        Rect multiColumnTreeViewRect
        {
            get { return new Rect(20, 110, position.width - 40, position.height - 130); }
        }

        Rect toolbarRect
        {
            get { return new Rect(20f, 90f, position.width - 40f, 20f); }
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

        Rect AddSelectivePropRect
        {
            get {return new Rect(20,70,18,18);}
        }

        Rect RemoveSelectivePropRect
        {
            get { return new Rect(40,70,18,18);}
        }

        Rect EditSelectedPropButtonRect
        {
            get {return new Rect(60,70, 40,18 );}
        }
        
        Rect EditSelectedPropDropDownRect
        {
            get {return new Rect(110,70, 400,18 );}
        }

        public AssetAuditTreeView treeView
        {
            get { return m_TreeView; }
        }

        private void  FixRule(AssetAuditTreeElement assetAuditTreeElement)
        {
            AssetAuditor.FixRule(assetAuditTreeElement , assetRules[selected]);

            elements = new List<AssetAuditTreeElement>();
            AssetAuditor.queueComplete += OnGatherDataComplete;
            AssetAuditor.AddEnumerator(AssetAuditor.GatherData(assetRules[selected], elements , selectedSelective));
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

            if (assetRules.Count > 0)
            {
                gatherAssetsComplete = true;
            }
            else
            {
                gatherAssetsComplete = false;
                state = State.NoAssetRules;
            }
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

            // Check if it already exists (deserialized from window layout file or scriptable object)
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_TreeView != null && elements.Count > 0)
            {
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
            switch (state)
            {
                case State.Uninitialized:
                    act = FixRule;
                    GatherAssetRules();
                    state = State.GatheringRules;
                    break;

                case State.GatheringRules:
                    if (gatherAssetsComplete)
                    {
                        GatherData();
                        state = State.GatheringData;
                    }
                    break;

                case State.GatheringData:
                    if (gatherDataComplete)
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
                        var treeModel = new TreeModel<AssetAuditTreeElement>(elements); 
                        m_TreeView = new AssetAuditTreeView(m_TreeViewState, multiColumnHeader, treeModel, act); 
                        GUILayout.Label(" no asset rules have been found in the project");

                        state = State.Initialized;
                    }
                    break;

                case State.Initialized:
                    DoRuleSelectionGUI();
                    SearchBar(toolbarRect);
                    DoTreeView(multiColumnTreeViewRect);
                    BottomToolBar(bottomToolbarRect);
                    break;
                    
                case State.NoAssetRules:
                    DoNoAssetRuleGUI();
                    break;
                        
            }
                    
            DoProgressBar(progressBarRect);
        }

        private void DoNoAssetRuleGUI()
        {
            GUILayout.Label(" No Asset Rules Are Present In The Project ");

            if (GUILayout.Button("Search Again For Assets ")) /// TODO add directory to string for proxy asset path
            {
                GatherAssetRules();
                state = State.GatheringRules;
            }
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
            if (assetRules != null && selected != -1)// && !string.IsNullOrEmpty(assetRules[selected].WildCard))
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
                    selectedSelective = EditorGUI.Popup(SelectivePropRect, "Selective Properties", selectedSelective, ar.SelectiveProperties.ToArray());
                }
                else
                {
                    EditorGUI.LabelField(SelectivePropRect , " No Selective Properties in the Asset Rule");
                }

                if (GUI.Button(AddSelectivePropRect, "+"))
                {
                    // add a new selective property              
                    if (ar.SelectiveProperties != null && !ar.SelectiveProperties.Contains("Unnasigned property")) ar.SelectiveProperties.Add("Unnasigned property");
                    assetRules[selected] = ar;
                    GatherData();
                    AssetAuditor.WriteUserData(AssetDatabase.GUIDToAssetPath(ar.AssetGuid), ar);
                }
                if (ar.SelectiveProperties != null && ar.SelectiveProperties.Count > 0)
                {
                    if (GUI.Button(RemoveSelectivePropRect, "-"))
                    {
                        // remove last selective property
                        ar.SelectiveProperties.RemoveAt(ar.SelectiveProperties.Count - 1);
                        
                        if (ar.SelectiveProperties.Count == 0)
                            ar.SelectiveMode = false;
                        
                        assetRules[selected] = ar;
                        GatherData();
                        AssetAuditor.WriteUserData(AssetDatabase.GUIDToAssetPath(ar.AssetGuid), ar);
                    }
                    editSelective = GUI.Toggle(EditSelectedPropButtonRect, editSelective, "Edit", "Button");
                    if (editSelective)
                    {
                        SerializedObject so = new SerializedObject(
                            AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(ar.AssetGuid)));
                        EditorGUI.BeginChangeCheck();
                        editSelectiveProp = EditorGUI.Popup(EditSelectedPropDropDownRect, editSelectiveProp,
                            AssetAuditor.GetPropertyNames(so));

                        if (EditorGUI.EndChangeCheck())
                        {
                            ar.SelectiveProperties[selectedSelective] =
                                AssetAuditor.GetPropertyNames(so)[editSelectiveProp];
                            
                            assetRules[selected] = ar;
                            GatherData();
                            AssetAuditor.WriteUserData(AssetDatabase.GUIDToAssetPath(ar.AssetGuid), ar);
                        }
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
                AssetAuditor.AddEnumerator(AssetAuditor.FixAll(m_TreeView , assetRules[selected]));
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
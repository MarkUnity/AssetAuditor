using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;


namespace UnityAssetAuditor
{
    public class AssetAuditTreeView : TreeViewWithTreeModel<AssetAuditTreeElement>
    {
        private readonly Action<AssetAuditTreeElement> _ruleFixEvent;
        const float kRowHeights = 20f;
        const float kIconWidth = 18f;
        public bool showControls { get; set; }

        // All columns
        enum MyColumns
        {
            Name,
            Conforms
        }


        public static void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new NullReferenceException("root");
            if (result == null)
                throw new NullReferenceException("result");

            result.Clear();

            if (root.children == null)
                return;

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for (int i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.children[i]);
                    }
                }
            }
        }

        public AssetAuditTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<AssetAuditTreeElement> model , Action<AssetAuditTreeElement> ruleFixEvent) : base(state, multicolumnHeader, model)
        {
            _ruleFixEvent = ruleFixEvent;

            // Custom setup
            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = kIconWidth;
            Reload();
        }


        // Note we We only build the visible rows, only the backend has the full tree information. 
        // The treeview only creates info for the row list.
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }


        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<AssetAuditTreeElement>)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<AssetAuditTreeElement> item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case MyColumns.Name:
                    {
                        // Do toggle
                        Rect iconRect = cellRect;
                        iconRect.x += GetContentIndent(item);
                        iconRect.width = kIconWidth;

                        Texture2D iconTex = null;
           /*             if (item.data.isAsset)
                        {
                            if (!string.IsNullOrEmpty(item.data.projectPath))
                            {
                                iconTex = AssetPreview.GetMiniThumbnail(
                                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                                        item.data.projectPath.Substring(Application.dataPath.Length - 6)));
                            }
                        }*/
                        switch (item.data.assetType)
                        {
                            case AssetAuditor.AssetType.Texture:
                                iconTex = AssetPreview.GetMiniThumbnail(
                                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                                        item.data.projectPath));
                                break;
                            case AssetAuditor.AssetType.Model:
                                iconTex = EditorGUIUtility.FindTexture("PrefabModel Icon");
                                break;
                            case AssetAuditor.AssetType.Audio:
                                iconTex = EditorGUIUtility.FindTexture("AudioClip Icon");
                                break;
                            case AssetAuditor.AssetType.Folder:
                                iconTex = EditorGUIUtility.FindTexture("Folder Icon");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        if (iconRect.xMax < cellRect.xMax)
                        {
                            GUI.DrawTexture(iconRect, iconTex);
                        }
                        // Default icon and label
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                    }
                    break;

                case MyColumns.Conforms:
                    var conforms = item.data.conforms;
                    if (item.data.isAsset)
                    {
                        if (conforms)
                        {
                            GUI.Label(cellRect , " Settings OK ");
                        }
                        else
                        {
                            if (GUI.Button(cellRect, "Fix"))
                            {
                                _ruleFixEvent.Invoke(item.data);
                            }
                        }
                    }
                    break;
            }
        }


        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    width = 0, // adjusted below
					minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false

                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Conforms"),
                    headerTextAlignment = TextAlignment.Left,
                    width = 150, // adjusted below
					minWidth = 150,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false

                },
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            // Set name column width (flexible)
            int nameColumn = (int)MyColumns.Name;
            columns[nameColumn].width = treeViewWidth - GUI.skin.verticalScrollbar.fixedWidth;
            for (int i = 0; i < columns.Length; ++i)
                if (i != nameColumn)
                    columns[nameColumn].width -= columns[i].width;

            if (columns[nameColumn].width < 60f)
                columns[nameColumn].width = 60f;

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }
}

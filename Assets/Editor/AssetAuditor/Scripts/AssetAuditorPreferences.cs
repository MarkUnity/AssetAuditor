using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetAuditorPreferences 
{
    private static string proxyAssetDir;
    private const string proxyAssetDirKey = "ProxyAssetDirectory";
    private const string proxyAssetDirDefault = "Assets/Editor/AssetAuditor/ProxyAssets";

    private static string proxyTexturePath;
    private const string proxyTexturePathKey = "ProxyTexturePath";
    private const string proxyTexturePathDefault = "Assets/Editor/AssetAuditor/Texture/DefaultTexture.jpg";

    private static string proxyModelPath;
    private const string proxyModelPathKey = "ProxyModelPath";
    private const string proxyModelPathDefault = "Assets/Editor/AssetAuditor/Models/DefaultAvatar.fbx";

    private static string proxyAudioPath;
    private const string proxyAudioPathKey = "ProxyAudioPath";
    private const string proxyAudioPathDefault = "Assets/Editor/AssetAuditor/Texture/DefaultTexture.jpg";


    static AssetAuditorPreferences()
    {
        proxyAssetDir = EditorPrefs.GetString(proxyAssetDirKey , proxyAssetDirDefault);
        proxyTexturePath = EditorPrefs.GetString(proxyTexturePathKey, proxyTexturePathDefault);
        proxyModelPath = EditorPrefs.GetString(proxyModelPathKey, proxyModelPathDefault);
        proxyAudioPath = EditorPrefs.GetString(proxyAudioPathKey, proxyAudioPathDefault);
    }
    

    [PreferenceItem("Asset Auditor")]
    public static void PreferencesGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Proxy Assets Directory" , EditorStyles.boldLabel);
        if (GUILayout.Button("...", EditorStyles.miniButton))
        {
            string path = EditorUtility.OpenFolderPanel("Select Proxy Assets Directory", proxyAssetDir, "");

            if (path.Length > 0)
            {
                proxyAssetDir = path.Substring(Application.dataPath.Length - 6);;
                EditorPrefs.SetString(proxyAssetDirKey, proxyAssetDir);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(proxyAssetDir);
        EditorGUILayout.Space();
        EditorGUILayout.Space();


        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Proxy Texture Path" , EditorStyles.boldLabel);
        if (GUILayout.Button("...", EditorStyles.miniButton))
        {
            string path  = EditorUtility.OpenFilePanel("Select Proxy Texture", proxyTexturePath, "jpg");

            if (path.Length > 0)
            {
                proxyTexturePath = path.Substring(Application.dataPath.Length - 6);
                EditorPrefs.SetString(proxyTexturePathKey, proxyTexturePath);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField( proxyTexturePath);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Proxy Model Path" , EditorStyles.boldLabel);
        if (GUILayout.Button("...", EditorStyles.miniButton))
        {
            string path  = EditorUtility.OpenFilePanel("Select Model Texture", proxyModelPath, "jpg");

            if (path.Length > 0)
            {
                proxyModelPath = path.Substring(Application.dataPath.Length - 6);
                EditorPrefs.SetString(proxyModelPathKey, proxyModelPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField( proxyModelPath);
        EditorGUILayout.Space();
        EditorGUILayout.Space();


        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Proxy Audio Path" , EditorStyles.boldLabel);
        if (GUILayout.Button("...", EditorStyles.miniButton))
        {
            string path  = EditorUtility.OpenFilePanel("Select Audio Texture", proxyAudioPath, "jpg");

            if (path.Length > 0)
            {
                proxyAudioPath = path.Substring(Application.dataPath.Length - 6);
                EditorPrefs.SetString(proxyAudioPathKey, proxyAudioPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField( proxyAudioPath);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }


    public static string ProxyAssetsDirectory
    {
        get { return proxyAssetDir; }
    }
    
    public static string ProxyTexturePath
    {
        get { return proxyTexturePath; }
    }
    
    public static string ProxyModelPath
    {
        get { return proxyModelPath; }
    }
    
    public static string ProxyAudioPath
    {
        get { return proxyAudioPath; }
    }
}

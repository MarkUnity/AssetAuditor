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
        EditorGUI.BeginChangeCheck();
        proxyAssetDir = EditorGUILayout.TextField("Proxy Asset Directory", proxyAssetDir);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(proxyAssetDirKey , proxyAssetDir);
        }

        EditorGUI.BeginChangeCheck();
        proxyTexturePath = EditorGUILayout.TextField("Proxy Texture Path", proxyTexturePath);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(proxyTexturePathKey , proxyTexturePath);
        }
        
        EditorGUI.BeginChangeCheck();
        proxyModelPath = EditorGUILayout.TextField("Proxy Model Path", proxyModelPath);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(proxyModelPathKey , proxyModelPath);
        }
        
        EditorGUI.BeginChangeCheck();
        proxyAudioPath = EditorGUILayout.TextField("Proxy Audio Path", proxyAudioPath);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(proxyAudioPathKey , proxyAudioPath);
        }
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

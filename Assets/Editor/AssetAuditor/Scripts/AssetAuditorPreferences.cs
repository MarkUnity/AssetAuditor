using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    private const string proxyAudioPathDefault = "Assets/Editor/AssetAuditor/Audio/DefaultAudio.wav";

    static AssetAuditorPreferences()
    {
        proxyAssetDir = EditorPrefs.GetString(proxyAssetDirKey , proxyAssetDirDefault);
        proxyTexturePath = EditorPrefs.GetString(proxyTexturePathKey, proxyTexturePathDefault);
        proxyModelPath = EditorPrefs.GetString(proxyModelPathKey, proxyModelPathDefault);
        proxyAudioPath = EditorPrefs.GetString(proxyAudioPathKey, proxyAudioPathDefault);
    }
    
    public static string ProxyAssetsDirectory
    {
        get
        {
            createProxyAssetsDirectory();
            return proxyAssetDir;
        }
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

    private static void createProxyAssetsDirectory()
    {
        if( AssetDatabase.IsValidFolder(proxyAssetDir) )
            return;

        AssetAuditorUtilities.CreatePath.Create(proxyAssetDir);
    }

    [PreferenceItem("Asset Auditor")]
    public static void PreferencesGUI()
    {
        EditorGUILayout.LabelField("Proxy Assets Directory" , EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        proxyAssetDir = "Assets/" + EditorGUILayout.TextField(proxyAssetDir.Remove(0,7));
        
        if (GUILayout.Button("Browse", EditorStyles.miniButton))
        {
            string path = EditorUtility.OpenFolderPanel("Select Proxy Assets Directory", proxyAssetDir, "");

            if (!path.Contains(Application.dataPath))
            {
                Debug.LogError("Selected path " + path + " is not a directory within the open project");
            }
            else if( path.Length > 0 )
            {
                proxyAssetDir = path.Substring(Application.dataPath.Length - 6);;
                EditorPrefs.SetString(proxyAssetDirKey, proxyAssetDir);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if( AssetDatabase.IsValidFolder(proxyAssetDir) == false )
        {
            EditorGUILayout.HelpBox("Folder does not exist at given path", MessageType.Warning);
            if (GUILayout.Button("Create Now", EditorStyles.miniButton))
            {
                createProxyAssetsDirectory();
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Proxy Asset Paths" , EditorStyles.boldLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texture");
        
        EditorGUILayout.BeginHorizontal();
        proxyTexturePath = "Assets/" + EditorGUILayout.TextField(proxyTexturePath.Remove(0,7));
        
        if (GUILayout.Button("Browse", EditorStyles.miniButton))
        {
            string path = EditorUtility.OpenFilePanel("Select Proxy Texture", proxyTexturePath, "jpg,png,bmp,tga");

            if( path.Length > 0 )
            {
                if( ! path.Contains(Application.dataPath) )
                {
                    Debug.LogError("Selected path <" + path + "> is not a directory within the open project");
                }
                else
                {
                    proxyTexturePath = path.Substring(Application.dataPath.Length - 6);
                    EditorPrefs.SetString(proxyTexturePathKey, proxyTexturePath);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        System.Type t = AssetDatabase.GetMainAssetTypeAtPath(proxyTexturePath);
        if( t == null || t != typeof(Texture2D) )
            EditorGUILayout.HelpBox("Proxy Texture does not exist at given path", MessageType.Warning);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Model");
        
        EditorGUILayout.BeginHorizontal();
        proxyModelPath = "Assets/" + EditorGUILayout.TextField(proxyModelPath.Remove(0,7));
        if (GUILayout.Button("Browse", EditorStyles.miniButton))
        {
            string path  = EditorUtility.OpenFilePanel("Select Proxy Model", proxyModelPath, "fbx,obj,3ds");

            if( path.Length > 0 )
            {
                if( ! path.Contains(Application.dataPath) )
                {
                    Debug.LogError("Selected path <" + path + "> is not a directory within the open project");
                }
                else
                {
                    proxyModelPath = path.Substring( Application.dataPath.Length - 6 );
                    EditorPrefs.SetString( proxyModelPathKey, proxyModelPath );
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        t = AssetDatabase.GetMainAssetTypeAtPath(proxyModelPath);
        if( t == null || t != typeof(GameObject) )
            EditorGUILayout.HelpBox("Proxy Texture does not exist at given path", MessageType.Warning);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio");

        EditorGUILayout.BeginHorizontal();
        proxyAudioPath = "Assets/" + EditorGUILayout.TextField(proxyAudioPath.Remove(0,7));
        if (GUILayout.Button("Browse", EditorStyles.miniButton))
        {
            string path  = EditorUtility.OpenFilePanel("Select Proxy Audio", proxyAudioPath, "wav,mp3,ogg");

            if( path.Length > 0 )
            {
                if( ! path.Contains(Application.dataPath) )
                {
                    Debug.LogError("Selected path <" + path + "> is not a directory within the open project");
                }
                else
                {
                    proxyAudioPath = path.Substring(Application.dataPath.Length - 6);
                    EditorPrefs.SetString(proxyAudioPathKey, proxyAudioPath);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        t = AssetDatabase.GetMainAssetTypeAtPath(proxyAudioPath);
        if( t == null || t != typeof(AudioClip) )
            EditorGUILayout.HelpBox("Proxy AudioClip does not exist at given path", MessageType.Warning);
    }
}

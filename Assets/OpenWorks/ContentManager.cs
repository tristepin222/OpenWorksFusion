using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Fusion;

using UnityEngine.SceneManagement;
using ModIO;
using ModTool;
using System.IO;
using UnityEngine.Analytics;
using System.Linq;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class ContentManager : MonoBehaviour
{
    public static ContentManager main;

    public List<Transform> syncedList = new List<Transform>();

    public NetworkObject TransformSynchronizerPrefab;

    public TransformSynchronizer sceneSynchronizer;
    public List<TransformSynchronizer> contentSynchronizers = new List<TransformSynchronizer>();

    private const long GAME_ID = 2708;

    public delegate void AuthEvent();

    private void Awake()
    {
        main = this;
    }


    private void Start()
    {
        // Starts mod.io and Will attempt authentification
        // Subcribe to OnDownloaded, which will trigger when a mod is finished downloading
        ModManager.ModFound += OnModFound;
        ModManager.ModLoaded += OnModLoaded;
    }

    public class ModData
    {
        public string authorName;
        public string modName;
        public Mod mod;

        List<GameObject> requesters = new List<GameObject>();

        public ModData(string authorName, string modName)
        {
            this.authorName = authorName;
            this.modName = modName;
        }

    }

    public Scene scene;

    public void UnloadScene()
    {
        sceneSynchronizer.Runner.Despawn(sceneSynchronizer.Object);
        SceneManager.UnloadSceneAsync(scene);
    }


    public Dictionary<string, ModData> mods = new Dictionary<string, ModData>();
    public Dictionary<string, ModData> modRequests = new Dictionary<string, ModData>();
    public ModData loadingMod;


    void Update()
    {
        if (loadingMod == null && modRequests.Count > 0)
            DownloadMod(modRequests.FirstOrDefault().Value);
    }


    public void SetScene(string authorName, string modName)
    {
        var g = NetworkRunner.Instances[0].Spawn(TransformSynchronizerPrefab, null, null, null, (Runner, NO) =>
        {
            TransformSynchronizer ts = NO.GetComponent<TransformSynchronizer>();
            ts.isSceneContent = true;
            ts.contentAuthor = authorName;
            ts.contentMod = modName;
        }
        );
    }

    public void SpawnContent(string authorName, string modName, string contentName)
    {
        var g = NetworkRunner.Instances[0].Spawn(TransformSynchronizerPrefab, null, null, null, (Runner, NO) =>
        {
            TransformSynchronizer ts = NO.GetComponent<TransformSynchronizer>();
            ts.isSceneContent = false;
            ts.contentAuthor = authorName;
            ts.contentMod = modName;
            ts.contentName = contentName;
            contentSynchronizers.Add(ts);
        }
        );
    }


    public ModData RequestMod(string authorName, string modName)
    {
        string key = authorName + "_" + modName;

        if(mods.ContainsKey(key))
        {
            return mods[key];
        }

        if (modRequests.ContainsKey(key))
            return null;

        if (loadingMod != null && loadingMod.authorName == authorName && loadingMod.modName == modName)
            return null;

        modRequests.Add(key, new ModData(authorName, modName));

        return null;
    }


    public async void DownloadMod(ModData modData)
    {
        modRequests.Remove(modData.authorName + "_" + modData.modName);
        loadingMod = modData;
        HandleDownloadedMod(await DownloadManager.main.DownloadModByAuthorAndName(modData.modName, modData.authorName));
        print("loading Mod ");
    }



    private void HandleDownloadedMod(long id)
    {
        // Will look for a folder starting  by a mod ID, each downloaded mod's folder will start by the mod id, and it pass the path to ModTool, so it can see the mod

        string modsFolder = Application.persistentDataPath + "/mod.io/0" + GAME_ID + "/data/mods/";
        string folderName = SearchForFolderWithPrefix(modsFolder, id.ToString());
        if (folderName != null)
        {
            string modFolder = modsFolder + folderName;
            // It will add the path to ModTool, so it can see the added mod, but since multiple mods can coexist, it looks for the specific mod downloaded, by its name, and then we load the first scene, it doesn't look for other scenes
            ModManager.AddSearchDirectory(modFolder);
            ModManager.Refresh();

        }
    }

    private void OnModFound(Mod mod)
    {
        // Load the mod
        mod.Load();

    }

    private void OnModLoaded(Mod mod)
    {
        //postLoadCallback?.Invoke(); // Call the callback after mod loading
        //postLoadCallback = null; // Reset callback to prevent repeated calls

        loadingMod.mod = mod;
        mods.Add(loadingMod.authorName + "_" + loadingMod.modName, loadingMod);
        loadingMod = null;
    }

    // Helper function to search for a sub folder starting with an Id in a specific folder
    string SearchForFolderWithPrefix(string searchPath, string id)
    {
        if (!Directory.Exists(searchPath))
        {
            Debug.LogError($"Path not found: {searchPath}");
            return null;
        }

        string[] directories = Directory.GetDirectories(searchPath);

        foreach (string directory in directories)
        {
            string folderName = Path.GetFileName(directory);
            if (folderName.StartsWith(id))
            {
                Debug.Log($"Found folder: {folderName}");
                return folderName;
            }
        }
        return null;
    }

}

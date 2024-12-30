using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.SceneManagement;

using Fusion;
using System;
using TMPro;

public class TransformSynchronizer : NetworkBehaviour
{
    [Networked] public bool isSceneContent { get; set; }
    [Networked] public string contentAuthor { get; set; }
    [Networked] public string contentMod { get; set; }
    [Networked] public string contentName { get; set; }

    struct locationData : INetworkStruct
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    [Networked, Capacity(100)]
    NetworkArray<locationData> locations => default;

    [Networked, Capacity(20)]
    NetworkArray<string> texts => default;


    private ChangeDetector _textChangeDetector;

    public Transform[] transforms = new Transform[100];

    public GameObject[] effects = new GameObject[100];
    public int effectsCounter = 0;

    public TextMeshPro[] textMeshes = new TextMeshPro[20];
    public int textsCounter = 0;

    public Material defaultMaterial;

    private void Awake()
    {
        defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
    public override void Spawned()
    {
        _textChangeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        //AddBehaviour<NetworkTransform>();
        StartCoroutine(LoadingRoutine());

        base.Spawned();
    }

    IEnumerator LoadingRoutine()
    {
        while (isSceneContent == false && contentName.Length == 0)
            yield return null;


        ContentManager.ModData mod = null;

        do
        {
            mod = ContentManager.main.RequestMod(contentAuthor, contentMod);

            if (mod == null)
            {
                yield return null;
                continue;
            }

            break;

        } while (mod == null);

        if (isSceneContent)
        {
            if (ContentManager.main.sceneSynchronizer)
                ContentManager.main.UnloadScene();

            var scene = mod.mod.scenes.FirstOrDefault();
            string sceneName = scene.name;

            scene.Load();

            var modScene = SceneManager.GetSceneByName(sceneName);

            while (!modScene.isLoaded)
            {
                yield return null;
            }

            ContentManager.main.scene = modScene;
            ContentManager.main.sceneSynchronizer = this;

            PrepareScene(modScene);
        }
        else
        {
            var gameObject = mod.mod.GetAsset(contentName);
            if (gameObject != null)
            {
                var g = (GameObject)Instantiate(mod.mod.GetAsset(contentName), transform.position, transform.rotation);
                g.name = contentName;
                PrepareContent(g);
            }
            else
            {
                ContentErrorNotifier.NotifyError("Object wasn't found");
            }
        }
    }

    int transformsCounter = 0;

    public void PrepareScene(Scene scene)
    {
        // Get all root objects in the scene
        GameObject[] rootObjects = scene.GetRootGameObjects();

        print("Objects Loaded " + rootObjects.Length);

        // Loop through each root object
        foreach (GameObject rootObject in rootObjects)
        {
            rootObject.transform.parent = transform;
            ScanGameObject(rootObject);
        }
    }

    public void PrepareContent(GameObject g)
    {
        g.transform.parent = transform;
        ScanGameObject(g);
    }

    // Recursively scans a GameObject and its children
    private void ScanGameObject(GameObject obj)
    {

        if (obj.name.EndsWith("_S"))
        {
            transforms[transformsCounter] = obj.transform;
            transformsCounter++;
        }

        if (obj.name.EndsWith("_E"))
        {
            effects[effectsCounter] = obj;
            effectsCounter++;
        }

        if (obj.name.EndsWith("_T"))
        {
            textMeshes[effectsCounter] = obj.GetComponent<TextMeshPro>();
            textsCounter++;
        }

        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            print("old material : " + meshRenderer.material.shader.name + " | " + "new material : " + defaultMaterial.shader.name);
            meshRenderer.material = defaultMaterial;
        }

        if (!Runner.IsServer)
        {
            var quad_ServerBehaviours = GetComponentsInChildren<IQuad_ServerBehaviour>();

            foreach (var quad_ServerBehaviour in quad_ServerBehaviours)
            {
                Destroy(quad_ServerBehaviour as Component);
            }
        }

        foreach (Transform child in obj.transform)
        {
            ScanGameObject(child.gameObject); // Recursively scan children
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (HasStateAuthority)
        {
            for (int i = 0; i < transformsCounter; i++)
            {
                locationData data;
                data.localPosition = transforms[i].localPosition;
                data.localRotation = transforms[i].localRotation;
                locations.Set(i, data);
            }
        }

        for (int i = 0; i < textsCounter; i++)
        {
            if (textMeshes[i].text != texts.Get(i))
                texts.Set(i, textMeshes[i].text);
        }

        base.FixedUpdateNetwork();
    }


    public void PlayEffect(string effectName)
    {
        for (int i = 0; i < effectsCounter; i++)
        {
            if (effects[i].name == effectName)
            {
                RPC_PlayEffect(i, effects[i].transform.localPosition, effects[i].transform.localRotation);
                return;
            }
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]

    public void RPC_PlayEffect(int index, Vector3 localPosition, Quaternion localRotation)
    {
        effects[index].transform.SetLocalPositionAndRotation(localPosition, localRotation);
        effects[index].GetComponent<AudioSource>().Play();
        effects[index].GetComponent<ParticleSystem>().Play();
    }

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            for (int i = 0; i < transformsCounter; i++)
            {
                transforms[i].SetLocalPositionAndRotation(locations[i].localPosition, locations[i].localRotation);
            }
        }


        foreach (var change in _textChangeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(texts):

                    for (int i = 0; i < textsCounter; i++)
                        if (textMeshes[i].text != texts.Get(i))
                            textMeshes[i].text = texts.Get(i);

                    break;
            }
        }

        base.Render();
    }
}
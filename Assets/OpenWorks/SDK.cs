using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SDK : QuaD_SDK
{
    public NetworkObject TransformSynchronizerPrefab;

    public override bool IsServer()
    {
        return NetworkRunner.Instances[0] && NetworkRunner.Instances[0].IsServer;
    }

    public override void RegisterContent(QuaD_Content content)
    {
        if (IsServer())
        {
            /*
            for (int i = 0; i < content.syncedTransforms.Length; i++)
            {
                Transform t = content.syncedTransforms[i].transform;
                TransformSynchronizer s = NetworkRunner.Instances[0].Spawn(TransformSynchronizerPrefab).GetComponent<TransformSynchronizer>();
                s.toSync = t;
                s.contentHash = content.hash;
                s.toSyncIndex = i;
            }
            */
        }


        base.RegisterContent(content);
    }

    public override void SpawnContent(string author, string modName, string contentName)
    {
        ContentManager.main.SpawnContent(author, modName, contentName);
    }

    public override void SetScene(string author, string modName, string sceneName = "")
    {
        ContentManager.main.SetScene(author, modName);
    }

    public override void PlayEffect(GameObject g)
    {
        g.GetComponentInParent<TransformSynchronizer>().PlayEffect(g.name);
    }
}

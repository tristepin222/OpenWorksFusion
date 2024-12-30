using Fusion;
using ModIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
public class DownloadManager : MonoBehaviour
{
    public static DownloadManager main;
    public string code;


    private void Awake()
    {
        Debug.Log("calling modio");
        StartCoroutine(ModIOManager.Start());
        main = this;
    }

    public async Task<long> DownloadMod(string modName)
    {
        while (!ModIOManager.afterAuthCheck)
        {
            await Task.Yield();
        }

        ModProfile mod = await ModIOManager.FindModByName(modName);
        if (mod.id != 0)
        {
            await ModIOManager.SubscribeToMod(mod);
        }
        return mod.id;
    }

    public async Task<long> DownloadModByAuthorAndName(string modName, string authorName)
    {
        while (!ModIOManager.afterAuthCheck)
        {
            await Task.Yield();
        }

        ModProfile mod = await ModIOManager.FindModByNameAndAuthor(modName, authorName);
        if (mod.id != 0)
        {
            await ModIOManager.SubscribeToMod(mod);
        }
        return mod.id;
    }

    [ContextMenu("Request Code")]
    public void SendCode()
    {
        ModIOManager.RequestAuthCode();
    }
    [ContextMenu("Send Code")]
    public void RecieveCode()
    {
        ModIOManager.SubmitAuthCode(code);
    }

    private async Task CheckAuthentification()
    {
        while (true)
        {
            Result authResult = await ModIOUnityAsync.IsAuthenticated();

            if (authResult.Succeeded() && ModIOManager.afterAuthCheck)
            {
                Debug.Log("User authenticated successfully!");
                break;
            }

            Debug.Log("Waiting for user authentication...");
            Task.Yield();
        }
    }

    private void Update()
    {
        // Handles download progress 
        ModIOManager.Update();
    }
}

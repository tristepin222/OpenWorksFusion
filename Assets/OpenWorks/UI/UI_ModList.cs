using ModIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ModList : MonoBehaviour
{
    [SerializeField] GameObject modItemPrefab;
    [SerializeField] GridLayoutGroup gridLayoutGroup;
    // Start is called before the first frame update
    void Start()
    {
        ShowScenes();
    }

    public async void ShowScenes()
    {
        ClearGrid();
        foreach (ModProfile mod in await ModIOManager.FindModsByTag("Scene"))
        {
            AddItemToGrid(mod, false);
        }
    }
    public async void ShowContents()
    {
        ClearGrid();
        foreach (ModProfile mod in await ModIOManager.FindModsByTag("Content"))
        {
            AddItemToGrid(mod, true);
        }
    }
    private void OnModsRefresh()
    {
        ShowScenes();
    }


    private void ClearGrid()
    {
        foreach (Transform child in gridLayoutGroup.transform)
        {
            Destroy(child.gameObject); // Destroy each child GameObject
        }
    }

    // Method to add a specified number of items to the GridLayoutGroup
    private async void AddItemToGrid(ModProfile mod, bool isContent)
    {

        // Instantiate the itemPrefab as a child of the gridLayoutGroup
        GameObject newItem = Instantiate(modItemPrefab, gridLayoutGroup.transform);
        UI_ModItem modItem = newItem.GetComponent<UI_ModItem>();
        modItem.image.texture = await ModIOManager.DownloadImage(mod);
        modItem.text.text = mod.name;
        modItem.author = mod.creator.username;
        modItem.modName = mod.name;
        modItem.contentName = mod.name;
        modItem.isContent = isContent;
        // A mod should only have one tag
        modItem.modTag = mod.tags[0];
 
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Fusion;
using Oculus.Interaction;
using UnityEngine.SceneManagement;

public class MockTest : MonoBehaviour
{
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            print("Starting Test");
            StartCoroutine(TestRoutine());
        }
    }

    #region SceneTests
    IEnumerator TestRoutine()
    {
        while (NetworkRunner.Instances.Count == 0)
            yield return null;

        NetworkRunner runner = NetworkRunner.Instances[0];

        if (runner.IsServer)
        {
            QuaD_SDK.main.SetScene("tristepin22", "ModSceneTest");
            QuaD_SDK.main.SpawnContent("tristepin22", "EffectTest", "Dog_E");
            QuaD_SDK.main.SpawnContent("tristepin22", "ModSceneTest", "Capsule_S");
            QuaD_SDK.main.SpawnContent("tristepin22", "ModContentTest", "Sphere_S");
            QuaD_SDK.main.SpawnContent("tristepin22", "ContentFrom", "Capsule_S");
            QuaD_SDK.main.SpawnContent("tristepin22", "ContentFrom", "InvalidContent");
        }
    }
    private void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
    {
        // Perform tests
        TestSceneHasObjects();
        TestSceneHasSpecificObjects();
        TestCubesHaveMovementScript();
        StartCoroutine(TestCubesPositionChange());
        ContentErrorNotifier.OnContentError += TestCubeAbsence;
    }

    void TestSceneHasObjects()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        if (allObjects.Length > 0)
        {
            print("Test Passed: Scene contains objects.");
        }
        else
        {
            print("Test Failed: Scene is empty.");
        }
    }

    void TestSceneHasSpecificObjects()
    {
        GameObject cube1 = GameObject.Find("Cube_S");
        GameObject cube2 = GameObject.Find("Cube2_S");

        if (cube1 != null && cube2 != null)
        {
            print("Test Passed: Scene contains Cube_S and Cube2_S.");
        }
        else
        {
            print("Test Failed: Missing Cube_S or Cube2_S.");
        }
    }

    void TestCubesHaveMovementScript()
    {
        GameObject cube1 = GameObject.Find("Cube_S");
        GameObject cube2 = GameObject.Find("Cube2_S");

        if (cube1 == null || cube2 == null)
        {
            print("Cannot test Movement script because one or both cubes are missing.");
            return;
        }

        bool cube1HasMovement = HasScriptWithName(cube1, "RandomMovement");
        bool cube2HasMovement = HasScriptWithName(cube2, "RandomMovement");

        if (cube1HasMovement && cube2HasMovement)
        {
            print("Test Passed: Cube_S and Cube2_S contain the Movement script.");
        }
        else
        {
            print("Test Failed: Cube_S or Cube2_S is missing the Movement script.");
        }
    }

    bool HasScriptWithName(GameObject obj, string scriptName)
    {
        foreach (MonoBehaviour script in obj.GetComponents<MonoBehaviour>())
        {
            if (script.GetType().Name == scriptName)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator TestCubesPositionChange()
    {
        GameObject cube1 = GameObject.Find("Cube_S");
        GameObject cube2 = GameObject.Find("Cube2_S");

        if (cube1 == null || cube2 == null)
        {
            print("Cannot test position change because one or both cubes are missing.");
            yield break;
        }

        Vector3 cube1StartPos = cube1.transform.position;
        Vector3 cube2StartPos = cube2.transform.position;

        yield return new WaitForSeconds(1f); // Wait for some time to observe movement

        Vector3 cube1EndPos = cube1.transform.position;
        Vector3 cube2EndPos = cube2.transform.position;

        if (cube1StartPos != cube1EndPos || cube2StartPos != cube2EndPos)
        {
            print("Test Passed: Cube_S and Cube2_S positions have changed.");
        }
        else
        {
            print("Test Failed: Cube_S and Cube2_S positions did not change.");
        }

        // Perform content tests
        TestSpherePresence();
        TestCapsulePresence();
    }
    #endregion

    #region Content Tests
    void TestSpherePresence()
    {
        GameObject sphere = GameObject.Find("Sphere_S");

        if (sphere != null)
        {
            print("Test Passed: Sphere_S is present in the scene.");
        }
        else
        {
            print("Test Failed: Sphere_S is missing.");
        }
    }

    void TestCapsulePresence()
    {
        GameObject capsule = GameObject.Find("Capsule_S");

        if (capsule != null)
        {
            print("Test Passed: Capsule_S is present in the scene.");
        }
        else
        {
            print("Test Failed: Capsule_S is missing.");
        }
    }

    void TestCubeAbsence(string error)
    {
        print("Test Passed: " + error);
    }
    #endregion

    
}


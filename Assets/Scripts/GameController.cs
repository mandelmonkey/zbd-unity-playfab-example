using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using System;

public class GameController : MonoBehaviour
{
    public static GameController Instance;
    public TMP_Text status;
    int pointCount;
    public GameObject addPointButton;
    public string titleId;
    // Start is called before the first frame update

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            PlayFabSettings.staticSettings.TitleId = titleId; // Please change this value to your own titleId from PlayFab Game Manager
        }
        Login();
    }

    private void Login()
    {
        var request = new LoginWithCustomIDRequest { CustomId = SystemInfo.deviceUniqueIdentifier, CreateAccount = true };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successfully logged in to PlayFab.");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "getPoints", // The name of the cloud script function
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptSuccess, OnCloudScriptFailure);

    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogWarning("Failed to log in to PlayFab. Error: " + error.GenerateErrorReport());
    }


    public void AddPoint()
    {
        addPointButton.SetActive(false);
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "addPoint", // The name of the cloud script function
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptSuccess, OnCloudScriptFailure);
    }


    public void Withdraw()
    {

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "withdraw", // The name of the cloud script function
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptSuccess, OnCloudScriptFailure);
    }



    private void OnCloudScriptSuccess(ExecuteCloudScriptResult result)
    {
        addPointButton.SetActive(true);

        Debug.Log("Successfully executed cloud script.");
        status.text = result.FunctionResult.ToString();
    }

    private void OnCloudScriptFailure(PlayFabError error)
    {
        addPointButton.SetActive(true);
        Debug.LogWarning("Failed to execute cloud script. Error: " + error.GenerateErrorReport());
    }

    // Update is called once per frame
    void Update()
    {

    }
}

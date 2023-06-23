using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Xml.Linq;

class AccessTokenRequest
{
    public string code;
    public string client_secret;
    public string client_id;
    public string code_verifier;
    public string grant_type;
    public string redirect_uri;
    public string refresh_token;
}
public class ActionResponse
{
    public bool error;
    public string response;
    public string type;
    public string data;
    public long responseCode;
}
public class ZBDLoginController : MonoBehaviour
{
    public string baseUrl;
    public string clientId;
    public string redirectUrl;

    public TMP_Text responseText;


    private void Awake()
    {


        // We can use this function to get uri parameters
        Application.deepLinkActivated += onDeepLinkActivated;
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            // Cold start and Application.absoluteURL not null so process Deep Link.
            onDeepLinkActivated(Application.absoluteURL);
        }

    }


    private void OnEventSuccess(WriteEventResponse response)
    {
        Debug.Log("Successfully sent event to PlayFab.");
    }

    private void OnEventFailure(PlayFabError error)
    {
        Debug.LogWarning("Failed to send event to PlayFab. Error: " + error.GenerateErrorReport());
    }



    private void OnCloudScriptSuccess(ExecuteCloudScriptResult result)
    {
        Debug.Log("Successfully executed cloud script.");


        string message = result.FunctionResult.ToString();
        Debug.Log("Received message: " + message);

        responseText.text = message;
    }

    private void OnCloudScriptFailure(PlayFabError error)
    {
        Debug.LogWarning("Failed to execute cloud script. Error: " + error.GenerateErrorReport());
    }




    // Loing via the native browser, advantages it supports all login function and shares cookies allowing faster login to socials as passwords/usernames are likley remembered, disadvantage is it leaves the app to open the flow in the OS browser.
    public void LoginViaBrowser()
    {
        StartLogin();
    }

    public void StartLogin()
    {
        responseText.text = "please wait..";

        //Generate the PKCE
        string[] pkce = GeneratePKCE();
        string verifier = pkce[0];
        string challenge = pkce[1];

        // state is used to track users on callback, just use a ranomd string for now, but ideally this would be a user id or unique user reference
        string state = Guid.NewGuid().ToString();

        //save to player prefs as OS may close the app duing reidrect so the app could be reopened with the deep link uri data hence we need to be able to get the last verifier and/or state
        PlayerPrefs.SetString("verifier", verifier);
        PlayerPrefs.SetString("state", state);

        // Generate the oauth url and open in webview or browser
        string zbdOauthUrl = baseUrl + "v0/oauth2/authorize?redirect_uri=";
        zbdOauthUrl += UnityWebRequest.EscapeURL(redirectUrl);
        zbdOauthUrl += "&scope=user&client_id=" + clientId;
        zbdOauthUrl += "&response_type=code&code_challenge=" + challenge + "&code_challenge_method=S256&state=" + state;


        Debug.Log("zbdOauthUrl:" + zbdOauthUrl);

        Application.OpenURL(zbdOauthUrl);




    }

    // ZEBEDEE will redirect to the app with the code and state to continue login
    void ContinueLogin(string url)
    {
        Debug.Log(redirectUrl + " " + url);
        if (!url.Contains(redirectUrl))
        {
            return;
        }
        Debug.Log("cont login " + url);
        String code = getParam(url, "code");
        string state = getParam(url, "state");
        string verifier = PlayerPrefs.GetString("verifier");

        //Get access token using the code and verifier, this SHOULD BE DONE on the server!

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "getUserData", // The name of the cloud script function
            FunctionParameter = new { client_id = clientId, code_verifier = verifier, grant_type = "authorization_code", redirect_uri = redirectUrl, code },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptSuccess, OnCloudScriptFailure);
        //  GetOauthAccessToken(code, verifier, state);
    }

    //On ios we can get the redirect info here
    private void onDeepLinkActivated(string url)
    {
        Debug.Log("deep link url " + url);
        ContinueLogin(url);

    }



    string getParam(string url, string param)
    {
        String res = "";

        String substr = param + "=";
        int index = url.IndexOf(substr, StringComparison.OrdinalIgnoreCase);

        // -1 if not found
        if (index >= 0)
        {
            int indexEnd = index + substr.Length;
            int stringLength = url.Length - indexEnd;
            res = url.Substring(indexEnd, stringLength);
            string[] arr = res.Split(char.Parse("&"));
            if (arr.Length > 0)
            {
                res = arr[0];
            }
            return res;

        }
        return null;
    }

    string[] GeneratePKCE()
    {

        byte[] randomBytes = new byte[32];
        System.Random random = new System.Random();
        random.NextBytes(randomBytes);

        // encode
        string verifier = URLEncode(Convert.ToBase64String(randomBytes));

        // hash and encode to get the challenge
        byte[] sha256 = Sha256(verifier);
        string challenge = URLEncode(Convert.ToBase64String(sha256));
        return new string[2] { verifier, challenge };


    }

    public static byte[] Sha256(string s)
    {
        // Form hash
        System.Security.Cryptography.SHA256 h = System.Security.Cryptography.SHA256.Create();
        byte[] data = h.ComputeHash(System.Text.Encoding.Default.GetBytes(s));
        return data;
    }


    public static string URLEncode(string s)
    {
        s = s.Replace('+', '-').Replace('/', '_').Replace("=", ""); // no padding
        return s;
    }
}

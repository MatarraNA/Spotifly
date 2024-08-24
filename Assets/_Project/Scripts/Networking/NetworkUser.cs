using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simply contains the connection data for this user
/// </summary>
public class NetworkUser : NetworkBehaviour
{
    /// <summary>
    /// The current user assigned to this client
    /// </summary>
    public static NetworkUser CurrentClientUser;

    public readonly SyncVar<ulong> SteamId = new SyncVar<ulong>();
    public readonly SyncVar<int> CorrectGuesses = new SyncVar<int>(0);
    public readonly SyncVar<string> SteamName = new SyncVar<string>();
    public readonly SyncVar<bool> PlayerReady= new SyncVar<bool>(false);
    public readonly SyncVar<NetworkConnection> NetworkConnection = new SyncVar<NetworkConnection>();

    [SerializeField]
    private NetworkUserUI _networkUIPrefab;
    private NetworkUserUI _networkUI;

    public override void OnStartClient()
    {
        base.OnStartClient();

        SteamId.OnChange += SteamId_OnChange;
        SteamName.OnChange += SteamName_OnChange;

        // instantiate our network user UI
        _networkUI = Instantiate(_networkUIPrefab, MainUI.instance.GameplayScreen.GetConnectionUIRoot());

        // send the steam id to server
        if (IsOwner)
        {
            // upon connecting to server, set the stats
            CurrentClientUser = this;
            RpcOnConnect(this.LocalConnection, SteamClient.SteamId, SteamClient.Name);
        }

        // if this is server, server is always ready
        if (IsServerInitialized) PlayerReady.Value = true;

        // default the guesses to 0
        _networkUI.SetGuessesCorrect(CorrectGuesses.Value);

        // CALLBACKS
        CorrectGuesses.OnChange += (prev, next, server) => _networkUI.SetGuessesCorrect(next);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner)
            CurrentClientUser = null;

        // destroy the respective network UI object
        if( _networkUI != null )
            Destroy(_networkUI.gameObject);
    }

    private void SteamName_OnChange(string prev, string next, bool asServer)
    {
        _networkUI.SetName(next);
    }

    private void SteamId_OnChange(ulong prev, ulong next, bool asServer)
    {
        // on id change, do the thing, on ALL clients
        StartCoroutine(DownloadAvatarCoro());
    }

    /// <summary>
    /// Syncing connection vars
    /// </summary>
    /// <param name="con"></param>
    /// <param name="steamName"></param>
    [ServerRpc(RequireOwnership = false)]
    private void RpcOnConnect(NetworkConnection con, SteamId steamId, string steamName )
    {
        SteamId.Value = steamId;
        SteamName.Value = steamName;
        NetworkConnection.Value = con;
        Debug.Log(SteamId.Value + " Connected");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RpcSetReadyState(bool ready) => PlayerReady.Value = ready;
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log(SteamId.Value + " Disconnected");
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RpcIncrementCorrectGuesses()
    {
        CorrectGuesses.Value++;
    }

    /// <summary>
    /// Will attempt to download and set the avatar img for this user
    /// </summary>
    /// <returns></returns>
    private IEnumerator DownloadAvatarCoro()
    {
        // the steam id just changed
        var request = SteamFriends.RequestUserInformation(SteamId.Value, false);
        var task = SteamFriends.GetLargeAvatarAsync(SteamId.Value);
        yield return new WaitUntil(() => task.IsCompleted);
        var result = task.Result;

        // does the image exist?
        if( result.HasValue )
        {
            //avatars are square so no need for height and width
            uint size = result.Value.Width;
            Texture2D texture = new Texture2D((int)size, (int)size); //create blank texture
            Color[] tempColors = new Color[size * size]; // intialize array of colors

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //using GetPixel was a sol'n from github
                    //The avatar pixels seem to be saved in a different order from Unity - image was flipped
                    //that's why the parameters in the GetPixel are adjusted
                    Steamworks.Data.Color steamColor = result.Value.GetPixel(j, (int)size - i - 1);

                    //convert byte data to floats and create new color
                    tempColors[i * size + j] = new Color(steamColor.r / 255f, steamColor.g / 255f, steamColor.b / 255f, steamColor.a / 255f);
                }
            }

            texture.SetPixels(tempColors);
            texture.filterMode = FilterMode.Trilinear;
            texture.Apply(); //crucial!


            _networkUI.SetIcon(texture);
        }
    }
}

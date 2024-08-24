using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class NetworkController : MonoBehaviour
{
    public static NetworkController Instance { get; private set; }

    /// <summary>
    /// Prefab spawned for each player that will pull data
    /// </summary>
    public NetworkUser NetworkUserPrefab;

    /// <summary>
    /// The network address of this server, or connection address for the client
    /// </summary>
    public string NetworkAddress { get; private set; }

    /// <summary>
    /// The network manager attached to this game object
    /// </summary>
    public NetworkManager NetworkManager { get; private set; }

    private void Awake()
    {
        if( Instance == null)
        {
            Instance = this;
            NetworkManager = GetComponent<NetworkManager>();
            NetworkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        }
        else
        {
            Destroy(this.gameObject);
        }
        
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        if( obj.ConnectionState == LocalConnectionState.Stopped )
        {
            // ensure transition back to main menu
            MainUI.instance.GameplayScreen.PreTransitionOutCleanup();
            MainUI.instance.StartCoroutine(MainUI.instance.ScreenTransitionCoro(MainUI.instance.GameplayScreen, MainUI.instance.MainScreen, 0.66f));
        }
    }

    private void OnDestroy()
    {
        StopConnection();
    }

    // CALLBACKS
    private void ServerManager_OnRemoteConnectionState(FishNet.Connection.NetworkConnection arg1, FishNet.Transporting.RemoteConnectionStateArgs arg2)
    {
        if (!NetworkManager.IsServerStarted) return;
        switch( arg2.ConnectionState )
        {
            case FishNet.Transporting.RemoteConnectionState.Started:
                // when a client joins, spawn their player OBJ
                var user = Instantiate(NetworkUserPrefab);
                NetworkManager.ServerManager.Spawn(user.gameObject, arg1);
                MainUI.instance.GameplayScreen.ActivePlayerList.Add(user);
                return;
            case FishNet.Transporting.RemoteConnectionState.Stopped:
                // when a client disconnects, remove their player obj from the connection list, IF NEEDED
                var existing = MainUI.instance.GameplayScreen.ActivePlayerList.Find(x=>x.Owner == arg1);
                if (existing != null) MainUI.instance.GameplayScreen.ActivePlayerList.Remove(existing);
                return;
        }
    }

    /// <summary>
    /// Closes any existing connection, and starts a new fishnet server
    /// </summary>
    public void StartServer()
    {
        if (!NetworkManager.IsOffline)
        {
            NetworkManager.ServerManager.StopConnection(true);
            NetworkManager.ClientManager.StopConnection();
        }

        // start the server
        if( NetworkManager.ServerManager.StartConnection() )
        {
            NetworkAddress = SteamClient.SteamId.ToString();
            NetworkManager.ClientManager.StartConnection();
        }
    }

    /// <summary>
    /// Connects to the steam user by ID, true on success
    /// </summary>
    public bool StartClient(string steamId)
    {
        if (!NetworkManager.IsOffline)
        {
            NetworkManager.ServerManager.StopConnection(true);
            NetworkManager.ClientManager.StopConnection();
        }

        // start the client
        NetworkAddress = steamId;
        return NetworkManager.ClientManager.StartConnection(steamId);
    }

    /// <summary>
    /// Stops all connections for this computer 
    /// </summary>
    public void StopConnection()
    {
        Debug.Log("Stopping connections");
        if( NetworkManager.IsClientStarted )
            NetworkManager.ClientManager.StopConnection();
        if( NetworkManager.IsServerStarted )
            NetworkManager.ServerManager.StopConnection(true);
    }
}

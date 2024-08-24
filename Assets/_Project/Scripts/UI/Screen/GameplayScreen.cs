using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using FuzzySharp.SimilarityRatio;
using SpotifyExplode.Tracks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using FuzzySharp;
using FuzzySharp.PreProcess;
using UnityEngine.EventSystems;
using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Text.Json;
using FishNet.Transporting;
using UnityEditor;
using Steamworks;
using FishNet.Connection;

[RequireComponent(typeof(CanvasGroup))]
public class GameplayScreen : NetworkBehaviour, IScreen
{
    /// <summary>
    /// Max number of seconds allowed in playback
    /// </summary>
    public static readonly float MAX_ALLOWED_TIME = 16f;

    private CanvasGroup _canvasGroup;


    [Header("NETWORK PREFABS")]
    [SerializeField] private GuessEntryUI _guessEntryUIPrefab;
    [SerializeField] private SongStatsUI _songStatsUIPrefab;
    private readonly SyncList<GuessEntryUI> _guessEntryList = new();


    [Header("UI ELEMENTS")]
    private readonly SyncVar<SongStatsUI> _songStatsUI = new();
    [SerializeField]
    private SongCompleteUI _songCompleteUI;
    [SerializeField]
    private TMP_InputField _searchSongField;
    [SerializeField]
    private Button _submitSongBtn;
    [SerializeField]
    private Image _timerFillImg;
    [SerializeField]
    private Image _timerAllowedFillImg;
    [SerializeField]
    private TextMeshProUGUI _currentTimeTM;
    [SerializeField]
    private Button _playBtn;
    [SerializeField]
    private Button _skipBtn;
    [SerializeField]
    private Image _playBtnImg;
    [SerializeField]
    private Transform _searchScrollRectContentRoot;
    [SerializeField]
    private SearchResultEntry _searchResultEntryPrefab;
    [SerializeField]
    private Button _quitBtn;
    [SerializeField]
    private Button _settingsBtn;
    [SerializeField]
    private Button _copyLobbyCodeBtn;
    [SerializeField]
    private Transform _connectionUI_Root;
    [SerializeField]
    private Transform _entryUIContentRoot;
    [SerializeField]
    private Transform _songStatsUIRoot;

    [Header("ICON INFO")]
    [SerializeField]
    private Sprite _pauseSprite;
    [SerializeField]
    private Sprite _playSprite;
    [SerializeField]
    private Color _incorrectColor;
    [SerializeField]
    private Color _correctColor;
    [SerializeField]
    private Color _closeColor;

    // GAMEPLAY VARS
    private PlaylistEntry _playlistEntry;
    private bool _trackGuessingTime = false;
    private PlaylistRoot.Item _currentTrack;
    private AudioClip _currentClip;
    private IEnumerable<string> _searchResultsEnumerable;

    // client syncs
    private readonly SyncVar<float> _currentMaxAllowedTime = new();
    private readonly SyncVar<int> _currentGuessCount = new();
    private readonly SyncVar<bool> _isServerDownloadingTrack = new();
    private readonly SyncVar<string> _currentSongStr = new SyncVar<string>();
    private readonly SyncVar<string> _currentTrackJsonStr = new SyncVar<string>();
    private readonly SyncList<string> _allSongNameList = new SyncList<string>();
    private readonly SyncVar<float> _currentClipStartOffset = new();
    private readonly SyncVar<float> _currentListenTime = new();
    private readonly SyncVar<float> _currentGuessTime = new();
    private SimpleDialogBox _clientDownloadingSongBox = null;
    private SimpleDialogBox _clientWaitingServerDownloadBox = null;
    
    // PLAYER LIST
    public readonly SyncList<NetworkUser> ActivePlayerList = new();

    // STAT VARS

    private void Awake()
    {
        // CALLBACKS
        _playBtn.onClick.AddListener(() => 
        {
            RpcOnPlayBtnServer();
        });
        _quitBtn.onClick.AddListener(() =>
        {
            NetworkController.Instance.StopConnection();
        });

        _skipBtn.onClick.AddListener(() =>
        {
            SoundManager.instance.PlayConfirmUI();
            RpcOnSkipBtnServer();
        });
        _searchSongField.onValueChanged.AddListener((x) => StartCoroutine(OnSearchFieldChangedCoro(x)));
        _submitSongBtn.onClick.AddListener(() =>
        {
            SoundManager.instance.PlayConfirmUI();

            if (!IsServerInitialized)
            {
                RpcOnSubmitBtnServer(this.LocalConnection, _searchSongField.text);
                _searchSongField.text = "";
            }
            else
                RpcOnSubmitBtnServer(this.LocalConnection, "");
        });
        _settingsBtn.onClick.AddListener( () =>
        {
            SoundManager.instance.PlayOpenUI();
            MainUI.instance.DisplaySettingsUI();
        });

        // just copy whatever the address is
        _copyLobbyCodeBtn.onClick.AddListener(() => 
        {
            SoundManager.instance.PlayConfirmUI();
            GUIUtility.systemCopyBuffer = NetworkController.Instance.NetworkAddress;
        });

        //////////////// CLIENT CALLBACKS
        _currentTrackJsonStr.OnChange += (old, next, server) =>
        {
            // skip on server
            if (this.IsServerInitialized) return;
            
            // attempt track parse
            _currentTrack = JsonSerializer.Deserialize<PlaylistRoot.Item>(next);

            // attempt DOWNLOAD the new track
            StopCoroutine(InitializeClientNextSongCoro());
            StartCoroutine(InitializeClientNextSongCoro());
        };
        _isServerDownloadingTrack.OnChange += (old, next, server) =>
        {
            if (IsServerInitialized) return;
            
            // if next is true, create the dialog box saying please wait for server to download song
            if( next )
            {
                if (_clientWaitingServerDownloadBox != null) return; // ignore, box is already present
                _clientWaitingServerDownloadBox = MainUI.instance.SimpleDialogBox("Waiting for Server...", false, this.GetCanvasGroup());
            }
            else
            {
                // it is now no longer waiting
                if (_clientWaitingServerDownloadBox == null) return;
                _clientWaitingServerDownloadBox.OnDialogOk();
            }
        };
    }

    public override void OnStartServer()
    {
        // spawn in all the bull shits
        if (!_guessEntryList.Any())
        {
            for (int i = 0; i < 6; i++)
            {
                var obj = Instantiate(_guessEntryUIPrefab, _entryUIContentRoot);
                NetworkController.Instance.NetworkManager.ServerManager.Spawn(obj.gameObject);
                _guessEntryList.Add(obj);
            }
        }
        if (_songStatsUI.Value == null)
        {
            var songStatsObj = Instantiate(_songStatsUIPrefab);
            NetworkController.Instance.NetworkManager.ServerManager.Spawn(songStatsObj.gameObject);
            _songStatsUI.Value = songStatsObj;
        }

        base.OnStartServer();
    }

    private void Start()
    {
        // ensure songcompleteUI is invisible, and uninteractable at start
        MainUI.instance.ToggleCanvasGroupInteract(_songCompleteUI.GetCanvasGroup(), false);
        MainUI.instance.ToggleCanvasGroupVisibility(_songCompleteUI.GetCanvasGroup(), false);
    }

    private void FixedUpdate()
    {
        if (IsServerInitialized )
        {
            // CALCULATIONS
            if (SoundManager.instance.GetIsPlaying() && _trackGuessingTime) _currentListenTime.Value += Time.fixedDeltaTime;
            if (_trackGuessingTime) _currentGuessTime.Value += Time.fixedDeltaTime;

            // STATS UI
            if( _songStatsUI != null )
            {
                _songStatsUI.Value.Guesses.Value = _currentGuessCount.Value;
                _songStatsUI.Value.ListenTime.Value = _currentListenTime.Value;
                _songStatsUI.Value.GuessTime.Value = _currentGuessTime.Value;
            }
        }


        // update UI elements
        var span = TimeSpan.FromSeconds(SoundManager.instance.GetCurrentPlaybackTimeNormalized());
        _currentTimeTM.text = span.Minutes + ":" + span.Seconds.ToString("00");
        _timerFillImg.fillAmount = SoundManager.instance.GetCurrentPlaybackTimeNormalized() / _currentMaxAllowedTime.Value;
        _timerAllowedFillImg.fillAmount = GetAllowedTime(_currentGuessCount.Value) / _currentMaxAllowedTime.Value;
        _playBtnImg.sprite = SoundManager.instance.GetIsPlaying() ? _pauseSprite : _playSprite;

        // GAMEPLAY
        if (SoundManager.instance.GetCurrentPlaybackTimeNormalized() >= GetAllowedTime(_currentGuessCount.Value) && _currentMaxAllowedTime.Value == MAX_ALLOWED_TIME)
            SoundManager.instance.StopSong();
    }

    /// <summary>
    /// Play btn will transmit to the server
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RpcOnPlayBtnServer()
    {
        // handle pause on server, then on all clients
        PlayBtnFunctionality();

        // start or stop depending on whether is playing
        RpcOnPlayBtnClient(SoundManager.instance.GetIsPlaying());
    }
    [ObserversRpc(ExcludeServer = true)]
    private void RpcOnPlayBtnClient(bool isServerPlaying)
    {
        PlayBtnFunctionality(isServerPlaying);
    }
    private void PlayBtnFunctionality(bool isServerPlaying = false)
    {
        if( this.IsServerInitialized )
        {
            // start or stop depending on whether is playing
            if (!SoundManager.instance.GetIsPlaying())
                SoundManager.instance.PlaySong(_currentClip, _currentClipStartOffset.Value);
            else
                SoundManager.instance.StopSong();
        }
        else
        {
            if (isServerPlaying)
                SoundManager.instance.PlaySong(_currentClip, _currentClipStartOffset.Value);
            else
                SoundManager.instance.StopSong();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcOnSkipBtnServer()
    {
        StartCoroutine(OnSkipBtnCoro());
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcOnSubmitBtnServer(NetworkConnection conn, string input = "")
    {
        StartCoroutine(OnSubmitBtnCoro(conn, input));
    }

    /// <summary>
    /// All cleanup that should be performed, before transitioning out of this screen, or starting another song
    /// </summary>
    public void PreTransitionOutCleanup()
    {
        // ensure songcompleteUI is invisible, and uninteractable before transitioning
        try
        {
            MainUI.instance.ToggleCanvasGroupInteract(_songCompleteUI.GetCanvasGroup(), false);
            MainUI.instance.ToggleCanvasGroupVisibility(_songCompleteUI.GetCanvasGroup(), false);

            if (IsServerInitialized)
            {
                // ensure guesses are cleared
                foreach (GuessEntryUI item in _guessEntryList)
                {
                    item.RpcClearEntry();
                }

                // dont conitue to track value next scene
                _trackGuessingTime = false;
            }

            // stop any music
            SoundManager.instance.StopSong();
        }
        catch (Exception) { }
    }

    public CanvasGroup GetCanvasGroup()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        return _canvasGroup;
    }

    /// <summary>
    /// Initialize with a playlist, pick a song, and start the gameplay loop
    /// </summary>
    /// <param name="playlist"></param>
    public void InitializePlaylist(PlaylistEntry playlist)
    {
        // now initialize the playlist
        _playlistEntry = playlist;
        StartCoroutine(InitializeNextSongCoro());
    }

    /// <summary>
    /// Reinitializes the gameplay loop with the same playlist it currently has
    /// </summary>
    public void ReinitializePlaylist()
    {
        InitializePlaylist(_playlistEntry);
    }

    /// <summary>
    /// Gets the allowed time given the amount of guesses the player is at
    /// </summary>
    /// <returns></returns>
    private float GetAllowedTime(int numGuesses)
    {
        switch(numGuesses)
        {
            case 0:
                return 1f;
            case 1:
                return 2f;
            case 2:
                return 4f;
            case 3:
                return 7f;
            case 4:
                return 11f;
            case 5:
                return 16f;
        }
        return _currentMaxAllowedTime.Value;
    }

    private void DeleteExistingSearchResults()
    {
        // delete existing
        if (_searchScrollRectContentRoot.childCount > 0)
        {
            for (int i = _searchScrollRectContentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_searchScrollRectContentRoot.GetChild(i).gameObject);
            }
        }
    }

    private IEnumerator OnSearchFieldChangedCoro(string newValue)
    {
        DeleteExistingSearchResults();

        if (string.IsNullOrWhiteSpace(newValue)) yield break;

        // sort based on lavestein distance
        _searchResultsEnumerable = _allSongNameList.OrderByDescending(x => Fuzz.PartialRatio(newValue.ToLower(), x.ToLower())).ToList();
        for( int i = 0; i < 6; i++ )
        {
            if (_searchResultsEnumerable.Count() <= i) break;
            var newResult = Instantiate(_searchResultEntryPrefab, _searchScrollRectContentRoot);
            newResult.Initialize(_searchResultsEnumerable.ElementAtOrDefault(i));
        }
        yield break;
    }

    private IEnumerator OnSkipBtnCoro()
    {
        if (_currentGuessCount.Value >= _guessEntryList.Count) yield break;

        _guessEntryList[_currentGuessCount.Value].RpcInitialize("SKIPPED", GuessEntryIconEnum.Skip, Color.clear);
        _currentGuessCount.Value++;

        // max guesses reached?
        if (_currentGuessCount.Value >= _guessEntryList.Count) RpcHandleSongCompleteServer(false);
    }

    private IEnumerator OnSubmitBtnCoro(NetworkConnection conn, string inputOverride = "")
    {
        bool fromClient = !string.IsNullOrWhiteSpace(inputOverride);
        string guessStr = _searchSongField.text;
        if (fromClient) guessStr = inputOverride;

        // submit over
        if (_currentGuessCount.Value >= _guessEntryList.Count) yield break;
        if (_guessEntryList.Where(x => x.GetText() == _currentSongStr.Value).Any()) yield break;

        // attempt to get the song from text
        var validInput = _allSongNameList.Where(x => x.Equals(guessStr)).Any();
        
        if( !validInput )
        {
            yield break; // for now, just do nothing
        }

        // was valid input, check if it was a correct guess
        var correctGuess = guessStr.Equals(_currentSongStr.Value);
        if( correctGuess ) 
        {
            // CORRECT GUESS WOOHOO
            _guessEntryList[_currentGuessCount.Value].RpcInitialize(guessStr, GuessEntryIconEnum.ThumbsUp, _correctColor);
            _currentGuessCount.Value++;
            if( !fromClient)
                _searchSongField.text = "";

            // increment the guess count, for the correct player
            var winningPlayer = ActivePlayerList.Find(x=>x.NetworkConnection.Value ==  conn);
            if( winningPlayer != null ) { winningPlayer.RpcIncrementCorrectGuesses(); };

            // handle winning guess
            RpcHandleSongCompleteServer(true);
            yield break;
        }

        // guess was INCORRECT, was it at least correct author?
        var correctAuthor = false;
        foreach( var author in _currentTrack.track.artists )
        {
            if (guessStr.Contains(author.name)) correctAuthor = true;
        }
        if( correctAuthor )
        {
            // HANDLE correct AUTHOR only
            _guessEntryList[_currentGuessCount.Value].RpcInitialize(guessStr, GuessEntryIconEnum.Cross, _closeColor);
            _currentGuessCount.Value++;

            if( !fromClient )
                _searchSongField.text = "";

            // max guesses reached?
            if( _currentGuessCount.Value >= _guessEntryList.Count ) RpcHandleSongCompleteServer(false);
            yield break;
        }

        // guess was INCCORRECT, with NO correct author
        _guessEntryList[_currentGuessCount.Value].RpcInitialize(guessStr, GuessEntryIconEnum.Cross, _incorrectColor);
        _currentGuessCount.Value++;
        
        if( !fromClient )
            _searchSongField.text = "";

        // max guesses reached?
        if (_currentGuessCount.Value >= _guessEntryList.Count) RpcHandleSongCompleteServer(false);
        yield break;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcHandleSongCompleteServer(bool won)
    {
        // trigger the handle song complete on server too
        StartCoroutine(HandleSongComplete(won));

        // play the handle song on all clients
        RpcHandleSongCompleteClient(won);
    }

    [ObserversRpc]
    private void RpcHandleSongCompleteClient(bool won)
    {
        // play the handle song on all clients
        if( !IsServerInitialized)
            StartCoroutine(HandleSongComplete(won));
    }

    /// <summary>
    /// Called on all guesses being used up
    /// </summary>
    /// <param name="won"></param>
    /// <returns></returns>
    private IEnumerator HandleSongComplete(bool won)
    {
        // make sure everything is deselected
        EventSystem.current.SetSelectedGameObject(null);

        // play victory sound on win
        if( won ) SoundManager.instance.PlayLoadSaveUI();

        // play the song out so something to listen to
        SoundManager.instance.PlaySong(_currentClip, _currentClipStartOffset.Value);
        _currentMaxAllowedTime.Value = float.MaxValue;
        _trackGuessingTime = false;

        // load the box
        MainUI.instance.ToggleCanvasGroupInteract(_songCompleteUI.GetCanvasGroup(), true);
        _songCompleteUI.SetBorderColor(won ? _correctColor : _incorrectColor);
        _songCompleteUI.GetCanvasGroup().DOFade(1f, 0.33f);

        if( IsServerInitialized )
        {
            // submit stats to database, ONLY ON SERVER
            var game = new GameEntry()
            {
                DatePlayed = DateTime.Now,
                GuessesUsed = _currentGuessCount.Value,
                GuessingTime = _currentGuessTime.Value,
                ListenTime = _currentListenTime.Value,
                PlaylistId = _playlistEntry.PlaylistId,
                TrackId = _currentTrack.track.id,
                Won = won
            };
            Database.Game.CreateOrUpdateGame(game);
        }
        yield break;
    }

    /// <summary>
    /// Downloads the new song for the CLIENT
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeClientNextSongCoro()
    {
        // does existing dialog exist?
        if( _clientDownloadingSongBox != null )
        {
            _clientDownloadingSongBox.OnDialogOk();
        }

        yield return new WaitForFixedUpdate();
        _clientDownloadingSongBox = MainUI.instance.SimpleDialogBox("Downloading Track...", false, this.GetCanvasGroup());

        // song aquired, attempt to download it
        var dlTask = SpotifyAPI.DownloadSongAsync(_currentTrack.track.id);
        yield return new WaitUntil(() => dlTask.IsCompleted);
        _currentClip = dlTask.Result;
        if (_currentClip == null)
        {
            // song download failed
            Debug.Log("FAILED TO DOWNLOAD: " + _currentTrack.track.name);
            _clientDownloadingSongBox.SetText("Error:\nFailed to download track...\nTrying again...");
            yield return new WaitForSeconds(2f);
            _clientDownloadingSongBox.OnDialogOk();
            yield return new WaitForFixedUpdate();
            StartCoroutine(InitializeClientNextSongCoro());
            yield break;
        }

        // set client ready
        NetworkUser.CurrentClientUser.RpcSetReadyState(true);

        // populate the end message now, so it can download the icon
        _clientDownloadingSongBox.OnDialogOk();
        string albumImgURL = _currentTrack.track.album.images.Any() ? _currentTrack.track.album.images.First().url : "";
        _songCompleteUI.SetTrack(_currentTrack.track.name, TrackToArtistString(_currentTrack.track), albumImgURL);
    }

    /// <summary>
    /// Pick a random track from the playlist, then initialize it to start the gameplay loop
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeNextSongCoro()
    {
        // reset variables
        _currentGuessCount.Value = 0;
        _currentClip = null;
        _currentTrack = null;
        _currentSongStr.Value = "";
        _currentMaxAllowedTime.Value = MAX_ALLOWED_TIME;
        _allSongNameList.Clear();
        _currentGuessTime.Value = 0f;
        _currentListenTime.Value = 0f;
        _songStatsUI.Value.FetchPlaylistStats(_playlistEntry.PlaylistId);

        // clear all guess entries
        foreach(GuessEntryUI entry in _guessEntryList)
        {
            entry.RpcClearEntry();
        }

        // set all players not ready
        foreach( NetworkUser player in ActivePlayerList )
        {
            if( !player.HasAuthority ) // skip the server / host player
                player.RpcSetReadyState(false);
        }

        // attempt gather a song
        _isServerDownloadingTrack.Value = true;
        var box = MainUI.instance.SimpleDialogBox("Downloading track...", false, MainUI.instance.GameplayScreen.GetCanvasGroup());
        _currentTrack = _playlistEntry.GetRandomViableTrack();
        if( _currentTrack == null ) 
        {
            // song grab failed
            box.SetText("Error:\nSomething went wrong polling a track...");
            box.SetInteractable(true);
            yield break;
        }

        // song aquired, attempt to download it
        var dlTask = SpotifyAPI.DownloadSongAsync(_currentTrack.track.id);
        yield return new WaitUntil(() => dlTask.IsCompleted);
        _currentClip = dlTask.Result;
        if( _currentClip == null )
        {
            // song download failed
            Debug.Log("FAILED TO DOWNLOAD: " + _currentTrack.track.name);
            box.SetText("Error:\nSomething went wrong downloading a track... Fetching new track...");
            yield return new WaitForSeconds(2f);
            box.OnDialogOk();
            yield return new WaitForEndOfFrame();
            StartCoroutine(InitializeNextSongCoro());
            yield break;
        }

        // sync the track with all clients
        _isServerDownloadingTrack.Value = false;
        _currentTrackJsonStr.Value = JsonSerializer.Serialize(_currentTrack);

        // get a random start offset
        _currentClipStartOffset.Value = UnityEngine.Random.Range(0f, _currentClip.length - _currentMaxAllowedTime.Value);
        _currentSongStr.Value = TrackToString(_currentTrack.track);
        foreach( var song in _playlistEntry.GetViablePlaylistTracks() )
        {
            _allSongNameList.Add(TrackToString(song.track));
        }

        // now wait for all clients to be ready
        box.SetText("Waiting for clients...");
        yield return new WaitUntil(() => ActivePlayerList.Where(x => x.PlayerReady.Value).Count() >= ActivePlayerList.Count);

        // populate the end message now, so it can download the icon
        string albumImgURL = _currentTrack.track.album.images.Any() ? _currentTrack.track.album.images.First().url : "";
        _songCompleteUI.SetTrack(_currentTrack.track.name, TrackToArtistString(_currentTrack.track), albumImgURL);

        // start tracking guessing time
        _trackGuessingTime = true;

        Debug.Log(_currentSongStr.Value);
        box.OnDialogOk();
        yield break;
    }

    /// <summary>
    /// Called from the search result entry upon being pressed
    /// </summary>
    /// <param name="entry"></param>
    public void OnResultBtnClick(string song)
    {
        _searchSongField.text = song;
        _searchSongField.ReleaseSelection();
        DeleteExistingSearchResults();
    }

    /// <summary>
    /// Converts a track to full Artist - Song Name string
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    private string TrackToString(PlaylistRoot.Track track)
    {
        var build = new StringBuilder("");

        for( int i = 0; i < track.artists.Count; i++ )
        {
            build.Append($"{track.artists[i].name}, ");

        }
        if( build.Length > 2) // remove last ", "
            build.Length -= 2;

        // now append the song name
        build.Append(" - " + track.name);
        return build.ToString();
    }

    /// <summary>
    /// Converts a track to JUST the artist string
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    private string TrackToArtistString(PlaylistRoot.Track track)
    {
        var build = new StringBuilder("");

        for (int i = 0; i < track.artists.Count; i++)
        {
            build.Append($"{track.artists[i].name}, ");

        }
        if (build.Length > 2) // remove last ", "
            build.Length -= 2;
        return build.ToString();
    }

    /// <summary>
    /// The root transform that will contain our connectionUIs
    /// </summary>
    /// <returns></returns>
    public Transform GetConnectionUIRoot()
    {
        return _connectionUI_Root;
    }
    public Transform GetGuessEntryRoot()
    {
        return _entryUIContentRoot;
    }
    public Transform GetSongStatsUIRoot()
    {
        return _songStatsUIRoot;
    }
}

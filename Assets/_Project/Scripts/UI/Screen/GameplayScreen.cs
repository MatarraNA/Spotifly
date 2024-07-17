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

[RequireComponent(typeof(CanvasGroup))]
public class GameplayScreen : MonoBehaviour, IScreen
{
    /// <summary>
    /// Max number of seconds allowed in playback
    /// </summary>
    public static readonly float MAX_ALLOWED_TIME = 16f;

    private CanvasGroup _canvasGroup;

    [SerializeField]
    private List<GuessEntryUI> _guessEntryList = new List<GuessEntryUI>();

    [Header("UI ELEMENTS")]
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

    [Header("ICON INFO")]
    [SerializeField]
    private Sprite _xCrossSprite;
    [SerializeField]
    private Sprite _thumbsUpSprite;
    [SerializeField]
    private Sprite _skipSprite;
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
    private PlaylistRoot.Item _currentTrack;
    private AudioClip _currentClip;
    private float _currentClipStartOffset;
    private float _currentMaxAllowedTime;
    private int _currentGuessCount;
    private string _currentSongStr;
    private List<string> _allSongNameList = new List<string>();

    private void Awake()
    {
        // CALLBACKS
        _playBtn.onClick.AddListener(() => 
        {
            // start or stop depending on whether is playing
            if( !SoundManager.instance.GetIsPlaying() )
                SoundManager.instance.PlaySong(_currentClip, _currentClipStartOffset); 
            else
                SoundManager.instance.StopSong(); 
        });

        _skipBtn.onClick.AddListener(() => StartCoroutine(OnSkipBtnCoro()));
        _searchSongField.onValueChanged.AddListener((x) => StartCoroutine(OnSearchFieldChangedCoro(x)));
        _submitSongBtn.onClick.AddListener(() => StartCoroutine(OnSubmitBtnCoro()));
    }

    private void Start()
    {
        // ensure songcompleteUI is invisible, and uninteractable at start
        MainUI.instance.ToggleCanvasGroupInteract(_songCompleteUI.GetCanvasGroup(), false);
        MainUI.instance.ToggleCanvasGroupVisibility(_songCompleteUI.GetCanvasGroup(), false);
    }

    private void FixedUpdate()
    {
        // update UI elements
        var span = TimeSpan.FromSeconds(SoundManager.instance.GetCurrentPlaybackTimeNormalized());
        _currentTimeTM.text = span.Minutes + ":" + span.Seconds.ToString("00");
        _timerFillImg.fillAmount = SoundManager.instance.GetCurrentPlaybackTimeNormalized() / _currentMaxAllowedTime;
        _timerAllowedFillImg.fillAmount = GetAllowedTime(_currentGuessCount) / _currentMaxAllowedTime;
        _playBtnImg.sprite = SoundManager.instance.GetIsPlaying() ? _pauseSprite : _playSprite;

        // GAMEPLAY
        if (SoundManager.instance.GetCurrentPlaybackTimeNormalized() >= GetAllowedTime(_currentGuessCount))
            SoundManager.instance.StopSong();
    }

    /// <summary>
    /// All cleanup that should be performed, before transitioning out of this screen, or starting another song
    /// </summary>
    public void PreTransitionOutCleanup()
    {
        // ensure songcompleteUI is invisible, and uninteractable before transitioning
        MainUI.instance.ToggleCanvasGroupInteract(_songCompleteUI.GetCanvasGroup(), false);
        MainUI.instance.ToggleCanvasGroupVisibility(_songCompleteUI.GetCanvasGroup(), false);

        // ensure guesses are cleared
        foreach (var item in _guessEntryList)
        {
            item.ClearEntry();
        }

        // stop any music
        SoundManager.instance.StopSong();
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
        return _currentMaxAllowedTime;
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
        _allSongNameList = _allSongNameList.OrderByDescending(x => Fuzz.PartialRatio(newValue.ToLower(), x.ToLower())).ToList();
        for( int i = 0; i < 6; i++ )
        {
            if (_allSongNameList.Count <= i) break;
            var newResult = Instantiate(_searchResultEntryPrefab, _searchScrollRectContentRoot);
            newResult.Initialize(_allSongNameList[i]);
        }
        yield break;
    }

    private IEnumerator OnSkipBtnCoro()
    {
        if (_currentGuessCount >= _guessEntryList.Count) yield break;

        _guessEntryList[_currentGuessCount].Initialize("SKIPPED", _skipSprite, Color.clear);
        _currentGuessCount++;

        // max guesses reached?
        if (_currentGuessCount >= _guessEntryList.Count) StartCoroutine(HandleSongComplete(false));
    }

    private IEnumerator OnSubmitBtnCoro()
    {
        // submit over
        if (_currentGuessCount >= _guessEntryList.Count) yield break;

        // attempt to get the song from text
        var validInput = _allSongNameList.Where(x => x.Equals(_searchSongField.text)).Any();
        
        if( !validInput )
        {
            yield break; // for now, just do nothing
        }

        // was valid input, check if it was a correct guess
        var correctGuess = _searchSongField.text.Equals(_currentSongStr);
        if( correctGuess ) 
        {
            // CORRECT GUESS WOOHOO
            _guessEntryList[_currentGuessCount].Initialize(_searchSongField.text, _thumbsUpSprite, _correctColor);
            _currentGuessCount++;
            _searchSongField.text = "";

            // handle winning guess
            StartCoroutine(HandleSongComplete(true));
            yield break;
        }

        // guess was INCORRECT, was it at least correct author?
        var correctAuthor = true;
        foreach( var author in _currentTrack.track.artists )
        {
            if (!_searchSongField.text.Contains(author.name)) correctAuthor = false;
        }
        if( correctAuthor )
        {
            // HANDLE correct AUTHOR only
            _guessEntryList[_currentGuessCount].Initialize(_searchSongField.text, _xCrossSprite, _closeColor);
            _currentGuessCount++;
            _searchSongField.text = "";

            // max guesses reached?
            if( _currentGuessCount >= _guessEntryList.Count ) StartCoroutine(HandleSongComplete(false));
            yield break;
        }

        // guess was INCCORRECT, with NO correct author
        _guessEntryList[_currentGuessCount].Initialize(_searchSongField.text, _xCrossSprite, _incorrectColor);
        _currentGuessCount++;
        _searchSongField.text = "";

        // max guesses reached?
        if (_currentGuessCount >= _guessEntryList.Count) StartCoroutine(HandleSongComplete(false));
        yield break;
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

        // play the song out so something to listen to
        SoundManager.instance.PlaySong(_currentClip, _currentClipStartOffset);
        _currentMaxAllowedTime = float.MaxValue;

        // load the box
        MainUI.instance.ToggleCanvasGroupInteract(_songCompleteUI.GetCanvasGroup(), true);
        _songCompleteUI.SetBorderColor(won ? _correctColor : _incorrectColor);
        _songCompleteUI.GetCanvasGroup().DOFade(1f, 0.33f);
        yield break;
    }

    /// <summary>
    /// Pick a random track from the playlist, then initialize it to start the gameplay loop
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeNextSongCoro()
    {
        // reset variables
        _currentGuessCount = 0;
        _currentClip = null;
        _currentTrack = null;
        _currentSongStr = "";
        _currentMaxAllowedTime = MAX_ALLOWED_TIME;
        _allSongNameList.Clear();

        // clear all guess entries
        foreach(var entry in _guessEntryList)
        {
            entry.ClearEntry();
        }

        // attempt gather a song
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

        // get a random start offset
        _currentClipStartOffset = UnityEngine.Random.Range(0f, _currentClip.length - _currentMaxAllowedTime);
        _currentSongStr = TrackToString(_currentTrack.track);
        foreach( var song in _playlistEntry.GetViablePlaylistTracks() )
        {
            _allSongNameList.Add(TrackToString(song.track));
        }

        // populate the end message now, so it can download the icon
        string albumImgURL = _currentTrack.track.album.images.Any() ? _currentTrack.track.album.images.First().url : "";
        _songCompleteUI.SetTrack(_currentTrack.track.name, TrackToArtistString(_currentTrack.track), albumImgURL);

        // populate the song list
        Debug.Log(_currentSongStr);
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
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlaylistEntryUI : MonoBehaviour
{
    [SerializeField]
    private Image _playlistIconImg;
    [SerializeField]
    private TextMeshProUGUI _playlistNameTM;
    [SerializeField]
    private TextMeshProUGUI _playlistOwnerTM;
    [SerializeField]
    private Button _downloadPlaylistBtn;
    [SerializeField]
    private Button _deletePlaylistBtn;
    [SerializeField]
    private Button _playBtn;

    /// <summary>
    /// The actual playlist database entry assigned to this UI element
    /// </summary>
    private PlaylistEntry _playlistEntry;

    private void Awake()
    {
        // callbacks
        _downloadPlaylistBtn.onClick.AddListener(() => StartCoroutine(OnDownloadPlaylistBtnCoro()));
        _deletePlaylistBtn.onClick.AddListener(() => StartCoroutine(OnDeletPlaylistBtnCoro()));
        _playBtn.onClick.AddListener(() => StartCoroutine(OnPlayBtnCoro()));
    }

    /// <summary>
    /// Populates this entry with data from the playlist
    /// </summary>
    /// <param name="entry"></param>
    public void Populate(PlaylistEntry entry)
    {
        this._playlistEntry = entry;

        // set everything import
        _playlistNameTM.text = entry.PlaylistName;
        _playlistOwnerTM.text = $"{entry.PlaylistOwner} • {entry.GetPlaylistTracks().Count} Songs";

        // set the ICON
        StartCoroutine(DownloadPlaylistIconCoro(entry.PlaylistIconURL));
    }

    private IEnumerator OnDeletPlaylistBtnCoro()
    {
        // remove this playlist from the database
        Database.DeletePlaylist(_playlistEntry.PlaylistId);
        MainUI.instance.MainScreen.PopulatePlaylists();
        yield break;
    }   
    
    private IEnumerator OnDownloadPlaylistBtnCoro()
    {
        var box = MainUI.instance.SimpleDialogBox("Fetching Playlist...", false, MainUI.instance.MainScreen.GetCanvasGroup());

        var playlistTask = SpotifyAPI.GetPlaylistAsync(_playlistEntry.PlaylistId);
        yield return new WaitUntil( () =>  playlistTask.IsCompleted);
        var playlist = playlistTask.Result;

        if (playlist == null)
        {
            box.SetText("Error:\nFailed to deserialize playlist");
            box.SetInteractable(true);
            yield break;
        }

        // does the playlist have any tracks?
        if (!playlist.tracks.items.Any())
        {
            Database.DeletePlaylist(_playlistEntry.PlaylistId);
            MainUI.instance.MainScreen.PopulatePlaylists();
            box.SetText("Error:\nPlaylist contains no tracks. Deleting playlist.");
            box.SetInteractable(true);
            yield break;
        }

        // update the entry, with new data
        _playlistEntry.PlaylistName = playlist.name;
        _playlistEntry.PlaylistIconURL = playlist.images.Any() ? playlist.images.FirstOrDefault().url : "";
        _playlistEntry.PlaylistOwner = playlist.owner.display_name;
        _playlistEntry.SetPlaylistTracks(playlist.tracks.items);
        Database.SetPlaylist(_playlistEntry);
        Populate(_playlistEntry);

        // update box!
        box.SetText("Playlist Updated!");
        box.SetInteractable(true);
    }

    private IEnumerator OnPlayBtnCoro()
    {
        // does this playlist have any viable songs?
        var song = _playlistEntry.GetRandomViableTrack();
        if (song == null)
        {
            MainUI.instance.SimpleDialogBox("Error:\nNo viable song to select in this playlist.", true, MainUI.instance.MainScreen.GetCanvasGroup());
            yield break;
        }

        // handle scene transition
        yield return MainUI.instance.ScreenTransitionCoro(MainUI.instance.MainScreen, MainUI.instance.GameplayScreen, 0.66f);

        // after transition, initialize it with a song
        MainUI.instance.GameplayScreen.InitializePlaylist(_playlistEntry);
    }

    /// <summary>
    /// Downloads the icon by URL, and sets it to the playlist img icon
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private IEnumerator DownloadPlaylistIconCoro(string url)
    {
        var request = UnityWebRequestTexture.GetTexture(url);
        request.SendWebRequest();
        yield return new WaitUntil(() => request.isDone);

        if (request.result != UnityWebRequest.Result.Success) yield break;

        // get the texture
        var tex = DownloadHandlerTexture.GetContent(request);

        // send to img
        var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        _playlistIconImg.sprite = sprite;
    }
}

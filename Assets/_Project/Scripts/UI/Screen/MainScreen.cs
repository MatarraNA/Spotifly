using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MainScreen : MonoBehaviour, IScreen
{
    [SerializeField]
    private PlaylistEntryUI _playlistEntryUI_Prefab;
    [SerializeField]
    private Transform _transformContentRoot;
    [SerializeField]
    private Button _playlistSearchButton;
    [SerializeField]
    private TMP_InputField _playlistSearchField;

    // getcomp
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // CALLBACKS
        _playlistSearchButton.onClick.AddListener(() => StartCoroutine(PlaylistSearchCoro()));
        _playlistSearchField.onSubmit.AddListener((x) => StartCoroutine(PlaylistSearchCoro()));
    }

    private void Start()
    {
        PopulatePlaylists();
    }

    /// <summary>
    /// Populates the playlists UI from the database. Safe to call again in future, will clean up
    /// </summary>
    public void PopulatePlaylists()
    {
        // delete all children of content root
        for (var i = _transformContentRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(_transformContentRoot.transform.GetChild(i).gameObject);
        }

        // now handle everything important
        var lists = Database.Playlist.GetPlaylists();
        foreach( var list in lists )
        {
            // create new
            var entry = Instantiate(_playlistEntryUI_Prefab, _transformContentRoot);
            entry.Populate(list);
        }
    }

    /// <summary>
    /// Search for a given playlist, by URL
    /// </summary>
    private IEnumerator PlaylistSearchCoro()
    {
        // popup!
        SoundManager.instance.PlayOpenUI();
        var box = MainUI.instance.SimpleDialogBox("Searching for Playlist...", false, GetCanvasGroup());
        string id = "";

        // attempt to get the URI 
        try
        {
            var uri = new Uri(_playlistSearchField.text);
            id = uri.Segments.Last();
        }
        catch(Exception)
        {
            // failed to get the playlist ID
            box.SetText("Error:\nFailed to get playlist ID from URL");
            box.SetInteractable(true);
            SoundManager.instance.PlayErrorUI();
            yield break;
        }

        // is the playlist ID already saved?
        var existing = Database.Playlist.GetPlaylist(id);
        if(existing != null)
        {
            box.SetText("Error:\nPlaylist is already added");
            box.SetInteractable(true);
            SoundManager.instance.PlayErrorUI();
            yield break;
        }

        // attempt to download the playlist info
        var playlistTask = SpotifyAPI.GetPlaylistAsync(id);
        yield return new WaitUntil(() => playlistTask.IsCompleted);

        // failed to parse playlist info
        if( !playlistTask.IsCompleted )
        {
            box.SetText("Error:\nFailed to download playlist");
            box.SetInteractable(true);
            SoundManager.instance.PlayErrorUI();
            yield break;
        }
        var playlist = playlistTask.Result;
        if (playlist == null)
        {
            box.SetText("Error:\nFailed to deserialize playlist");
            box.SetInteractable(true);
            SoundManager.instance.PlayErrorUI();
            yield break;
        }

        // does the playlist have any tracks?
        if (!playlist.tracks.items.Any())
        {
            box.SetText("Error:\nPlaylist contains no tracks");
            box.SetInteractable(true);
            SoundManager.instance.PlayErrorUI();
            yield break;
        }

        // playlist aquired, now save to database
        var plist = Database.Playlist.CreatePlaylist(id);
        try
        {
            plist.PlaylistIconURL = playlist.images.Any() ? playlist.images.FirstOrDefault().url : "";
            plist.PlaylistName = playlist.name;
            plist.PlaylistOwner = playlist.owner.display_name;
            plist.SetPlaylistTracks(playlist.tracks.items);
            Database.Playlist.SetPlaylist(plist);
        }
        catch( Exception )
        {
            // failed to parse playlist
            box.SetText("Error:\nDeserialized playlist missing info");
            box.SetInteractable(true);
            SoundManager.instance.PlayErrorUI();
            yield break;
        }

        // save complete! now populate
        PopulatePlaylists();

        // finally, update textbox
        box.SetText($"Playlist Imported:\n\n{playlist.name}");
        box.SetInteractable(true);
        SoundManager.instance.PlayConfirmUI();
        _playlistSearchField.text = "";
        yield break;
    }

    public CanvasGroup GetCanvasGroup()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        return _canvasGroup;
    }
}

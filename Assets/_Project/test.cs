using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltraLiteDB;
using Unity.VisualScripting;
using UnityEngine;

public class test : MonoBehaviour
{
    private void Awake()
    {
        var playlist = Database.GetPlaylist("6zYazLUuTzsYbYwnyuyYCw");
        Debug.Log(playlist.PlaylistName);
        Debug.Log(playlist.PlaylistIconURL);
        Debug.Log(playlist.GetPlaylistTracks().Count);
        //StartCoroutine(coro2());
    }

    IEnumerator coro2()
    {
        SpotifyAPI.Initialize();

        // timer
        var time = Time.time;

        // get the playlist
        var playlistTask = SpotifyAPI.GetPlaylistAsync("6zYazLUuTzsYbYwnyuyYCw");
        yield return new WaitUntil(() => playlistTask.IsCompleted);
        var playlist = playlistTask.Result;
        Debug.Log($"TIME FOR PLAYLIST: SONGS: {playlist.tracks.items.Count} TIME: {Time.time - time}s");

        // get the track
        var track = SpotifyAPI.DownloadSongAsync(playlist.tracks.items.Last().track.id);
        yield return new WaitUntil(() => track.IsCompleted);

        AudioSource source = GetComponent<AudioSource>();
        source.clip = track.Result;
        source.Play();

        // time it!
        Debug.Log($"TIME FOR PLAYLIST + SONG DL: {Time.time - time}s");

        // insert this playlist into our database
        var play = Database.CreatePlaylist(playlist.id);

        // set everything needed
        play.PlaylistName = playlist.name;
        play.PlaylistIconURL = playlist.images.Any() ? playlist.images.First().url : "";
        play.SetPlaylistTracks(playlist.tracks.items);
        Database.SetPlaylist(play);
    }
}
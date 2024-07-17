using SpotifyExplode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public static class SpotifyAPI
{
    private static SpotifyClient _client;
    private static bool _initialized = false;

    /// <summary>
    /// Initialize the SPOTIFY API, only need to call this once
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _client = new SpotifyClient();
        _initialized = true;
    }

    /// <summary>
    /// Gets the specific playlist by ID
    /// </summary>
    /// <param name="playlistId"></param>
    /// <returns></returns>
    public static async Task<PlaylistRoot> GetPlaylistAsync(string playlistId)
    {
        // initliazd?
        Initialize();

        var listRaw = await _client.Playlists.GetPlaylistRawAsync(playlistId);
        PlaylistRoot playlist = null;
        try
        {
            playlist = JsonSerializer.Deserialize<PlaylistRoot>(listRaw);
        }
        catch (Exception) { return null; }

        // iterate over this list, pulling all songs as needed
        string nextBatch = playlist.tracks.next;

        // download the next batch of tracks
        while ( !string.IsNullOrWhiteSpace(nextBatch) )
        {
            var tracks = await GetNextTrackBatch(nextBatch);
            playlist.tracks.items.AddRange(tracks.items);
            nextBatch = tracks.next;
        }

        // full tracklist downloaded now
        return playlist;
    }

    /// <summary>
    /// Returns the next batch of tracks for a given playlist track NEXT URI
    /// </summary>
    /// <param name="nextURI"></param>
    /// <returns></returns>
    private static async Task<PlaylistRoot.Tracks> GetNextTrackBatch(string nextURI)
    {
        // initliazd?
        Initialize();

        try
        {
            var raw = await _client.Playlists.GetRawAsync(nextURI);
            return JsonSerializer.Deserialize<PlaylistRoot.Tracks>(raw);
        }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// Downloads the given song
    /// </summary>
    /// <param name="playlistId"></param>
    /// <returns></returns>
    public static async Task<AudioClip> DownloadSongAsync(string trackID)
    {
        // initliazd?
        Initialize();

        try
        {
            var uri = await _client.Tracks.GetDownloadUrlAsync(trackID);
            using (var uwr = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
            {
                uwr.SendWebRequest();
                while (!uwr.isDone) await Task.Delay(25);
                return DownloadHandlerAudioClip.GetContent(uwr);
            }
        }
        catch( Exception ) { return null; }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltraLiteDB;
using System;
using System.Text.Json;
using System.Linq;

[System.Serializable]
public class PlaylistEntry
{
    public ObjectId Id { get; set; }
    public string PlaylistName { get; set; }
    public string PlaylistId { get; set; }
    public string PlaylistIconURL { get; set; }
    public string PlaylistOwner { get; set; }
    public DateTime TrackListUpdateTime{ get; set; }

    /// <summary>
    /// DONT ACCESS DIRECTLY, use Getter Setter playlist tracks
    /// </summary>
    public string _playlistTrackListJsonStr { get; set; }

    public void SetPlaylistTracks(List<PlaylistRoot.Item> playlistTrackList)
    {
        // is the input null?
        if (playlistTrackList == null) return;

        // serialize and set
        try
        {
            _playlistTrackListJsonStr = System.Text.Json.JsonSerializer.Serialize(playlistTrackList);

            // update datetime
            TrackListUpdateTime = DateTime.Now;
        }
        catch (Exception) { }
    }

    public List<PlaylistRoot.Item> GetPlaylistTracks()
    {
        var list = new List<PlaylistRoot.Item>();

        // attempt to parse the existing one
        try
        {
            list = System.Text.Json.JsonSerializer.Deserialize<List<PlaylistRoot.Item>>(_playlistTrackListJsonStr);
        }
        catch( Exception ex )
        {
            Debug.LogError($"FAILED TO PARSE TRACK LIST FOR: {PlaylistId}\n{ex.Message}");
        }

        return list;
    }

    /// <summary>
    /// Get a list of tracks that are at least 32s and NOT local
    /// </summary>
    /// <returns></returns>
    public List<PlaylistRoot.Item> GetViablePlaylistTracks()
    {
        var list = new List<PlaylistRoot.Item>();

        // attempt to parse the existing one
        try
        {
            list = GetPlaylistTracks();
            list = list.Where(x => x.track != null && !x.is_local.GetValueOrDefault() && x.track.duration_ms.GetValueOrDefault() > 20000).ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"FAILED TO PARSE TRACK LIST FOR: {PlaylistId}\n{ex.Message}");
        }

        return list;
    }

    /// <summary>
    /// Grabs a track that is at least 20s long, and is NOT LOCAL
    /// </summary>
    /// <returns></returns>
    public PlaylistRoot.Item GetRandomViableTrack()
    {
        var list = new List<PlaylistRoot.Item>();

        try
        {
            list = GetViablePlaylistTracks();
            if (!list.Any()) return null;
        }
        catch (Exception) { }
        return list.ElementAtOrDefault(UnityEngine.Random.Range(0, list.Count-1));
    }
}

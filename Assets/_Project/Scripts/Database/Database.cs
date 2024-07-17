using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltraLiteDB;
using System.Linq;
using System;

public static class Database
{
    /// <summary>
    /// Connection string for database
    /// </summary>
    private static readonly string DB_PATH = "data.litedb";
    private static readonly string PLAYLIST_COLLECTION = "playlists";

    /// <summary>
    /// Get a collection of ALL playlists in the database
    /// </summary>
    public static List<PlaylistEntry> GetPlaylists()
    {
        using( var db = new UltraLiteDatabase(DB_PATH)) 
        {
            // get the coll
            var col = db.GetCollection<PlaylistEntry>(PLAYLIST_COLLECTION);

            // return it!
            return col.FindAll().ToList();
        }
    }

    /// <summary>
    /// Creates a new playlist in the database
    /// </summary>
    public static PlaylistEntry CreatePlaylist(string playlistId)
    {
        using (var db = new UltraLiteDatabase(DB_PATH))
        {
            // get the coll
            var col = db.GetCollection<PlaylistEntry>(PLAYLIST_COLLECTION);

            // does list already exist with that id?
            var existing = col.FindAll().Where(x => x.PlaylistId == playlistId).FirstOrDefault();
            if (existing != null) return existing; // cannot create new, one exists already

            // create new
            var input = new PlaylistEntry()
            {
                PlaylistId = playlistId
            };

            // insert into database
            col.Insert(input);

            // now return it
            return col.FindAll().Where(x => x.PlaylistId == playlistId).FirstOrDefault();
        }
    }

    /// <summary>
    /// Deletes an existing item from the database by ID
    /// </summary>
    /// <param name="playlistId"></param>
    public static void DeletePlaylist(string playlistId)
    {
        using (var db = new UltraLiteDatabase(DB_PATH))
        {
            // get the coll
            var col = db.GetCollection<PlaylistEntry>(PLAYLIST_COLLECTION);

            var existing = col.FindAll().Where(x => x.PlaylistId == playlistId).FirstOrDefault();
            if( existing == null ) return; // nothing to delete
            col.Delete(existing.Id);
        }
    }

    public static PlaylistEntry GetPlaylist(string playlistId) 
    {
        using (var db = new UltraLiteDatabase(DB_PATH))
        {
            // get the coll
            var col = db.GetCollection<PlaylistEntry>(PLAYLIST_COLLECTION);

            // does list already exist with that id?
            var existing = col.FindAll().Where(x => x.PlaylistId == playlistId).FirstOrDefault();
            return existing;
        }
    }
    public static void SetPlaylist(PlaylistEntry input)
    {
        using (var db = new UltraLiteDatabase(DB_PATH))
        {
            // get the coll
            var col = db.GetCollection<PlaylistEntry>(PLAYLIST_COLLECTION);

            // update it!
            col.Update(input);
        }
    }
}

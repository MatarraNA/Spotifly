using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.Json;
using System.Text.Json.Serialization;


public class PlaylistRoot
{
    [JsonPropertyName("collaborative")]
    public bool? collaborative { get; set; }

    [JsonPropertyName("description")]
    public string description { get; set; }

    [JsonPropertyName("external_urls")]
    public ExternalUrls external_urls { get; set; }

    [JsonPropertyName("followers")]
    public Followers followers { get; set; }

    [JsonPropertyName("href")]
    public string href { get; set; }

    [JsonPropertyName("id")]
    public string id { get; set; }

    [JsonPropertyName("images")]
    public List<Image> images { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("owner")]
    public Owner owner { get; set; }

    [JsonPropertyName("primary_color")]
    public object primary_color { get; set; }

    [JsonPropertyName("public")]
    public bool? @public { get; set; }

    [JsonPropertyName("snapshot_id")]
    public string snapshot_id { get; set; }

    [JsonPropertyName("tracks")]
    public Tracks tracks { get; set; }

    [JsonPropertyName("type")]
    public string type { get; set; }

    [JsonPropertyName("uri")]
    public string uri { get; set; }


    public class AddedBy
    {
        [JsonPropertyName("external_urls")]
        public ExternalUrls external_urls { get; set; }

        [JsonPropertyName("href")]
        public string href { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("uri")]
        public string uri { get; set; }
    }

    public class Album
    {
        [JsonPropertyName("available_markets")]
        public List<string> available_markets { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("album_type")]
        public string album_type { get; set; }

        [JsonPropertyName("href")]
        public string href { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("images")]
        public List<Image> images { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("release_date")]
        public string release_date { get; set; }

        [JsonPropertyName("release_date_precision")]
        public string release_date_precision { get; set; }

        [JsonPropertyName("uri")]
        public string uri { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist> artists { get; set; }

        [JsonPropertyName("external_urls")]
        public ExternalUrls external_urls { get; set; }

        [JsonPropertyName("total_tracks")]
        public int? total_tracks { get; set; }
    }

    public class Artist
    {
        [JsonPropertyName("external_urls")]
        public ExternalUrls external_urls { get; set; }

        [JsonPropertyName("href")]
        public string href { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("uri")]
        public string uri { get; set; }
    }

    public class ExternalIds
    {
        [JsonPropertyName("isrc")]
        public string isrc { get; set; }
    }

    public class ExternalUrls
    {
        [JsonPropertyName("spotify")]
        public string spotify { get; set; }
    }

    public class Followers
    {
        [JsonPropertyName("href")]
        public object href { get; set; }

        [JsonPropertyName("total")]
        public int? total { get; set; }
    }

    public class Image
    {
        [JsonPropertyName("height")]
        public int? height { get; set; }

        [JsonPropertyName("url")]
        public string url { get; set; }

        [JsonPropertyName("width")]
        public int? width { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("added_at")]
        public DateTime? added_at { get; set; }

        [JsonPropertyName("added_by")]
        public AddedBy added_by { get; set; }

        [JsonPropertyName("is_local")]
        public bool? is_local { get; set; }

        [JsonPropertyName("primary_color")]
        public object primary_color { get; set; }

        [JsonPropertyName("track")]
        public Track track { get; set; }

        [JsonPropertyName("video_thumbnail")]
        public VideoThumbnail video_thumbnail { get; set; }
    }

    public class Owner
    {
        [JsonPropertyName("display_name")]
        public string display_name { get; set; }

        [JsonPropertyName("external_urls")]
        public ExternalUrls external_urls { get; set; }

        [JsonPropertyName("href")]
        public string href { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("uri")]
        public string uri { get; set; }
    }


    public class Track
    {
        [JsonPropertyName("preview_url")]
        public string preview_url { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string> available_markets { get; set; }

        [JsonPropertyName("explicit")]
        public bool? @explicit { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("episode")]
        public bool? episode { get; set; }

        [JsonPropertyName("track")]
        public bool? track { get; set; }

        [JsonPropertyName("album")]
        public Album album { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist> artists { get; set; }

        [JsonPropertyName("disc_number")]
        public int? disc_number { get; set; }

        [JsonPropertyName("track_number")]
        public int? track_number { get; set; }

        [JsonPropertyName("duration_ms")]
        public int? duration_ms { get; set; }

        [JsonPropertyName("external_ids")]
        public ExternalIds external_ids { get; set; }

        [JsonPropertyName("external_urls")]
        public ExternalUrls external_urls { get; set; }

        [JsonPropertyName("href")]
        public string href { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("popularity")]
        public int? popularity { get; set; }

        [JsonPropertyName("uri")]
        public string uri { get; set; }

        [JsonPropertyName("is_local")]
        public bool? is_local { get; set; }
    }

    public class Tracks
    {
        [JsonPropertyName("href")]
        public string href { get; set; }

        [JsonPropertyName("items")]
        public List<Item> items { get; set; }

        [JsonPropertyName("limit")]
        public int? limit { get; set; }

        [JsonPropertyName("next")]
        public string next { get; set; }

        [JsonPropertyName("offset")]
        public int? offset { get; set; }

        [JsonPropertyName("previous")]
        public object previous { get; set; }

        [JsonPropertyName("total")]
        public int? total { get; set; }
    }

    public class VideoThumbnail
    {
        [JsonPropertyName("url")]
        public object url { get; set; }
    }
}


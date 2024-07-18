using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltraLiteDB;
using System;
using System.Text.Json;
using System.Linq;

[System.Serializable]
public class GameEntry
{
    public ObjectId Id { get; set; }
    public string PlaylistId { get; set; }
    public string TrackId { get; set; }
    public int GuessesUsed { get; set; }
    public bool Won { get; set; }
    public float ListenTime { get; set; }
    public float GuessingTime { get; set; }
    public DateTime DatePlayed { get; set; }
}

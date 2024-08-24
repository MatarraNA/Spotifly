using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using static Database;

public class SongStatsUI : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _statsDisplayTM;

    // ALL THE SETTABLE SONG STATS
    private readonly int _firstSpaces = -18;
    private readonly int _secondSpaces = -7;

    // CURRENT SONG STATS
    public readonly SyncVar<float> ListenTime = new();
    public readonly SyncVar<float> GuessTime= new();
    public readonly SyncVar<int> Guesses = new();
    
    // ALL TIME PLAYLIST STATS
    public readonly SyncVar<float> AvgListenTime = new();
    public readonly SyncVar<float> AvgGuessTime = new();
    public readonly SyncVar<float> AvgGuesses = new();
    
    public readonly SyncVar<int> WinStreak = new();
    public readonly SyncVar<float> WinRate = new();
    public readonly SyncVar<int> Wins = new();
    public readonly SyncVar<int> Losses = new();

    public override void OnStartClient()
    {
        // ensure it is connected to the correct transform
        this.transform.SetParent(MainUI.instance.GameplayScreen.GetSongStatsUIRoot(), false);
        base.OnStartClient();
    }

    /// <summary>
    /// Pulls and sets the stats for a given playlist ID
    /// </summary>
    /// <param name="playlistId"></param>
    public void FetchPlaylistStats(string playlistId)
    {
        if (!IsServerInitialized) return;

        // reset playlist stats
        AvgListenTime.Value = 0;
        AvgGuessTime.Value = 0;
        AvgGuesses.Value = 0;
        WinStreak.Value = 0;
        WinRate.Value = 0;
        Wins.Value = 0;
        Losses.Value = 0;
        bool reachedLoss = false;

        // now run thru and calc
        var games = Database.Game.GetGamesForPlaylist(playlistId);
        for( int i = games.Count-1; i >= 0; i-- )
        {
            if( !reachedLoss )
            {
                // continue winstrea?
                if (games[i].Won) WinStreak.Value++;
                else reachedLoss = true;
            }
            Wins.Value += games[i].Won ? 1 : 0;
            Losses.Value += !games[i].Won ? 1 : 0;
            AvgListenTime.Value += games[i].ListenTime;
            AvgGuessTime.Value += games[i].GuessingTime;
            AvgGuesses.Value += games[i].GuessesUsed;
        }
        AvgListenTime.Value = games.Count > 0 ? AvgListenTime.Value /= games.Count : 0;
        AvgGuessTime.Value = games.Count > 0 ? AvgGuessTime.Value /= games.Count : 0;
        AvgGuesses.Value = games.Count > 0 ? AvgGuesses.Value /= games.Count : 0;

        WinRate.Value = Mathf.Round(((float)Wins.Value / (Wins.Value + Losses.Value)) * 100f);
        WinRate.Value = float.IsNaN(WinRate.Value) ? 0 : WinRate.Value;
    }

    private void FixedUpdate()
    {
        // UPDATE THE TM WITH THE NEW STATS
        var builder = new StringBuilder();

        // SONG STATS
        builder.AppendLine("<b><u>CURRENT SONG</b></u>");
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Listen Time", TimeSpan.FromSeconds(ListenTime.Value).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Guess Time", TimeSpan.FromSeconds(GuessTime.Value).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Guesses", Guesses.Value));
        builder.AppendLine("");
        builder.AppendLine("");
        builder.AppendLine("<b><u>ALL TIME PLAYLIST</b></u>");
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Avg. Listen Time", TimeSpan.FromSeconds(AvgListenTime.Value).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Avg. Guess Time", TimeSpan.FromSeconds(AvgGuessTime.Value).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Avg. Guesses", AvgGuesses.Value.ToString("N1")));
        builder.AppendLine("");
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Win Streak", WinStreak.Value));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Winrate", WinRate.Value + "%"));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Wins", Wins.Value));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Losses", Losses.Value));

        _statsDisplayTM.text = builder.ToString();
    }
}

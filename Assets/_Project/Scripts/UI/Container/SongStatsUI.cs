using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using static Database;

public class SongStatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _statsDisplayTM;

    // ALL THE SETTABLE SONG STATS
    private readonly int _firstSpaces = -18;
    private readonly int _secondSpaces = -7;

    // CURRENT SONG STATS
    public float ListenTime { get; set; }
    public float GuessTime { get; set; }
    public int Guesses { get; set; }
    
    // ALL TIME PLAYLIST STATS
    public float AvgListenTime { get; set; }
    public float AvgGuessTime { get; set; }
    public float AvgGuesses { get; set; }
    
    public int WinStreak { get; set; }
    public float WinRate { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }

    /// <summary>
    /// Pulls and sets the stats for a given playlist ID
    /// </summary>
    /// <param name="playlistId"></param>
    public void FetchPlaylistStats(string playlistId)
    {
        // reset playlist stats
        AvgListenTime = 0;
        AvgGuessTime = 0;
        AvgGuesses = 0;
        WinStreak = 0;
        WinRate = 0;
        Wins = 0;
        Losses = 0;
        bool reachedLoss = false;

        // now run thru and calc
        var games = Database.Game.GetGamesForPlaylist(playlistId);
        for( int i = games.Count-1; i >= 0; i-- )
        {
            if( !reachedLoss )
            {
                // continue winstrea?
                if (games[i].Won) WinStreak++;
                else reachedLoss = true;
            }
            Wins += games[i].Won ? 1 : 0;
            Losses += !games[i].Won ? 1 : 0;
            AvgListenTime += games[i].ListenTime;
            AvgGuessTime += games[i].GuessingTime;
            AvgGuesses += games[i].GuessesUsed;
        }
        AvgListenTime = games.Count > 0 ? AvgListenTime /= games.Count : 0;
        AvgGuessTime = games.Count > 0 ? AvgGuessTime /= games.Count : 0;
        AvgGuesses = games.Count > 0 ? AvgGuesses /= games.Count : 0;

        WinRate = Mathf.Round(((float)Wins / (Wins + Losses)) * 100f);
        WinRate = float.IsNaN(WinRate) ? 0 : WinRate;
    }

    private void FixedUpdate()
    {
        // UPDATE THE TM WITH THE NEW STATS
        var builder = new StringBuilder();

        // SONG STATS
        builder.AppendLine("<b><u>CURRENT SONG</b></u>");
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Listen Time", TimeSpan.FromSeconds(ListenTime).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Guess Time", TimeSpan.FromSeconds(GuessTime).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Guesses", Guesses));
        builder.AppendLine("");
        builder.AppendLine("");
        builder.AppendLine("<b><u>ALL TIME PLAYLIST</b></u>");
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Avg. Listen Time", TimeSpan.FromSeconds(AvgListenTime).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Avg. Guess Time", TimeSpan.FromSeconds(AvgGuessTime).ToString(@"m\:ss")));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Avg. Guesses", AvgGuesses.ToString("N1")));
        builder.AppendLine("");
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Win Streak", WinStreak));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Winrate", WinRate + "%"));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Wins", Wins));
        builder.AppendLine(string.Format($"{{0,{_firstSpaces}}}{{1,{_secondSpaces}}}", "Losses", Losses));

        _statsDisplayTM.text = builder.ToString();
    }
}

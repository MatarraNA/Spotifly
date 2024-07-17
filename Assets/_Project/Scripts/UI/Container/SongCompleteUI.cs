using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SongCompleteUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    [SerializeField] private TextMeshProUGUI _trackNameTM;
    [SerializeField] private TextMeshProUGUI _trackAristTM;
    [SerializeField] private Image _trackIconIMG;

    [SerializeField] private Image _trackBorderIMG;

    [SerializeField] private Button _mainMenuBtn;
    [SerializeField] private Button _playAgainBtn;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        _mainMenuBtn.onClick.AddListener(() =>
        {
            // clean up all the shit
            MainUI.instance.GameplayScreen.PreTransitionOutCleanup();

            // just transition to main menu
            StartCoroutine(MainUI.instance.ScreenTransitionCoro(MainUI.instance.GameplayScreen, MainUI.instance.MainScreen, 0.66f));
        });

        _playAgainBtn.onClick.AddListener(() =>
        {
            // clean up all the shit
            MainUI.instance.GameplayScreen.PreTransitionOutCleanup();

            // start it up again
            MainUI.instance.GameplayScreen.ReinitializePlaylist();
        });   
    }

    public CanvasGroup GetCanvasGroup()
    {
        return _canvasGroup;
    }

    public void SetTrack(string trackName, string artistStr, string iconURL)
    {
        _trackNameTM.text = trackName;
        _trackAristTM.text = $"- {artistStr} - ";
        StartCoroutine(DownloadTrackIconCoro(iconURL));
    }

    public void SetBorderColor( Color color )
    {
        _trackBorderIMG.color = color;
    }

    /// <summary>
    /// Downloads the icon by URL, and sets it to the playlist img icon
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private IEnumerator DownloadTrackIconCoro(string url)
    {
        if( string.IsNullOrWhiteSpace(url) ) { yield break; }

        var request = UnityWebRequestTexture.GetTexture(url);
        request.SendWebRequest();
        yield return new WaitUntil(() => request.isDone);

        if (request.result != UnityWebRequest.Result.Success) yield break;

        // get the texture
        var tex = DownloadHandlerTexture.GetContent(request);

        // send to img
        var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        _trackIconIMG.sprite = sprite;
    }
}

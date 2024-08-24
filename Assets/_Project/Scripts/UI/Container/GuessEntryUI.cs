using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GuessEntryUI : NetworkBehaviour
{
    [SerializeField]
    private TextMeshProUGUI GuessTM;
    [SerializeField]
    private Image GuessIconImg;

    private CanvasGroup _canvasGroup;

    [Header("Sprites")]
    [SerializeField] private Sprite _skipSprite;
    [SerializeField] private Sprite _xCrossSprite;
    [SerializeField] private Sprite _thumbsUpSprite;

    // syncvars
    private readonly SyncVar<string> _currentText = new SyncVar<string>("");
    private readonly SyncVar<Color> _currentColor = new SyncVar<Color>();
    private readonly SyncVar<GuessEntryIconEnum> _currentGuessEntry = new SyncVar<GuessEntryIconEnum>();

    // initial info
    private string _initalText = "";
    private Sprite _initialSprite;
    private Color _initialColor;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // make it fade IN on spawn
        _canvasGroup.alpha = 0f;

        // make sure the parent is the gameplay container root
        this.transform.SetParent(MainUI.instance.GameplayScreen.GetGuessEntryRoot());

        _initalText = GuessTM.text;
        _initialSprite = GuessIconImg.sprite;
        _initialColor = GuessIconImg.color;

        // CALLBACKS
        _currentText.OnChange += _currentText_OnChange;
        _currentColor.OnChange += _currentColor_OnChange;
        _currentGuessEntry.OnChange += _currentGuessEntry_OnChange;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _canvasGroup.DOFade(1f, 0.66f);
    }

    private void _currentText_OnChange(string prev, string next, bool asServer)
    {
        GuessTM.text = next;
    }
    private void _currentColor_OnChange(Color prev, Color next, bool asServer)
    {
        GuessIconImg.color = next;
    }
    private void _currentGuessEntry_OnChange(GuessEntryIconEnum prev, GuessEntryIconEnum next, bool asServer)
    {
        switch (next)
        {
            case GuessEntryIconEnum.None:
                GuessIconImg.sprite = _initialSprite;
                break;
            case GuessEntryIconEnum.Skip:
                GuessIconImg.sprite = _initialSprite; // blank looks better tbh
                break;
            case GuessEntryIconEnum.Cross:
                GuessIconImg.sprite = _xCrossSprite;
                break;
            case GuessEntryIconEnum.ThumbsUp:
                GuessIconImg.sprite = _thumbsUpSprite;
                break;
        }
    }

    public string GetText()
    {
        return _currentText.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RpcInitialize(string message, GuessEntryIconEnum iconType, Color iconColor)
    {
        // set the syncvars
        _currentColor.Value = iconColor;
        _currentGuessEntry.Value = iconType;
        _currentText.Value = message;
    }

    /// <summary>
    /// Resets this entry back to default
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RpcClearEntry()
    {
        _currentText.Value = _initalText;
        _currentGuessEntry.Value = GuessEntryIconEnum.None;
        _currentColor.Value = _initialColor;
    }
}

public enum GuessEntryIconEnum
{
    ThumbsUp, Cross, Skip, None
}

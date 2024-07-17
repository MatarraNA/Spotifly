using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuessEntryUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI GuessTM;
    [SerializeField]
    private Image GuessIconImg;

    // initial info
    private string _initalText = "";
    private Sprite _initialSprite;
    private Color _initialColor;

    private void Awake()
    {
        _initalText = GuessTM.text;
        _initialSprite = GuessIconImg.sprite;
        _initialColor = GuessIconImg.color;
    }

    public void Initialize(string message, Sprite icon, Color iconColor)
    {
        GuessTM.text = message;
        GuessIconImg.sprite = icon;
        GuessIconImg.color = iconColor;
    }

    /// <summary>
    /// Resets this entry back to default
    /// </summary>
    public void ClearEntry()
    {
        GuessTM.text = _initalText;
        GuessIconImg.sprite = _initialSprite;
        GuessIconImg.color = _initialColor;
    }
}

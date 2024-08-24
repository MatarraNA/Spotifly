using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUserUI : MonoBehaviour
{
    [SerializeField] private RawImage _playerIconImg;
    [SerializeField] private TextMeshProUGUI _playerNameTM;
    [SerializeField] private TextMeshProUGUI _guessesCorrectTM;

    public void SetIcon(Texture2D icon)
    {
        _playerIconImg.texture = icon;
    }

    public void SetName(string name)
    {
        _playerNameTM.text = name;
    }

    public void SetGuessesCorrect(int correct)
    {
        _guessesCorrectTM.text = $"Correct: " + correct;
    }
}

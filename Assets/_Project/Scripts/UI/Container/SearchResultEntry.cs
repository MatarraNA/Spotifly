using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchResultEntry : MonoBehaviour
{
    [SerializeField]
    private Button _selectSongBtn;    
    [SerializeField]
    private TextMeshProUGUI _selectSongTM;

    private string _fullSongName = "";

    private void Awake()
    {
        _selectSongBtn.onClick.AddListener(() =>
        {
            // set this song tm as the field
            MainUI.instance.GameplayScreen.OnResultBtnClick(_fullSongName);
        });
    }

    private void OnDestroy()
    {
        _selectSongBtn.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Initialize this search result, with the proper info
    /// </summary>
    public void Initialize(string fullSongName)
    {
        _fullSongName = fullSongName;
        _selectSongTM.text = fullSongName;
    }
}

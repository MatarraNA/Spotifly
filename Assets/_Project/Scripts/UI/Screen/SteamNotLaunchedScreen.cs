using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SteamNotLaunchedScreen : MonoBehaviour
{
    [SerializeField] private Button _closeApplicationBtn;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _closeApplicationBtn.onClick.AddListener(() => Application.Quit(-1));
    }

    private void Start()
    {
        if( !SteamClient.IsValid )
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}

using DG.Tweening;
using FishNet.Managing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
    public static MainUI instance { get; private set; }
    public MainScreen MainScreen;
    public GameplayScreen GameplayScreen;

    [SerializeField]
    private SimpleDialogBox _dialogBoxPrefab;
    [SerializeField]
    private SettingsUI _settingsPrefab;


    /// <summary>
    /// All screens that are a children of this main canvas UI
    /// </summary>
    private List<IScreen> _screenList = new List<IScreen>();

    private void Awake()
    {
        instance = this;

        // gather all screens
        _screenList = GetComponentsInChildren<IScreen>(true).ToList();

        // ensure main screen is active
        foreach (var screen in _screenList)
        {
            ToggleCanvasGroupInteract(screen.GetCanvasGroup(), false);
            ToggleCanvasGroupVisibility(screen.GetCanvasGroup(), false);

            // just ensure all screens are setactive
            screen.GetCanvasGroup().gameObject.SetActive(true);
        }
        ToggleCanvasGroupInteract(MainScreen.GetCanvasGroup(), true);
        ToggleCanvasGroupVisibility(MainScreen.GetCanvasGroup(), true);
    }

    /// <summary>
    /// A coroutine that will fade to the new screen, while toggling interactibility
    /// </summary>
    /// <param name="prev"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public IEnumerator ScreenTransitionCoro(IScreen prev, IScreen next, float transitionTime)
    {
        var prevGroup = prev.GetCanvasGroup();
        var nextGroup = next.GetCanvasGroup();
        prevGroup.alpha = 1f;
        nextGroup.alpha = 0f;
        ToggleCanvasGroupInteract(prevGroup, false);
        ToggleCanvasGroupInteract(nextGroup, false);

        // fade alpha
        prevGroup.DOFade(0f, transitionTime/2f)
            .OnComplete( () => nextGroup.DOFade(1f, transitionTime/2f));
        yield return new WaitForSeconds(transitionTime);
        yield return new WaitForFixedUpdate();

        // now toggle new screen on
        ToggleCanvasGroupInteract(nextGroup, true);
        ToggleCanvasGroupVisibility(nextGroup, true);
    }


    public void ToggleCanvasGroupInteract(CanvasGroup group, bool toggle)
    {
        group.interactable = toggle;
        group.blocksRaycasts = toggle;
    }
    public void ToggleCanvasGroupVisibility(CanvasGroup group, bool toggle)
    {
        group.alpha = toggle ? 1f : 0f;
    }

    /// <summary>
    /// Creates a simple dialog popup. blockScreenInteraction will disable interaction on that group, until this dialog is resolved.
    /// </summary>
    /// <param name="message"></param>
    public SimpleDialogBox SimpleDialogBox(string message, bool allowButtonInteract = true, CanvasGroup blockScreenInteraction = null)
    {
        var box = Instantiate(_dialogBoxPrefab, this.transform);
        box.Initialize(message, allowButtonInteract, blockScreenInteraction);
        return box;
    }

    /// <summary>
    /// Safe to call from anywhere, will display a settings box
    /// </summary>
    public void DisplaySettingsUI()
    {
        // ensure nothing behind settings is controlled
        EventSystem.current.SetSelectedGameObject(null);

        // spawn the settings UI
        Instantiate(_settingsPrefab, this.transform);
    }
}

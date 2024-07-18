using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleDialogBox : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _dialogBoxTM;
    [SerializeField]
    private Button _dialogBoxOKBtn;

    private CanvasGroup _screenToBlock = null;

    private void Awake()
    {
        _dialogBoxOKBtn.onClick.AddListener(() =>
        {
            SoundManager.instance.PlayCloseUI();
            OnDialogOk();
        });
    }

    /// <summary>
    /// Change the message of this box
    /// </summary>
    /// <param name="message"></param>
    public void SetText(string message)
    {
        _dialogBoxTM.text = message;
    }

    public void SetInteractable(bool interactable) 
    {
        _dialogBoxOKBtn.interactable = interactable;
    }

    /// <summary>
    /// Initialize this BOX with the message
    /// </summary>
    public void Initialize(string message, bool interactable, CanvasGroup screenToBlock = null)
    {
        _dialogBoxTM.text = message;
        _dialogBoxOKBtn.interactable = interactable;
        _dialogBoxOKBtn.Select();

        if( screenToBlock != null) 
        {
            _screenToBlock = screenToBlock;
            _screenToBlock.blocksRaycasts = false;
            _screenToBlock.interactable = false;
        }
    }

    /// <summary>
    /// Called when dialog box is resolved
    /// </summary>
    public void OnDialogOk()
    {
        // allow the underlying to be interactable once again
        if (_screenToBlock != null)
        {
            _screenToBlock.blocksRaycasts = true;
            _screenToBlock.interactable = true;
        }
        Destroy(this.gameObject);
    }
}

using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class MenuManager : MonoBehaviour
{
    #region Publics

    [Header("Actions")]
    public InputAction pauseButton;
    public InputAction LeftTrigger;
    public InputAction RightTrigger;
    public InputAction SaveButton;

    [Header( "Params" )]
    [Range( 0f, 1f )] public float maxOpacity = 0.4f;
    [Space(5)]
    public float panelInTweenDuration = 0.7f;
    public Ease panelInEase = Ease.InBounce;
    [Space(5)]
    public float panelOutTweenDuration = 0.3f;
    public Ease panelOutEase = Ease.InCirc;
    [Space(5)]
    public float panelRotationDuration = 0.2f;
    public Ease panelRotationEase = Ease.InOutQuad;

    [Header( "References" )]
    public Transform panelRotator;
    public Transform leftButton;
    public Transform rightButton;
    public AudioSource menuSFX;
    public Transform savePanel;
    public Transform selectionCursor;
    public MenuJoystickNavigation menuJoystickNavigation;

    [Header( "Panel First Selected" )]
    public PanelFirstSelectedItem[] panelFirstSelected;

    [Header( "SFX Audio Clips" )]
    public AudioClip sfxMenu_Open;
    public AudioClip sfxMenu_Close;
    public AudioClip sfxMenu_Turn;

    [Header( "Events" )]
    public UnityEvent OnMenuOpen;
    public UnityEvent OnMenuClose;

    #endregion

    #region Privates

    private bool isMenuOpen = false;
    private bool isSavePanelOpen = false;
    private Canvas _canvas;
    private List<Transform> _panels = new();
    private Transform activePanel = null;
    #endregion

    private void OnEnable()
    {
        pauseButton.Enable();
        LeftTrigger.Enable();
        RightTrigger.Enable();
        SaveButton.Enable();
    }

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();

        foreach ( Transform t in panelRotator )
        {
            _panels.Add( t );
            //Set default opacity to 0
            t.GetComponent<Image>().DOFade( 0f, 0f );
        }

        leftButton.GetComponent<Image>().DOFade( 0f, 0f );
        rightButton.GetComponent<Image>().DOFade( 0f, 0f );
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _canvas.enabled = false;
        menuJoystickNavigation.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if( pauseButton.WasPerformedThisFrame() )
        {
            isMenuOpen = !isMenuOpen;

            if ( isMenuOpen )
            {
                menuJoystickNavigation.enabled = true;
                RotateInPanels();
                FadeButtons();
                OnMenuOpen.Invoke();
            }
            else
            {
                menuJoystickNavigation.enabled = false;
                RotateOutPanels();
                FadeButtons(false);
                OnMenuClose.Invoke();
            }
        }

        //On commence à avoir besoin d'une State Machine D:
        if( isMenuOpen )
        {
            //Est ce qu'on a appuyé sur le bouton ? Est ce que le panel n'est pas en cours d'animation ? Est ce que le panneau de sauvegarde est fermé ?
            if( LeftTrigger.WasPerformedThisFrame() && !DOTween.IsTweening( panelRotator ) && !isSavePanelOpen )
            {
                panelRotator.DORotate( new Vector3( panelRotator.rotation.eulerAngles.x, panelRotator.rotation.eulerAngles.y + 90f, panelRotator.rotation.eulerAngles.z ), panelRotationDuration )
                    .SetEase( panelRotationEase )
                    .OnComplete( () =>
                    {
                        activePanel = GetActivePanel();
                        Button firstSelectedButton = GetFirstSelectedButton( activePanel.gameObject );
                        menuJoystickNavigation.SetSelectedItem( firstSelectedButton.gameObject );
                    } ); ;
                AnimateButtons();

                activePanel = GetActivePanel();
                Button firstSelectedButton = GetFirstSelectedButton( activePanel.gameObject );
                menuJoystickNavigation.SetSelectedItem( firstSelectedButton.gameObject );
            }
            else if( RightTrigger.WasPerformedThisFrame() && !DOTween.IsTweening( panelRotator ) && !isSavePanelOpen )
            {
                panelRotator.DORotate( new Vector3( panelRotator.rotation.eulerAngles.x, panelRotator.rotation.eulerAngles.y - 90f, panelRotator.rotation.eulerAngles.z ), panelRotationDuration )
                    .SetEase( panelRotationEase )
                    .OnComplete( () =>
                    {
                        activePanel = GetActivePanel();
                        Button firstSelectedButton = GetFirstSelectedButton( activePanel.gameObject );
                        menuJoystickNavigation.SetSelectedItem( firstSelectedButton.gameObject );
                    } );
                AnimateButtons();

                
            }
            else if( SaveButton.WasPerformedThisFrame() && !DOTween.IsTweening( panelRotator ) && !DOTween.IsTweening( savePanel ) )
            {
                if( !isSavePanelOpen )
                {
                    OpenSavePanel();
                }
                else
                {
                    CloseSavePanel();
                }
            }
        }
    }

    private void RotateInPanels()
    {
        menuSFX.PlayOneShot( sfxMenu_Open );
        panelRotator.Rotate( new Vector3( 0f, -90f, 0f ) );
        panelRotator.DORotate( new Vector3( 0f, panelRotator.rotation.eulerAngles.y + 90f, 0f ), panelInTweenDuration ).SetEase( panelInEase );

        _canvas.enabled = true;
        foreach ( Transform panel in _panels )
        {
            panel.DOLocalRotate( new Vector3( 0f, panel.localRotation.eulerAngles.y, panel.localRotation.eulerAngles.z ), panelInTweenDuration )
                .SetEase( panelInEase );
            panel.GetComponent<Image>().DOFade( maxOpacity, panelInTweenDuration )
                .SetEase( panelInEase );
        }
    }

    private void RotateOutPanels()
    {
        menuSFX.PlayOneShot( sfxMenu_Close );
        foreach ( Transform panel in _panels )
        {
            panel.GetComponent<Image>().DOFade( 0f, panelOutTweenDuration )
                .SetEase( panelOutEase );
            panel.DOLocalRotate( new Vector3( 90f, panel.localRotation.eulerAngles.y, panel.localRotation.eulerAngles.z ), panelOutTweenDuration )
                .SetEase( panelOutEase )
                .OnComplete( () => _canvas.enabled = false );
        }

        if( isSavePanelOpen )
        {
            SetPivot( savePanel.GetComponent<RectTransform>(), new Vector2( 0.5f, 0f ) );
            savePanel.DOLocalRotate( new Vector3( 90f, savePanel.localRotation.eulerAngles.y, savePanel.localRotation.eulerAngles.z ), panelOutTweenDuration )
                .SetEase( panelOutEase )
                .OnComplete( () => {
                    SetPivot( savePanel.GetComponent<RectTransform>(), new Vector2( 0.5f, 0.5f ) );
                    SetPivot( activePanel.GetComponent<RectTransform>(), new Vector2( 0.5f, 0f ) );
                    activePanel = null;
                    isSavePanelOpen = false;
                } );
        }
    }

    private void OpenSavePanel()
    {
        activePanel = GetActivePanel();

        if ( activePanel == null ) return;

        savePanel.DORotate( new Vector3( 0f, 0f, 0f ), panelRotationDuration )
                    .SetEase( panelRotationEase );

        Button firstSelectedButton = GetFirstSelectedButton( savePanel.gameObject );
        menuJoystickNavigation.SetSelectedItem( firstSelectedButton.gameObject );

        SetPivot( activePanel.GetComponent<RectTransform>(), new Vector2( 0.5f, 0.5f ) );
        activePanel.GetComponent<RectTransform>().ForceUpdateRectTransforms();
        activePanel.DOLocalRotate( new Vector3( -180f, activePanel.localRotation.eulerAngles.y, activePanel.localRotation.eulerAngles.z ), panelRotationDuration )
                    .SetEase( panelRotationEase );
        //selectionCursor.gameObject.SetActive( false );
        isSavePanelOpen = true;
    }
    
    private void CloseSavePanel()
    {
        if ( activePanel == null ) return;

        savePanel.DORotate( new Vector3( 180f, 0f, 0f ), panelRotationDuration )
                    .SetEase( panelRotationEase );

        activePanel.DOLocalRotate( new Vector3( -180f, activePanel.localRotation.eulerAngles.y, activePanel.localRotation.eulerAngles.z ), panelRotationDuration )
                    .SetEase( panelRotationEase )
                    .OnComplete( () => {
                        SetPivot( activePanel.GetComponent<RectTransform>(), new Vector2( 0.5f, 0f ) );
                        Button firstSelectedButton = GetFirstSelectedButton( activePanel.gameObject );
                        menuJoystickNavigation.SetSelectedItem( firstSelectedButton.gameObject );
                        activePanel = null;
                    });
        selectionCursor.gameObject.SetActive( true );
        isSavePanelOpen = false;
    }

    private Transform GetActivePanel()
    {
        float panelAbsRotation =  360f - panelRotator.localRotation.eulerAngles.y;

        foreach ( Transform panel in _panels )
        {
            if( (int) panel.localRotation.eulerAngles.y == (int) ( panelAbsRotation % 360f ) )
            {
                return panel;
            }  
        }

        return null;
    }

    private void AnimateButtons()
    {
        menuSFX.PlayOneShot( sfxMenu_Turn );

        Sequence leftBtnSeq = DOTween.Sequence();
        leftBtnSeq.Append( leftButton.DOLocalMoveX( -7f, 0.3f ).SetEase(Ease.OutCubic ) )
            .Append( leftButton.DOLocalMoveX( -5.5f, 0.3f ).SetEase( Ease.OutCubic ) );

        Sequence rightBtnSeq = DOTween.Sequence();
        rightBtnSeq.Append( rightButton.DOLocalMoveX( 7f, 0.3f ).SetEase( Ease.OutCubic ) )
            .Append( rightButton.DOLocalMoveX( 5.5f, 0.3f ).SetEase( Ease.OutCubic ) );

        leftBtnSeq.Play();
        rightBtnSeq.Play();
    }

    private void FadeButtons( bool fadein = true )
    {
        float fadeValue = fadein ? 1f : 0f;

        leftButton.GetComponent<Image>().DOFade( fadeValue, panelInTweenDuration );
        rightButton.GetComponent<Image>().DOFade( fadeValue, panelOutTweenDuration );
    }

    public static void SetPivot( RectTransform rectTransform, Vector2 pivot )
    {
        if ( rectTransform == null ) return;

        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = rectTransform.pivot - pivot;
        Vector3 deltaPosition = new Vector3( deltaPivot.x * size.x, deltaPivot.y * size.y );
        rectTransform.pivot = pivot;
        rectTransform.localPosition -= deltaPosition;
    }

    private Button GetFirstSelectedButton( GameObject currentPanel )
    {
        foreach( PanelFirstSelectedItem row in panelFirstSelected )
        {
            if ( row.Panel == currentPanel )
            {
                return row.FirstSelected;
            }
        }

        return null;
    }
}

[System.Serializable]
public class PanelFirstSelectedItem
{
    public GameObject Panel;
    public Button FirstSelected;
}

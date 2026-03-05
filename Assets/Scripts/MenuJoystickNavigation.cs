using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MenuJoystickNavigation : MonoBehaviour
{
    public GameObject firstSelected;

    [Header( "Refs" )]
    public EventSystem eventSystem;
    public GameObject selectCursor;
    public AudioSource joystickSFX;


    [Header( "Audio Clips" )]
    public AudioClip stickMovement;
    public AudioClip itemSelected;

    private GameObject currentSelected;
    private GameObject lastSelected;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        eventSystem.sendNavigationEvents = true;
        eventSystem.firstSelectedGameObject = firstSelected;
        currentSelected = firstSelected;
        selectCursor.transform.position = firstSelected.transform.position;
    }

    private void OnEnable()
    {
        selectCursor.GetComponent<Image>().DOFade( 1f, 0.2f );
    }

    private void OnDisable()
    {
        //selectCursor.GetComponent<Image>().DOFade( 0f, 0.1f );
    }

    // Update is called once per frame
    void Update()
    {
        currentSelected = EventSystem.current.currentSelectedGameObject;

        if ( currentSelected != null && currentSelected != lastSelected )
        {
            joystickSFX.PlayOneShot( stickMovement, 0.8f );
            lastSelected = currentSelected;
        }

        selectCursor.transform.position = currentSelected.transform.position;
    }

    public void SetSelectedItem( GameObject item )
    {
        if ( item is null ) return;

        
        eventSystem.SetSelectedGameObject( item );
        currentSelected = item;
        selectCursor.transform.position = item.transform.position;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThePCModeController : MonoBehaviour
{
    public GameObject[] goUI_Panels;
    public GameObject goUI_Place;
    public GameObject goUI_Squid;
    public GameObject goUI_Photogrammetry;
    public GameObject goUI_Done;

    public ToggleGroundPlaneMode tgpm;
    public TakePhotogrammetryPhoto tpp;
    public TheDoneScreenMechanics tdm;

    private void Start()
    {
        UI_Place();
    }

    public void UI_Place()
    {
        ToggleUI(goUI_Panels, false);
        tgpm.EnableGroundPlaneMode(true);
        goUI_Place.SetActive(true);
    }

    public void UI_Squid()
    {
        ToggleUI(goUI_Panels, false);
        tpp.CameraStarted(false);
        tgpm.EnableGroundPlaneMode(false);
        goUI_Squid.SetActive(true);
    }

    public void UI_Photogrammetry()
    {
        ToggleUI(goUI_Panels, false);
        tpp.CameraStarted(true);

        goUI_Photogrammetry.SetActive(true);
    }

    public void UI_Done()
    {
        ToggleUI(goUI_Panels, false);
        tpp.CameraStarted(false);
        tdm.Init();

        goUI_Done.SetActive(true);
    }

    void ToggleUI(GameObject[] g,bool f)
    {
        for (int i = 0; i < goUI_Panels.Length; i++) goUI_Panels[i].SetActive(f);
    }

}

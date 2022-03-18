using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

// 1) Init()
// 2) call AllDone() AFTER TheUploader.Upload (which will extract the data)
public class TheDoneScreenMechanics : MonoBehaviour
{
    public GridLayoutGroup gridThumbs;
    public TakePhotogrammetryPhoto tpp;
    public GameObject goPrefabGrid;

    List<string> listNames = new List<string>();

    public void Init()
    {
        listNames.Clear();
        LoadData();
        LoadThumbnails();
    }

    void LoadData()
    {
        // TODO load into dictionary
        string s = PlayerPrefs.GetString(TakePhotogrammetryPhoto.pf_sessionLedger);
        if (!string.IsNullOrEmpty(s))
        {
            string[] ledger = Parse.SSV(s);

            print("LoadData: " + ledger.Length);

            
            for(int i = 0; i < ledger.Length; i++)
            {
                listNames.Add(ledger[i]);
                print("Added >>>> listNames " + listNames[i]);
            }
        }

    }

    void CreateGridElement(Texture2D tex,string name)
    {
        GameObject g = Instantiate(goPrefabGrid);
        g.GetComponent<RectTransform>().SetParent(gridThumbs.transform);
        g.name = name;
        g.transform.localScale = Vector3.one;
        g.transform.localPosition = Vector3.zero;
        g.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
    }

    public void AllDone()
    {
        //TODO

        // start new photogrammetry session
        tpp.Init();
    }

    public void LoadMoreInfo(string name)
    {
        // .png, _thumb.png, _gravity.txt,_attitude.txt
        //TODO
    }

    void LoadThumbnails()
    {
        foreach(Transform t in gridThumbs.transform)
        {
            t.parent = null;
            Destroy(t.gameObject);
        }

       for(int i = 0; i < listNames.Count; i++)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(File.ReadAllBytes(TakePhotogrammetryPhoto.GetThumbPath(listNames[i])));
            tex.Apply();
            CreateGridElement(tex,Path.GetFileNameWithoutExtension(listNames[i]));
        }
    }

    public void SelectThumbnail(string name)
    {
        // parse name to find out which squid
        name = name.Replace(".png", "");
        string[] usv = Parse.USV(name);
        if (usv.Length == 3)
        {
            string squidname = usv[2];

            //TODO

        }
    }
    

}

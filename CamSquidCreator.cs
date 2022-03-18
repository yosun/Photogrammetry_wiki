using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CamSquidCreator : MonoBehaviour
{

    /*
        # the number of different viewpoints from which we want to render the mesh.
        num_views = 20

        # Get a batch of viewing angles. 
        elev = torch.linspace(0, 360, num_views)
        azim = torch.linspace(-180, 180, num_views)
     */

    public GameObject goPrefabSquid;
    public GameObject goPrefabSquidNoRB;
    public Transform squidParent;
    public Transform centerPoint;

    public Material matSemiTransparent;

    public Slider sliderRadius;

    int lastNumView=20; 

    public void ToggleSquid()
    {
        ToggleSquid(!squidParent.gameObject.activeSelf);
    }

    public void CreateFloatingPhoto(Texture2D tex,Vector3 pos,Quaternion rot)
    {
        GameObject g = CreateSquid(pos + Camera.main.transform.forward*Camera.main.nearClipPlane, -1,true, false);

        // texture g
        g.GetComponent<Renderer>().material = Instantiate(matSemiTransparent);
        g.GetComponent<Renderer>().material.mainTexture = tex;

        // rotate it
        g.transform.rotation = rot;
        /* g.transform.localRotation *= Quaternion.Euler(180, 0, 0);
         g.transform.localRotation *= Quaternion.Euler(0, 0, 180);*/
#if !UNITY_EDITOR
        g.transform.localRotation *= Quaternion.Euler(0, 0, 270);
       // g.transform.localRotation *= Quaternion.Euler(0,180,0);
#endif

        // size it
        Vector3 s = g.transform.localScale;
        g.transform.localScale = new Vector3( (float)tex.width / (float)tex.height * s.x,s.y,s.z);

    }

    public void ToggleSquid(bool f)
    {
        /* Renderer[] ren = squidParent.GetComponentsInChildren<Renderer>();
         for (int i = 0; i < ren.Length; i++) ren[i].enabled = f;*/
        squidParent.gameObject.SetActive(f);
    }

    private void Start()
    {
        Vector3 v = goPrefabSquid.transform.localScale;
        Vector3 s = new Vector3(((float)Screen.width / (float)Screen.height) * v.y, v.y, v.z);
        
        goPrefabSquid.transform.localScale = s;


        // test
        CreateCamSquid(20, .1f, true);
    }

    public void UI_AdjustRadius()
    {
        CreateCamSquid(-1, sliderRadius.value, false);
    }

    public void ClearSquids()
    {
        // clear squids
        foreach (Transform t in squidParent)
        {
            if (t.name != "Cube") { t.parent = null; t.position = new Vector3(9999, 9999, 9999); Destroy(t.gameObject); }
        }
    }

    // generates based on https://gyazo.com/afc9f59cabe2eea7f7eb2374403a0c36
    public void CreateCamSquid(int num_views = -1,float radius = 1f,bool create=true)
    {
        if (num_views < 0) num_views = lastNumView;
        else lastNumView = num_views;
        if (num_views < 20 || num_views > 200) return;

       
        int k = 0;

        int n_levels = Mathf.Min( (int)(num_views*.1f) + 1, 11);
        int top = (num_views - 1);
        int bot = (n_levels - 1);
        int num_per_level = Mathf.FloorToInt(((float)top)/((float)bot));
        int remainder = top % bot;
        int level1 = num_per_level + remainder;
        print(n_levels + " " + num_per_level);

        float deg360 = 360f / (float)num_per_level * Mathf.Deg2Rad;
        float deg90 = 90f / (float)(n_levels) * Mathf.Deg2Rad;

        // create top piece
        CreateSquid(new Vector3(0, radius, 0), k,create);
        k++;

        // create level1
        for (int i = 0; i < level1; i++)
        {
            float t90 = deg90;
            float cosf = Mathf.Cos(t90);
            float t360 = i * deg360;
            Vector3 pos = radius * new Vector3(cosf * Mathf.Sin(t360), Mathf.Sin(t90), cosf * Mathf.Cos(t360));

            CreateSquid(pos,k,create);
            k++;
        }

        // create other levels
        for (int n=2;n<n_levels;n++)
        {
            
            for (int i = 0; i < num_per_level; i++)
            {
                float t90 = n * deg90;
                float cosf = Mathf.Cos(t90);
                float t360 = i * deg360;
                Vector3 pos = radius * new Vector3(cosf * Mathf.Sin(t360), Mathf.Sin(t90), cosf * Mathf.Cos(t360));

                CreateSquid(pos,k,create);
                k++;
            }
        }

        // redistribute a bit if needed
        if (num_views >= 50)
        {
            k--;
            for (int i = 0; i < (int)(num_per_level); i++)
            {
                float t90 = deg90;
                float cosf = Mathf.Cos(t90);
                float t360 = (i + .5f) * deg360;
                Vector3 pos = radius * new Vector3(cosf * Mathf.Sin(t360), Mathf.Sin(t90), cosf * Mathf.Cos(t360));

                CreateSquid(pos, k, false);
                k -= 2;
            }


            if (num_views > 100)
            {
                for (int i = 0; i < (int)(num_per_level); i++)
                {
                    float t90 = 2 * deg90;
                    float cosf = Mathf.Cos(t90);
                    float t360 = (i + .5f) * deg360;
                    Vector3 pos = radius * new Vector3(cosf * Mathf.Sin(t360), Mathf.Sin(t90), cosf * Mathf.Cos(t360));

                    CreateSquid(pos, k, false);
                    k -= 2;
                }
            }

            if (num_views > 150)
            {
                for (int i = 0; i < (int)(num_per_level); i++)
                {
                    float t90 = 3 * deg90;
                    float cosf = Mathf.Cos(t90);
                    float t360 = (i + .5f) * deg360;
                    Vector3 pos = radius * new Vector3(cosf * Mathf.Sin(t360), Mathf.Sin(t90), cosf * Mathf.Cos(t360));

                    CreateSquid(pos, k, false);
                    k -= 2;
                }
            }
        }

       
        

        /*int totalX = (int)( (float)(num_views + 1) / (float) 3 );

        float deg360 = 360f / (float)totalX * Mathf.Deg2Rad;
        float deg180 = 180f / (float)3 * Mathf.Deg2Rad;

        for (int j=0;j<3;j++)
            for (int i = 0; i < totalX; i++)
            {
                float t180 = j * deg180;
                float cosf = Mathf.Cos(t180);
                float t360 = i * deg360;
                Vector3 pos = radius * new Vector3( cosf * Mathf.Sin(t360), Mathf.Sin(t180), cosf * Mathf.Cos(t360));
                GameObject g = Instan();
                g.transform.position = pos;
                g.transform.parent = squidParent;
                g.transform.LookAt (centerPoint);
            }*/
    }

    GameObject CreateSquid(Vector3 pos,int k,bool instantiate=true,bool lookcenter=true)
    {
        GameObject g;
        if (instantiate)
        {
            if (lookcenter)
                g = Instan();
            else g = Instantiate(goPrefabSquidNoRB);
            g.name = k.ToString();
        }
        else { print("CreateSquid " + k); g = squidParent.Find(k.ToString()).gameObject;   }

        if (!lookcenter)
            g.transform.position = pos;

        g.transform.parent = squidParent;

        if (lookcenter)
        {

            g.transform.localPosition = pos;

            g.transform.LookAt(centerPoint);
        }
        else
            g.transform.parent = squidParent.parent;
       

        return g;
    }

    GameObject Instan()
    {
        return Instantiate(goPrefabSquid);
    }

}

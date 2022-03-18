using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using web3storage;

public class TheUploader : MonoBehaviour
{
    public static string sessionToUpload;

    public StoreData sd;

    public void Upload()
    {
        // copy the session data
        sessionToUpload = TakePhotogrammetryPhoto.pf_sessionLedger;
        sd.BeginStoreProcess(sessionToUpload);
    }

}

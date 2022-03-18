using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using BestHTTP;
using System;
using BestHTTP.Forms;
using Newtonsoft.Json;
using UnityEngine.UI;

namespace web3storage
{
    [System.Serializable]
    public class Filesetlet
    {
        public string imagepath;
        public string thumbpath;
        public string gravitypath;
        public string attitudepath;

        public string imagecid;
        public string thumbcid;
        public string gravitycid;
        public string attitudecid;

        public Filesetlet(string[] paths)
        {
            imagepath = paths[1];
            thumbpath = paths[2];
            gravitypath = paths[3];
            attitudepath = paths[4];
        }

        public bool CheckComplete()
        {
            if (!string.IsNullOrEmpty(imagecid) && !string.IsNullOrEmpty(thumbcid) && !string.IsNullOrEmpty(gravitycid) && !string.IsNullOrEmpty(attitudecid)) return true;
            return false;
        }

        public string DumpMe()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class PhotogrammetrySchema
    {
        public string prettyName;
        public string session_ledger;
        public string uuid_user;
        public string[] photos; // cids of photos
        public string[] thumbs; // corresponding thumb cid (optional)
        public string[] gravitys; // txt of gravity (optional)
        public string[] attitudes; // txt of attitude (optional)
        public string photozip; // car or zip of photo (optional)
        public string[] meshes;// processed serverside (optional)
        public string prev;

        public PhotogrammetrySchema(string ledger,string uuiduser)
        {
            session_ledger = ledger;
            uuid_user = uuiduser;
        }

        public void InitArrays(int n)
        {
            photos = new string[n];
            thumbs = new string[n];
            gravitys = new string[n];
            attitudes = new string[n];
        }

        public string DumpMe()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Web3Response
    {
        public string cid;
    }

    public class StoreData : MonoBehaviour
    {
        const string url_upload = "https://api.web3.storage/upload";

        public Text txtUploadStatus;

        PhotogrammetrySchema theCurrentSchema;
        static Dictionary<string, Filesetlet> dicFilesetlet = new Dictionary<string, Filesetlet>();
        // these two must match in Count
        static List<string> necessaryFilesetlets = new List<string>();
        static List<string> completedFilesetlets = new List<string>();

        // start with this - use sessionledger from TakePhotogrammetry
        public void BeginStoreProcess(string sessionledger) {
            theCurrentSchema = new PhotogrammetrySchema(sessionledger, UUID.pseudo_device_udid);
            dicFilesetlet.Clear();
            necessaryFilesetlets.Clear();
            completedFilesetlets.Clear();

            txtUploadStatus.text = "Upload starting...";

            StartCoroutine(UploadFiles(sessionledger));
        }

        // upload all files in session ledger to web3.storage
        IEnumerator UploadFiles(string sessionledger)
        {
            // open ledger
            string[] s = TakePhotogrammetryPhoto.Pf2String(sessionledger);

            txtUploadStatus.text = "Processing ledger";

            // prepare check list
            for (int i = 0; i < s.Length; i++)
            {
                necessaryFilesetlets.Add(s[i]);
            }

            // initiate theCurrentSchema arrays
            theCurrentSchema.InitArrays(  necessaryFilesetlets.Count );

            txtUploadStatus.text = "Initializing schema";

            // upload each
            for (int i = 0; i < s.Length; i++)
            {
                string[] filepaths = TakePhotogrammetryPhoto.GetAllFiles(s[i]);
                dicFilesetlet.Add(filepaths[0], new Filesetlet(filepaths));
                for(int j = 1; j < filepaths.Length; j++)
                {
                    
                    UploadFile(filepaths[j], OnRequestFinishedEachFile);

                    // Let's handicap this because of web3.storage rate limit issues
                    yield return new WaitForSeconds(1.1f);

                    // TODO delete
                }

                // note because getting a cid back is async, we have a check in OnRequestFinished when all cids for all files are received
            }
        }

        public string authenticationToken;
          
        void UploadFile(string path,OnRequestFinishedDelegate v)
        {
            print("UploadFile " + path);
            txtUploadStatus.text = "Uploading " + Path.GetFileName(path);
            // TODO remove
            //return;

            HTTPRequest request = new HTTPRequest(new Uri(url_upload), HTTPMethods.Post, v);
            FormDataStream m = new FormDataStream(request);
            FileStream stream = new FileStream(path,FileMode.Open); 
            m.AddStreamField(stream, "file");
            request.UploadStream = m;
            request.SetHeader("Authorization", "Bearer " + authenticationToken);
            request.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddField("filename", path);
            request.Send();
        }

        private void OnRequestFinishedEachFile(HTTPRequest req, HTTPResponse resp)
        {
            switch (req.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (resp.IsSuccess)
                    {
                        ProcessSuccess(req, resp);
                    }
                    else
                    {
                        Debug.LogWarning(string.Format("Request finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                                                        resp.StatusCode,
                                                        resp.Message,
                                                        resp.DataAsText));
                    }
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    List<HTTPFieldData> list = req.GetFormFields();
                    HTTPFieldData fd = list.Find(x => x.Name == "filename");
                    string fdtext = fd.Text;
                    Debug.LogError("Request Finished with Error! " + (req.Exception != null ? (req.Exception.Message + "\n" + fdtext + "\n" + req.Exception.StackTrace) : "No Exception"));
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    Debug.LogWarning("Request Aborted!");
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    Debug.LogError("Connection Timed Out!");
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    Debug.LogError("Processing the request Timed Out!");
                    break;

            }
            
        }

            void ProcessSuccess(HTTPRequest originalRequest, HTTPResponse response)
            {
                List<HTTPFieldData> list = originalRequest.GetFormFields();
                HTTPFieldData fd = list.Find(x => x.Name == "filename");
                string fdtext = fd.Text;
                string path = TakePhotogrammetryPhoto.RetrieveBaseFilepath(fdtext);

                /*if (response == null || response.DataAsText == null)
                {
                    print("response is null");
                    return;
                }
                print(response.DataAsText);*/
                Web3Response res = JsonUtility.FromJson<Web3Response>(response.DataAsText);

                // populate dictionary with relevant cid field 
                if (fdtext.Contains("_thumb"))
                {
                    dicFilesetlet[path].thumbcid = res.cid;
                }
                else if (fdtext.Contains("_gravity"))
                {
                    dicFilesetlet[path].gravitycid = res.cid;
                }
                else if (fdtext.Contains("_attitude"))
                {
                    dicFilesetlet[path].attitudecid = res.cid;
                }
                else
                {
                    // meat of the pie has no appends
                    dicFilesetlet[path].imagecid = res.cid;
                }

                // if our dictionary of cids have fileset complete, add to completion check list
                if (dicFilesetlet[path].CheckComplete())
                {
                    completedFilesetlets.Add(path);
                }

                // check list if we are ready to create schema and finalize the entire fileset
                if (completedFilesetlets.Count == necessaryFilesetlets.Count)
                {
                    CreateSchemaJson();
                }
            }

        // this is done after we have received cid's of all files
        public void CreateSchemaJson()
        {
            for(int i = 0; i < necessaryFilesetlets.Count; i++)
            {
                Filesetlet fsl = dicFilesetlet[necessaryFilesetlets[i]];
                theCurrentSchema.photos[i] = fsl.imagecid;
                theCurrentSchema.thumbs[i] = fsl.thumbcid;
                theCurrentSchema.gravitys[i] = fsl.gravitycid;
                theCurrentSchema.attitudes[i] = fsl.attitudecid;
            }

            // update ledger with uuid version
            theCurrentSchema.session_ledger = GenerateUniqueLedger();

            // write schema
            File.WriteAllText(pathSessionSchema(), theCurrentSchema.DumpMe());

            UploadSchema();
        }

        public void UploadSchema()
        {
            // upload schema to web3.storage 
            UploadFile(pathSessionSchema(), OnRequestFinishedSchema);            
        }

        string pathSessionSchema()
        {
            return Path.Combine(TakePhotogrammetryPhoto.GenBasePath(theCurrentSchema.session_ledger) , "_schema.json");
        }

        public string url_UpdateIndex;

        void OnRequestFinishedSchema(HTTPRequest o,HTTPResponse r)
        {
            //  update our hacky permanent URL index listing - TODO this should ideally be updated on IPFS

            txtUploadStatus.text = "Updating index";

            Web3Response res = JsonUtility.FromJson<Web3Response>(r.DataAsText);

            HTTPRequest request = new HTTPRequest(new Uri(url_UpdateIndex), HTTPMethods.Post, OnIndexFinished);

            request.AddField("cid", res.cid);
            request.AddField("ledger", theCurrentSchema.session_ledger);
            request.AddField("photocount", theCurrentSchema.photos.Length.ToString());

            print("cid=" + res.cid + " photocount=" + theCurrentSchema.photos.Length);// + " "+unique_ledger);

            request.Send();
        }

       // public string GeneratedUniqueLedger;
        string GenerateUniqueLedger()
        {
            return theCurrentSchema.session_ledger + "_" + UUID.GetUUID();
        }

        void OnIndexFinished(HTTPRequest o,HTTPResponse r)
        {
            txtUploadStatus.text = "Upload Completed!";

            //TODO
            print("L>>> "+r.DataAsText);
        }

    }

}

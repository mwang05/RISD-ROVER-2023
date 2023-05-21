using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
// using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Geosampling : MonoBehaviour
{
    public class GeosamplingPhoto
    {
        public TimeSpan timestamp;
        public Sprite photo;

        public GeosamplingPhoto(TimeSpan _timestamp, Sprite _photo)
        {
            timestamp = _timestamp;
            photo = _photo;
        }
    }

    public class GeosamplingItem
    {
        public int itemID;
        public String lithology;
        public List<GeosamplingPhoto> photos;

        public GeosamplingItem(String litho, int id)
        {
            lithology = litho;
            itemID = id;
            photos = new List<GeosamplingPhoto>();
        }

        private Sprite GetScreenshot()
        {
            RenderTexture rt = new RenderTexture(mainCamera.pixelWidth, mainCamera.pixelHeight, 24);
            mainCamera.targetTexture = rt;
            mainCamera.Render();
            
            Texture2D screenShot = new Texture2D(mainCamera.pixelWidth, mainCamera.pixelHeight);
            RenderTexture.active = rt;
            Rect dim = new Rect(0, 0, mainCamera.pixelWidth, mainCamera.pixelHeight);
            screenShot.ReadPixels(dim, 0, 0); 
            mainCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            return Sprite.Create(screenShot, dim, new Vector2(0.5f, 0.5f));
        }

        public IEnumerator TakeNPhotos(int delaySec,
                                       DateTime startTime,
                                       ScreenspaceCanvas ssCanvas,
                                       GameObject mainPanel,
                                       int N = 1)
        {
            // Hide the main panel
            mainPanel.transform.localScale = new Vector3(0, 0, 0);

            // Take N photos
            for (int n = 0; n < N; n++)
            {
                // Each waits for delaySec
                for (int s = delaySec; s > 0; s--)
                {
                    Debug.Log(s);
                    ssCanvas.DisplayCountdown(s.ToString());
                    yield return new WaitForSeconds(1);
                }

                ssCanvas.HideCountdown();

                var timestamp = DateTime.Now - startTime;
                // var photoData = ScreenCapture.CaptureScreenshotAsTexture(); 
                // var photo = new GeosamplingPhoto(timestamp, Sprite.Create(photoData, new Rect(0, 0, photoData.width, photoData.height), new Vector2(0.5f, 0.5f)));
                // photos.Add(photo);
                // UpdateGeoPanel();
                // geoPanel.SetActive(true);

                String msg = String.Format(
                    "Cheese! Geosample {0} taken {1}:{2}:{3} after Egress finished, (Item {4}, {5})",
                    photos.Count, timestamp.Hours, timestamp.Minutes, timestamp.Seconds, itemID, lithology);
                ssCanvas.DisplayMessage(msg, 5);
                
                //
                // Debug.LogFormat("Cheese! Geosample {0} taken {1}:{2}:{3} after Egress finished, (Item {4}, {5})",
                //     photos.Count, timestamp.Hours, timestamp.Minutes, timestamp.Seconds, itemID, lithology);
            }

            // TODO: Should be automatically hidden in DisplayMessage after nsecs
            yield return new WaitForSeconds(2);
            // geoPanel.SetActive(false);
            ssCanvas.HideMessage();

            // Bring back the main panel
            mainPanel.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private List<GeosamplingItem> _geosamplingItems;
    private GameObject _mainPanel;  // hide during countdown
    private MapController _mapControllerScript;
    private ScreenspaceCanvas _ssCanvasScript;
    private static Camera mainCamera;

    void Awake()
    {
        // _mainPanel = GameObject.Find("Main Panel");
        // _mapControllerScript = GameObject.Find("Map Panel").GetComponent<MapController>();
        _ssCanvasScript = GameObject.Find("SS Canvas").GetComponent<ScreenspaceCanvas>();
        mainCamera = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        _geosamplingItems = new List<GeosamplingItem>();
    }

    public void ScannerCallback()
    {
        // Get timestamp since Egress starts
        // var deltaTime = DateTime.Now - _mapControllerScript._startTimestamp;
        if (!_mapControllerScript.StartTimestamp.HasValue)
        {
            String msg = "Cannot take Geosamples: Egress not finished yet.";
            Debug.Log(msg);
            _ssCanvasScript.DisplayMessage(msg, 3);
            return;
        }

        _geosamplingItems.Add(new GeosamplingItem("Lithology", _geosamplingItems.Count+1));

        // Take N=3 photos, each 5 seconds apart
        IEnumerator coroutine = _geosamplingItems.Last().TakeNPhotos(
            3, _mapControllerScript.StartTimestamp.Value,
            _ssCanvasScript, _mainPanel);
        StartCoroutine(coroutine);
    }

    // public void LateUpdate()
    // {
    //     Debug.Log("yo");
    //     var photoData = ScreenCapture.CaptureScreenshotAsTexture();
    //     var photo = Sprite.Create(photoData, new Rect(0, 0, photoData.width, photoData.height),
    //         new Vector2(0.5f, 0.5f));
    //     geoImage.sprite = photo;
    // }
}

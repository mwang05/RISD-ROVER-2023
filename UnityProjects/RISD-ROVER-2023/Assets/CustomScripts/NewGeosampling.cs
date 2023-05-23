using System.Collections;
using System.Collections.Generic;
using TSS.Msgs;
using UnityEngine;
using UnityEngine.UI;

public class NewGeosampling : MonoBehaviour
{
    public class SampleMetadata {
        public int SampleNum;
        public int MissionNum;
        public string RockType;

        public SampleMetadata(int sampleNum, int missionNum, string rockType)
        {
            SampleNum = sampleNum;
            MissionNum = missionNum;
            RockType = rockType;
        }

        public SampleMetadata()
        {
            RockType = "";
        }

        public void DisplayMetadata()
        {
            if (RockType == "")
            {
                missionNum_Text.text = "";
                rockType_Text.text = "Unknown Sample";
                sampleNum_Text.text = "";
            }
            else
            {
                missionNum_Text.text = "Mission #" + MissionNum.ToString();
                rockType_Text.text = RockType;
                sampleNum_Text.text = "Sample #" + SampleNum.ToString();
            }
        }
    };

    public class SampleSpec {
        public float SiO2;
        public float TiO2;
        public float Al2O3;
        public float FeO;
        public float MnO;
        public float MgO;
        public float CaO;
        public float K2O;
        public float P2O3;

        public SampleSpec(float siO2, float tiO2, float al2O3, float feO, float mnO, float mgO, float caO, float k2O, float p2O3)
        {
            SiO2 = siO2;
            TiO2 = tiO2;
            Al2O3 = al2O3;
            FeO = feO;
            MnO = mnO;
            MgO = mgO;
            CaO = caO;
            K2O = k2O;
            P2O3 = p2O3;
        }

        public SampleSpec(SpecMsg msg)
        {
            SiO2 = msg.SiO2;
            TiO2 = msg.TiO2;
            Al2O3 = msg.Al2O3;
            FeO = msg.FeO;
            MnO = msg.MnO;
            MgO = msg.MgO;
            CaO = msg.CaO;
            K2O = msg.K2O;
            P2O3 = msg.P2O3;
        }

        public void DisplaySpec()
        {
            SiO2_Text.text = SiO2.ToString();
            TiO2_Text.text = TiO2.ToString();
            Al2O3_Text.text = Al2O3.ToString();
            FeO_Text.text = FeO.ToString();
            MnO_Text.text = MnO.ToString();
            MgO_Text.text = MgO.ToString();
            CaO_Text.text = CaO.ToString();
            K2O_Text.text = K2O.ToString();
            P2O3_Text.text = P2O3.ToString();
        }
    }

    private NotificationController notificationController;

    private GameObject numberScanned;
    private TMPro.TMP_Text numberText;

    private GameObject compositionInfo;
    private static TMPro.TMP_Text missionNum_Text, rockType_Text, sampleNum_Text;
    private static TMPro.TMP_Text SiO2_Text, TiO2_Text, Al2O3_Text, FeO_Text, MnO_Text, MgO_Text, CaO_Text, K2O_Text, P2O3_Text;

    private SampleMetadata[] sampleMetadatas = {
        new SampleMetadata(70215312, 17, "Mare basalt"),
        new SampleMetadata(1555611, 15, " Vesicular basalt"), 
        new SampleMetadata(12002492, 12, " Olivine basalt"),
        new SampleMetadata(14310220, 14, " Feldspathic basalt"),
        new SampleMetadata(1205226, 12, "Pigeonite basalt"),
        new SampleMetadata(1555562, 15, "Olivine basalt"),
        new SampleMetadata(1001730, 11, "Ilmenite basalt"),
    };

    private SampleSpec[] sampleSpecs = {
        new SampleSpec(40.58f, 12.83f, 10.91f, 13.18f, 0.19f, 6.7f, 10.64f, -0.11f, 0.34f),
        new SampleSpec(36.89f, 2.44f, 9.6f, 14.52f, 0.24f, 5.3f, 8.22f, -0.13f, 0.29f),
        new SampleSpec(41.62f, 2.44f, 9.52f, 18.12f, 0.27f, 11.1f, 8.12f, -0.12f, 0.28f),
        new SampleSpec(46.72f, 1.1f, 19.01f, 7.21f, 0.14f, 7.83f, 14.22f, 0.43f, 0.65f),
        new SampleSpec(46.53f, 3.4f, 11.68f, 16.56f, 0.24f, 6.98f, 11.11f, -0.02f, 0.38f),
        new SampleSpec(42.45f, 1.56f, 11.44f, 17.91f, 0.27f, 10.45f, 9.37f, -0.08f, 0.34f),
        new SampleSpec(42.56f, 9.38f, 12.03f, 11.27f, 0.17f, 9.7f, 10.52f, 0.28f, 0.44f),
    };

    private List<SampleSpec> scannedSpecs;
    private List<SampleMetadata> scannedMetadatas;

    private GameObject nextButton, prevButton;

    int currentIndex;

    void Awake()
    {
        notificationController = GameObject.Find("Notifications").GetComponent<NotificationController>();

        numberScanned = GameObject.Find("Number Scanned");
        numberText = GameObject.Find("Number Text").GetComponent<TMPro.TMP_Text>();

        compositionInfo = GameObject.Find("Composition Info");
        missionNum_Text = GameObject.Find("Mission Num").GetComponent<TMPro.TMP_Text>();
        rockType_Text = GameObject.Find("Rock Type").GetComponent<TMPro.TMP_Text>();
        sampleNum_Text = GameObject.Find("Sample Number").GetComponent<TMPro.TMP_Text>();
        SiO2_Text = GameObject.Find("Percent 1").GetComponent<TMPro.TMP_Text>();
        TiO2_Text = GameObject.Find("Percent 2").GetComponent<TMPro.TMP_Text>();
        Al2O3_Text = GameObject.Find("Percent 3").GetComponent<TMPro.TMP_Text>();
        FeO_Text = GameObject.Find("Percent 4").GetComponent<TMPro.TMP_Text>();
        MnO_Text = GameObject.Find("Percent 5").GetComponent<TMPro.TMP_Text>();
        MgO_Text = GameObject.Find("Percent 6").GetComponent<TMPro.TMP_Text>();
        CaO_Text = GameObject.Find("Percent 7").GetComponent<TMPro.TMP_Text>();
        K2O_Text = GameObject.Find("Percent 8").GetComponent<TMPro.TMP_Text>();
        P2O3_Text = GameObject.Find("Percent 9").GetComponent<TMPro.TMP_Text>();

        scannedSpecs = new List<SampleSpec>();
        scannedMetadatas = new List<SampleMetadata>();

        nextButton = GameObject.Find("Geo Next Button");
        prevButton = GameObject.Find("Geo Previous Button");
    }

    float startTime;
    bool t1;
    bool t2;
    bool t3;

    void FixedUpdate()
    {
        if (!t1 & Time.time - startTime > 1) {
            t1 = true;
            SpecMsgUpdateCallback(new SpecMsg());
        }
        if (!t2 & Time.time - startTime > 6) {
            t2 = true;
            SpecMsg msg = new SpecMsg();
            msg.SiO2 = 40.58f;
            msg.TiO2 = 12.83f;
            msg.Al2O3 = 10.91f;
            msg.FeO = 13.18f;
            msg.MnO = 0.19f;
            msg.MgO = 6.7f;
            msg.CaO = 10.64f;
            msg.K2O = -0.11f;
            msg.P2O3 = 0.34f;
            
            SpecMsgUpdateCallback(msg);
        }
        if (!t3 & Time.time - startTime > 15) {
            t3 = true;
            SpecMsgUpdateCallback(new SpecMsg());
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // startTime = Time.time;
        numberScanned.SetActive(true);
        compositionInfo.SetActive(false);
        nextButton.SetActive(false);
        prevButton.SetActive(false);
    }

    public void SpecMsgUpdateCallback(SpecMsg msg)
    {
        scannedSpecs.Add(new SampleSpec(msg));

        bool match = false;
        for (int i = 0; i < sampleSpecs.Length; i++)
        {
            if (
                sampleSpecs[i].SiO2 == msg.SiO2 &&
                sampleSpecs[i].TiO2 == msg.TiO2 &&
                sampleSpecs[i].Al2O3 == msg.Al2O3 &&
                sampleSpecs[i].FeO == msg.FeO &&
                sampleSpecs[i].MnO == msg.MnO &&
                sampleSpecs[i].MgO == msg.MgO &&
                sampleSpecs[i].CaO == msg.CaO &&
                sampleSpecs[i].K2O == msg.K2O &&
                sampleSpecs[i].P2O3 == msg.P2O3
            )
            {
                scannedMetadatas.Add(sampleMetadatas[i]);
                match = true;
                break;
            }
        }

        if (!match) scannedMetadatas.Add(new SampleMetadata());
        notificationController.PushGeoComplete(3);
        numberText.text = scannedSpecs.Count.ToString();
        
        ShowCompositionInfo();
    }

    private void DisplaySample()
    {
        scannedMetadatas[currentIndex].DisplayMetadata();
        scannedSpecs[currentIndex].DisplaySpec();
    }

    public void CloseCompositionInfo()
    {
        notificationController.HideGeoComplete();
        compositionInfo.SetActive(false);
        numberScanned.SetActive(true);
        nextButton.SetActive(false);
        prevButton.SetActive(false);
    }

    public void ShowCompositionInfo()
    {
        if (scannedSpecs.Count == 0) return;
        currentIndex = scannedSpecs.Count - 1;
        DisplaySample();
        numberScanned.SetActive(false);
        compositionInfo.SetActive(true);
        UpdateButtons();
    }

    public void NextSample()
    {
        currentIndex++;
        DisplaySample();
        UpdateButtons();
    }

    public void PrevSample()
    {
        currentIndex--;
        DisplaySample();
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        nextButton.SetActive(currentIndex < scannedSpecs.Count - 1);
        prevButton.SetActive(currentIndex > 0);
    }
}

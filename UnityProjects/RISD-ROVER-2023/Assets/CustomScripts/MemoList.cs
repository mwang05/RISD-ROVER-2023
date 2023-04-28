using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Microsoft.MixedReality.Toolkit.UX;
using TMPro;


public class MemoList : VirtualizedScrollRectList
{
    enum VoiceMemoState
    {
        MemoIdle,
        MemoRecording,
        MemoPaused,
        MemoPlaying,
    };

	struct CurrPlayingGO
	{
		public Button button;
        public TextMeshProUGUI timeL;
		public RectTransform pbarRT;

		public CurrPlayingGO(Button button, TextMeshProUGUI timeL, RectTransform pbarRT)
		{
			this.button = button;
			this.timeL = timeL;
			this.pbarRT = pbarRT;
		}
	}

    private int _nextID = 1;    // Next avaiable ID (name) for voice memo
    private VoiceMemoState _currState = VoiceMemoState.MemoIdle;  // Are we idle/recording/playing?
    private float _recordTime = 0.0f;  // If MemoRecording, how many secs have elapsed?
    private AudioClip _currReco;
    private int _currPlayingIdx = -1;  // index of the playing reco
	private CurrPlayingGO? _currPlayingGO = null;  // if memoItem _currPlayingIdx is visible, the references to its gameobjects

    private AudioSource _audioSource;
    private List<AudioClip> _recos = new List<AudioClip>();

    private TextMeshProUGUI _recordTimeTMP;

    private Button _recordBut;
    public Sprite recordOnSprite, recordOffSprite;
    public Sprite memoPlaySprite, memoPauseSprite;

    // On deletion of memoItem: InitializePool(); then Scroll.set(Scroll.get()); ?

    void PlayButtonCallback()
    {
        var selectedBtnGO = EventSystem.current.currentSelectedGameObject;
        var selectedBtn = selectedBtnGO.GetComponent<Button>();
        var memoItemGO = selectedBtnGO.transform.parent.gameObject;
        int recoIdx = Mathf.RoundToInt(PosToScroll(-memoItemGO.transform.localPosition.y));

        switch (_currState)
        {
            case VoiceMemoState.MemoIdle:
                _currState = VoiceMemoState.MemoPlaying;
                _currPlayingIdx = recoIdx;
				UpdateCurrPlayingGO();
                selectedBtn.image.sprite = memoPauseSprite;

                _audioSource.clip = _recos[recoIdx];
                _audioSource.time = 0.0f;
                _audioSource.Play();

                break;
            case VoiceMemoState.MemoPlaying:
                if (_currPlayingIdx == recoIdx)
                {
                    // Pause currently playing memo
                    _audioSource.Pause();
                    _currState = VoiceMemoState.MemoPaused;
                    selectedBtn.image.sprite = memoPlaySprite;
                }
                else
                {
                    // Stop _currPlayingIdx and start playing recoIdx
                    // if _currPlayingIdx visible, update sprite to Play and playLen to 00:00
					if (_currPlayingGO.HasValue)
                    // if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                    {
                        // var currPlayingTf = poolDict[_currPlayingIdx].transform;
                        // var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
                        // var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();
						// var currPlayingPbarRT = currPlayingTf.Find(
						//						"Progress Bar").gameObject.transform.Find(
						//						"Progress Bar FG").gameObject.GetComponent<RectTransform>();
						UpdatePlayBarAndTime(_currPlayingGO.Value.pbarRT, _currPlayingGO.Value.timeL, 0.0f);
                        _currPlayingGO.Value.button.image.sprite = memoPlaySprite;
                    }

                    selectedBtn.image.sprite = memoPauseSprite;
                    _currPlayingIdx = recoIdx;
					UpdateCurrPlayingGO();
					_audioSource.Stop();
                    _audioSource.clip = _recos[recoIdx];
                    _audioSource.Play();
                }
                break;
            case VoiceMemoState.MemoPaused:
                _currState = VoiceMemoState.MemoPlaying;
                selectedBtn.image.sprite = memoPauseSprite;

                if (_currPlayingIdx == recoIdx)
                {
                    // Resume currently paused memo
                    _audioSource.Play();
                }
                else
                {
                    // Stop playing current, start playing recoIdx
                    // TODO: If _currPlayingIdx visible, update playLen to 00:00
					if (_currPlayingGO.HasValue)
                    // if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                    {
                        // var currPlayingTf = poolDict[_currPlayingIdx].transform;
                        // var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();
						// var currPlayingPbarRT = currPlayingTf.Find(
						//						"Progress Bar").gameObject.transform.Find(
						//						"Progress Bar FG").gameObject.GetComponent<RectTransform>();

						UpdatePlayBarAndTime(_currPlayingGO.Value.pbarRT, _currPlayingGO.Value.timeL, 0.0f);
                    }

                    _currPlayingIdx = recoIdx;
					UpdateCurrPlayingGO();
                    _audioSource.clip = _recos[recoIdx];
                    _audioSource.time = 0.0f;
                    _audioSource.Play();
                }
                break;
            default: // MemoRecording: ignore play button
                break;
        }

        // Debug.LogFormat("Play button {0} clicked, offset: {1}", memoItem.name, PosToScroll(-memoItem.transform.localPosition.y));
    }

    void TrashButtonCallback()
    {
        var selectedBtnGO = EventSystem.current.currentSelectedGameObject;
        var selectedBtn = selectedBtnGO.GetComponent<Button>();
        var memoItemGO = selectedBtnGO.transform.parent.gameObject;
        int recoIdx = Mathf.RoundToInt(PosToScroll(-memoItemGO.transform.localPosition.y));
        Debug.LogFormat("Trash button clicked at index {0}!", recoIdx);

		if (recoIdx == _currPlayingIdx)
		{
			_audioSource.Stop();
			_currState = VoiceMemoState.MemoIdle;
			_currPlayingIdx = -1;
			_currPlayingGO = null;
		}
		else if (_currPlayingIdx != -1 && recoIdx < _currPlayingIdx)
		{
			_currPlayingIdx--;
		}

		_recos.RemoveAt(recoIdx);

		InitializePool();
		SetItemCount(_recos.Count);
    }

    void OnVisibleCallback(GameObject go, int index)
    {
        Debug.Log("GameObject " + go.name + " is now visible at index " + index);

        var playBut = go.transform.Find("Play Button").gameObject.GetComponent<Button>();

        var recName = go.transform.Find("Record Name").gameObject.GetComponent<TextMeshProUGUI>();
        recName.text = _recos[index].name;

        var timeR = go.transform.Find("TimeR").gameObject.GetComponent<TextMeshProUGUI>();
        var recoLen = ConvertSecsToMinSecs(_recos[index].length);
        timeR.text = string.Format("{0:00}:{1:00}", recoLen.minutes, recoLen.seconds);

		var pbarRT = go.transform.Find(
					 	"Progress Bar").gameObject.transform.Find(
						"Progress Bar FG").gameObject.GetComponent<RectTransform>();
        var timeL = go.transform.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();
        if (index == _currPlayingIdx)
        {
			UpdatePlayBarAndTime(pbarRT, timeL, GetAudioPercentage());
            playBut.image.sprite = (_currState == VoiceMemoState.MemoPlaying)
                                 ? memoPauseSprite
                                 : memoPlaySprite;
			// TODO: Update _currPlayingGO
			UpdateCurrPlayingGO();
        }
        else
        {
			UpdatePlayBarAndTime(pbarRT, timeL, 0.0f);
            playBut.image.sprite = memoPlaySprite;
        }

        go.SetActive(true);
    }

    void OnInvisibleCallback(GameObject go, int index)
    {
        // Debug.Log("GameObject " + go.name + " is now invisible at index " + index);
        go.SetActive(false);
        if (index == _currPlayingIdx)
		{
			_currPlayingGO = null;
		}
    }

	protected void UpdateCurrPlayingGO()
	{
        var currPlayingTf = poolDict[_currPlayingIdx].transform;
        var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
        var currPlayingTimeL = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();
		var currPlayingPbarRT = currPlayingTf.Find(
									"Progress Bar").gameObject.transform.Find(
									"Progress Bar FG").gameObject.GetComponent<RectTransform>();
		_currPlayingGO = new CurrPlayingGO(currPlayingBtn, currPlayingTimeL, currPlayingPbarRT);
	}

    new protected void Start()
    {
        base.Start();

        OnVisible = OnVisibleCallback;
        OnInvisible = OnInvisibleCallback;
        SetItemCount(0);

        _recordBut = GameObject.Find("Record Button").GetComponent<Button>();
        _recordTimeTMP = GameObject.Find("Record Duration").GetComponent<TextMeshProUGUI>();
        _recordBut.image.sprite = recordOffSprite;

        _audioSource = GetComponent<AudioSource>();
    }

	override protected void InstantiatePrefabs()
	{
        int poolSize = screenCount + layoutRowsOrColumns;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(prefab, ItemLocation(-(i + 1)), Quaternion.identity, scrollRect.content);
            var playBut = go.transform.Find("Play Button").gameObject.GetComponent<Button>();
       		playBut.onClick.AddListener(PlayButtonCallback);
        	var trashBut = go.transform.Find("Trash Button").gameObject.GetComponent<Button>();
        	trashBut.onClick.AddListener(TrashButtonCallback);
			pool.Enqueue(go);
        }
	}

	protected float GetAudioPercentage()
	{
		return _audioSource.time / _audioSource.clip.length;
	}

	protected void UpdatePlayBarAndTime(RectTransform playBarRT, TextMeshProUGUI timeL, float percentage)
    {
		if (percentage == 0.0f)
		{
        	timeL.text = "00:00";
		}
		else
		{
			var playLen = ConvertSecsToMinSecs(_audioSource.time);
        	timeL.text = string.Format("{0:00}:{1:00}", playLen.minutes, playLen.seconds);
		}
		// TODO: get max width (54.0f) programatically
		float newWidth = Mathf.Max(percentage * 54.0f, 3.0f);
		playBarRT.sizeDelta = new Vector2 (newWidth, playBarRT.sizeDelta.y);
	}


    // Update is called once per frame
    void Update()
    {
        switch (_currState)
        {
            case VoiceMemoState.MemoRecording:
                _recordTime += Time.deltaTime;
                UpdateRecordTimeDisplay();
                break;
            case VoiceMemoState.MemoPlaying:
                // TODO: update PlayTimeDisplay
				if (_currPlayingGO.HasValue)
                // if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                {
					// TODO: This needs optimization
                    // var currPlayingTf = poolDict[_currPlayingIdx].transform;
                    // var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();
					// var currPlayingPbarRT = currPlayingTf.Find(
					//							"Progress Bar").gameObject.transform.Find(
					//							"Progress Bar FG").gameObject.GetComponent<RectTransform>();

					if (_audioSource.isPlaying)
					{
						UpdatePlayBarAndTime(_currPlayingGO.Value.pbarRT, _currPlayingGO.Value.timeL, GetAudioPercentage());
						// UpdatePlayBarAndTime(currPlayingPbarRT, currPlayingPlayLen, GetAudioPercentage());
					}
					else
					{
						UpdatePlayBarAndTime(_currPlayingGO.Value.pbarRT, _currPlayingGO.Value.timeL, 0.0f);
                    	_currPlayingGO.Value.button.image.sprite = memoPlaySprite;
						_currState = VoiceMemoState.MemoIdle;
						_currPlayingIdx = -1;
						_currPlayingGO = null;
					}

                }
                break;
            default:  // Idle or Paused
                break;
        }

    }

    public void RecordButtonOnClick()
    {
        switch (_currState)
        {
            case VoiceMemoState.MemoRecording:
                // End recording
                EndVoiceMemo();
                UpdateRecordTimeDisplay();
                break;
            case VoiceMemoState.MemoPlaying:
            case VoiceMemoState.MemoPaused:
                // Stop playing and start recording
				_audioSource.Stop();

				if (_currPlayingGO.HasValue)
                // if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                {
                    // var currPlayingTf = poolDict[_currPlayingIdx].transform;
                    // var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
                    // var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();
					// var currPlayingPbarRT = currPlayingTf.Find(
					//							"Progress Bar").gameObject.transform.Find(
					//							"Progress Bar FG").gameObject.GetComponent<RectTransform>();

                    _currPlayingGO.Value.button.image.sprite = memoPlaySprite;
					UpdatePlayBarAndTime(_currPlayingGO.Value.pbarRT, _currPlayingGO.Value.timeL, 0.0f);

					// currPlayingPbarRT.sizeDelta = new Vector2 (0.0f, currPlayingPbarRT.sizeDelta.y);
                    // currPlayingPlayLen.text = "00:00";
                }
                _currPlayingIdx = -1;
				_currPlayingGO = null;

                StartVoiceMemo();
                break;
            default:  // Idle
                // Start recording
                StartVoiceMemo();
                break;
        }
    }

    private void EndVoiceMemo()
    {
        Microphone.End("");

        // Trim the audioclip by the length of the recording
        AudioClip recoTrimmed = GetTrimmedAudioClip(
            _currReco, string.Format("Recording {0}", _nextID++));
        // Debug.Log(recoTrimmed.length);
        // Debug.Log(recoTrimmed.name);
        _recos.Add(recoTrimmed);
        SetItemCount(_recos.Count);

        _currReco = null;

        _currState = VoiceMemoState.MemoIdle;
        _recordTime = 0.0f;

		_recordBut.image.sprite = recordOffSprite;
    }

    private void StartVoiceMemo()
    {
        _currReco = Microphone.Start("", false, 600, 44100);  // cap 10 mins
        _currState = VoiceMemoState.MemoRecording;
        _recordBut.image.sprite = recordOnSprite;
    }

    private AudioClip GetTrimmedAudioClip(AudioClip ac, string name)
    {
        int nSamples = (int)(_recordTime * ac.frequency);
        AudioClip trimmed = AudioClip.Create(name, nSamples, ac.channels, ac.frequency, false);
        float[] data = new float[nSamples];
        ac.GetData(data, 0);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    (int minutes, float seconds) ConvertSecsToMinSecs(float secs)
    {
        int minutes = Mathf.FloorToInt(secs / 60);
        float seconds = secs % 60;
        return (minutes, seconds);
    }

    void UpdateRecordTimeDisplay()
    {
        // Convert the time to minutes and seconds
        var displayTime = ConvertSecsToMinSecs(_recordTime);

        // Update the timer text
        _recordTimeTMP.text = string.Format("{0:00}:{1:00.00}", displayTime.minutes, displayTime.seconds);
    }


}

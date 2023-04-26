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

    private int _nextID = 1;    // Next avaiable ID (name) for voice memo
    private VoiceMemoState _currState = VoiceMemoState.MemoIdle;  // Are we idle/recording/playing?
    private float _recordTime = 0.0f;  // If MemoRecording, how many secs have elapsed?
    private AudioClip _currReco;
    private int _currPlayingIdx = -1;  // index of the playing reco

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

        Debug.LogFormat("currState: {0}", _currState);
        Debug.LogFormat("currPlaying: {0}", _currPlayingIdx);
        switch (_currState)
        {
            case VoiceMemoState.MemoIdle:
                _currState = VoiceMemoState.MemoPlaying;
                _currPlayingIdx = recoIdx;
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
                    if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                    {
                        var currPlayingTf = poolDict[_currPlayingIdx].transform;
                        var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
                        var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();

                        currPlayingBtn.image.sprite = memoPlaySprite;
                        currPlayingPlayLen.text = "00:00";
                    }

                    selectedBtn.image.sprite = memoPauseSprite;
                    _currPlayingIdx = recoIdx;
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
                    if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                    {
                        var currPlayingTf = poolDict[_currPlayingIdx].transform;
                        // var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
                        var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();

                        // currPlayingBtn.image.sprite = memoPlaySprite;
                        currPlayingPlayLen.text = "00:00";
                    }

                    _currPlayingIdx = recoIdx;
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
        Debug.Log("Trash button clicked!");
    }

    void OnVisibleCallback(GameObject go, int index)
    {
        Debug.Log("GameObject " + go.name + " is now visible at index " + index);

        // // TODO: Are there better ways?
        var playBut = go.transform.Find("Play Button").gameObject.GetComponent<Button>();
        // var trashBut = go.transform.Find("Trash Button").gameObject.GetComponent<Button>();

        var recName = go.transform.Find("Record Name").gameObject.GetComponent<TextMeshProUGUI>();
        recName.text = _recos[index].name;

        var recoLen = ConvertSecsToMinSecs(_recos[index].length);
        var timeR = go.transform.Find("TimeR").gameObject.GetComponent<TextMeshProUGUI>();
        timeR.text = string.Format("{0:00}:{1:00}", recoLen.minutes, recoLen.seconds);

        var timeL = go.transform.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();

        if (index == _currPlayingIdx)
        {
			// Debug.Log(_audioSource.time);
            var playLen = ConvertSecsToMinSecs(_audioSource.time);
            timeL.text = string.Format("{0:00}:{1:00}", playLen.minutes, playLen.seconds);

            playBut.image.sprite = (_currState == VoiceMemoState.MemoPlaying)
                                 ? memoPauseSprite
                                 : memoPlaySprite;
        }
        else
        {
            timeL.text = "00:00";
            playBut.image.sprite = memoPlaySprite;
        }

        go.SetActive(true);
    }

    void OnInvisibleCallback(GameObject go, int index)
    {
        // Debug.Log("GameObject " + go.name + " is now invisible at index " + index);
        go.SetActive(false);
    }

    void Start()
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
                if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                {
                    var currPlayingTf = poolDict[_currPlayingIdx].transform;
                    var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();

					if (_audioSource.isPlaying)
					{
						var playLen = ConvertSecsToMinSecs(_audioSource.time);
        				currPlayingPlayLen.text = string.Format("{0:00}:{1:00}", playLen.minutes, playLen.seconds);
					}
					else
					{
					    currPlayingPlayLen.text = "00:00";
						_currPlayingIdx = -1;
						_currState = VoiceMemoState.MemoIdle;
						_audioSource.time = 0.0f;

                    	var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
                    	currPlayingBtn.image.sprite = memoPlaySprite;
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
                _recordBut.image.sprite = recordOffSprite;
                break;
            case VoiceMemoState.MemoPlaying:
            case VoiceMemoState.MemoPaused:
                // Stop playing and start recording
				_audioSource.Stop();
                if (_currPlayingIdx >= visibleStart && _currPlayingIdx < visibleEnd)
                {
                    var currPlayingTf = poolDict[_currPlayingIdx].transform;
                    var currPlayingBtn = currPlayingTf.Find("Play Button").gameObject.GetComponent<Button>();
                    var currPlayingPlayLen = currPlayingTf.Find("TimeL").gameObject.GetComponent<TextMeshProUGUI>();

                    currPlayingBtn.image.sprite = memoPlaySprite;
                    currPlayingPlayLen.text = "00:00";
                }
                _currPlayingIdx = -1;

                StartVoiceMemo();
                _recordBut.image.sprite = recordOnSprite;
                break;
            default:  // Idle
                // Start recording
                StartVoiceMemo();
                _recordBut.image.sprite = recordOnSprite;
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
    }

    private void StartVoiceMemo()
    {
        _currReco = Microphone.Start("", false, 600, 44100);  // cap 10 mins
        _currState = VoiceMemoState.MemoRecording;
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

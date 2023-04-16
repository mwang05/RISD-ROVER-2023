using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Compass : MonoBehaviour
{
    [SerializeField] private float distanceFromUser = 0.48f;
    [SerializeField] private float offsetUp = 0.2f;

    private RawImage _compassImage;
    private TextMeshProUGUI _compassDirText;
    private Camera _mainCamera;

	private Transform _compassTF;
	private MeshRenderer _compassRenderer;

	private Vector3 _compassScale;

    void Awake()
    {
        _compassImage = GameObject.Find("Compass Image").GetComponent<RawImage>();
        // _compassDirText = GameObject.Find("Compass Degree").GetComponent<TextMeshProUGUI>();
        _mainCamera = Camera.main;

		_compassTF = GameObject.Find("Compass").GetComponent<Transform>();
		_compassRenderer = GameObject.Find("Compass").GetComponent<MeshRenderer>();
		_compassScale = _compassTF.localScale;
    }

	float calculateOffsetUp(float angleUp) {
		return offsetUp + Mathf.Max(10.0f - angleUp, 0.0f) * 0.01f;
	}

    // Update is called once per frame
    void Update()
    {
		// compass angle
        float angle = _mainCamera.transform.localEulerAngles.y;
        _compassImage.uvRect = new Rect(angle / 360.0f, 0.0f, 1.0f, 1.0f);
        // _compassDirText.text = Mathf.RoundToInt(angle).ToString();

		// vert angle
		float angleUp = 360.0f - _mainCamera.transform.localEulerAngles.x;
		if (angleUp > 90.0f)
		{
			_compassTF.localScale = new Vector3(0, 0, 0);
			return;
		}

		// else: normal scale
		_compassTF.localScale = _compassScale;

		Vector3 userLook = _mainCamera.transform.forward;
		Vector3 userUp = _mainCamera.transform.up;
		_compassTF.position = (_mainCamera.transform.position +
								userLook * distanceFromUser +
								userUp * calculateOffsetUp(angleUp));

    }
}

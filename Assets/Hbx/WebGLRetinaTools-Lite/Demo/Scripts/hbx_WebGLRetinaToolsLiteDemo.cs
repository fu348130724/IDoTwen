using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class hbx_WebGLRetinaToolsLiteDemo : MonoBehaviour {

	public Canvas _canvas;
	public Text _resolutionText;
	public Text _cursorText;

	void Update()
	{
		_resolutionText.text = "Screen\n" + Screen.width + "x" + Screen.height + "\nViewport\n" + Camera.main.pixelRect.width + "x" + Camera.main.pixelHeight;
		_cursorText.text = Mathf.FloorToInt(Input.mousePosition.x) + "x" + Mathf.FloorToInt(Input.mousePosition.y);

		Vector2 pos;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, Input.mousePosition, _canvas.worldCamera, out pos);
		_cursorText.gameObject.transform.position = _canvas.transform.TransformPoint(pos) + new Vector3(0,10,0);
	}

	public void OnFullscreenClicked()
	{
		Screen.fullScreen = !Screen.fullScreen;
	}
}

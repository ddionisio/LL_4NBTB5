using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Preservatives : MonoBehaviour {
	public Camera _cam;
	public Graphic _img;
	public CanvasGroup _canvasGroup;
	public RectTransform _rectTransform;
	public Blob _blob;
	public SpriteRenderer _spriteRenderer;
	public M8.SpriteColorAlpha _spriteColorAlpha;
	public ParticleSystem _particleSystem;
	public M8.SoundPlaylistProxy _sfxPlayProx;
	public M8.ColorFromPaletteBase _colorFromPalette;
	public M8.SpriteColorGroup _spriteColorGroup;
	public TextMeshProUGUI _textGUI;

	void Awake() {
		if(_cam) {
			_cam.backgroundColor = Color.white;
		}

		if(_img) {
			_img.color = Color.white;
		}

		if(_canvasGroup) {
			_canvasGroup.alpha = 1f;
			_canvasGroup.interactable = true;
			_canvasGroup.blocksRaycasts = true;
		}

		if(_rectTransform) {
			_rectTransform.anchoredPosition = Vector2.zero;
			_rectTransform.sizeDelta = Vector2.zero;
		}

		if(_blob) {
			_blob.colorAlpha = 1f;
		}

		if(_spriteRenderer) {
			_spriteRenderer.sprite = null;
			_spriteRenderer.color = Color.white;
		}

		if(_spriteColorAlpha) {
			_spriteColorAlpha.alpha = 1f;
		}

		if(_particleSystem) {
			_particleSystem.Play();
			_particleSystem.Stop();
		}

		if(_sfxPlayProx) {
			_sfxPlayProx.Play();
		}

		if(_colorFromPalette) {
			_colorFromPalette.alpha = 1f;
			_colorFromPalette.index = 0;
			_colorFromPalette.brightness = 1f;
		}

		if(_spriteColorGroup) {
			_spriteColorGroup.color = Color.white;
			_spriteColorGroup.ApplyColor();
			_spriteColorGroup.Revert();
		}

		if(_textGUI) {
			_textGUI.color = Color.white;
		}
	}
}

﻿using UnityEngine;
using System.Collections;

public class GameoverCanvasController : MonoBehaviour, IResultCanvas {
	// Use this for initialization
	void Start() {
		gameObject.GetComponent<Canvas>().enabled = false;
	}

	public void ShowResult(int score) {
		gameObject.GetComponent<Canvas>().enabled = true;
	}
}

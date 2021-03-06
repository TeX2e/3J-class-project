﻿using UnityEngine;
using System.Collections;

public class Wall : MonoBehaviour {
	private Range<float> xRange = new Range<float>(-0, 0);
	private Range<float> zRange = new Range<float>(-0, 0);

	public float GetMinX() { return xRange.min; }
	public float GetMaxX() { return xRange.max; }
	public float GetMinZ() { return zRange.min; }
	public float GetMaxZ() { return zRange.max; }
	public GameObject GetWall(string path) {
		return GameObject.Find("BlockPool/Walls/" + path);
	}

	void Update() {
		float minX = GetWall("min-x").transform.position.x;
		float maxX = GetWall("max-x").transform.position.x;
		float minZ = GetWall("min-z").transform.position.z;
		float maxZ = GetWall("max-z").transform.position.z;
		xRange = new Range<float>(minX, maxX);
		zRange = new Range<float>(minZ, maxZ);
	}
}


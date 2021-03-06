﻿/// 
/// @file   BlockPoolController.cs
/// @brief 
///   This script controls Pool which is stored cubes position.
///   control means merge new block in Pool and delete completed rows.
/// 

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

///
/// Behavior about BlockPoolController class
/// 
// Script Lifecycle Flowchart
// 
//       |call
//       ∨
//     BlockPoolController
//       ControlBlock()  << -----+
//       | InitPool()            | update()
//       | MergeBlock()          |   if (_DummyParent.isLanded) 
//       | SearchCubePos()       |     dummyParent.FinishDropping()
//       | RemoveCompletedRow()  |     recall
//       ∨                       |
//    
//     _DummyParent --------> isLanded = true
//       StartDropping()
/// 
/// After remove completed row, we need to wait cubes dropping.
/// then recall ControlBlock()
/// 
public class BlockPoolController : MonoBehaviour {
	private BlockPool blockPool = new BlockPool(5, 8, 5);

	public int GetSizeX() { return blockPool.GetSizeX(); }
	public int GetSizeY() { return blockPool.GetSizeY(); }
	public int GetSizeZ() { return blockPool.GetSizeZ(); }

	public BlockPool GetPool() { return blockPool; }

	private GameObject GetPoolCubesObj() { return GameObject.Find("BlockPool/Cubes"); }
	private GameObject GetGroundObj()    { return GameObject.Find("BlockPool/Ground"); }

	private _DummyParent GetDummyParent() {
		return GameObject.Find("_DummyParent").GetComponent<_DummyParent>();
	}
	private GameManager GetGameManager() {
		return GameObject.Find("GameManager").GetComponent<GameManager>();
	}

	void Start() {
		BlockController.StopFalling += new EventHandler(AddBlock);

		int size = blockPool.GetSizeX();
		// set position of BlockPool obj and scale
		if (size == 5) {
			// size : 5
			// position : (0, -2.5, 0)
			// scale : (0.5, 1.0, 0.5)
			transform.Translate(new Vector3(0f, -2.5f, 0f), Space.World);
			transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
		} else if (size == 6) {
			// size : 6
			// position : (0.5, -2.5, 0.5)
			// scale : (0.6, 1.0, 0.6)
			transform.Translate(new Vector3(0.5f, -2.5f, 0.5f), Space.World);
			transform.localScale = new Vector3(0.6f, 1.0f, 0.6f);
		} else {
			throw new Exception("POOL_X is expected to be 5 or 6. Instead of " + blockPool.GetSizeX());
		}
	}
	
	void Update() {
		if (GetDummyParent().isLanded) {
			GetDummyParent().FinishDropping();
			UpdateBlockPool();
		}

		// for debug
		/*
		if (Input.GetKeyDown("p")) {
			for (int z = 0; z < POOL_Z; z++) {
				for (int y = 0; y < POOL_Y; y++) {
					string str = "";
					for (int x = 0; x < POOL_X; x++) {
						str += (blockPool[x, y, z] == null) ? "_": "o";
					}
					print("(z, y) = (" + z + ", " + y + ") : " + str);
				}
			}
		}
		*/
	}

	private void AddBlock(object sender, EventArgs e) {
		MonoBehaviour block = (MonoBehaviour)sender;
		AddBlock(block.gameObject);
	}

	/// main process in Pool
	public void AddBlock(GameObject block) {
		MergeBlock(block);
		UpdateBlockPool();
	}

	public void UpdateBlockPool() {
		InitPool();
		SearchCubePos();
		FixCubePos();
		if (RemoveCompletedRow()) {
			// start to drop dummyParent
			GetDummyParent().StartDropping();
		}
	}
	
	public Wall GetWall() {
		return GameObject.Find("BlockPool/Walls").GetComponent<Wall>();
	}


	// private methods ------------------------------

	/// init the Pool
	private void InitPool() {
		blockPool.Init();
	}

	/// merge block cubes in BlockPool/Cubes
	/// 
	///     before:              ->       after:
	///                                   
	///       ├── BlockPool                 └── BlockPool
	///       │   ├── Ground                    ├── Ground
	///       │   ├── Walls                     ├── Walls
	///       │   └── Cubes                     └── Cubes
	///       └── block(dropping)                   ├── Cube
	///           ├── Cube                          ├── Cube
	///           ├── Cube                          └── Cube
	///           └── Cube                
	/// 
	private void MergeBlock(GameObject block) {
		if (block == null) return;
		// move block cubes into poolCubes
		block.tag = "BlockPool";
		block.name = "Cube";
		block.transform.parent = GetPoolCubesObj().transform;
		// remove rigitbody
		Destroy(block.GetComponent<Rigidbody>());

		var blockCubes = new ArrayList();
		foreach (Transform cube in block.transform) {
			blockCubes.Add(cube);
		}
		ChangeMaterial(block.transform);
		foreach (Transform cube in blockCubes) {
			cube.tag = "BlockPool";
			cube.transform.parent = GetPoolCubesObj().transform;
			ChangeMaterial(cube);  //change material of gameObject and gameObject.transform
		}
	}

	/// collect each position of block's cubes
	/// blockPool[,,] has each cubes position
	/// @see SetCubePos()
	private void SearchCubePos() {
		Wall wallPos = GetWall();

		Vector3 offset = new Vector3(0, 0, 0);
		offset.x = -wallPos.GetMinX();
		offset.z = -wallPos.GetMinZ();
		offset.y = -GetGroundObj().transform.position.y;

		foreach (Transform cube in GetPoolCubesObj().transform) {
			SetCubePos(cube, offset);
		}
	}

	/// set the cube position to Pool
	private void SetCubePos(Transform obj, Vector3 offset) {
		float halfOfWidth = 0.5f;
		int x = (int)Mathf.Round(obj.position.x + offset.x - halfOfWidth);
		int y = (int)Mathf.Round(obj.position.y + offset.y - halfOfWidth);
		int z = (int)Mathf.Round(obj.position.z + offset.z - halfOfWidth);
		// print("offset: " + offset);
		// print("target: " + obj.transform.position);
		// print("index : >> " + new Vector3(x, y, z));
		try {
			blockPool.SetGameObjectAt(x, y, z, obj.gameObject);
		} catch (IndexOutOfRangeException) {
			GetGameManager().GameOver();
		}
	}

	/// sometimes, cube is put unexpected position.
	/// therefore, cubes need to correct y position.
	private void FixCubePos() {
		for (int z = 0; z < blockPool.GetSizeZ(); z++) {
			for (int y = 0; y < blockPool.GetSizeY(); y++) {
				for (int x = 0; x < blockPool.GetSizeX(); x++) {
					if (blockPool.GetGameObject(x, y, z) == null) continue;
					blockPool.GetGameObject(x, y, z).transform.position = 
						VectorUtil.RoundY(blockPool.GetGameObject(x, y, z).transform.position);
				}
			}
		}
	}

	private bool RemoveCompletedRow() {
		return blockPool.RemoveCompletedRow();
	}
	
	/// change material of cube
	public void ChangeMaterial(Transform cube) {
		//Debug.Log(gameObject.GetComponent<MeshRenderer>().material.name);
		Material color = cube.GetComponent<MeshRenderer>().material;
		switch (color.name) {
			case "can (Instance)":
				color = (Material)Resources.Load("Prefabs/yellow");
				break;
			case "garbage (Instance)":
				color = (Material)Resources.Load("Prefabs/blue");
				break;
			case "garbage_bag_red (Instance)":
				color = (Material)Resources.Load("Prefabs/red");
				break;
			case "garbage_bag_green (Instance)":
				color = (Material)Resources.Load("Prefabs/green");
				break;
			case "garbage_bag_yellow (Instance)":
				color = (Material)Resources.Load("Prefabs/yellow");
				break;
			case "bottle (Instance)":
				color = (Material)Resources.Load("Prefabs/wood");
				break;
			case "recyclable_garbage (Instance)":
				color = (Material)Resources.Load("Prefabs/skyblue");
				break;
		}
		cube.GetComponent<MeshRenderer>().material = color;
	}
}












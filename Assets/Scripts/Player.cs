﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Player : MonoBehaviour
{
    // GameObject selected;

    WorldManager worldManager;
    UIManager uiController;

    GameObject controllerObject;
    public PlayerController controller; // shouldn't be public

    protected List<GameObject> playerUnitList;
    protected List<GameObject> playerTownList;
    protected List<GameObject> playerObjectiveList;

    bool[,] tilesExplored;

    GameObject mainTown;

    public int playerNumber;

    Camera mainCamera;

    Vector3 playerCameraPosition;

    public bool playerActive;

    // int unitVisionDistance = 3;
    int townVisionDistance = 6;

    public int gold;
    public int baseGPT; // base gold per turn
    public int townGPT; // additional gold per turn for each town constructed
    public int mineGPT; // additional gold per turn for each mine controlled

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main; //y tho?

        playerCameraPosition = mainTown.transform.position + new Vector3(0, 0, -10);

        playerObjectiveList = new List<GameObject>();

        controllerObject = Instantiate(worldManager.playerControllerPrefab, this.gameObject.transform);
        controller = controllerObject.GetComponent<PlayerController>();
        controller.player = this;
        controller.uiManager = uiController;
        controller.worldManager = worldManager;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Initialize(int p, WorldManager wm, UIManager uiC)
    {
        playerNumber = p;
        worldManager = wm;
        uiController = uiC;

        playerActive = true;

        playerUnitList = new List<GameObject>();
        playerTownList = new List<GameObject>();

        tilesExplored = new bool[worldManager.getMapDimensions()[0], worldManager.getMapDimensions()[1]];

        gold = 20;
        baseGPT = 2;
        townGPT = 0;
        mineGPT = 1;
    }

    public void CheckVision(int x, int y, int visionRange)
    {
        int xmin = Math.Max(0, x - visionRange);
        int xmax = Math.Min(tilesExplored.Length, x + visionRange);
        int ymin = Math.Max(0, y - visionRange);
        int ymax = Math.Min(tilesExplored.Length, y + visionRange);

        for (int i = xmin; i < xmax; i++)
        {
            for (int j = ymin; j < ymax; j++)
            {
                if (Math.Abs(x - i) + Math.Abs(y - j) < visionRange)
                {
                    tilesExplored[i, j] = true;
                    SetTileVisibility(i, j, tilesExplored[i, j]);
                }
            }
        }
    }

    public GameObject TownRecruit(GameObject t, GameObject unitToBuild)
    {
        TownScript townScript = t.GetComponent<TownScript>();

        int x = townScript.mapX;
        int y = townScript.mapY + 1;

        GameObject newUnit = null;

        if (worldManager.unitGrid[x, y] == null)
        {
            newUnit = townScript.BuildUnit(unitToBuild);
            if (newUnit != null)
            {
                // worldManager.unitList.Add(newUnit);
                worldManager.unitGrid[x, y] = newUnit;
                newUnit.GetComponent<UnitScript>().mapX = x;
                newUnit.GetComponent<UnitScript>().mapY = y;

                int[] screenCoords = mapToScreenCoordinates(x, y);
                newUnit.transform.position = new Vector3(screenCoords[0], screenCoords[1], -1);
            }
        }
        else
        {
            Debug.Log("Space occupied");
        }

        uiController.SetSelectedObject(t);

        return newUnit;
    }

    public void AttackTown(UnitScript attacker, TownScript target)
    {
        if (attacker.TryAttackTown(target))
        {
            target.ReceiveDamage(attacker.AttackDamage());
        }

    }

    public void AttackUnit(UnitScript attacker, UnitScript target)  // should be moved to unit script! or something
    {
        if (attacker.TryAttack(target))
        {
            target.ReceiveDamage(attacker.AttackDamage());
        }
    }

    public void StartTurn()
    {
        mainCamera.transform.position = playerCameraPosition;

        foreach (GameObject u in playerUnitList)
        {
            u.GetComponent<UnitScript>().ResetMovePoints();
        }
        foreach (GameObject t in playerTownList)
        {
            TownScript s = t.GetComponent<TownScript>();
            s.TurnStart();
            CheckVision(s.mapX, s.mapY, townVisionDistance);
        }

        AddGPT();

        ShowExplored();

        controller.OnTurnStart();
    }

    public void EndTurn()
    {
        HideAll();

        playerCameraPosition = mainCamera.transform.position;

        uiController.SetSelectedObject(null);
    }

    public int goldPerTurn()
    {
        return baseGPT + townGPT * playerTownList.Count + mineGPT * GetPlayerMineCount();
    }

    public void AddGPT()
    {
        gold += goldPerTurn();
    }

    public int[] mapToScreenCoordinates(int x, int y)
    {
        int[] mapDimensions = worldManager.GetMapDimensions();

        int a = x - mapDimensions[1] / 2;
        int b = -y + mapDimensions[0] / 2;

        return new int[] { a, b };
    }

    public GameObject addUnit(GameObject newUnit)
    {
        playerUnitList.Add(newUnit);

        return newUnit;
    }

    public GameObject addTown(GameObject newTown)
    {
        playerTownList.Add(newTown);

        return newTown;
    }

    public GameObject setMainTown(GameObject t)
    {
        mainTown = t;
        return mainTown;
    }

    public bool deleteUnit(GameObject u)
    {
        playerUnitList.Remove(u);

        return true;
    }

    public bool deleteTown(GameObject t)    // could possibly be combined with deleteUnit
    {

        playerTownList.Remove(t);

        if (playerTownList.Count <= 0)
        {
            playerActive = false;
            worldManager.CheckForWinner();
        }

        return true;
    }

    void HideAll()
    {
        GameObject[,] terrainGrid = worldManager.terrainGrid;

        for (int i = 0; i < terrainGrid.GetLength(0); i++)
        {
            for (int j = 0; j < terrainGrid.GetLength(1); j++)
            {
                SetTileVisibility(i, j, false);
            }
        }
    }
    void ShowAll()
    {
        for (int i = 0; i < worldManager.terrainGrid.GetLength(0); i++)
        {
            for (int j = 0; j < worldManager.terrainGrid.GetLength(1); j++)
            {
                SetTileVisibility(i, j, true);
            }
        }
    }

    void ShowExplored()
    {
        for (int i = 0; i < worldManager.terrainGrid.GetLength(0); i++)
        {
            for (int j = 0; j < worldManager.terrainGrid.GetLength(1); j++)
            {
                SetTileVisibility(i, j, tilesExplored[i, j]);
            }
        }
    }

    void SetTileVisibility(int x, int y, bool visible)
    {
        worldManager.terrainGrid[x, y].GetComponent<TileScript>().tileRenderer.enabled = visible;

        if (worldManager.unitGrid[x, y] != null)
        {
            // worldManager.unitGrid[x, y].GetComponent<Renderer>().enabled = visible;
            Renderer[] renderers = worldManager.unitGrid[x, y].GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = visible;
            }
        }

        if (worldManager.featureGrid[x, y] != null)
        {
            worldManager.featureGrid[x, y].GetComponent<Renderer>().enabled = visible;
        }
    }

    int GetPlayerShrineCount()
    {
        return playerObjectiveList.Count;
    }

    int GetStrengthShrineCount()
    {
        int c = 0;
        foreach (GameObject o in playerObjectiveList)
        {
            if (o.GetComponent<MapObjectiveScript>().objectiveType == 1)
            {
                c++;
            }
        }
        return c;
    }

    int GetDefenseShrineCount()
    {
        int c = 0;
        foreach (GameObject o in playerObjectiveList)
        {
            if (o.GetComponent<MapObjectiveScript>().objectiveType == 2)
            {
                c++;
            }
        }
        return c;
    }

    int GetPlayerMineCount()
    {
        int c = 0;
        foreach (GameObject o in playerObjectiveList)
        {
            if (o.GetComponent<MapObjectiveScript>().objectiveType == 0)
            {
                c++;
            }
        }
        return c;
    }

    public void ClaimShrine(GameObject s)
    {
        playerObjectiveList.Add(s);
        s.GetComponent<MapObjectiveScript>().Claim(this.playerNumber);
        Debug.Log("Shrine Claimed");
    }

    public bool RemoveShrine(GameObject s)
    {
        return playerObjectiveList.Remove(s);
    }

    public float ShrineDamageBonus()
    {
        return 1.0f + (0.1f * GetStrengthShrineCount());
    }

    public float ShrineDefenseBonus()
    {
        return 1.0f + (0.1f * GetDefenseShrineCount());
    }

    public int ShrineCount()
    {
        return playerObjectiveList.Count;
    }

    public GameObject NextUnit()
    {
        return controller.NextUnit();
    }

    public List<GameObject> UnitList()
    {
        return playerUnitList;
    }
}

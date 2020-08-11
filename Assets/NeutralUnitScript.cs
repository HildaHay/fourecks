﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NeutralUnitScript : UnitScript
{
    GameObject target;

    int chaseRange;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    bool FindTarget(UnitScript[] unitList)
    {
        GameObject newTarget = null;
        int newTargetDistance = 9999;   // might need to be increased if the map size gets big enough

        foreach(UnitScript u in unitList)
        {
            int d = u.MapDistance(u.xy(), this.xy());

            if (d <= chaseRange && d <= newTargetDistance)
            {
                newTarget = u.gameObject;
                newTargetDistance = d;
            }
        }

        target = newTarget;

        return true;
    }

    public void AutoMove()
    {
        while (movementPoints > 0) {
            // 0 = up, 1 = down, 2 = left, 3 = right

            string[] directions = { "up", "down", "left", "right" };

            int r = UnityEngine.Random.Range(0, 4);

            int[] targetSquare = this.xy();

            switch (directions[r]) {
                case "up":
                    targetSquare[1] -= 1;   // Should check if the Y-values for up/down are correct
                    break;
                case "down":
                    targetSquare[1] += 1;
                    break;
                case "left":
                    targetSquare[0] -= 1;
                    break;
                case "right":
                    targetSquare[0] += 1;
                    break;
                default:
                    this.movementPoints = 0;
                    break;
            }

            if (gameController.unitGrid[targetSquare[0], targetSquare[1]] != null)
            {

            }
            else
            {
                if(!gameController.MoveUnit(this.gameObject, targetSquare[0], targetSquare[1])){
                    this.movementPoints = 0;
                }
            }
        }

        // Debug.Log("Neutral unit moving - not implemented yet");
    }
    public void NeutralAttackUnit(UnitScript target)   // This is a mess
    {
        if (target.GetComponent<UnitScript>().GetPlayer() != -1)    // probably won't have to change unless I add multiple neutral factions or something
        {
            if (TryAttack(target))
            {
                target.ReceiveDamage(AttackDamage());
            }
        }
    }

}

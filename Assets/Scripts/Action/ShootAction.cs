using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootAction : BaseAction
{

    public event EventHandler<OnShootEventArgs> OnShoot;
    public static event EventHandler<OnShootEventArgs> OnAnyShoot;

    public class OnShootEventArgs : EventArgs
    {
        public Unit targetUnit;
        public Unit shootingUnit;
    }

    private enum State 
    {
        Aiming,
        Shooting,
        Cooloff,
    }

    [SerializeField] private LayerMask obstaclesLayerMask;
    private State state;
    private float stateTimer;
    private int maxShootDistance = 7;
    private Unit targetUnit;
    private bool canShootBullet;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Aiming:
                Vector3 aimDir = (targetUnit.GetWorldPosition() - unit.GetWorldPosition()).normalized;
                aimDir.y = 0f;
                float rotateSpeed = 10f;
                transform.forward = Vector3.Slerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                break;
            case State.Shooting:
                if (canShootBullet)
                {
                    Shoot();
                    canShootBullet = false;
                }
                break;
            case State.Cooloff:

                break;
        }
        
        if (stateTimer <= 0f)
        {
            NextState();
        }
    }

    private void NextState()
    {
        switch (state)
        {
            case State.Aiming:
                state = State.Shooting;
                float shootingStateTime = 0.1f;
                stateTimer = shootingStateTime;
                break;
            case State.Shooting:
                state = State.Cooloff;
                float coolOffStateTime = 0.5f;
                stateTimer = coolOffStateTime;
                break;
            case State.Cooloff:
                ActionComplete();
                break;
        }
    }

    private void Shoot()
    {
        OnShoot?.Invoke(this, new OnShootEventArgs {
            targetUnit = targetUnit,
            shootingUnit = unit
        });

        OnAnyShoot?.Invoke(this, new OnShootEventArgs {
            targetUnit = targetUnit,
            shootingUnit = unit
        });
        
        targetUnit.Damage(40);
    }

    public override string GetActionName()
    {
        return "Shoot";
    }

    public override List<GridPosition> GetVaildActionGridPositionList()
    {
       GridPosition unitGridPosition = unit.GetGridPosition();
       return GetVaildActionGridPositionList(unitGridPosition);

    }

    public List<GridPosition> GetVaildActionGridPositionList(GridPosition unitGridPosition)
    {
        List<GridPosition> vaildGridPositionList = new List<GridPosition>();

        for (int x = -maxShootDistance; x <= maxShootDistance; ++x)
        {
            for (int z = -maxShootDistance; z <= maxShootDistance; ++z)
            {
                for (int floor = - maxShootDistance; floor <= maxShootDistance; ++floor)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, z, floor);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsVaildGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    int testDistance = Mathf.Abs(x) + Mathf.Abs(z);

                    if (testDistance > maxShootDistance)
                    {
                        continue;
                    }

                    if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) 
                    {
                        // Grid position is empty, no unit
                        continue;
                    }

                    Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);

                    if (targetUnit.IsEnemy() == unit.IsEnemy())
                    {
                        // Both Units on same 'team'
                        continue;
                    }

                    float unitShoulderHeight = 1.7f;
                    Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition);
                    Vector3 shootDir = (targetUnit.GetWorldPosition() - unitWorldPosition).normalized;
                    if (Physics.Raycast(
                            unitWorldPosition + Vector3.up * unitShoulderHeight, 
                            shootDir,
                            Vector3.Distance(unitWorldPosition, targetUnit.GetWorldPosition()),
                            obstaclesLayerMask))
                        {
                            // Blocked by an Obstacle
                            continue;
                        }

                    vaildGridPositionList.Add(testGridPosition);
                }
            }
        }
        
        return vaildGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {

        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

        state = State.Aiming;
        float aimingStateTime = 1f;
        stateTimer = aimingStateTime;

        canShootBullet = true;

        ActionStart(onActionComplete);
    }

    public Unit GetTargetUnit()
    {
        return targetUnit;
    }

    public int GetMaxShootDistance()
    {
        return maxShootDistance;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        
        return new EnemyAIAction 
        {
            gridPosition = gridPosition,
            actionValue = 100 + Mathf.RoundToInt((1 - targetUnit.GetHealthNormalized()) * 100f),
        };
    }

    public int GetTargetCountAtPosition(GridPosition gridPosition)
    {
        return GetVaildActionGridPositionList(gridPosition).Count;
    }
}

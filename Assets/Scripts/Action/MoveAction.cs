using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    public event EventHandler<OnChangedFloorsStartedEventArgs> OnChangedFloorsStarted;
    public class OnChangedFloorsStartedEventArgs : EventArgs
    {
        public GridPosition unitGridPosition;
        public GridPosition targetGridPosition;
    }

    [SerializeField] private int maxMoveDistance = 4;

    private List<Vector3> positionList;
    private int currentPositionIndex;
    private bool isChangingFloors;
    private float differentFloorsTeleportTimer;
    private float differentFloorsTeleportTimerMax = .3f;
    
    // protected override void Awake()
    // {
    //     base.Awake();
    // }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        Vector3 targetPosition = positionList[currentPositionIndex];

        if (isChangingFloors)
        {
            // Stop and Teleport Logic
            Vector3 targetSameFloorPosition = targetPosition;
            targetSameFloorPosition.y = transform.position.y;
            Vector3 rotateDirection = (targetSameFloorPosition - transform.position).normalized;

            
            float rotateSpeed = 10f;
            transform.forward = Vector3.Slerp(transform.forward, rotateDirection, Time.deltaTime * rotateSpeed);

            differentFloorsTeleportTimer -= Time.deltaTime;
            if (differentFloorsTeleportTimer < 0f)
            {
                transform.position = targetPosition;
                isChangingFloors = false;
            }
        }
        else
        {
            // Regular move logic
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            
            float rotateSpeed = 10f;
            transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
                                    
            float moveSpeed = 4f;
            transform.position += moveDirection * Time.deltaTime * moveSpeed;
        }




        float stoppingDistance = .1f;

        if ( Vector3.Distance(targetPosition, transform.position) < stoppingDistance )
        {
            currentPositionIndex++;
            if (currentPositionIndex >= positionList.Count)
            {
                OnStopMoving?.Invoke(this, EventArgs.Empty);

                ActionComplete();
            }
            else
            {
                targetPosition = positionList[currentPositionIndex];
                GridPosition targetGirdPosition = LevelGrid.Instance.GetGridPosition(targetPosition);
                GridPosition unitGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);

                if (targetGirdPosition.floor != unitGridPosition.floor)
                {
                    // Different floors
                    isChangingFloors = true;
                    differentFloorsTeleportTimer = differentFloorsTeleportTimerMax;
                    OnChangedFloorsStarted?.Invoke(this, new OnChangedFloorsStartedEventArgs{
                        unitGridPosition = unitGridPosition,
                        targetGridPosition = targetGirdPosition,
                    });
                }
            }
        }

    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        List<GridPosition> pathGridPositionsList = Pathfinding.Instance.FindPath(unit.GetGridPosition(), gridPosition, out int pathLength);
        currentPositionIndex = 0;
        positionList = new List<Vector3>();

        foreach (GridPosition pathGridPosition in pathGridPositionsList)
        {
            positionList.Add(LevelGrid.Instance.GetWorldPosition(pathGridPosition));
        }

        OnStartMoving?.Invoke(this, EventArgs.Empty);

        ActionStart(onActionComplete);
    }

    public override List<GridPosition> GetVaildActionGridPositionList()
    {
        List<GridPosition> vaildGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = unit.GetGridPosition();
        for (int x = -maxMoveDistance; x <= maxMoveDistance; ++x)
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; ++z)
            {
                for (int floor = - maxMoveDistance; floor <= maxMoveDistance; floor++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, z, floor);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsVaildGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (unitGridPosition == testGridPosition)
                    {
                        // Same Grid where the unit is already at
                        continue;
                    }

                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) 
                    {
                        // Grid position already occupied with another unit
                        continue;
                    }

                    if (!Pathfinding.Instance.IsWalkableGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (!Pathfinding.Instance.HasPath(unitGridPosition, testGridPosition))
                    {
                        continue;
                    }

                    int pathfindingDistanceMultiplier = 10;
                    if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) > maxMoveDistance * pathfindingDistanceMultiplier)
                    {
                        // Path length is too long
                        continue;
                    }


                    vaildGridPositionList.Add(testGridPosition);
                }
            }
        }
        
        return vaildGridPositionList;
    }

    public override string GetActionName()
    {
        return "Move";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int targetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition);
        return new EnemyAIAction 
        {
            gridPosition = gridPosition,
            actionValue = targetCountAtGridPosition * 10,
        };
    }
}

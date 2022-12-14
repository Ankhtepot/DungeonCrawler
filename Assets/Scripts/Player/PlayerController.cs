using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Scripts.Building.PrefabsSpawning.Configurations;
using Scripts.Building.PrefabsSpawning.Walls;
using Scripts.Building.PrefabsSpawning.Walls.Indentificators;
using Scripts.Building.Tile;
using Scripts.EventsManagement;
using Scripts.Helpers;
using Scripts.Helpers.Extensions;
using Scripts.MapEditor.Services;
using Scripts.ScriptableObjects;
using Scripts.System;
using UnityEngine;

namespace Scripts.Player
{
    public class PlayerController : MonoBehaviour
    {
        public bool smoothTransition;
        public float transitionSpeed = 10f;
        public float wallBashSpeed = 1f;
        public float wallBashSpeedReturnMultiplier = 1.5f;
        public float transitionRotationSpeed = 500f;

        [SerializeField] private Camera playerCamera;

        public static float TransitionRotationSpeed { get; private set; }

        private Vector3 _targetPosition;
        private Vector3 _prevTargetPosition;
        private Vector3 _targetRotation;
        private Vector3 _lastBashDirection;

        private List<Waypoint> _waypoints;

        private bool _isStartPositionSet;
        private bool _isBashingIntoWall;
        private bool _atRest = true;

        private float _defaultMoveSpeed;
        private float _defaultRotationSpeed;

        private void Awake()
        {
            _defaultMoveSpeed = transitionSpeed;
            _defaultRotationSpeed = transitionRotationSpeed;
            _waypoints = new List<Waypoint>();
        }

        public void SetPositionAndRotation(Vector3 gridPosition, Quaternion rotation)
        {
            Transform playerTransform = transform;
            playerTransform.position = _targetPosition = _prevTargetPosition = new Vector3(gridPosition.y, -gridPosition.x, gridPosition.z);
            playerTransform.rotation = rotation;
            _targetRotation = rotation.eulerAngles;
            _isStartPositionSet = true;
            _waypoints.Clear();

            StartCoroutine(GroundCheckCoroutine(true));
        }

        public void RotateLeft() => SetMovement(() => _targetRotation -= Vector3.up * 90f);
        public void RotateRight() => SetMovement(() => _targetRotation += Vector3.up * 90f);
        public void MoveForward() => SetMovement(() => _targetPosition += transform.forward);
        public void MoveBackwards() => SetMovement(() => _targetPosition -= transform.forward);
        public void MoveLeft() => SetMovement(() => _targetPosition -= transform.right);
        public void MoveRight() => SetMovement(() => _targetPosition += transform.right);

        private void SetMovement(Action movementSetter)
        {
            if (!_isStartPositionSet || !GameManager.Instance.MovementEnabled || !_atRest) return;

            _targetPosition = _targetPosition.ToVector3Int();
            _prevTargetPosition = _prevTargetPosition.ToVector3Int();
            transform.position = transform.position.ToVector3Int();
            
            movementSetter?.Invoke();

            if (IsTargetPositionValid() && !_waypoints.Any())
            {
                _atRest = false;

                _prevTargetPosition = _targetPosition;
                StartCoroutine(PerformMovementCoroutine());
                return;
            }

            if (_waypoints.Any())
            {
                _atRest = false;
                
                StartCoroutine(PerformWaypointMovementCoroutine());
                return;
            }

            if (!_isBashingIntoWall && (_targetPosition != _prevTargetPosition))
            {
                _atRest = false;
                StartCoroutine(BashIntoWallCoroutine());
                return;
            }

            _targetPosition = _prevTargetPosition;
        }

        private IEnumerator PerformWaypointMovementCoroutine()
        {
            if (!_waypoints.Any()) yield break;
            bool isLadderDown = false;
            
            for (int index = 1; index < _waypoints.Count; index++)
            {
                Waypoint waypoint = _waypoints[index];

                _targetPosition = waypoint.position;
                if (IsWaypointStraightUp(index, out Vector3 rot))
                {
                    _targetRotation = rot;
                }
                else if (index == 1 && IsLadderDownStart(out rot))
                {
                    isLadderDown = true;
                    _targetRotation = rot;
                }
                else if (index == 1)
                {
                    _targetRotation = Quaternion.LookRotation(waypoint.position - _waypoints[0].position, Vector3.up).eulerAngles;
                }
                else if (index != _waypoints.Count - 1)
                {
                    _targetRotation = isLadderDown 
                        ? transform.rotation.eulerAngles
                        :Quaternion.LookRotation(_waypoints[index + 1].position - transform.position, Vector3.up).eulerAngles;

                    isLadderDown = false;
                }
                else if (index == _waypoints.Count - 1)
                {
                    _targetRotation = isLadderDown 
                        ? transform.rotation.eulerAngles
                        :Quaternion.LookRotation(_waypoints[^1].position - _waypoints[^2].position, Vector3.up).eulerAngles;

                    isLadderDown = false;
                }
                
                AdjustTransitionSpeeds(_waypoints[index].moveSpeedModifier);

                yield return PerformMovementCoroutine(false, false);

                AdjustTransitionSpeeds();
            }

            Transform playerTransform = transform;
            Vector3 rotation = playerTransform.rotation.eulerAngles;
            
            _targetRotation = new Vector3(0, rotation.y, 0);
            
            StartCoroutine(PerformMovementCoroutine());
        }

        private bool IsWaypointStraightUp(int index, out Vector3 rotation)
        {
            if (index == _waypoints.Count - 1)
            {
                Vector3 currentRotation = transform.rotation.eulerAngles;
                currentRotation.x = 0;
                rotation = currentRotation;
                return false;
            }
            Vector3 vector =  (_waypoints[index].position.Round(1) - _waypoints[index - 1].position.Round(1)).normalized ;
            bool isUp = vector == Vector3.up;
            
            if (isUp)
            {
                Vector3 bashDirection = _lastBashDirection.normalized;
                if (!V3Extensions.DirectionRotationMap.ContainsKey(bashDirection))
                {
                    rotation = Vector3.zero;
                    return false;
                }
                rotation = V3Extensions.DirectionRotationMap[bashDirection];
                return true;
            }

            rotation = transform.rotation.eulerAngles;
            return false;
        }

        private bool IsLadderDownStart(out Vector3 rotation)
        {
            bool straightDownFromSecondWaypoint = WayPointService.IsLadderDownAtPathStart(_waypoints);

            if (!straightDownFromSecondWaypoint)
            {
                rotation = Vector3.zero;
                return false;
            }
            
            rotation = (_waypoints[0].position.Round(1) - _waypoints[1].position.Round(1)).normalized;

            if (!V3Extensions.DirectionRotationMap.ContainsKey(rotation))
            {
                rotation = Vector3.zero;
                return false;
            }

            rotation = V3Extensions.DirectionRotationMap[rotation];
            return true;
        }

        private void AdjustTransitionSpeeds(float? modifier = null)
        {
            transitionSpeed = modifier == null ? _defaultMoveSpeed : _defaultMoveSpeed * (float) modifier;
            TransitionRotationSpeed = modifier == null ? _defaultRotationSpeed : _defaultRotationSpeed * (float) modifier;
        }

        private IEnumerator PerformMovementCoroutine(bool isRestingOnFinish = true, bool doGroundCheck = true)
        {
            Transform myTransform = transform;
            float currentRotY = myTransform.eulerAngles.y;
            Vector3 currentPosition = myTransform.position;

            while (NotAtTargetPosition(_targetPosition) || NotAtTargetRotation())
            {
                Vector3 targetPosition = _targetPosition;

                if (_targetRotation.y is > 270f and < 361f) _targetRotation.y = 0f;
                if (_targetRotation.y < 0f) _targetRotation.y = 270f;
                if (_targetRotation.x is > 330f and < 361f) _targetRotation.x = 0f;
                if (_targetRotation.x < 0f) _targetRotation.x = 270f;

                if (!smoothTransition)
                {
                    transform.position = targetPosition;
                    transform.rotation = Quaternion.Euler(_targetRotation);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(myTransform.position, targetPosition, Time.deltaTime * transitionSpeed);
                    transform.rotation = Quaternion.RotateTowards(myTransform.rotation, Quaternion.Euler(_targetRotation),
                        Time.deltaTime * transitionRotationSpeed);
                }

                yield return null;
            }

            if (doGroundCheck)
            {
                yield return GroundCheckCoroutine();
            }

            _atRest = isRestingOnFinish;
            if (isRestingOnFinish)
            {
                _waypoints.Clear();
            }

            _prevTargetPosition = _targetPosition;

            if (Math.Abs(currentRotY - transform.rotation.eulerAngles.y) > float.Epsilon)
            {
                TransitionRotationSpeed = transitionRotationSpeed;
                EventsManager.TriggerOnPlayerRotationChanged(_targetRotation);
            }

            if (NotAtTargetPosition(currentPosition))
            {
                EventsManager.TriggerOnPlayerPositionChanged(transform.position);
            }
        }

        private IEnumerator GroundCheckCoroutine(bool setAtRestAtTheEnd = false)
        {
            while (!GroundCheck())
            {
                _atRest = false;

                Vector3 targetPosition = transform.position + Vector3.down;

                while (NotAtTargetPosition(targetPosition))
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * transitionSpeed);
                    yield return null;
                }

                Transform ownTransform = transform;
                ownTransform.position = _targetPosition = _prevTargetPosition = transform.position.ToVector3Int();

                yield return null;
            }

            if (setAtRestAtTheEnd) _atRest = true;
        }

        private IEnumerator BashIntoWallCoroutine()
        {
            Vector3 position = transform.position;
            Vector3 targetPosition = position + ((_targetPosition - position) * 0.2f);

            while (NotAtTargetPosition(targetPosition))
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition,
                    Time.deltaTime * wallBashSpeed);
                yield return null;
            }

            while (NotAtTargetPosition(_prevTargetPosition))
            {
                transform.position = Vector3.MoveTowards(transform.position, _prevTargetPosition,
                    Time.deltaTime * wallBashSpeed * wallBashSpeedReturnMultiplier);
                yield return null;
            }

            Vector3Int newPosition = Vector3Int.RoundToInt(transform.position).SwapXY();
            newPosition.x = 0 - newPosition.x;

            SetPositionAndRotation(newPosition, Quaternion.Euler(_targetRotation));
            _isBashingIntoWall = false;
            _atRest = true;
        }

        private bool IsTargetPositionValid()
        {
            Vector3Int gridPosition = _targetPosition.ToGridPosition();

            return !IsMidWallInTargetDirection() && IsTargetTileEligibleToMoveOn(gridPosition);
        }

        private static bool IsTargetTileEligibleToMoveOn(Vector3Int griPosition)
        {
            TileDescription[,,] layout = GameManager.Instance.CurrentMap.Layout;
            bool isTargetTileOnSameLevelEligible = layout.ByGridV3Int(griPosition) is {IsForMovement: true};

            return isTargetTileOnSameLevelEligible /*&& tileBellowIsEligible*/;
        }

        private bool IsMidWallInTargetDirection()
        {
            if (_targetPosition == _prevTargetPosition) return false;

            Vector3 currentPosition = transform.position;
            _lastBashDirection = (_targetPosition.ToVector3Int() - currentPosition.ToVector3Int());
            Ray ray = new(currentPosition, _lastBashDirection);
            RaycastHit[] hits = new RaycastHit[5];
            int size = Physics.RaycastNonAlloc(ray, hits, 0.7f, LayerMask.GetMask(LayersManager.WallMaskName));

            if (size == 0) return false;

            foreach (RaycastHit hit in hits)
            {
                if (!hit.collider) continue;

                WallPrefabBase wallScript = hit.collider.gameObject.GetComponent<WallPrefabBase>();

                if (!wallScript) continue;

                if (wallScript is IObstacle) return true;

                if (wallScript is IMovementWall)
                {
                    Transform hitTransform = hit.transform;
                    _waypoints = new List<Waypoint>(
                        (GameManager.Instance.MapBuilder
                            .GetPrefabConfigurationByTransformData(
                                new PositionRotation(hitTransform.position, hitTransform.rotation)) as WallConfiguration)?
                        .WayPoints ?? new List<Waypoint>());

                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if bellow player position is collider with wall tag and if not, returns true (as ground is there - check).
        /// </summary>
        /// <returns>False means player will fall</returns>
        private bool GroundCheck()
        {
            Vector3 currentPosition = transform.position;
            Vector3Int bellowGridPosition = currentPosition.ToGridPosition();
            bellowGridPosition.x += 1;

            if (!IsTargetTileEligibleToMoveOn(bellowGridPosition)) return true;

            Ray ray = new(currentPosition, Vector3.down);
            return Physics.Raycast(ray, 0.7f, LayerMask.GetMask(LayersManager.WallMaskName));
        }

        public void SetCamera()
        {
            CameraManager.Instance.SetMainCamera(playerCamera);
        }

        private bool NotAtTargetPosition(Vector3 targetPosition) => Vector3.Distance(transform.position, targetPosition) > 0.05f;

        private bool NotAtTargetRotation() => Vector3.Distance(transform.eulerAngles, _targetRotation) > 0.05;
    }
}
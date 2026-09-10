using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using BattleshipRoguelike.Enemy;
using BattleshipRoguelike.Grid;
using BattleshipRoguelike.Map;
using BattleshipRoguelike.Ships;
using BattleshipRoguelike.Upgrades;

namespace BattleshipRoguelike.Game
{
    public class GameManager : MonoBehaviour
    {
        private enum BattleState
        {
            PreStart,
            PlayerAiming,
            WaitingForGo,
            EnemyFiring,
            GameOver
        }

        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonLabel;
        [SerializeField] private BattleSummaryPanel summaryPanel;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private EnemyController enemyPrefabFallback;
        [SerializeField] private Vector3 enemyBoardOffset = new Vector3(9f, 0f, 0f);
        [SerializeField] private float panDuration = 0.3f;
        [SerializeField] private Color hitColor = Color.yellow;
        [SerializeField] private Color missColor = Color.white;
        [SerializeField] private Color sunkColor = Color.red;
        [SerializeField] private float fireResolveDelay = 0.5f;

        private EnemyController enemy;
        private List<ShipController> playerShips;
        private BattleState state = BattleState.PreStart;
        private int shotsRemaining;
        private int shotsFiredThisBattle;
        private bool isResolvingShot;
        private Vector3 playerCameraPosition;
        private Vector3 enemyCameraPosition;

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = Camera.main;
            }
        }

        private void Start()
        {
            playerShips = FindObjectsByType<ShipController>(FindObjectsInactive.Exclude)
                .Where(s => s.GetComponentInParent<EnemyController>() == null)
                .ToList();

            EnemyController enemyPrefab = ResolveEnemyPrefab();
            enemy = Instantiate(enemyPrefab);

            // Boards must clear each other's camera framing at any aspect ratio, not just the one
            // this scene happens to be authored at, so the required separation is derived from the
            // camera's actual view width rather than trusting the serialized offset alone.
            float halfViewWidth = boardCamera.orthographicSize * boardCamera.aspect;
            float panelWidth = (GridManager.Instance.Width + 1) * GridManager.Instance.CellSize;
            float minOffsetX = panelWidth + halfViewWidth + 1f;
            if (enemyBoardOffset.x < minOffsetX)
            {
                enemyBoardOffset.x = minOffsetX;
            }

            enemy.transform.position = GridManager.Instance.transform.position + enemyBoardOffset;

            playerCameraPosition = boardCamera.transform.position;
            enemyCameraPosition = playerCameraPosition + enemyBoardOffset;

            actionButton.onClick.AddListener(OnActionButtonClicked);
            ShowButton("Start");
        }

        private EnemyController ResolveEnemyPrefab()
        {
            if (RunManager.Instance != null)
            {
                GameObject prefab = RunManager.Instance.GetEnemyPrefabForZone(RunManager.Instance.CurrentNodeId);
                if (prefab != null)
                {
                    EnemyController controller = prefab.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        return controller;
                    }
                }
            }

            return enemyPrefabFallback;
        }

        private void OnActionButtonClicked()
        {
            switch (state)
            {
                case BattleState.PreStart:
                    TryBeginBattle();
                    break;
                case BattleState.WaitingForGo:
                    BeginEnemyTurn();
                    break;
            }
        }

        private void TryBeginBattle()
        {
            if (!playerShips.Any(s => s.IsPlaced))
            {
                Debug.Log("Cannot start: place at least one ship on the board.");
                return;
            }

            foreach (ShipController ship in playerShips)
            {
                ship.LockForRoundStart();
            }

            HideButton();
            StartCoroutine(PanThenBeginPlayerTurn());
        }

        private IEnumerator PanThenBeginPlayerTurn()
        {
            yield return PanCamera(enemyCameraPosition);
            state = BattleState.PlayerAiming;
            int extraShots = RunManager.Instance != null ? RunManager.Instance.GetUpgradeCount(UpgradeId.DoubleShot) : 0;
            shotsRemaining = 1 + extraShots;
        }

        private void BeginEnemyTurn()
        {
            HideButton();
            state = BattleState.EnemyFiring;
            StartCoroutine(EnemyFireSequence());
        }

        private void Update()
        {
            if (state != BattleState.PlayerAiming || isResolvingShot || shotsRemaining <= 0 || Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = boardCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -boardCamera.transform.position.z));

            GridManager enemyBoard = enemy.Board;
            if (enemyBoard == null || !enemyBoard.TryGetNearestCell(worldPos, out int gx, out int gy))
            {
                return;
            }

            GridCell cell = enemyBoard.GetCell(gx, gy);
            if (cell == null || cell.fired)
            {
                return;
            }

            cell.fired = true;
            isResolvingShot = true;
            shotsFiredThisBattle++;
            StartCoroutine(PlayerFireSequence(cell));
        }

        private IEnumerator PlayerFireSequence(GridCell cell)
        {
            yield return ResolveFire(cell);

            if (enemy.AllSunk)
            {
                state = BattleState.GameOver;
                ShowBattleSummaryAndProceed(true);
                yield break;
            }

            shotsRemaining--;
            if (shotsRemaining > 0)
            {
                isResolvingShot = false;
                yield break;
            }

            yield return PanCamera(playerCameraPosition);
            ShowButton("Go");
            state = BattleState.WaitingForGo;
            isResolvingShot = false;
        }

        private IEnumerator EnemyFireSequence()
        {
            if (enemy.TryGetNextTarget(GridManager.Instance, out int gx, out int gy))
            {
                GridCell cell = GridManager.Instance.GetCell(gx, gy);
                if (cell != null)
                {
                    cell.fired = true;
                    yield return ResolveFire(cell);
                }
            }

            if (RunManager.Instance != null)
            {
                foreach (ShipController ship in playerShips)
                {
                    if (ship.IsSunk && ship.SourcePrefab != null)
                    {
                        RunManager.Instance.RemoveShip(ship.SourcePrefab);
                    }
                }
            }

            if (playerShips.All(s => s.IsSunk))
            {
                state = BattleState.GameOver;
                ShowBattleSummaryAndProceed(false);
                yield break;
            }

            yield return PanCamera(enemyCameraPosition);
            state = BattleState.PlayerAiming;
            int extraShots = RunManager.Instance != null ? RunManager.Instance.GetUpgradeCount(UpgradeId.DoubleShot) : 0;
            shotsRemaining = 1 + extraShots;
        }

        private void ShowBattleSummaryAndProceed(bool won)
        {
            List<ShipController> enemyKills = enemy.Ships.Where(s => s.IsSunk).ToList();
            List<ShipController> playerLosses = playerShips.Where(s => s.IsSunk).ToList();

            if (RunManager.Instance != null && summaryPanel != null)
            {
                List<ProfitLineItem> breakdown = RunManager.Instance.ApplyBattleProfit();
                int total = breakdown.Sum(item => item.amount);
                summaryPanel.Show(won, breakdown, total, enemyKills, playerLosses, shotsFiredThisBattle,
                    () => RunManager.Instance.CompleteCurrentZone());
            }
            else
            {
                Debug.Log(won ? "You Win" : "Game Over");
            }
        }

        private IEnumerator PanCamera(Vector3 to)
        {
            Vector3 from = boardCamera.transform.position;
            float elapsed = 0f;
            while (elapsed < panDuration)
            {
                elapsed += Time.deltaTime;
                boardCamera.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / panDuration));
                yield return null;
            }

            boardCamera.transform.position = to;
        }

        private void ShowButton(string label)
        {
            actionButtonLabel.text = label;
            actionButton.gameObject.SetActive(true);
        }

        private void HideButton()
        {
            actionButton.gameObject.SetActive(false);
        }

        private IEnumerator ResolveFire(GridCell cell)
        {
            yield return new WaitForSeconds(fireResolveDelay / 2);

            if (cell.occupied && cell.occupant != null)
            {
                cell.ShowMark("X", hitColor);
                ShipController ship = cell.occupant.GetComponentInParent<ShipController>();
                if (ship != null)
                {
                    ship.NotifyHitAndCheckSunk(cell.occupant, sunkColor);
                }
            }
            else
            {
                cell.ShowMark("O", missColor);
            }

            yield return new WaitForSeconds(fireResolveDelay / 2);
        }
    }
}

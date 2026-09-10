using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleshipRoguelike.Map
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private Camera mapCamera;
        [SerializeField] private ZoneNode zoneNodePrefab;
        [SerializeField] private float nodeSpacing = 2f;
        [SerializeField] private float laneSpacing = 1.2f;
        [SerializeField] private float mapY = 0f;
        [SerializeField] private Vector2 origin = Vector2.zero;
        [SerializeField] private float scrollSpeed = 5f;

        private readonly Dictionary<ZoneNodeId, ZoneNode> instantiatedNodes = new Dictionary<ZoneNodeId, ZoneNode>();
        private ZoneNodeId? selectedNodeId;
        private float cameraZ;
        private float minCameraX;
        private float maxCameraX;

        private void Awake()
        {
            if (mapCamera == null)
            {
                mapCamera = Camera.main;
            }

            cameraZ = mapCamera.transform.position.z;
        }

        private void Start()
        {
            RunManager.Instance.EnsureInitialized();
            BuildCurrentStretch();
        }

        private void BuildCurrentStretch()
        {
            ZoneNodeId currentId = RunManager.Instance.CurrentNodeId;
            int stretchLength = RunManager.Instance.StretchLength;
            int stretchStart = currentId.depth < 0 ? 0 : (currentId.depth / stretchLength) * stretchLength;

            var graphNodes = RunManager.Instance.GetStretchNodes(stretchStart).ToList();

            instantiatedNodes.Clear();
            foreach (ZoneGraphNode gn in graphNodes)
            {
                ZoneNode node = Instantiate(zoneNodePrefab, transform);
                node.name = $"Zone_{gn.Id}";
                int localDepth = gn.Id.depth - stretchStart;
                node.transform.position = NodePosition(localDepth, gn.Id.lane);
                node.Initialize(gn.Id, gn.Data.type);
                instantiatedNodes[gn.Id] = node;
            }

            var reachable = new HashSet<ZoneNodeId>(RunManager.Instance.GetChildren(currentId)) { currentId };
            foreach (KeyValuePair<ZoneNodeId, ZoneNode> entry in instantiatedNodes)
            {
                entry.Value.SetDimmed(!reachable.Contains(entry.Key));
                entry.Value.SetCurrent(entry.Key == currentId);
            }

            foreach (ZoneGraphNode gn in graphNodes)
            {
                foreach (ZoneNodeId childId in gn.Children)
                {
                    if (!instantiatedNodes.TryGetValue(childId, out ZoneNode childNode))
                    {
                        continue;
                    }

                    GameObject connectorObject = new GameObject($"Connector_{gn.Id}_{childId}");
                    connectorObject.transform.SetParent(transform, false);
                    MapConnector connector = connectorObject.AddComponent<MapConnector>();
                    connector.Draw(instantiatedNodes[gn.Id].transform.position, childNode.transform.position);
                }
            }

            minCameraX = origin.x;
            maxCameraX = origin.x + stretchLength * nodeSpacing;
            int currentLocalDepth = currentId.depth - stretchStart;
            float startX = Mathf.Clamp(origin.x + currentLocalDepth * nodeSpacing, minCameraX, maxCameraX);
            mapCamera.transform.position = new Vector3(startX, origin.y + mapY, cameraZ);
        }

        private Vector3 NodePosition(int localDepth, int lane)
        {
            return new Vector3(origin.x + localDepth * nodeSpacing, origin.y + mapY + lane * laneSpacing, 0f);
        }

        private void Update()
        {
            HandleScroll();
            HandleClick();
        }

        private void HandleScroll()
        {
            if (Mouse.current == null)
            {
                return;
            }

            float delta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            Vector3 pos = mapCamera.transform.position;
            pos.x = Mathf.Clamp(pos.x + Mathf.Sign(delta) * scrollSpeed, minCameraX, maxCameraX);
            mapCamera.transform.position = pos;
        }

        private void HandleClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mapCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cameraZ));

            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null)
            {
                return;
            }

            ZoneNode node = hit.GetComponent<ZoneNode>();
            if (node == null)
            {
                return;
            }

            IReadOnlyList<ZoneNodeId> validChildren = RunManager.Instance.GetChildren(RunManager.Instance.CurrentNodeId);
            if (!validChildren.Contains(node.Id))
            {
                return;
            }

            if (selectedNodeId.HasValue && selectedNodeId.Value.Equals(node.Id))
            {
                RunManager.Instance.EnterZone(node.Id);
            }
            else
            {
                if (selectedNodeId.HasValue && instantiatedNodes.TryGetValue(selectedNodeId.Value, out ZoneNode previous))
                {
                    previous.SetSelected(false);
                }

                selectedNodeId = node.Id;
                node.SetSelected(true);
            }
        }
    }
}

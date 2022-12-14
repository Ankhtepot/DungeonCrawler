﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Scripts.Building.Tile;
using Scripts.Building.Walls.Configurations;
using Scripts.Helpers;
using Scripts.System;
using Scripts.System.Pooling;
using UnityEngine;
using LayoutType = System.Collections.Generic.List<System.Collections.Generic.List<Scripts.Building.Tile.TileDescription>>;
using Logger = Scripts.Helpers.Logger;

namespace Scripts.Building
{
    public class MapBuilder : MonoBehaviour
    {
        public DefaultBuildPartsProvider defaultsProvider;
        [SerializeField] private GameObject levelPartsParent;

        private TileBuilderBase _playBuilder;
        private TileBuilderBase _editorBuilder;

        public event Action OnLayoutBuilt;

        internal Transform LayoutParent;
        internal GameObject PrefabsParent;
        internal TileDescription[,,] Layout;
        internal Dictionary<Vector3Int, GameObject> PhysicalTiles;
        internal HashSet<GameObject> Prefabs;
        internal MapDescription MapDescription;

        private void Awake()
        {
            PhysicalTiles = new Dictionary<Vector3Int, GameObject>();
            Prefabs = new HashSet<GameObject>();

            if (!LayoutParent)
            {
                LayoutParent = new GameObject("Layout").transform;
                LayoutParent.transform.parent = levelPartsParent.transform;

                PrefabsParent = new GameObject("Prefabs")
                {
                    transform =
                    {
                        parent = levelPartsParent.transform
                    }
                };
            }
        }

        public void BuildMap(MapDescription mapDescription)
        {
            DemolishMap();

            MapDescription = mapDescription;

            StartCoroutine(BuildLayoutCoroutine(mapDescription.Layout));
            StartCoroutine(BuildPrefabsCoroutine(mapDescription.PrefabConfigurations));
        }

        public void SetLayout(TileDescription[,,] layout) => Layout = layout;

        public void DemolishMap()
        {
            foreach (GameObject tile in PhysicalTiles.Values)
            {
                ObjectPool.Instance.ReturnToPool(tile);
            }
            
            foreach (GameObject prefab in Prefabs)
            {
                ObjectPool.Instance.ReturnToPool(prefab);
            }

            PhysicalTiles.Clear();
            Prefabs.Clear();
        }

        /// <summary>
        /// Build a new tile where was previously null tile. Or null tile where was previously a tile.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="floor"></param>
        public void RebuildTile(int floor, int row, int column)
        {
            if (GameManager.Instance.GameMode is GameManager.EGameMode.Play)
            {
                _playBuilder.BuildTile(floor, row, column);
            }
            else
            {
                // Logger.Log($"Rebuilding tile: {floor},{row},{column}");
                _editorBuilder.BuildTile(floor, row, column);
            }
        }

        public void RegenerateTilesAround(int floor, int row, int column)
        {
            foreach (Vector3Int direction in TileDirections.VectorDirections)
            {
                if (Layout[floor + direction.y, row + direction.x, column + direction.z] != null)
                {
                    RegenerateTile(floor + direction.y, row + direction.x, column + direction.z);
                }
            }
        }

        public static MapDescription GenerateDefaultMap(int floors, int rows, int columns)
        {
            TileDescription[,,] layout = new TileDescription[floors, rows, columns];

            Vector3Int center = new(floors / 2, rows / 2, columns / 2);

            layout = AddTilesToCenterOfLayout(layout);

            return new MapDescription
            {
                Layout = layout,
                StartGridPosition = center,
            };
        }

        public GameObject GetPhysicalTileByGridPosition(int floor, int row, int column)
        {
            Vector3Int worldPosition = new(row, -floor, column);

            return PhysicalTiles[worldPosition];
        }

        private IEnumerator BuildLayoutCoroutine(TileDescription[,,] layout)
        {
            Layout = layout;

            _playBuilder = new PlayModeBuilder(this);
            _editorBuilder = new EditorModeBuilder(this);

            for (int floor = 0; floor < layout.GetLength(0); floor++)
            {
                for (int row = 0; row < layout.GetLength(1); row++)
                {
                    for (int column = 0; column < layout.GetLength(2); column++)
                    {
                        if (GameManager.Instance.GameMode is GameManager.EGameMode.Play)
                        {
                            _playBuilder.BuildTile(floor, row, column);
                        }
                        else
                        {
                            _editorBuilder.BuildTile(floor, row, column);
                        }

                        yield return null;
                    }
                }
            }

            OnLayoutBuilt?.Invoke();
        }
        
        private IEnumerator BuildPrefabsCoroutine(List<PrefabConfiguration> configurations)
        {
            foreach (PrefabConfiguration configuration in configurations)
            {
                BuildPrefab(configuration);

                yield return null;
            }
        }

        /// <summary>
        /// Works over physical tile, shows or hides walls after assumed changed layout. 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="floor"></param>
        private void RegenerateTile(int floor, int row, int column)
        {
            Vector3Int worldKey = new(row, -floor, column);

            TileController tileController = PhysicalTiles[worldKey].GetComponent<TileController>();

            if (!tileController)
            {
                return;
            }

            foreach (Vector3Int direction in TileDirections.VectorDirections)
            {
                if (Layout[floor + direction.y, row + direction.x, column + direction.z] == null)
                    tileController.ShowWall(TileDirections.WallDirectionByVector[direction]);
                else
                    tileController.HideWall(TileDirections.WallDirectionByVector[direction]);
            }
        }

        private static TileDescription[,,] AddTilesToCenterOfLayout(TileDescription[,,] layout)
        {
            Vector2Int center = new(layout.GetLength(1) / 2, layout.GetLength(2) / 2);
            int floor = layout.GetLength(0) / 2;

            layout[floor, center.x - 1, center.y - 1] = DefaultMapProvider.FullTile;
            layout[floor, center.x - 1, center.y + 1] = DefaultMapProvider.FullTile;
            layout[floor, center.x - 1, center.y] = DefaultMapProvider.FullTile;
            layout[floor, center.x, center.y - 1] = DefaultMapProvider.FullTile;
            layout[floor, center.x, center.y] = DefaultMapProvider.FullTile;
            layout[floor, center.x, center.y + 1] = DefaultMapProvider.FullTile;
            layout[floor, center.x + 1, center.y - 1] = DefaultMapProvider.FullTile;
            layout[floor, center.x + 1, center.y] = DefaultMapProvider.FullTile;
            layout[floor, center.x + 1, center.y + 1] = DefaultMapProvider.FullTile;

            return layout;
        }

        /// <summary>
        /// Builds new prefab and both stores configuration in MapDescription and GameObject in Prefabs list.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public bool BuildPrefab(PrefabConfiguration configuration)
        {
            GameObject newPrefab = PrefabStore.Instantiate(configuration.PrefabName, PrefabsParent);

            if (!newPrefab)
            {
                Logger.LogError($"Prefab \"{configuration.PrefabName}\" was not found.");
                return false;
            }

            newPrefab.transform.position = configuration.TransformData.Position;
            newPrefab.transform.localRotation = configuration.TransformData.Rotation;

            if (configuration is WallConfiguration wallConfiguration)
            {
                Vector3 position = newPrefab.transform.position;
                position.y += wallConfiguration.Offset;
                newPrefab.transform.position = position;
            }

            if (!MapDescription.PrefabConfigurations.Contains(configuration))
            {
                MapDescription.PrefabConfigurations.Add(configuration);
            }

            Prefabs.Add(newPrefab);

            return true;
        }

        public void RemovePrefab(PrefabConfiguration configuration)
        {
            PrefabConfiguration config = MapDescription.PrefabConfigurations.FirstOrDefault(c =>
                c.PrefabName == configuration.PrefabName && c.TransformData.Position == configuration.TransformData.Position);

            if (config == null)
            {
                Logger.LogWarning($"No prefab of name \"{configuration.PrefabName}\" was found for removal in PrefabConfigurations.");
                return;
            }

            MapDescription.PrefabConfigurations.Remove(config);

            GameObject prefabGo = Prefabs.FirstOrDefault(go =>
                go.name == configuration.PrefabName && go.transform.position == configuration.TransformData.Position);
            
            if (!prefabGo)
            {
                Logger.LogWarning($"No prefab of name \"{configuration.PrefabName}\" found for removal in Prefabs.");
                return;
            }

            Prefabs.Remove(prefabGo);
            ObjectPool.Instance.ReturnToPool(prefabGo);
        }
    }
}
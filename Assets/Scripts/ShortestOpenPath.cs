using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ShortestOpenPath_Algorithm
{
    public class ShortestOpenPath : MonoBehaviour
    {
        // represents a sigle point in "forest"
        [Serializable]
        public class Tree
        {
            public int treeID;
            public List<double> coordinates;

            public Tree()
            {
                coordinates = new List<double>();
            }
        }

        [Serializable]
        public class Edge
        {
            public List<Tree> points;
            public List<int> indexes;
            public Edge(Tree a, Tree b)
            {
                points = new List<Tree> { a, b };
                indexes = new List<int> { a.treeID, b.treeID };
            }

            public override string ToString()
            {
                return "Indexes: { " + indexes[0] + " , " + indexes[1] + " }";
            }
        }

        [SerializeField]
        private Transform treeRoot;
        [SerializeField]
        private Transform connectionRoot;

        [SerializeField]
        private List<GameObject> drawForest;
        [SerializeField]
        private List<Tree> trees;
        [SerializeField]
        private List<GameObject> connectionRenderers;

        [SerializeField]
        private GameObject treePrefab;
        [SerializeField]
        private GameObject connectionPrefab;
        [SerializeField]
        private List<Edge> edges;

        private bool isCycle;

        private void Start()
        {
            isCycle = false;
            drawForest = new List<GameObject>();
        }

        public void CalculationScenario()
        {
            // read file and save data as separate items
            trees = ReadCsv_("D:/Programming/MachineLearning/Yefimov/testing_1.csv", true, 1);
            // spawn trees in 3d space with 2D coordinates
            SpawnTrees(trees);

            // setting connection with nearest neighbour
            for (int i = 0; i < drawForest.Count; i++)
                SetNearestNeighbour(trees[i], trees);
            print("Edges: " + edges.Count);

            List<Edge> connected = new List<Edge>(edges);
            //for (int i = 0; i < trees.Count; i++)
            /*for (int i = 0; i < trees.Count; i++)
                DoConnection(trees[i].treeID, connected);*/
            /*DoConnection(trees[0].treeID);
            DoConnection(trees[5].treeID);*/
            DoConnection(trees[6].treeID, connected);
            print("### Edges::"+ edges.Count);
            DoConnection(trees[9].treeID, connected);
            print("### Edges::" + edges.Count);
            //DoConnection(trees[9].treeID, connected);
            //DoConnection(trees[0].treeID);
            //print("Edges: " + edges.Count);
            DrawEdges();
        }

        private void DrawEdges()
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].points[0].treeID == edges[i].points[1].treeID)
                    continue;

                GameObject connection = Instantiate(connectionPrefab, connectionRoot);
                connection.GetComponent<LineRenderer>().positionCount = 2;
                connection.GetComponent<LineRenderer>().SetPosition(0, drawForest[edges[i].points[0].treeID].transform.position);
                connection.GetComponent<LineRenderer>().SetPosition(1, drawForest[edges[i].points[1].treeID].transform.position);
                connectionRenderers.Add(connection);
            }
        }

        private void SpawnTrees(List<Tree> _forest)
        {
            for (int i = 0; i < _forest.Count; i++)
            {
                _forest[i].treeID = i;
                GameObject _tree = Instantiate(treePrefab, treeRoot);
                _tree.transform.localPosition = new Vector3((float)_forest[i].coordinates[0] * 0.1f, (float)_forest[i].coordinates[1] * 0.1f);
                drawForest.Add(_tree);
            }
        }

        private List<Tree> ReadCsv_(string filepath, bool skipFirst, int classColumnId)
        {
            int currentPos = 0;

            List<Tree> tokens = new List<Tree>();
            using (var reader = new StreamReader(filepath))
            {
                while (!reader.EndOfStream)
                {
                    if (skipFirst)
                    {
                        reader.ReadLine();
                        currentPos++;
                        skipFirst = false;
                        continue;
                    }

                    Tree token = new Tree();
                    int i = 0;
                    string line = reader.ReadLine();
                    if (line == null)
                        break;
                    line = line.Replace('"', ' ');
                    currentPos++;

                    foreach (var item in line.Split(','))
                    {
                        i++;
                        if (i == classColumnId)
                            continue;

                        if (!Double.TryParse(item.Replace('.', ','), out double _item))
                        {
                            Console.WriteLine("Something went wrong. [" + item + "]");
                            continue;
                        }
                        if (token.coordinates.Count >= 2)
                            continue;
                        token.coordinates.Add(_item);
                    }
                    tokens.Add(token);
                }
            }
            return tokens;
        }

        public void SetNearestNeighbour(Tree currentTree, List<Tree> forest)
        {
            int nearestTreeId = -1;
            double minDistance = -1;
            List<double> distance = new List<double>();
            foreach (var item in forest)
            {
                if (item.treeID == currentTree.treeID)
                    continue;

                double sum = 0;
                for (int j = 0; j < trees[item.treeID].coordinates.Count; j++)
                    sum += Math.Pow(trees[item.treeID].coordinates[j] - currentTree.coordinates[j], 2);

                distance.Add(Math.Sqrt(sum));

                if (minDistance == -1)
                {
                    minDistance = sum;
                    nearestTreeId = item.treeID;
                }

                bool doSave = minDistance > sum;
                nearestTreeId = doSave ? item.treeID : nearestTreeId;
                minDistance = doSave ? sum : minDistance;
            }

            foreach (var item in edges)
            {
                if (item.indexes.Contains(nearestTreeId) && item.indexes.Contains(currentTree.treeID))
                {
                    return;
                }
                continue;
            }

            if (nearestTreeId == -1 || minDistance == -1)
                return;
            if (currentTree.treeID == trees[nearestTreeId].treeID)
                return;

            Edge edge = new Edge(currentTree, trees[nearestTreeId]);
            edges.Add(edge);

            // cycle check
            RemoveCycle(edge);
        }

        private void DoConnection(int treeIndex, List<Edge> connected)
        {
            List<Tree> disconnected = new List<Tree>();

            disconnected.Clear();
            for (int j = 0; j < trees.Count; j++)
            {
                if (treeIndex == j)
                    continue;

                bool isExists = false;
                foreach (var item in connected)
                {
                    if (item.indexes.Contains(trees[treeIndex].treeID) && item.indexes.Contains(trees[j].treeID))
                        isExists = true;
                }
                if (!isExists)
                    disconnected.Add(trees[j]);
            }

            if (disconnected.Count == 0)
                return;
            SetNearestNeighbour(trees[treeIndex], disconnected);
        }

        private void RemoveCycle(Edge current)
        {
            print("**************************\nCurrent: " + current);
            List<Edge> visited = new List<Edge> { current };
            List<Edge> neighbours = GetNeighbours(current.indexes[0], visited);
            neighbours.Remove(current);

            // if no neighbours - return
            if (neighbours.Count == 0)
                return;

            print("RemoveCycle::neighbours count: " + neighbours.Count);
            foreach (var item in neighbours)
                print(item);

            foreach (var item in neighbours)
                DoEdgeTransition(current.indexes[0], item, visited);
        }

        private void DoEdgeTransition(int rootId, Edge current, List<Edge> visited)
        {
            if (isCycle)
                return;
            visited.Add(current);
            print("Visited::count: " + visited.Count);
            foreach (var item in visited)
                print(item);

            int nextRoot = current.indexes[0] == rootId ? current.indexes[1] : current.indexes[0];
            print("Next root: " + nextRoot);
            List<Edge> neighbours = GetNeighbours(nextRoot, visited);
            neighbours.Remove(current);

            // if no neighbours - return
            if (neighbours.Count == 0)
            {
                print("No other neighbours for tree #" + nextRoot);
                visited.Remove(current);
                return;
            }

            print("DoEdgeTransition::Neighbours count: " + neighbours.Count);
            foreach (var item in neighbours)
                print(item);

            foreach (var item in neighbours)
            {
                if (!visited.Contains(item))
                    continue;

                print("Cycle found:: "+item);
                isCycle = true;
                string edges = "";
                foreach (var itemEdge in visited)
                    edges += " | " + itemEdge;
                print(edges);

                RemoveLongestEdge(visited);
                return;
            }

            foreach (var item in neighbours)
            {
                DoEdgeTransition(nextRoot, item, visited);
            }
        }

        private List<Edge> GetNeighbours(int root, List<Edge> visited)
        {
            List<Edge> neighbours = new List<Edge>();
            foreach (var item in edges)
            {
                if (item.indexes.Contains(root))
                    neighbours.Add(item);
            }

            return neighbours;
        }

        private void RemoveLongestEdge(List<Edge> list)
        {
            List<double> distances = new List<double>();
            foreach (var item in list)
            {
                double sum = 0;
                for (int j = 0; j < item.points[0].coordinates.Count; j++)
                    sum += Math.Pow(item.points[1].coordinates[j] - item.points[0].coordinates[j], 2);
                distances.Add(Math.Sqrt(sum));
            }

            double maxDist = distances[0];
            int maxDistIndex = 0;
            for (int i = 0; i < distances.Count; i++)
            {
                bool doSave = maxDist < distances[i];
                maxDist = doSave ? distances[i] : maxDist;
                maxDistIndex = doSave ? i : maxDistIndex;
            }

            //edges.Remove(list[maxDistIndex]);
            print("## Remove:: " + list[maxDistIndex]);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
        private List<Edge> builtEdges;
        [SerializeField]
        private List<Edge> projectedEdges;

        // How much klusters we will\n get after K-1 cuts (min: 2)
        [Header("Clusters: ")]
        [SerializeField]
        private int k;

        private bool isCycle;

        private void Start()
        {
            isCycle = false;
            drawForest = new List<GameObject>();
        }

        public void CalculationScenario()
        {
            // read file and save data as separate items
            trees = ReadCsv_(Path.Combine(Application.dataPath, "testing_1.csv"), true, 1);
            // spawn trees in 3d space with 2D coordinates
            SpawnTrees(trees);

            // setting connection with nearest neighbour
            for (int i = 0; i < trees.Count; i++)
                SetNearestNeighbour(trees[i], trees);
            /*print("Edges: " + builtEdges.Count + " | Trees: " + trees.Count);
            print("=========");
            print("=========");
            print("=========");*/

            // connects losted
            while (builtEdges.Count < trees.Count-1)
            //for (int q = 0; q < 3; q++)
            {
                List<Edge> connected = new List<Edge>(projectedEdges);
                for (int i = 0; i < trees.Count; i++)
                    DoConnection(trees[i].treeID, connected);
                //print("Edges: " + builtEdges.Count + " | Trees: " + trees.Count);
            }

            // do cuts
            if (k < 2)
                print("To small value K.");
            else if (k > trees.Count - 1)
            {
                k = trees.Count - 1;
            }

            for (int i = 1; i < k; i++)
                RemoveLongestEdge(builtEdges);

            DrawEdges();
        }

        private void DrawEdges()
        {
            for (int i = 0; i < builtEdges.Count; i++)
            {
                if (builtEdges[i].points[0].treeID == builtEdges[i].points[1].treeID)
                    continue;

                GameObject connection = Instantiate(connectionPrefab, connectionRoot);
                connection.GetComponent<LineRenderer>().positionCount = 2;
                connection.GetComponent<LineRenderer>().SetPosition(0, drawForest[builtEdges[i].points[0].treeID].transform.position);
                connection.GetComponent<LineRenderer>().SetPosition(1, drawForest[builtEdges[i].points[1].treeID].transform.position);
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

            foreach (var item in builtEdges)
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
            builtEdges.Add(edge);
            projectedEdges.Add(edge);

            // cycle check
            RemoveCycle(edge);
        }

        private void DoConnection(int treeIndex, List<Edge> connected)
        {
            isCycle = false;
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
            //print("************ Current **************\t" + current);
            List<Edge> visited = new List<Edge> { current };
            List<Edge> neighbours = GetNeighbours(current.indexes[0], visited);
            neighbours.Remove(current);

            // if no neighbours - return
            if (neighbours.Count == 0)
            {
                //print("************ Finished **************[0]\t" + current);
                return;
            }

            /*print("RemoveCycle::neighbours count: " + neighbours.Count);
            foreach (var item in neighbours)
                print(item);*/

            foreach (var item in neighbours)
                DoEdgeTransition(current.indexes[0], item, visited);
            //print("************ Finished **************\t" + current);
        }

        private void DoEdgeTransition(int rootId, Edge current, List<Edge> visited)
        {
            if (isCycle)
                return;

            visited.Add(current);
            /*print("Visited::count: " + visited.Count);
            foreach (var item in visited)
                print(item);*/

            // detect the other side of edge for seaching next heighbours
            int nextRoot = current.indexes[0] == rootId ? current.indexes[1] : current.indexes[0];
            //print("Next root: " + nextRoot);
            List<Edge> neighbours = GetNeighbours(nextRoot, visited);
            neighbours.Remove(current);

            // if no neighbours - return
            if (neighbours.Count == 0)
            {
                //print("No other neighbours for tree #" + nextRoot);
                visited.Remove(current);
                return;
            }

            /*print("DoEdgeTransition::Neighbours count: " + neighbours.Count);
            foreach (var item in neighbours)
                print(item);*/

            foreach (var item in neighbours)
            {
                // if we find neighbour in visited list -
                // we reached the tail, i.e. got a cycle,
                // otherwise - skip neighbour.
                if (!visited.Contains(item))
                    continue;

                //print("Cycle found:: " + item);
                isCycle = true;
                /*string builtEdges = "";
                foreach (var itemEdge in visited)
                    builtEdges += " | " + itemEdge;
                print(builtEdges);*/

                // removing the longest edge in cycle
                // and finish search
                RemoveLongestEdge(visited);
                return;
            }

            // no one neighbour creates a cycle, so
            // go further and try to get cycle in neighbours
            // of each current neighbour
            foreach (var item in neighbours)
                DoEdgeTransition(nextRoot, item, visited);

            // if no one path from neighbours creates cycle 
            // - remove current edge and return to previous
            // for checking its` last neighbours
            visited.Remove(current);
        }

        private List<Edge> GetNeighbours(int root, List<Edge> visited)
        {
            List<Edge> neighbours = new List<Edge>();
            foreach (var item in builtEdges)
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

            builtEdges.Remove(list[maxDistIndex]);
            //print("## Remove:: " + list[maxDistIndex]);
        }
    }
}

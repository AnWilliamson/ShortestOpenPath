using System;
using System.Collections.Generic;
using System.IO;
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

        private void Start()
        {
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

            DrawEdges();
        }

        private void DrawEdges() {
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
            for (int i = 0; i < forest.Count; i++)
            {
                if (i == currentTree.treeID)
                {
                    //distance.Add(0);
                    continue;
                }

                double sum = 0;
                for (int j = 0; j < forest[i].coordinates.Count; j++)
                    sum += Math.Pow(forest[i].coordinates[j] - currentTree.coordinates[j], 2);

                distance.Add(Math.Sqrt(sum));

                if (minDistance == -1)
                {
                    minDistance = sum;
                    nearestTreeId = i;
                }

                bool doSave = minDistance > sum;
                nearestTreeId = doSave ? i : nearestTreeId;
                minDistance = doSave ? sum : minDistance;
            }

            foreach (var item in edges)
            {
                if (item.indexes.Contains(nearestTreeId) && item.indexes.Contains(currentTree.treeID))
                    return;
                continue;
            }

            if (nearestTreeId == -1 || minDistance == -1)
                return;

            Edge edge = new Edge(currentTree, forest[nearestTreeId]);
            edges.Add(edge);
        }

        /*
        private void RemoveCycle(Tree currentTree)
        {
            print("Started");
            // if no neighbours - return
            if (currentTree.neighbours.Count == 0)
                return;
            visitedIds.Add(currentTree.treeID);

            foreach (var neighbour in currentTree.neighbours)
            {
                foreach (var item in neighbour.neighbours)
                {
                    if (item.treeID == currentTree.treeID)
                        continue;
                    if (visitedIds.Contains(item.treeID))
                    {
                        print("cycle found: " + currentTree.treeID + " - " + neighbour.treeID + " - " + item.treeID);
                        //neighbour.neighbours.Remove(item);
                        break;
                    }
                    //else RemoveCycle(item);
                }
            }
            //visitedIds.Add(currentTree.treeID);
            //foreach (var neighbour in currentTree.neighbours)
                //RemoveCycle(neighbour);
        }
        */
    }
}

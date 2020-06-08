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
        private List<GameObject> forest_;
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

        private bool initStep;
        private List<int> visitedIds;
        List<int> colors;

        private void Start()
        {
            forest_ = new List<GameObject>();
            visitedIds = new List<int>();
            colors = new List<int>();
        }

        public void CalculationScenario()
        {
            // read file and save data as separate items
            trees = ReadCsv_("D:/Programming/MachineLearning/Yefimov/testing_1.csv", true, 1);
            // spawn trees in 3d space with 2D coordinates
            SpawnTrees(trees);

            // setting connection with nearest neighbour
            /* for (int i = 0; i < forest_.Count; i++)
                 SetNearestNeighbour(trees[i], trees);*/

            SetNearestNeighbour(trees[0], trees);

            /*for (int i = 0; i < forest.Count; i++)
                SetNearestNeighbour(trees[i], trees, i);*/

            // draw set connections
            /*for (int i = 0; i < edges.Count; i++)
            {
                GameObject connection = Instantiate(connectionPrefab, connectionRoot);
                connection.GetComponent<LineRenderer>().positionCount = 2;

                if (edges[i].points[0].treeID == edges[i].points[1].treeID)
                    continue;

                connection.GetComponent<LineRenderer>().SetPosition(0, forest[edges[i].points[0].treeID].transform.position);
                connection.GetComponent<LineRenderer>().SetPosition(1, forest[edges[i].points[1].treeID].transform.position);
                connectionRenderers.Add(connection);
            }*/
        }

        private void SpawnTrees(List<Tree> _forest)
        {
            for (int i = 0; i < _forest.Count; i++)
            {
                _forest[i].treeID = i;
                GameObject _tree = Instantiate(treePrefab, treeRoot);
                _tree.transform.localPosition = new Vector3((float)_forest[i].coordinates[0] * 0.1f, (float)_forest[i].coordinates[1] * 0.1f);
                forest_.Add(_tree);
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
            print("===========================");
            int nearestTreeId = -1;
            double minDistance = -1;
            List<double> distance = new List<double>();
            for (int i = 0; i < forest.Count; i++)
            {
                print("i: " + i);
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
                    minDistance = sum;
                bool isConnected = false;
                foreach (var item in edges)
                {
                    Debug.Log(item + " | curr: " + currentTree.treeID + " | i: " + i);
                    if (item.indexes.Contains(i) && item.indexes.Contains(currentTree.treeID))
                    {
                        //Debug.Log(item + " | curr: " + currentTree.treeID + " | i: " + i);
                        print("Connected with: " + i);
                        isConnected = true;
                        break;
                    }
                    continue;
                }

                bool doSave = minDistance > sum && !isConnected;
                print("i: " + i + " | minDist: " + minDistance + " | sum: " + sum);
                nearestTreeId = doSave ? i : nearestTreeId;
                minDistance = doSave ? sum : minDistance;
            }

            if (nearestTreeId == -1 || minDistance == -1)
            {
                print(currentTree.treeID);
                print(nearestTreeId);
                print(minDistance);
                print("------------------");
                return;
            }

            Edge edge = new Edge(currentTree, forest[nearestTreeId]);
            edges.Add(edge);

            // draw edge
            if (edge.points[0].treeID == edge.points[1].treeID)
                return;

            GameObject connection = Instantiate(connectionPrefab, connectionRoot);
            connection.GetComponent<LineRenderer>().positionCount = 2;
            connection.GetComponent<LineRenderer>().SetPosition(0, forest_[edge.points[0].treeID].transform.position);
            connection.GetComponent<LineRenderer>().SetPosition(1, forest_[edge.points[1].treeID].transform.position);
            connectionRenderers.Add(connection);


            SetNearestNeighbour(trees[nearestTreeId], trees);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Lumibricks
{
    class ProbeTree
    {
        class Cluster
        {
            Vector3 left;
            Vector3 right;
            public UnityEngine.Vector3 Left
            {
                get { return left; }
                set { left = value; }
            }
            public UnityEngine.Vector3 Right
            {
                get { return right; }
                set { right = value; }
            }
        };

        public float distance(Vector3 A, Vector3 B)
        {
            return 0.0f;
        }

        // naive exponential clustering
        public void generateTree(Vector3[] probePositions)
        {
            var active = new List<Vector3>(probePositions);
            while (active.Count > 1)
            {
                double bestD = double.MaxValue;
                Vector3 left = new Vector3();
                Vector3 right = new Vector3();
                foreach (var A in active)
                {
                    foreach (var B in active)
                    {
                        float dist = distance(A, B);
                        if ((A != B) && (dist < bestD))
                        {
                            bestD = dist;
                            left = A;
                            right = B;
                        }
                    }
                }

                //active.RemoveAt(left);
                //active.RemoveAt(right);
                Cluster c = new Cluster();
                c.Left = left;
                c.Right = right;
                // active.add();
            }
        }

    }
}

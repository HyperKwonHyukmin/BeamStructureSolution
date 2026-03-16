using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeamStructureSolution.Model
{
  public struct Point3D
  {
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Point3D(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public override string ToString()
    {
      return $"(X:{X}, Y:{Y}, Z:{Z})";
    }
  }

  public class Nodes : IEnumerable<KeyValuePair<int, Point3D>>
  {
    public int nodeID = 0;
    private Dictionary<int, Point3D> nodes = new Dictionary<int, Point3D>();
    private Dictionary<string, int> nodeLookup = new Dictionary<string, int>();

    // 해당 좌표의 노드가 존재하면 ID 반환, 없으면 새로 추가
    public int AddOrGet(double X, double Y, double Z)
    {
      string key = X + "," + Y + "," + Z;
      if (nodeLookup.TryGetValue(key, out int existingNodeID))
      {
        return existingNodeID;
      }

      nodeID += 1;
      nodes[nodeID] = new Point3D(X, Y, Z);
      nodeLookup[key] = nodeID;
      return nodeID;
    }

    // 지정된 ID로 노드 추가
    public void AddWithID(int NodeID, double X, double Y, double Z)
    {
      {
        string key = X + "," + Y + "," + Z;

        nodes[NodeID] = new Point3D(X, Y, Z);
        nodeLookup[key] = NodeID;
        nodeID = NodeID;
      }
    }

    // 해당 ID의 노드가 존재하는지 여부 반환
    public bool Contains(int nodeID)
    {
      return nodes.ContainsKey(nodeID);
    }

    // ID에 해당하는 노드를 삭제
    public void Remove(int inputNodeID)
    {
      if (!nodes.ContainsKey(inputNodeID))
      {
        throw new KeyNotFoundException($"Node ID {inputNodeID} does not exist.");
      }

      Point3D removedNode = nodes[inputNodeID];
      nodes.Remove(inputNodeID);
      nodeLookup.Remove(removedNode.X + "," + removedNode.Y + "," + removedNode.Z);
    }

    // 좌표를 기반으로 노드 ID 반환
    public int FindNodeID(double X, double Y, double Z)
    {
      string key = X + "," + Y + "," + Z;
      return nodeLookup.TryGetValue(key, out int nodeID) ? nodeID : -1;
    }

    // 특정 노드의 좌표 반환
    public Point3D GetNodeCoordinates(int nodeID)
    {
      if (!nodes.ContainsKey(nodeID))
      {
        throw new KeyNotFoundException($"Node ID {nodeID} does not exist.");
      }
      return nodes[nodeID];
    }

    // 전체 노드 리스트 반환
    public List<KeyValuePair<int, Point3D>> GetAllNodes()
    {
      return new List<KeyValuePair<int, Point3D>>(nodes);
    }

    // 현재 노드 수 반환
    public int GetNodeCount()
    {
      return nodes.Count;
    }

    // 모든 노드를 지정 방향만큼 평행 이동
    public void TranslateAllNodes(double dx, double dy, double dz)
    {
      Dictionary<int, Point3D> updatedNodes = new Dictionary<int, Point3D>();
      Dictionary<string, int> updatedNodeLookup = new Dictionary<string, int>();

      foreach (var node in nodes)
      {
        Point3D oldPoint = node.Value;
        Point3D newPoint = new Point3D(oldPoint.X + dx, oldPoint.Y + dy, oldPoint.Z + dz);

        updatedNodes[node.Key] = newPoint;
        updatedNodeLookup[$"{newPoint.X},{newPoint.Y},{newPoint.Z}"] = node.Key;
      }

      nodes = updatedNodes;
      nodeLookup = updatedNodeLookup;
    }

    // 특정 노드 위치 변경
    public void MoveNodeTo(int nodeID, double newX, double newY, double newZ)
    {
      if (!nodes.ContainsKey(nodeID))
      {
        throw new KeyNotFoundException($"Node ID {nodeID} does not exist.");
      }

      Point3D oldPoint = nodes[nodeID];
      nodeLookup.Remove($"{oldPoint.X},{oldPoint.Y},{oldPoint.Z}");

      Point3D newPoint = new Point3D(newX, newY, newZ);
      nodes[nodeID] = newPoint;
      nodeLookup[$"{newX},{newY},{newZ}"] = nodeID;
    }

    // 인덱서: 노드 ID로 좌표 직접 접 가능
    public Point3D this[int nodeID]
    {
      get
      {
        if (!nodes.ContainsKey(nodeID))
        {
          throw new KeyNotFoundException($"node ID {nodeID} does not exist.");
        }

        return nodes[nodeID];
      }
    }

    // 반복자: 전체 노드를 순회 가능하게 함
    public IEnumerator<KeyValuePair<int, Point3D>> GetEnumerator()
    {
      return nodes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}

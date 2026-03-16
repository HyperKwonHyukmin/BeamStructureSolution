using BeamStructureSolution.Model;
using BeamStructureSolution.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeamStructureSolution.Utils
{

  public class BoundingBox
  {
    public Point3D MinPoint;
    public Point3D MaxPoint;

    public BoundingBox(List<Point3D> nodes)
    {
      MinPoint = new Point3D(nodes.Min(n => n.X), nodes.Min(n => n.Y), nodes.Min(n => n.Z));
      MaxPoint = new Point3D(nodes.Max(n => n.X), nodes.Max(n => n.Y), nodes.Max(n => n.Z));
    }

    public bool Overlaps(BoundingBox other, double tolerance)
    {
      return (MaxPoint.X >= other.MinPoint.X - tolerance &&
              MinPoint.X <= other.MaxPoint.X + tolerance &&
              MaxPoint.Y >= other.MinPoint.Y - tolerance &&
              MinPoint.Y <= other.MaxPoint.Y + tolerance &&
              MaxPoint.Z >= other.MinPoint.Z - tolerance &&
              MinPoint.Z <= other.MaxPoint.Z + tolerance);
    }

    public bool Contains(Point3D point)
    {
      return (point.X >= MinPoint.X && point.X <= MaxPoint.X &&
              point.Y >= MinPoint.Y && point.Y <= MaxPoint.Y &&
              point.Z >= MinPoint.Z && point.Z <= MaxPoint.Z);
    }

    // static 유틸 메서드 추가
    public static BoundingBox FromElementGroup(List<int> elementIDs, Nodes nodes, Elements elements)
    {
      var pointList = new List<Point3D>();

      foreach (int eid in elementIDs)
      {
        var element = elements[eid];
        foreach (int nid in element.NodeIDs)
        {
          if (nodes.Contains(nid))
            pointList.Add(nodes[nid]);
        }
      }
      return new BoundingBox(pointList);
    }
  }



  // 벡터 계산을 위한 클래스 
  public class Vector3D
  {
    public double X, Y, Z;

    public Vector3D(double x, double y, double z)
    {
      X = x; Y = y; Z = z;
    }

    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    // 벡터 외 (Cross Product) 계산
    public static Vector3D Cross(Vector3D v1, Vector3D v2)
    {
      return new Vector3D(
          v1.Y * v2.Z - v1.Z * v2.Y,
          v1.Z * v2.X - v1.X * v2.Z,
          v1.X * v2.Y - v1.Y * v2.X
      );
    }

    public double Magnitude() => Math.Sqrt(X * X + Y * Y + Z * Z);

    public double AngleBetween(Vector3D other)
    {
      double dotProduct = this.Dot(other);
      double magnitudeProduct = this.Magnitude() * other.Magnitude();
      return Math.Acos(dotProduct / magnitudeProduct) * (180.0 / Math.PI);
    }

    // 벡터 정규화 (Normalize)
    public Vector3D Normalize()
    {
      double magnitude = Magnitude();
      if (magnitude < 1e-6) return new Vector3D(0, 0, 0); // 0 벡터 예외 처리
      return new Vector3D(X / magnitude, Y / magnitude, Z / magnitude);
    }
  }


  // ConnectivityTracker 클래스에 그룹 정보 관련 메서드 추가

  public class ConnectivityTracker
  {
    private Dictionary<int, int> parent = new();
    public Nodes nodes;
    public Elements elements;

    // 클래스 초기화
    public ConnectivityTracker(Nodes nodes, Elements elements)
    {
      this.nodes = nodes;
      this.elements = elements;

      foreach (var node in nodes.GetAllNodes())
        MakeSet(node.Key);

      foreach (var element in elements)
      {
        var nodeIDs = element.Value.NodeIDs;
        if (nodeIDs.Count == 2)
          Union(nodeIDs[0], nodeIDs[1]);
      }
    }

    public void MakeSet(int nodeID)
    {
      if (!parent.ContainsKey(nodeID))
        parent[nodeID] = nodeID;
    }

    public int Find(int nodeID)
    {
      if (parent[nodeID] != nodeID)
        parent[nodeID] = Find(parent[nodeID]);
      return parent[nodeID];
    }

    public void Union(int nodeA, int nodeB)
    {
      int rootA = Find(nodeA);
      int rootB = Find(nodeB);
      if (rootA != rootB)
        parent[rootB] = rootA;
    }

    public bool IsConnected(int nodeA, int nodeB)
    {
      return Find(nodeA) == Find(nodeB);
    }

    public int CountGroups()
    {
      var groups = new HashSet<int>();
      foreach (var nodeID in parent.Keys)
        groups.Add(Find(nodeID));
      return groups.Count;
    }

    public Dictionary<int, List<int>> GetConnectedComponents(bool debugPrint = false)
    {
      var groups = new Dictionary<int, List<int>>();

      foreach (var nodeID in parent.Keys)
      {
        int root = Find(nodeID);
        if (!groups.ContainsKey(root))
          groups[root] = new List<int>();
        groups[root].Add(nodeID);
      }

      if (debugPrint)
      {
        Console.WriteLine($"총 연결 그룹 수: {groups.Count}");
        foreach (var kv in groups)
        {
          Console.WriteLine($"Group {kv.Key}: {string.Join(", ", kv.Value)}");
        }
      }

      return groups;
    }

    public List<int> GetNodesInSameComponent(int nodeID)
    {
      int root = Find(nodeID);
      var list = new List<int>();

      foreach (var kv in parent)
      {
        if (Find(kv.Key) == root)
          list.Add(kv.Key);
      }

      return list;
    }

    public int GetGroupID(int nodeID)
    {
      return Find(nodeID);
    }

    public Dictionary<int, int> GetGroupSizes()
    {
      var sizes = new Dictionary<int, int>();

      foreach (var nodeID in parent.Keys)
      {
        int root = Find(nodeID);
        if (!sizes.ContainsKey(root))
          sizes[root] = 0;
        sizes[root]++;
      }

      return sizes;
    }

    public List<List<int>> GetAllGroups()
    {
      return GetConnectedComponents().Values.ToList();
    }

    public Dictionary<int, List<int>> GroupElementsByNodeConnectivity()
    {
      var result = new Dictionary<int, List<int>>();

      foreach (var kv in elements)
      {
        int elementID = kv.Key;
        var nodeIDs = kv.Value.NodeIDs;

        if (nodeIDs.Count == 0) continue;
        int groupID = Find(nodeIDs[0]);

        if (!result.ContainsKey(groupID))
          result[groupID] = new List<int>();

        result[groupID].Add(elementID);
      }

      return result;
    }

    public Dictionary<int, List<int>> BuildFromElementsAndGroupElements()
    {
      foreach (var node in nodes.GetAllNodes())
        MakeSet(node.Key);

      foreach (var element in elements)
      {
        var nodeIDs = element.Value.NodeIDs;
        if (nodeIDs.Count == 2)
          Union(nodeIDs[0], nodeIDs[1]);
      }

      return GroupElementsByNodeConnectivity();
    }
  }



}

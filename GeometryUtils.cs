using BeamStructureSolution.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BeamStructureSolution.Utils
{

  public static class GeometryUtils
  {
    // 특정 노드가 선분(A-B)에서 얼마나 떨어져 있는지 계산
    public static double CalculateDistanceFromLineToNode(int nodeA, int nodeB, int nodeC, Nodes Nodes)
    {
      Point3D A = Nodes[nodeA];
      Point3D B = Nodes[nodeB];
      Point3D C = Nodes[nodeC];

      double[] AB = { B.X - A.X, B.Y - A.Y, B.Z - A.Z };
      double[] AC = { C.X - A.X, C.Y - A.Y, C.Z - A.Z };

      // 벡터 AB 크기 계산
      double AB_mag = Math.Sqrt(AB[0] * AB[0] + AB[1] * AB[1] + AB[2] * AB[2]);
      if (AB_mag == 0) return double.NaN; // 두 점이 동일한 경우 예외 처리

      // AC 벡터를 AB 벡터 위에 투영
      double projectionFactor = (AC[0] * AB[0] + AC[1] * AB[1] + AC[2] * AB[2]) / (AB_mag * AB_mag);

      // 투영점이 선분 범위 [0, 1] 사이에 있는지 검사
      if (projectionFactor < 0 || projectionFactor > 1)
        return -1; // 📌 선분을 벗어나는 경우 -1 반환

      // 투영점 좌표 계산
      double[] projection = {
                A.X + projectionFactor * AB[0],
                A.Y + projectionFactor * AB[1],
                A.Z + projectionFactor * AB[2]
            };

      // C와 투영점(projection) 사이 거리 계산
      return Math.Sqrt(Math.Pow(C.X - projection[0], 2) +
                       Math.Pow(C.Y - projection[1], 2) +
                       Math.Pow(C.Z - projection[2], 2));
    }


    public static double GetDistanceBetweenNodes(int firstNodeID, int secondNodeID, Nodes nodes)
    {
      if (!nodes.Contains(firstNodeID) || !nodes.Contains(secondNodeID))
      {
        throw new KeyNotFoundException("One or both Node IDs do not exist.");
      }

      Point3D firstNode = nodes[firstNodeID];
      Point3D secondNode = nodes[secondNodeID];

      return Math.Sqrt(
          Math.Pow(secondNode.X - firstNode.X, 2) +
          Math.Pow(secondNode.Y - firstNode.Y, 2) +
          Math.Pow(secondNode.Z - firstNode.Z, 2)
      );
    }

    public static double GetDistanceBetweenNodes(Point3D firstNode, Point3D secondNode)
    {
      return Math.Sqrt(
          Math.Pow(secondNode.X - firstNode.X, 2) +
          Math.Pow(secondNode.Y - firstNode.Y, 2) +
          Math.Pow(secondNode.Z - firstNode.Z, 2)
      );
    }


    // 요소가 이루는 각도 계산
    public static double CalculateAngleBetweenElements(int elementA_ID, int elementB_ID, Elements elements)
    {
      if (!elements.ContainsKey(elementA_ID) || !elements.ContainsKey(elementB_ID))
        throw new KeyNotFoundException("Element ID가 존재하지 않습니다.");

      ElementAttribute elementA = elements[elementA_ID];
      ElementAttribute elementB = elements[elementB_ID];

      if (elementA.NodeIDs.Count < 2 || elementB.NodeIDs.Count < 2)
        throw new InvalidOperationException("각 요소는 최소 두 개의 노드를 포함해야 합니다.");

      int nodeA1 = elementA.NodeIDs[0];
      int nodeA2 = elementA.NodeIDs[1];
      int nodeB1 = elementB.NodeIDs[0];
      int nodeB2 = elementB.NodeIDs[1];

      // 요소의 방향 벡터 계산
      Vector3D V1 = new Vector3D(
          elements.Nodes[nodeA2].X - elements.Nodes[nodeA1].X,
          elements.Nodes[nodeA2].Y - elements.Nodes[nodeA1].Y,
          elements.Nodes[nodeA2].Z - elements.Nodes[nodeA1].Z
      );

      Vector3D V2 = new Vector3D(
          elements.Nodes[nodeB2].X - elements.Nodes[nodeB1].X,
          elements.Nodes[nodeB2].Y - elements.Nodes[nodeB1].Y,
          elements.Nodes[nodeB2].Z - elements.Nodes[nodeB1].Z
      );

      // 벡터가 이루는 각도 반환
      return V1.AngleBetween(V2);
    }

    public static Vector3D CreateVector(Point3D from, Point3D to)
    {
      return new Vector3D(
        to.X - from.X,
        to.Y - from.Y,
        to.Z - from.Z
      );
    }


    public static int AddMidpointNode(int nodeID1, int nodeID2, Nodes nodes)
    {
      if (!nodes.Contains(nodeID1) || !nodes.Contains(nodeID2))
        throw new KeyNotFoundException("입력한 Node ID가 존재하지 않습니다.");

      // 두 노드의 절대 좌표 가져오기
      Point3D node1 = nodes[nodeID1];
      Point3D node2 = nodes[nodeID2];

      // 정중앙 좌표 계산
      double midX = (node1.X + node2.X) / 2.0;
      double midY = (node1.Y + node2.Y) / 2.0;
      double midZ = (node1.Z + node2.Z) / 2.0;

      // 새로운 노드 추가 (중복 방지)
      int newNodeID = nodes.AddOrGet(midX, midY, midZ);

      //Console.WriteLine($"새 노드 {newNodeID}가 추가됨 (위치: {midX}, {midY}, {midZ})");

      return newNodeID; // 새로 생성된 노드 ID 반환
    }

    public static double CalculateDistanceBetweenElements(int elementA_ID, int elementB_ID,
    Nodes nodeInstance, Elements elementInstance)
    {
      if (!elementInstance.ContainsKey(elementA_ID) || !elementInstance.ContainsKey(elementB_ID))
        throw new KeyNotFoundException("Element ID가 존재하지 않습니다.");

      ElementAttribute elementA = elementInstance[elementA_ID];
      ElementAttribute elementB = elementInstance[elementB_ID];

      if (elementA.NodeIDs.Count < 2 || elementB.NodeIDs.Count < 2)
        throw new InvalidOperationException("각 요소는 최소 두 개의 노드를 포함해야 합니다.");

      int a1 = elementA.NodeIDs[0];
      int a2 = elementA.NodeIDs[1];
      int b1 = elementB.NodeIDs[0];
      int b2 = elementB.NodeIDs[1];

      Point3D A1 = nodeInstance[a1];
      Point3D A2 = nodeInstance[a2];
      Point3D B1 = nodeInstance[b1];
      Point3D B2 = nodeInstance[b2];

      //Console.WriteLine($"A1 : {A1}");
      //Console.WriteLine($"A2 : {A2}");
      //Console.WriteLine($"B1 : {B1}");
      //Console.WriteLine($"B2 : {B2}");

      Vector3D u = new Vector3D(A2.X - A1.X, A2.Y - A1.Y, A2.Z - A1.Z);
      Vector3D v = new Vector3D(B2.X - B1.X, B2.Y - B1.Y, B2.Z - B1.Z);
      Vector3D w = new Vector3D(A1.X - B1.X, A1.Y - B1.Y, A1.Z - B1.Z);

      double a = u.Dot(u);         // |u|^2
      double b = u.Dot(v);
      double c = v.Dot(v);         // |v|^2
      double d = u.Dot(w);
      double e = v.Dot(w);

      double D = a * c - b * b;
      double sc, sN, sD = D;
      double tc, tN, tD = D;

      const double EPS = 1e-6;

      // if lines are almost parallel
      if (D < EPS)
      {
        sN = 0.0;
        sD = 1.0;
        tN = e;
        tD = c;
      }
      else
      {
        sN = (b * e - c * d);
        tN = (a * e - b * d);

        if (sN < 0.0)
        {
          sN = 0.0;
          tN = e;
          tD = c;
        }
        else if (sN > sD)
        {
          sN = sD;
          tN = e + b;
          tD = c;
        }
      }

      if (tN < 0.0)
      {
        tN = 0.0;

        if (-d < 0.0) sN = 0.0;
        else if (-d > a) sN = sD;
        else
        {
          sN = -d;
          sD = a;
        }
      }
      else if (tN > tD)
      {
        tN = tD;

        if ((-d + b) < 0.0) sN = 0;
        else if ((-d + b) > a) sN = sD;
        else
        {
          sN = (-d + b);
          sD = a;
        }
      }

      sc = (Math.Abs(sN) < EPS ? 0.0 : sN / sD);
      tc = (Math.Abs(tN) < EPS ? 0.0 : tN / tD);

      Point3D pointOnA = new Point3D(
          A1.X + sc * u.X,
          A1.Y + sc * u.Y,
          A1.Z + sc * u.Z
      );

      Point3D pointOnB = new Point3D(
          B1.X + tc * v.X,
          B1.Y + tc * v.Y,
          B1.Z + tc * v.Z
      );

      return Math.Sqrt(
          Math.Pow(pointOnA.X - pointOnB.X, 2) +
          Math.Pow(pointOnA.Y - pointOnB.Y, 2) +
          Math.Pow(pointOnA.Z - pointOnB.Z, 2)
      );

    }

    // 이 함수의 주요 역할은 "Node C의 투영점을 구하는 것"
    public static (Point3D projectionPoint, double distance) ProjectPointOntoLineSegment(int nodeA, int nodeB, int nodeC, Nodes Nodes)
    {
      Point3D A = Nodes[nodeA];
      Point3D B = Nodes[nodeB];
      Point3D C = Nodes[nodeC];

      double[] AB = { B.X - A.X, B.Y - A.Y, B.Z - A.Z };
      double[] AC = { C.X - A.X, C.Y - A.Y, C.Z - A.Z };

      // 벡터 AB의 크기 (정규화 전)
      double AB_mag = Math.Sqrt(AB[0] * AB[0] + AB[1] * AB[1] + AB[2] * AB[2]);
      if (AB_mag == 0) return (new Point3D(double.NaN, double.NaN, double.NaN), double.NaN); // 동일한 노드 예외 처리

      // AC 벡터를 AB 벡터 위에 투영
      double projectionFactor = (AC[0] * AB[0] + AC[1] * AB[1] + AC[2] * AB[2]) / (AB_mag * AB_mag);

      // 투영점 계산
      Point3D projection = new Point3D(
          A.X + projectionFactor * AB[0],
          A.Y + projectionFactor * AB[1],
          A.Z + projectionFactor * AB[2]
      );

      //  **투영점이 선분 범위 [0, 1] 사이에 있는지 검사**
      if (projectionFactor < 0 || projectionFactor > 1)
      {
        return (projection, -1); //투영점이 선분 밖이면 -1 반환
      }

      // nodeC와 수선의 발(projection) 사이 거리 계산
      double distance = Math.Sqrt(
          Math.Pow(C.X - projection.X, 2) +
          Math.Pow(C.Y - projection.Y, 2) +
          Math.Pow(C.Z - projection.Z, 2)
      );

      return (projection, distance);
    }

    // 메써드 오버라이딩 - Point3D가 들어와도 작동
    public static (Point3D projectionPoint, double distance) ProjectPointOntoLineSegment(Point3D A, Point3D B, Point3D C)
    {
      double[] AB = { B.X - A.X, B.Y - A.Y, B.Z - A.Z };
      double[] AC = { C.X - A.X, C.Y - A.Y, C.Z - A.Z };

      // 벡터 AB의 크기 (정규화 전)
      double AB_mag = Math.Sqrt(AB[0] * AB[0] + AB[1] * AB[1] + AB[2] * AB[2]);
      if (AB_mag == 0) return (new Point3D(double.NaN, double.NaN, double.NaN), double.NaN); // 동일한 노드 예외 처리

      // AC 벡터를 AB 벡터 위에 투영
      double projectionFactor = (AC[0] * AB[0] + AC[1] * AB[1] + AC[2] * AB[2]) / (AB_mag * AB_mag);

      // 투영점 계산
      Point3D projection = new Point3D(
          A.X + projectionFactor * AB[0],
          A.Y + projectionFactor * AB[1],
          A.Z + projectionFactor * AB[2]
      );

      //  **투영점이 선분 범위 [0, 1] 사이에 있는지 검사**
      if (projectionFactor < 0 || projectionFactor > 1)
      {
        return (projection, -1); //투영점이 선분 밖이면 -1 반환
      }

      // nodeC와 수선의 발(projection) 사이 거리 계산
      double distance = Math.Sqrt(
          Math.Pow(C.X - projection.X, 2) +
          Math.Pow(C.Y - projection.Y, 2) +
          Math.Pow(C.Z - projection.Z, 2)
      );

      return (projection, distance);
    }

    public static (int closestNodeA, int closestNodeB, double minDistance) FindClosestNodes(
     int eleA_ID, int eleB_ID, Nodes nodeInstance, Elements elementInstance)
    {
      double minDistance = double.MaxValue;
      int closestNodeA = -1;
      int closestNodeB = -1;

      var nodesA = elementInstance[eleA_ID].NodeIDs;
      var nodesB = elementInstance[eleB_ID].NodeIDs;

      foreach (var nodeA in nodesA)
      {
        foreach (var nodeB in nodesB)
        {
          double distance = GeometryUtils.GetDistanceBetweenNodes(nodeA, nodeB, nodeInstance);
          if (distance < minDistance)
          {
            minDistance = distance;
            closestNodeA = nodeA;
            closestNodeB = nodeB;
          }
        }
      }

      return (closestNodeA, closestNodeB, minDistance);
    }




    public static bool IsPointWithinBoundingBox(Point3D point, Point3D a, Point3D b, double margin)
    {
      double minX = Math.Min(a.X, b.X) - margin;
      double maxX = Math.Max(a.X, b.X) + margin;
      double minY = Math.Min(a.Y, b.Y) - margin;
      double maxY = Math.Max(a.Y, b.Y) + margin;
      double minZ = Math.Min(a.Z, b.Z) - margin;
      double maxZ = Math.Max(a.Z, b.Z) + margin;

      return (point.X >= minX && point.X <= maxX) &&
             (point.Y >= minY && point.Y <= maxY) &&
             (point.Z >= minZ && point.Z <= maxZ);
    }


    public static Vector3D GetDirectionVector(ElementAttribute element)
    {
      var p1 = element.NodeInstance[element.NodeIDs[0]];
      var p2 = element.NodeInstance[element.NodeIDs[1]];
      return new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z).Normalize();
    }


  }
}

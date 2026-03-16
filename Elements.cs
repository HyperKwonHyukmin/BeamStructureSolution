using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BeamStructureSolution.Utils;
using BeamStructureSolution.Model;

namespace BeamStructureSolution.Model
{

  // 요소 속성 정의 (연결된 노드 ID 및 속성 ID)
  public struct ElementAttribute
  {
    public List<int> NodeIDs { get; }
    public int PropertyID { get; }
    public Properties Properties { get; }
    public Nodes NodeInstance { get; }
    public double[] LocalAxis { get; }
    // 기타 데이터를 담기 위한 컨테이너 
    public Vector3D Vector { get; }
    public Dictionary<string, string> ExtraData { get; set; }

    public ElementAttribute(List<int> nodeIDs, int propertyID, double[] localAxis, Properties properties, Nodes nodeInstance,
      Vector3D vector, Dictionary<string, string> extraData = null)
    {
      NodeIDs = nodeIDs;
      PropertyID = propertyID;
      LocalAxis = localAxis;
      Properties = properties;
      NodeInstance = nodeInstance;
      Vector = vector;
      ExtraData = extraData ?? new Dictionary<string, string>();
    }

    public override string ToString()
    {
      string extraInfo = (ExtraData != null && ExtraData.Count > 0)
          ? string.Join(", ", ExtraData.Select(kv => $"{kv.Key}: {kv.Value}"))
          : "None";
      return $"Nodes: [{string.Join(", ", NodeIDs)}], PropertyID: {PropertyID}, " +
        $"LocalAxis:[{LocalAxis[0]},{LocalAxis[1]},{LocalAxis[2]}] ExtraData: {extraInfo}";
    }
  }

  public class Elements : IEnumerable<KeyValuePair<int, ElementAttribute>> // IEnumerable 구현 (foreach 지원)
  {
    public Nodes Nodes { get; private set; } // Nodes 객체 참조
    public Properties Properties { get; private set; } // Properties 객체 참조

    public int elementID = 0; // 요소 ID 카운터
    private Dictionary<int, ElementAttribute> elements = new Dictionary<int, ElementAttribute>(); // 요소 저장
    private Dictionary<string, int> elementLookup = new Dictionary<string, int>(); // 요소 중복 방지용

    public Elements(Nodes nodes, Properties properties)
    {
      Nodes = nodes;
      Properties = properties;

    }

    // 인덱서 추가 (element_cls[elementID] 형태로 요소 조회 가능)
    public ElementAttribute this[int elementID]
    {
      get
      {
        if (!elements.ContainsKey(elementID))
        {
          throw new KeyNotFoundException($"Element ID {elementID} does not exist.");
        }
        return elements[elementID];
      }
    }

    // 고유한 새 Element ID를 자동으로 생성하여 요소 추가
    public int AddNew(List<int> nodeIDs, int propertyID, double[] localAxis, Dictionary<string, string> extraData = null)
    {
      int newElementID = (elements.Keys.Count > 0) ? elements.Keys.Max() + 1 : 1;

      // 절대 중복되지 않는 ID 보장
      while (elements.ContainsKey(newElementID))
        newElementID++;

      Vector3D vector = CalculateVector(nodeIDs);

      elements[newElementID] = new ElementAttribute(nodeIDs, propertyID, localAxis, Properties, Nodes, vector, extraData);

      string key = $"{string.Join(",", nodeIDs)}|{propertyID}";
      elementLookup[key] = newElementID;

      elementID = newElementID;

      return newElementID;
    }


    // 사용자가 지정한 ID로 요소 추가
    public void AddWithID(int eleID, List<int> nodeIDs, int propertyID, double[] localAxis, Dictionary<string, string> extraData = null)
    {
      string key = $"{string.Join(",", nodeIDs)}|{propertyID}";

      Vector3D vector = CalculateVector(nodeIDs);

      ElementAttribute newElement = new ElementAttribute(
        nodeIDs, propertyID, localAxis, Properties, Nodes, vector, extraData);
      elements[eleID] = newElement;
      elementLookup[key] = eleID;
      elementID = eleID; // 인스턴스의 현대 elementID를 맞춰준다. 
    }

    // 요소 제거 (존재하지 않으면 예외 발생)
    public void Remove(int inputElementID)
    {
      if (!elements.ContainsKey(inputElementID))
      {
        throw new KeyNotFoundException($"Element ID {inputElementID} does not exist.");
      }

      ElementAttribute removedElement = elements[inputElementID];
      string key = $"{string.Join(",", removedElement.NodeIDs)}|{removedElement.PropertyID}";

      elements.Remove(inputElementID);
      elementLookup.Remove(key);
    }

    // 해당 ID의 요소 존재 여부 확인
    public bool ContainsKey(int elementID)
    {
      return elements.ContainsKey(elementID);
    }

    public IEnumerable<int> Keys => elements.Keys;


    // 가장 마지막으로 추가된 요소의 ID 반환
    public int GetLastID()
    {
      return elementID > 0 ? elementID : throw new InvalidOperationException("No elements exist.");
    }

    // 요소 개수 반환
    public int GetCount()
    {
      return elements.Count;
    }

    private Vector3D CalculateVector(List<int> nodeIDs)
    {
      if (nodeIDs.Count != 2)
        throw new ArgumentException("Element must be defined by exactly 2 nodes to compute vector.");

      Point3D point1 = Nodes[nodeIDs[0]];
      Point3D point2 = Nodes[nodeIDs[1]];

      double dx = point2.X - point1.X;
      double dy = point2.Y - point1.Y;
      double dz = point2.Z - point1.Z;

      double magnitude = Math.Sqrt(dx * dx + dy * dy + dz * dz);

      if (magnitude == 0)
        return new Vector3D(0, 0, 0); // or throw exception depending on use case

      return new Vector3D(dx / magnitude, dy / magnitude, dz / magnitude);
    }



    // Elements 클래스 내부에 추가
    public int CountNodeUsage(int targetNodeID)
    {
      int count = 0;

      foreach (var element in elements.Values)
      {
        if (element.NodeIDs.Contains(targetNodeID))
        {
          count++;
        }
      }

      return count;
    }


    // 특정 속성을 가진 요소 검색
    public List<int> FindElementsByProperty(int propertyID)
    {
      return elements
          .Where(element => element.Value.PropertyID == propertyID)
          .Select(element => element.Key)
          .ToList();
    }

    // 요소를 지정한 축 방향으로 평행 이동
    public void MoveToAxis(List<int> inputElements, string Axis, double distance)
    {
      // 유효한 축인지 확인
      Axis = Axis.ToUpper();
      if (Axis != "X" && Axis != "Y" && Axis != "Z")
      {
        throw new ArgumentException("Invalid axis. Please use 'X', 'Y', or 'Z'.");
      }

      // 이미 이동한 노드를 저장할 HashSet (중복 이동 방지)
      HashSet<int> movedNodes = new HashSet<int>();

      // 노드 이동 벡터 결정
      double moveX = (Axis == "X") ? distance : 0;
      double moveY = (Axis == "Y") ? distance : 0;
      double moveZ = (Axis == "Z") ? distance : 0;

      // 요소들을 순회하며 이동
      foreach (int elementID in inputElements)
      {
        if (!elements.ContainsKey(elementID))
        {
          throw new KeyNotFoundException($"Element ID {elementID} does not exist.");
        }

        ElementAttribute eleAttribute = elements[elementID];
        var nodeIDs = new List<int>(eleAttribute.NodeIDs);

        foreach (int nodeID in nodeIDs)
        {
          // 중복 이동 방지
          if (!movedNodes.Contains(nodeID))
          {
            Point3D position = Nodes[nodeID];
            double newX = position.X + moveX;
            double newY = position.Y + moveY;
            double newZ = position.Z + moveZ;

            Nodes.MoveNodeTo(nodeID, newX, newY, newZ);
            movedNodes.Add(nodeID);
          }
        }
      }
    }

    // 요소를 벡터 방향으로 이동
    public void MoveByVector(List<int> inputElements, Vector3D moveVector)
    {
      // 중복 이동 방지를 위한 노드 추적
      HashSet<int> movedNodes = new HashSet<int>();

      foreach (int elementID in inputElements)
      {
        if (!elements.ContainsKey(elementID))
        {
          throw new KeyNotFoundException($"Element ID {elementID} does not exist.");
        }

        ElementAttribute eleAttribute = elements[elementID];
        var nodeIDs = new List<int>(eleAttribute.NodeIDs);

        foreach (int nodeID in nodeIDs)
        {
          if (!movedNodes.Contains(nodeID))
          {
            Point3D position = Nodes[nodeID];
            double newX = position.X + moveVector.X;
            double newY = position.Y + moveVector.Y;
            double newZ = position.Z + moveVector.Z;

            Nodes.MoveNodeTo(nodeID, newX, newY, newZ);
            movedNodes.Add(nodeID);
          }
        }
      }
    }

    // 요소들을 복사해서 특정 거리만큼 이동한 새 요소를 생성
    public List<int> ElementsCopyMove(List<int> moveElements, double X, double Y, double Z,
   ref List<List<int>> HorizontalConnectionElements_list)
    {
      var newElementID_list = new List<int>();
      Dictionary<int, int> newNodeConnection_dict = new Dictionary<int, int>();

      foreach (var elementID in moveElements)
      {
        if (!elements.ContainsKey(elementID))
        {
          throw new KeyNotFoundException($"Element ID {elementID} does not exist.");
        }

        ElementAttribute eleAttribute = elements[elementID];
        int propertyID = eleAttribute.PropertyID;

        var localAxis = eleAttribute.LocalAxis;
        var copyNodeIDs = new List<int>(eleAttribute.NodeIDs);
        int newNodeA, newNodeB;

        // 기존 노드 ID를 새로운 노드 ID로 변환하는 과정
        if (!newNodeConnection_dict.ContainsKey(copyNodeIDs[0]))
        {
          Point3D Position = Nodes[copyNodeIDs[0]];
          newNodeA = Nodes.AddOrGet(Position.X + X, Position.Y + Y, Position.Z + Z);
          newNodeConnection_dict[copyNodeIDs[0]] = newNodeA;
        }
        else
        {
          newNodeA = newNodeConnection_dict[copyNodeIDs[0]];
        }

        if (!newNodeConnection_dict.ContainsKey(copyNodeIDs[1]))
        {
          Point3D Position = Nodes[copyNodeIDs[1]];
          newNodeB = Nodes.AddOrGet(Position.X + X, Position.Y + Y, Position.Z + Z);
          newNodeConnection_dict[copyNodeIDs[1]] = newNodeB;
        }
        else
        {
          newNodeB = newNodeConnection_dict[copyNodeIDs[1]];  // 여기서 copyNodeIDs[1]을 참조해야 함
        }

        // 새로운 Element 추가
        int newElementID = AddNew(new List<int> { newNodeA, newNodeB }, propertyID, localAxis);
        newElementID_list.Add(newElementID);
      }

      // 수평 연결 요소 업데이트
      foreach (var entry in newNodeConnection_dict)
      {
        HorizontalConnectionElements_list.Add(new List<int> { entry.Key, entry.Value });
      }

      return newElementID_list;
    }


    // 특정 노드 2개가 연결된 요소를 찾아서 ID 반환
    public int FindElementByNodeIDs(int nodeID1, int nodeID2)
    {
      // 노드 ID들을 정렬하여 순서가 달라도 동일하게 처리할 수 있도록 함
      var sortedNodeIDs = new List<int> { nodeID1, nodeID2 };
      sortedNodeIDs.Sort();

      // 요소들을 순회하며, 노드 ID들이 일치하는 요소를 찾음
      foreach (var element in elements)
      {
        var elementNodeIDs = element.Value.NodeIDs;
        var sortedElementNodeIDs = new List<int>(elementNodeIDs);
        sortedElementNodeIDs.Sort();

        if (sortedElementNodeIDs.SequenceEqual(sortedNodeIDs))
        {
          return element.Key; // 첫 번째로 일치하는 요소 ID 반환
        }
      }

      throw new KeyNotFoundException("해당하는 요소를 찾을 수 없습니다.");
    }

    public List<int> SubdivideElementsBySize(double meshSize)
    {
      var newElementIDs = new List<int>();
      var originalElements = elements.ToList(); // 복사본

      foreach (var kvp in originalElements)
      {
        int elementID = kvp.Key;
        var attr = kvp.Value;
        var nodeIDs = attr.NodeIDs;

        if (nodeIDs.Count != 2) continue;

        Point3D pt1 = Nodes[nodeIDs[0]];
        Point3D pt2 = Nodes[nodeIDs[1]];

        double totalLength = Math.Sqrt(
            Math.Pow(pt2.X - pt1.X, 2) +
            Math.Pow(pt2.Y - pt1.Y, 2) +
            Math.Pow(pt2.Z - pt1.Z, 2));

        int divisions = (int)Math.Ceiling(totalLength / meshSize);
        if (divisions <= 1) continue;

        // 방향 벡터 계산
        double dx = (pt2.X - pt1.X) / divisions;
        double dy = (pt2.Y - pt1.Y) / divisions;
        double dz = (pt2.Z - pt1.Z) / divisions;

        // 기존 요소 제거
        Remove(elementID);

        int prevNodeID = nodeIDs[0];
        int propertyID = attr.PropertyID;
        double[] localAxis = attr.LocalAxis;
        var extraData = attr.ExtraData;

        for (int i = 1; i <= divisions; i++)
        {
          double newX = pt1.X + dx * i;
          double newY = pt1.Y + dy * i;
          double newZ = pt1.Z + dz * i;

          int newNodeID = (i == divisions) ? nodeIDs[1] : Nodes.AddOrGet(newX, newY, newZ);
          int newElementID = AddNew(new List<int> { prevNodeID, newNodeID }, propertyID, localAxis, extraData);
          newElementIDs.Add(newElementID);

          prevNodeID = newNodeID;
        }
      }

      return newElementIDs;
    }





    // 요소 ID 기반 전체 요소 리스트 반환
    public List<KeyValuePair<int, ElementAttribute>> GetAllElements()

    {
      return new List<KeyValuePair<int, ElementAttribute>>(elements);
    }

    // IEnumerable<ElementAttribute> 인터페이스 구현 (foreach 지원)
    public IEnumerator<KeyValuePair<int, ElementAttribute>> GetEnumerator()
    {
      return elements.GetEnumerator();
    }

    // IEnumerable 인터페이스 구현 (비제네릭 버전)
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}

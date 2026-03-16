using BeamStructureSolution.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeModelGenerator.Control
{
  public class BdfBuilder
  {
    public int sol;
    public Materials materialInstance;
    public Properties propertyInstance;
    public Nodes nodeInstance;
    public Elements elementInstance;
    Dictionary<int, string> BoundaryContion_dict = new Dictionary<int, string>();
    Dictionary<int, double> Load_dict = new Dictionary<int, double>();

    // BDF에 출력된 텍스트 모음 리스트 
    public List<String> BdfLines = new List<String>();

    public BdfBuilder(
      int Sol, Materials materialInstance, Properties propertyInstance,
      Nodes nodeInstance, Elements elementInstance, Dictionary<int, string> BoundaryContion_dict,
      Dictionary<int, double> load_dict)
    {
      this.sol = Sol;
      this.materialInstance = materialInstance;
      this.propertyInstance = propertyInstance;
      this.nodeInstance = nodeInstance;
      this.elementInstance = elementInstance;
      this.BoundaryContion_dict = BoundaryContion_dict;
      this.Load_dict = load_dict;
    }

    public void Run()
    {
      // 01. 해석 종류 설정
      ExecutiveControlSection();

      // 02. 출력결과 종류 설정, 하중, 경계조건 ID 설정
      CaseControlSection();

      // 03. Node, Element 데이터 입력
      NodeElementSection();

      // 04. Property, Material 데이터 입력
      PropertyMaterialSection();

      // 05. 경계조건 구현
      SpcSection();

      // 06. 하중조건 구현
      //LoadSection("GRAV", new double[] { 1.0, 0.0, -1.2 });
      LoadSection();
      EndData();

      //foreach (var line in this.elementInstance)
      //{
      //  Console.WriteLine(line);
      //}

    }

    public void ExecutiveControlSection()
    {
      BdfLines.Add(FormatField($"SOL {this.sol}"));
      BdfLines.Add(FormatField($"CEND"));
    }

    public void CaseControlSection()
    {
      BdfLines.Add("DISPLACEMENT = ALL");
      BdfLines.Add("FORCE = ALL");
      BdfLines.Add("SPCFORCES = ALL");
      BdfLines.Add("STRESS = ALL");
      BdfLines.Add("SUBCASE 1");
      BdfLines.Add("    ANALYSIS = STATICS");
      BdfLines.Add("    LABEL = Load Case 1");
      BdfLines.Add("    SPC = 1");
      BdfLines.Add("    LOAD = 2");
      BdfLines.Add("    ANALYSIS = STATICS");
      BdfLines.Add("BEGIN BULK");
      BdfLines.Add("PARAM,POST,-1");
    }

    public void NodeElementSection()
    {
      foreach (var node in this.nodeInstance)
      {
        string nodeText = $"{FormatField("GRID")}"
          + $"{FormatField(node.Key, "right")}"
          + $"{FormatField("")}"
          + $"{FormatField(node.Value.X, "right")}"
          + $"{FormatField(node.Value.Y, "right")}"
          + $"{FormatField(node.Value.Z, "right")}";
        BdfLines.Add(nodeText);
      }

      foreach (var element in this.elementInstance)
      {
        string elementText = $"{FormatField("CBEAM")}"
         + $"{FormatField(element.Key, "right")}"
         + $"{FormatField(element.Value.PropertyID, "right")}"
         + $"{FormatField(element.Value.NodeIDs[0], "right")}"
         + $"{FormatField(element.Value.NodeIDs[1], "right")}"
         + $"{FormatField(element.Value.LocalAxis[0], "right")}"
         + $"{FormatField(element.Value.LocalAxis[1], "right")}"
         + $"{FormatField(element.Value.LocalAxis[2], "right")}"
         + $"{FormatField("BGG", "right")}";
        BdfLines.Add(elementText);
      }
    }

    public void PropertyMaterialSection()
    {
      foreach (var property in this.propertyInstance)
      {
        // Angle 형태
        if (property.Value.Type == "L")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("L", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(property.Value.Dim[1], "right")}"
            + $"{FormatField(property.Value.Dim[2], "right")}"
            + $"{FormatField(property.Value.Dim[3], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // Bar 형태
        else if (property.Value.Type == "BAR")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("BAR", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(property.Value.Dim[1], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // T 형태
        else if (property.Value.Type == "T")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("T", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(property.Value.Dim[1], "right")}"
            + $"{FormatField(property.Value.Dim[2], "right")}"
            + $"{FormatField(property.Value.Dim[3], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // H 형태
        else if (property.Value.Type == "H")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("H", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(property.Value.Dim[1], "right")}"
            + $"{FormatField(property.Value.Dim[2], "right")}"
            + $"{FormatField(property.Value.Dim[3], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // CHAN 형태
        else if (property.Value.Type == "CHAN")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("CHAN", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(property.Value.Dim[1], "right")}"
            + $"{FormatField(property.Value.Dim[2], "right")}"
            + $"{FormatField(property.Value.Dim[3], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // Tube 형태
        else if (property.Value.Type == "TUBE")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("TUBE", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(property.Value.Dim[1], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // Rod 형태
        else if (property.Value.Type == "ROD")
        {
          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("ROD", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(property.Value.Dim[0], "right")}"
            + $"{FormatField(0.0, "right")}";
          BdfLines.Add(propertyText);
        }

        // Beam 형태
        else if (property.Value.Type == "BEAM")
        {
          // Nastran 입력 형식에 맞게 Dimension 수정
          double[] originDim = new double[4] { property.Value.Dim[0],
          property.Value.Dim[1], property.Value.Dim[2], property.Value.Dim[3]};
          double[] DimRevised = new double[4];
          DimRevised[0] = (originDim[0] - (originDim[3] * 2));
          DimRevised[1] = originDim[3] * 2;
          DimRevised[2] = originDim[1];
          DimRevised[3] = originDim[3];

          string propertyText = $"{FormatField("PBEAML")}"
            + $"{FormatField(property.Key, "right")}"
            + $"{FormatField(property.Value.MaterialID, "right")}"
            + $"{FormatField("", "right")}"
            + $"{FormatField("H", "right")}";
          BdfLines.Add(propertyText);

          propertyText = $"{FormatField("")}"
            + $"{FormatField(DimRevised[0], "right")}"
            + $"{FormatField(DimRevised[1], "right")}"
            + $"{FormatField(DimRevised[2], "right")}"
            + $"{FormatField(DimRevised[3], "right")}";
          BdfLines.Add(propertyText);
        }
      }

      foreach (var material in this.materialInstance)
      {
        string materialText = $"{FormatField("MAT1")}"
            + $"{FormatField(material.Key, "right")}"
            + $"{FormatField(material.Value.E, "right")}"
            + $"{FormatField("")}"
            + $"{FormatField(material.Value.Nu, "right")}"
            + $"{FormatField(material.Value.Rho, "right", true)}";

        BdfLines.Add(materialText);

      }

    }

    public void SpcSection()
    {
      foreach (var bound in BoundaryContion_dict)
      {
        string condition = "";
        if (bound.Value == "Fix")
        {
          condition = $"{FormatField(123456, "right")}";
        }
        else
        {
          condition = $"{FormatField(12346, "right")}";
        }

        string boundText = $"{FormatField("SPC")}"
          + $"{FormatField("1", "right")}"
          + $"{FormatField(bound.Key, "right")}"
          + condition
          + $"{FormatField(0.0, "right")}";

        BdfLines.Add(boundText);
      }
    }


    //public void LoadSection(string type, double[] Value)
    //{
    //  if (type == "GRAV")
    //  {
    //    string loadText = $"{FormatField(type)}"
    //       + $"{FormatField("2", "right")}"
    //       + $"{FormatField("")}"
    //       + $"{FormatField(9800.0, "right")}"
    //       + $"{FormatField(Value[0], "right")}"
    //       + $"{FormatField(Value[1], "right")}"
    //       + $"{FormatField(Value[2], "right")}";

    //    BdfLines.Add(loadText);
    //  }
    //}

    public void LoadSection()
    {
      foreach(var loadInfo in Load_dict)
      {
        string loadText = $"{FormatField("FORCE")}"
          + $"{FormatField("2", "right")}" 
          + $"{FormatField(loadInfo.Key, "right")}"
          + $"{FormatField(0, "right")}"
          + $"{FormatField(1.0, "right")}"
          + $"{FormatField(0.0, "right")}"
          + $"{FormatField(0.0, "right")}"
          + $"{FormatField(loadInfo.Value, "right")}";

        BdfLines.Add(loadText);
      }     
    }

    public void EndData()
    {
      BdfLines.Add("ENDDATA");
    }

    // 하나의 자열을 8칸에 넣어서 문자열을 반환하는 메써드
    public string FormatField(string data, string direction = "left")
    {
      if (direction == "right")
      {
        return data.PadLeft(8).Substring(0, 8);
      }

      return data.PadRight(8).Substring(0, 8);
    }

    // int 지원
    public string FormatField(int data, string direction = "left")
    {
      return FormatField(data.ToString(), direction);
    }

    public string FormatField(double data, string direction = "left", bool isRho = false)
    {
      if (isRho)
      {
        return FormatField(ConvertScientificNotation(data), direction);
      }
      return FormatField(data.ToString("0.00"), direction);  // 기본적으로 소수점 2자리
    }

    // 지수 표기법 변환 (E-표기법 → "-지수" 형태)
    private string ConvertScientificNotation(double data)
    {
      string scientific = data.ToString("0.00E+0");  // "7.85E-09" 형식
      if (scientific.Contains("E"))
      {
        string[] parts = scientific.Split('E'); // ["7.85", "-09"]
        return $"{parts[0]}{int.Parse(parts[1])}"; // "7.85-9"
      }
      return scientific;
    }

  }
}

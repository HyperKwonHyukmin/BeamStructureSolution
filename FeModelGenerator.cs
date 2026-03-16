using BeamStructureSolution.Model;
using FeModelGenerator.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;
using System.Diagnostics;

namespace BeamStructureSolution.Control
{
  public class FeModelGenerator
  {
    Materials materialInstance;
    Properties propertyInstance;
    public Nodes nodeInstance;
    Elements elementInstance;

    public string beamType;
    public double length;
    List<double> dimList;
    List<(double pos, string constraint)> boundaryConditions;
    List<(double pos, double mag)> loads;
    string selectedFolderPath;

    // 차후 참조할 딕셔너리 생성
    Dictionary<int, string> BoundaryContion_dict = new Dictionary<int, string>();
    Dictionary<int, double> Load_dict = new Dictionary<int, double>();

    public FeModelGenerator(
      string beamType,
      double length,
      List<double> dimList,
      List<(double pos, string constraint)> boundaryConditions,
      List<(double pos, double mag)> loads,
      Materials materialInstance,
      Properties propertyInstance,
      Nodes nodeInstance,
      Elements elementInstance,
      string selectedFolderPath)
    {
      this.beamType = beamType;
      this.length = length;
      this.dimList = dimList;
      this.boundaryConditions = boundaryConditions;
      this.loads = loads;
      this.materialInstance = materialInstance;
      this.propertyInstance = propertyInstance;
      this.nodeInstance = nodeInstance;
      this.elementInstance = elementInstance;
      this.selectedFolderPath = selectedFolderPath;
    }

    private void Log(string message)
    {
      string logPath = Path.Combine(selectedFolderPath, "FeModelGeneratorLog.txt");
      using (StreamWriter writer = new StreamWriter(logPath, append: true))
      {
        writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
      }
    }

    //public string Run()
    public (string, FeModelGenerator) Run()
    {
      // 유한요소 모델 생성
      ModelProcessor();

      // BDF 파일 출력하기 
      string BdfFullPath = BdfProcessor();

      // Nastran 해석 수행하기 
      //RunCommandAndWait("nastran", BdfFullPath);

      //string f06File = BdfFullPath.Replace(".bdf", ".f06");

      //return f06File;

      // 해석 결과 f06 파싱하기 
      //string f06File = BdfFullPath.Replace(".bdf", ".f06");
      //string f06File = @"C:\temp\AM_FEA\20250415_131955.f06";
      return (BdfFullPath, this);

    }

    public void ModelProcessor()
    {
      int BcNodeID = 0;
      int LoadNodeID = 0;
      List<int> NodeSequence = new List<int>();

      // zeor Node 생성
      int zeroNodeID = nodeInstance.AddOrGet(0.0, 0.0, 0.0);
      NodeSequence.Add(zeroNodeID);


      // 경계 조건 해당 Node 생성
      foreach (var bc in boundaryConditions)
      {
        BcNodeID = nodeInstance.AddOrGet(bc.pos, 0.0, 0.0);
        BoundaryContion_dict[BcNodeID] = bc.constraint;
        NodeSequence.Add(BcNodeID);
      }

      // 하중 조건 해당 Node 생성
      foreach (var load in loads)
      {
        LoadNodeID = nodeInstance.AddOrGet(load.pos, 0.0, 0.0);
        Load_dict[LoadNodeID] = load.mag;
        NodeSequence.Add(LoadNodeID);
      }

      // 최종 Node 추가 
      int lastNodeID = nodeInstance.AddOrGet(length, 0.0, 0.0);
      NodeSequence.Add(lastNodeID);

      NodeSequence = NodeSequence.Distinct().ToList();

      // NodeSequence를 X 좌표 기준으로 정렬
      NodeSequence = NodeSequence
        .OrderBy(id => nodeInstance.GetNodeCoordinates(id).X)
        .ToList();

      // material Mild Steel 생성
      int materialID = materialInstance.AddOrGet(206000, 0.03, 7.85e-09);

      double[] dimArray = dimList.ToArray();

      // property 생성
      int propertyID = propertyInstance.AddOrGet(beamType, dimArray, materialID);

      // element 생성
      for (int i = 0; i < NodeSequence.Count - 1; i++)
      {
        int startNode = NodeSequence[i];
        int endNode = NodeSequence[i + 1];

        var nodeIDs = new List<int> { startNode, endNode };
        int elementID = elementInstance.AddNew(nodeIDs, 
          propertyID, 
          new double[] { 0.0, 0.0, 1.0 });
      }

      // mesh size로 나누기 
      var newElements = elementInstance.SubdivideElementsBySize(100.0);

    }

    public string BdfProcessor()
    {
      var bdfBuilder = new BdfBuilder(
        101, materialInstance, propertyInstance, nodeInstance,
        elementInstance, BoundaryContion_dict, Load_dict);
      bdfBuilder.Run();

      string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string fileName = $"{timestamp}.bdf";
      string BdfFullPath = Path.Combine(selectedFolderPath, fileName);

      File.WriteAllLines(BdfFullPath, bdfBuilder.BdfLines);

      return BdfFullPath;
    }

    public void RunCommandAndWait(string command, string arguments)
    {
      // 작업 위 변경
      string wokingFolder = Path.GetDirectoryName(arguments);
      Environment.CurrentDirectory = wokingFolder;

      ProcessStartInfo psi = new ProcessStartInfo
      {
        FileName = command,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using (Process process = new Process())
      {
        process.StartInfo = psi;
        process.Start();
        process.WaitForExit();  
      }
    }    

  }
}

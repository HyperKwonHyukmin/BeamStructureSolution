using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeamStructureSolution.Control;
using BeamStructureSolution.Model;



namespace BeamStructureSolution.Control
{
  public class BeamStructureSolutionRun
  {
    public string beamType;
    public double length;
    List<double> dimList;
    List<(double pos, string constraint)> boundaryConditions;
    List<(double pos, double mag)> loads;
    List<(double pos, double shearValue)> resultShear;
    List<(double pos, double momentValue)> resultMoment;
    string selectedFolderPath;

    // UpdateStatus 전달을 위해
    public Action<string> OnStatusUpdate;


    public BeamStructureSolutionRun(
      string beamType, 
      double length,
      List<double> dimList, 
      List<(double pos, string constraint)> boundaryConditions, 
      List<(double pos, double mag)> loads, 
      string selectedFolderPath)
    {
      this.beamType = beamType;
      this.length = length;
      this.dimList = dimList;
      this.boundaryConditions = boundaryConditions;
      this.loads = loads;
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

    //public (
    //    List<(double pos, double startShear, double endShear)>,
    //    List<(double pos, double startMoment, double endMoment)>,
    //    (double? Area, double? I) SectionProperty,
    //    (int eleID, double stress) maxStressItem,
    //    (int eleID, double displacement) maxDisplacement
    //  ) Run()
    public (string, FeModelGenerator) Run()
    {



      //FE Model 구축
      Materials materialInstance = new Materials();
      Properties propertyInstance = new Properties(materialInstance);
      Nodes nodeInstance = new Nodes();
      Elements elementInstance = new Elements(nodeInstance, propertyInstance);

    // 모델 생성 및 해석 수행 
    //OnStatusUpdate?.Invoke("Nastran 구조 해석 수행 중입니다...");
    FeModelGenerator modelGenerator = new FeModelGenerator(
          beamType, length, dimList, boundaryConditions, loads,
          materialInstance, propertyInstance, nodeInstance, elementInstance, selectedFolderPath);

      (string BdfFullPath, FeModelGenerator updatedModelGenerator) = modelGenerator.Run();
      //string f06File = modelGenerator.Run();

      // 결과 처리
      //(resultShear, resultMoment, sectionProperty, maxStressItem, maxDisplacement) =
      //    AnalysisResultProcessor(f06File, nodeInstance);

      //return (resultShear, resultMoment, sectionProperty, maxStressItem, maxDisplacement);
      return (BdfFullPath, modelGenerator);
    }


    public static (
    List<(double pos, double startShear, double endShear)>,
    List<(double pos, double startMoment, double endMoment)>, 
      (double? Area, double? I) SectionProperty,
      (int eleID, double stress) maxStressItem,
      (int eleID, double displacement) maxDisplacement
      ) AnalysisResultProcessor(string f06File, Nodes nodeInstance)
    {
      string[] lines = File.ReadAllLines(f06File);

      // 부재의 Area와 I값을 가지고 오기
      var beamForces = F06Parser.ParseBeamElementForces(lines);
      (double? Area, double? I) SectionProperty = F06Parser.ParseSectionProperty(lines);

      // 부재의 응력 가지고 오기 
      List<(int eleID, double stress)> stressResults = F06Parser.ParseBeamElementStresses(lines);
      var maxStressItem = stressResults.OrderByDescending(x => Math.Abs(x.stress)).First();

      // 변위 정보 가지고 오기 
      List<(int nodeID, double displacement)> disResults = F06Parser.ParseDisplacement(lines);
      var maxDisplacement = disResults.OrderByDescending(d => Math.Abs(d.displacement)).First();


      // 결과 리스트 (시작점과 종료점 구분)
      var shearResult = new List<(double pos, double startShear, double endShear)>();
      var momentResult = new List<(double pos, double startMoment, double endMoment)>();

      // 1. x 좌표 그룹화
      var groupedByX = beamForces
          .SelectMany(beam => beam.Values)
          .GroupBy(item => nodeInstance[item.Grid].X);

      foreach (var group in groupedByX)
      {
        double x = group.Key;
        var itemList = group.ToList();

        var first = itemList.First();
        var last = itemList.Last();

        // 저장 (시작점, 종료점)
        shearResult.Add((x, -first.S1, -last.S1));
        momentResult.Add((x, first.BM1, last.BM1));
      }

      return (shearResult, momentResult, SectionProperty, maxStressItem, maxDisplacement);
    }
  }


}

using BeamStructureSolution.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BeamStructureSolution.Control
{
  public static class F06Parser
  {
    public static List<BeamElementForce> ParseBeamElementForces(string[] lines)
    {
      var result = new List<BeamElementForce>();

      // 공백 제거 후 찾기
      int start = Array.FindIndex(lines, l => l.Replace(" ", "").Contains("FORCESINBEAMELEMENTS"));
      if (start == -1)
      {
        return result;
      }
      for (int i = start + 1; i < lines.Length; i++)
      {
        string line = lines[i].Trim();

        // 요소 ID 줄: "0    1234" 형식
        if (line.StartsWith("0") && line.Length > 2)
        {
          var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
          if (parts.Length < 2) continue;

          if (!int.TryParse(parts[1], out int elementID))
            continue;

          var beam = new BeamElementForce { ElementID = elementID };

          for (int j = 1; j <= 2 && i + j < lines.Length; j++)
          {
            var rowParts = lines[i + j].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (rowParts.Length < 9) continue;

            if (int.TryParse(rowParts[0], out int grid) &&
                double.TryParse(rowParts[1], out double len) &&
                double.TryParse(rowParts[2], out double bm1) &&
                double.TryParse(rowParts[3], out double bm2) &&
                double.TryParse(rowParts[4], out double s1) &&
                double.TryParse(rowParts[5], out double s2) &&
                double.TryParse(rowParts[6], out double axial) &&
                double.TryParse(rowParts[7], out double torque) &&
                double.TryParse(rowParts[8], out double warping))
            {
              beam.Values.Add(new BeamForceRow
              {
                Grid = grid,
                Length = len,
                BM1 = bm1,
                BM2 = bm2,
                S1 = s1,
                S2 = s2,
                Axial = axial,
                Torque = torque,
                Warping = warping
              });
            }
          }

          if (beam.Values.Count > 0)
          {
            result.Add(beam);
            i += 2; // force row 2줄 건너뛰기
          }
        }
      }
      return result;
    }

    public static List<(int eleID, double stress)> ParseBeamElementStresses(string[] lines)
    {
      List<(int eleID, double stress)> result = new();
      bool inStressSection = false;
      int currentElementId = -1;

      foreach (string line in lines)
      {
        // 스트레스 구간 시작 감지
        if (Regex.IsMatch(line, @"S\s*T\s*R\s*E\s*S\s*S\s*E\s*S\s+.*B\s*E\s*A\s*M\s+.*E\s*L\s*E\s*M\s*E\s*N\s*T\s*S", RegexOptions.IgnoreCase))
        {
          inStressSection = true;
          continue;
        }

        if (inStressSection)
        {
          // ELEMENT ID 줄
          if (Regex.IsMatch(line, @"^0\s+\d+"))
          {
            var match = Regex.Match(line, @"^0\s+(\d+)");
            currentElementId = int.Parse(match.Groups[1].Value);
          }
          // Grid 응력 값 줄
          else if (Regex.IsMatch(line, @"^\s+\d+\s+[\d\.\-E\+]+"))
          {
            var tokens = Regex.Split(line.Trim(), @"\s+");
            if (tokens.Length >= 8 && currentElementId != -1)
            {
              try
              {
                double smax = double.Parse(tokens[6]);
                double smin = double.Parse(tokens[7]);
                double maxAbsStress = Math.Abs(smax) >= Math.Abs(smin) ? smax : smin;
                result.Add((currentElementId, Math.Round(maxAbsStress,1)));
              }
              catch (Exception ex)
              {
                Console.WriteLine($"[파싱 오류] {line} : {ex.Message}");
              }
            }
          }
          // 섹 종료 감지
          else if (line.StartsWith(" ***") || line.StartsWith("1 "))
          {
            break;
          }
        }
      }

      return result;
    }

    public static List<(int nodeID, double displacement)> ParseDisplacement(string[] lines)
    {
      var resultDis = new List<(int, double)>();
      bool inDisplacementSection = false;
      string displacementHeader = "D I S P L A C E M E N T   V E C T O R";

      foreach (var line in lines)
      {
        if (line.Contains(displacementHeader))
        {
          inDisplacementSection = true;
          continue;
        }

        // 다음 섹션 시작되면 종료
        if (inDisplacementSection && line.Trim().StartsWith("F O R C E S"))
        {
          break;
        }

        if (inDisplacementSection)
        {
          // 숫자 라인 파싱
          var match = Regex.Match(line, @"^\s*(\d+)\s+G\s+([-\d.E+]+)\s+([-\d.E+]+)\s+([-\d.E+]+)");

          if (match.Success)
          {
            int nodeID = int.Parse(match.Groups[1].Value);
            double t3 = Math.Round(Math.Abs(double.Parse(match.Groups[4].Value)), 1);
            resultDis.Add((nodeID, t3));
          }
        }
      }

      return resultDis;
    }

    public static List<SpcForce> ParseSpcForces(string[] lines)
    {
      var result = new List<SpcForce>();

      int start = Array.FindIndex(lines, l => l.Replace(" ", "").Contains("SPCFORCES"));
      if (start == -1)
      {
        return result;
      }

      for (int i = start + 1; i < lines.Length; i++)
      {
        string line = lines[i].Trim();
        if (line.Length < 10 || !char.IsDigit(line[0])) continue;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 &&
            int.TryParse(parts[0], out int id) &&
            int.TryParse(parts[1], out int dof) &&
            double.TryParse(parts[2], out double val))
        {
          result.Add(new SpcForce
          {
            ID = id,
            DOF = dof,
            Value = val
          });
        }
      }

      return result;
    }


    public static (double? Area, double? I) ParseSectionProperty(string[] lines)
    {
      foreach (var line in lines)
      {
        if (line.TrimStart().StartsWith("PBEAM"))
        {
          var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
          if (parts.Length >= 5 &&
              double.TryParse(parts[3], out double area) &&
              double.TryParse(parts[4], out double i1))
          {
            return (area, i1); // 필요 시 i1, i2 평균으로 변경 가능
          }
        }
      }
      return (null, null);
    }
    


  }

}

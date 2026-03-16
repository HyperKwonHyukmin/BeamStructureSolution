using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeamStructureSolution.Model
{
  public class BeamElementForce
  {
    public int ElementID { get; set; }
    public List<BeamForceRow> Values { get; set; } = new();
  }

  public class BeamForceRow
  {
    public int Grid { get; set; }
    public double Length { get; set; }
    public double BM1 { get; set; }
    public double BM2 { get; set; }
    public double S1 { get; set; }
    public double S2 { get; set; }
    public double Axial { get; set; }
    public double Torque { get; set; }
    public double Warping { get; set; }
  }

  public class BeamElementStress
  {
    public int ElementID { get; set; }
    public List<BeamStressRow> Values { get; set; } = new();
  }

  public class BeamStressRow
  {
    public int eleID { get; set; }
    public int Grid { get; set; }
    public double Length { get; set; }
    public double SXC { get; set; }
    public double SXD { get; set; }
    public double SXE { get; set; }
    public double SXF { get; set; }
    public double SMax { get; set; }
    public double SMin { get; set; }
  }

  public class DisplacementResult
  {
    public int NodeID { get; set; }
    public double T1 { get; set; }
    public double T2 { get; set; }
    public double T3 { get; set; }
    public double R1 { get; set; }
    public double R2 { get; set; }
    public double R3 { get; set; }
  }



  public class SpcForce
  {
    public int ID { get; set; }
    public int DOF { get; set; }
    public double Value { get; set; }
  }



}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeamStructureSolution.Model
{
  // 속성 정의 (유형 및 치수)
  public struct PropertyAttribute
  {
    public string Type { get; }
    public double[] Dim { get; } // 변경: Dictionary<string, double> → double[]
    public int MaterialID { get; }

    public PropertyAttribute(string type, double[] dim, int materialID)
    {
      Type = type;
      Dim = dim; // 직접 배열로 받음
      MaterialID = materialID;
    }

    public override string ToString()
    {
      return $"Type: {Type}, Dimensions: [{string.Join(", ", Dim)}], MaterialID: {MaterialID}";
    }
  }

  public class Properties : IEnumerable<KeyValuePair<int, PropertyAttribute>>
  {
    public Materials Material { get; private set; }
    private int propertyID = 0;
    private Dictionary<int, PropertyAttribute> properties = new Dictionary<int, PropertyAttribute>();
    private Dictionary<string, int> propertyLookup = new Dictionary<string, int>();

    public Properties(Materials material)
    {
      Material = material;
    }

    public PropertyAttribute this[int propertyID]
    {
      get
      {
        if (!properties.TryGetValue(propertyID, out PropertyAttribute property))
        {
          throw new KeyNotFoundException($"Property ID {propertyID} does not exist.");
        }
        return property;
      }
    }

    public int AddOrGet(string type, double[] dim, int materialID)
    {
      string key = $"{type}|{string.Join(";", dim)}|{materialID}";

      if (propertyLookup.TryGetValue(key, out int existingPropertyID))
      {
        return existingPropertyID;
      }

      propertyID++;
      PropertyAttribute newProperty = new PropertyAttribute(type, dim, materialID);
      properties[propertyID] = newProperty;
      propertyLookup[key] = propertyID;

      return propertyID;
    }

    public void Remove(int inputPropertyID)
    {
      if (!properties.TryGetValue(inputPropertyID, out PropertyAttribute removedProperty))
      {
        throw new KeyNotFoundException($"Property ID {inputPropertyID} does not exist.");
      }

      string key = $"{removedProperty.Type}|{string.Join(";", removedProperty.Dim)}|{removedProperty.MaterialID}";
      properties.Remove(inputPropertyID);
      propertyLookup.Remove(key);
    }

    public int GetLastID()
    {
      return propertyID > 0 ? propertyID : -1;
    }

    public int GetCount()
    {
      return properties.Count;
    }

    public PropertyAttribute GetProperty(int inputPropertyID)
    {
      if (!properties.TryGetValue(inputPropertyID, out PropertyAttribute property))
      {
        throw new KeyNotFoundException($"Property with ID {inputPropertyID} does not exist.");
      }
      return property;
    }

    public double GetMaxDimension(int inputPropertyID)
    {
      double maxDim;

      if (!properties.ContainsKey(inputPropertyID))
        throw new KeyNotFoundException($"Property ID {inputPropertyID} does not exist.");

      if (properties[inputPropertyID].Type == "TUBE")
        maxDim = properties[inputPropertyID].Dim.Max() / 2;
      else
        maxDim = properties[inputPropertyID].Dim.Max();

      return maxDim; // 가장 큰 치수 반환
    }

    public double GetMaxDimensionTotal()
    {
      double maxValue = double.MinValue;

      foreach (var kv in this)
      {
        var dims = kv.Value.Dim;
        if (dims != null && dims.Count() > 0) // Count → Count() 로 수정
        {
          double localMax = dims.Max();
          if (localMax > maxValue)
            maxValue = localMax;
        }
      }

      return maxValue == double.MinValue ? 0.0 : maxValue;
    }




    public IEnumerator<KeyValuePair<int, PropertyAttribute>> GetEnumerator()
    {
      return properties.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}

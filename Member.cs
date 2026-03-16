using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BeamStructureSolution.Utils
{
  public class MemberLoginResult
  {
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("userID")]
    public string UserID { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; }

    [JsonPropertyName("userDept")]
    public string UserDept { get; set; }

    [JsonPropertyName("userPos")]
    public string UserPos { get; set; }

    [JsonPropertyName("userCompany")]
    public string UserCompany { get; set; }
  }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatumNode
{
  public class Parameter
  { 
    public string parameter_name { get; set; }
    public string parameter_value { get; set; }
  }
  public static class Script
  {
    public static void Run(DatumNodeService datumnode, string equipment_serial, out System.Collections.Generic.List<DatumNode.Parameter> result)
    {
      if (equipment_serial == null)
        throw new Exception("equipment_serial is empty");

      result = new List<DatumNode.Parameter>();

      try
      {
        var resultGetCpe = datumnode.ExecuteQuery("*.*.gs_api.store_api.Api.get_cpe", new Dictionary<string, object>()
        {
          { "sn", equipment_serial }
        });

        int? resultId = Int32.Parse(resultGetCpe["result"].ToString());
        var result_text = resultGetCpe["result_text"] as string;
        
        if (resultId.HasValue == false || resultId.HasValue == true && resultId.Value < 1)
          throw new Exception(result_text);

        int? cpeId = Int32.Parse(resultGetCpe["result_num"].ToString());

        var cpeInfoList = datumnode.ExecuteQuery("*.*.ossResource.sltu.common.getcpeparams", new Dictionary<string, object>()
        {
          { "cpeid", cpeId }
        }).Elements.Where(x => x.Name == "Entity" && !x.Element("descriptionname").ToString().Contains("Сервисные модели")).Select(x =>
        {
          return new Parameter()
          {
            parameter_name = (string)x.Element("descriptionname"),
            parameter_value = (string)x.Element("descriptionvalue")
          };
        }).ToList();

        var cpeInfo = new DatumNode.Parameter
        {
          parameter_name = "Идентификатор в Склад CPE",
          parameter_value = cpeId.Value.ToString()
        };

        result.Add(cpeInfo);
        result.AddRange(cpeInfoList);
      }
      catch (Exception e)
      {
        throw new Exception(e.Message);
      }
    }
  }
}

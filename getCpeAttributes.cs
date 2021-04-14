using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DatumNode
{
  public class Parameter
  { 
    public string parameter_name { get; set; }
    public string parameter_value { get; set; }
    public Parameter(string name, string val)
    {
      parameter_name = name;
      parameter_value = val;
    }
  }
  public static class Script
  {
    public static void Run(DatumNodeService datumnode, string equipment_serial, string equipment_prov_method, out System.Collections.Generic.List<Parameter> result)
    {
      if (equipment_serial == null)
        throw new Exception("equipment_serial is empty");

      result = new List<Parameter>();

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

        var cpeInfoSet = datumnode.ExecuteQuery("*.*.gs_api.store_api.Api.get_cpe_info", new Dictionary<string, object>()
        {
          {"sn", equipment_serial},
          {"comm_id",  cpeId}
        });

        resultId = Int32.Parse(cpeInfoSet["result"].ToString());
        result_text = cpeInfoSet["result_text"] as string;
        if (resultId.HasValue == false || resultId.HasValue == true && resultId.Value < 1)
          throw new Exception(result_text);

        var xDocXml = cpeInfoSet["xml"] as string;
        var xDoc = XDocument.Parse(xDocXml);
        var cpe = xDoc.Descendants("cpe").First();

        const string cpeTypeNameTag = "TYPDEVICE_NAME";
        const string cpeModelTag = "MARKACOMM_NAME";
        const string cpeVendorTag = "VENDOR_NAME";
        const string cpeMacAddressTag = "MAC_ADDRESS";
        const string cpePonSerialTag = "PON_SERIAL";
        const string cpeConditionTag = "DEV_CONDITION_NAME";
        const string cpeTransferConditionTag = "TRANSFER_CONDITION_NAME";
        const string cpeExploitStatusTag = "EXPLOIT_STATUS_NAME";

        if (cpe == null)
          return;

        if (cpe.Element(cpeTypeNameTag) != null)
          result.Add(new Parameter("Тип оборудования", (string)cpe.Element(cpeTypeNameTag)));

        if (cpe.Element(cpeModelTag) != null)
          result.Add(new Parameter("Модель оборудования", (string)cpe.Element(cpeModelTag)));

        if (cpe.Element(cpeVendorTag) != null)
          result.Add(new Parameter("Производитель", (string)cpe.Element(cpeVendorTag)));

        if (cpe.Element(cpeMacAddressTag) != null)
          result.Add(new Parameter("MAC-адрес", (string)cpe.Element(cpeMacAddressTag)));

        if (cpe.Element(cpePonSerialTag) != null)
          result.Add(new Parameter("PON Номер", (string)cpe.Element(cpePonSerialTag)));

        if (cpe.Element(cpeConditionTag) != null)
          result.Add(new Parameter("Состояние", (string)cpe.Element(cpeConditionTag)));

        if (cpe.Element(cpeExploitStatusTag) != null)
          result.Add(new Parameter("Статус", (string)cpe.Element(cpeExploitStatusTag)));

        if (!string.IsNullOrEmpty(equipment_prov_method))
        {
          var cpeTransferResult = datumnode.Execute("*.*.oss.external.sri.getTag", new Dictionary<string, object>()
          {
            {"synonimTag", equipment_prov_method},
            {"tagGroupSyn",  "equipmentProvidingMethod"},
            {"extSystem",  "COMB2B"},
          });

          var cpeTransferCondition = cpeTransferResult["result"] as string;

          if (!string.IsNullOrEmpty(cpeTransferCondition))
            result.Add(new Parameter("Условие передачи", cpeTransferCondition));
          else if (cpe.Element(cpeTransferConditionTag) != null)
            result.Add(new Parameter("Условие передачи", (string)cpe.Element(cpeTransferConditionTag)));
        }
        else
				{
          if (cpe.Element(cpeTransferConditionTag) != null)
            result.Add(new Parameter("Условие передачи", (string)cpe.Element(cpeTransferConditionTag)));
        }
        
        var cpeInfo = new Parameter("Идентификатор в Склад CPE", cpeId.Value.ToString());
        result.Add(cpeInfo);
      }
      catch (Exception e)
      {
        throw new Exception(e.Message);
      }
    }
  }
}

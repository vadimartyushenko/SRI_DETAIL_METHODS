using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace DatumNode.GetDescendentCfs
{
  public class Cfs
  {
	  public decimal? Id { get; set; }
    public string ExtId { get; set; }
    public string ServiceName { get; set; }
    public DateTime? Begin { get; set; }
    public DateTime? End { get; set; }
    public decimal? ParentId { get; set; }
    public string CfsParentsStr { get; set; }
    public string PublicResource { get; set; }
    public string PublicResourceType { get; set; }
    public string SBMSStatus { get; set; }
    public string Status { get; set; }
		[IgnoreDataMember]
		public string ItemPath { get; set; }
  }

  public static class Script
  {

    public static void Run(DatumNodeService datumnode, string cfs_id, out System.Collections.Generic.List<Cfs> result)
    {
      if (cfs_id == null)
        throw new Exception("cfs_id is empty");

      Func<Cfs, bool> isClosed = x =>
      {
	      var now = DateTime.Now;
				return now < x.Begin || x.End != null && now > x.End;
      };

      result = new List<Cfs>();

      try
      {
	      var resultGetCfs = datumnode.ExecuteQuery("*.*.oss.sri.cfs.get", new Dictionary<string, object>()
	      {
		      { "cfs_id", Decimal.Parse(cfs_id) },
		      { "descendants", 1 }
	      });

	      var sourceCfs = resultGetCfs.Elements.Where(x => x.Name == "Entity").Select(x => new Cfs()
	      {
		      Id = (Decimal?) x.Element("cfs_id"),
		      ExtId = (String) x.Element("cfs_public_id"),
		      ServiceName = (String) x.Element("service_name"),
		      Begin = (DateTime?) x.Element("cfs_begin"),
		      End = (DateTime?) x.Element("cfs_end"),
		      ParentId = (Decimal?) x.Element("cfs_parent_id"),
					CfsParentsStr = (String) x.Element("cfs_parents_str"),
		      PublicResource = (String) x.Element("public_resource"),
		      PublicResourceType = (String) x.Element("public_resource_type")
	      }).ToList();

	      if (!sourceCfs.Any())
		      return;

	      var extIds = sourceCfs.Select(x => x.ExtId).ToArray();
	      var resultCheckSbms = datumnode.Execute("*.*.oss_api.sri.action_api.check_sbms_exists", new Dictionary<string, object>()
	      {
			      { "cfs_public_ids", String.Join(";", extIds)}
	      });

	      var notExistIdsSrc = resultCheckSbms["not_existed_ids"] as string;

	      if (notExistIdsSrc == null)
		      notExistIdsSrc = string.Empty;

        var notExistIds = new HashSet<string>(notExistIdsSrc.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries));

	      foreach (var item in sourceCfs)
	      {
		      item.SBMSStatus = notExistIds.Contains(item.ExtId) ? "Нет" : "Да";
		      
		      item.Status = !isClosed(item) ? "Открыт" : "Закрыт";

		      item.ItemPath = GetPath(sourceCfs, item.Id);
	      }

	      result = sourceCfs.OrderBy(x => x.ItemPath).ToList();
      }
      catch (Exception e)
      {
	      throw new Exception(e.Message);
      }
    }

    public static string GetPath(List<Cfs> list, decimal? cfsId)
    {
	    var item = list.FirstOrDefault(x => x.Id == cfsId);
	    if (item == null)
		    return "";

	    Decimal? parentId;
	    if (!item.ParentId.HasValue && !String.IsNullOrWhiteSpace(item.CfsParentsStr))
	    {
		    var parentStr = item.CfsParentsStr.Split(new string[] {";"}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		    parentId = Decimal.Parse(parentStr);
	    }
	    else
	    {
		    parentId = item.ParentId;
	    }

	    return GetPath(list, parentId) + "\\" + item.Id;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DatumNode
{

  public class CfsBlockItem
  {
    public string type_block { get; set; }
    public DateTime? date_begin { get; set; }
    public DateTime? date_end { get; set; }
    public string orderId_begin { get; set; }
    public string orderId_end { get; set; }
    public DateTime cfs_audit_date { get; set; }
  }
  public class AuditItem
  {
    public DateTime? cfs_block_begin { get; set; }
    public DateTime? cfs_block_end { get; set; }
    public DateTime? cfs_suspend_begin { get; set; }
    public DateTime? cfs_suspend_end { get; set; }
    public DateTime? cfs_fin_block_begin { get; set; }
    public DateTime? cfs_fin_block_end { get; set; }
    public string public_document_group_id { get; set; }
    public DateTime cfs_audit_date { get; set; }
  }
  public static class Script
  {
    public static void Run(DatumNodeService datumnode, string cfs_id, out System.Collections.Generic.List<DatumNode.CfsBlockItem> result)
    {
      if (cfs_id == null)
        throw new Exception("cfs_id is empty");

      result = new List<DatumNode.CfsBlockItem>();
      try
			{
        var resultAudit = datumnode.ExecuteQuery("*.*.oss.sri.cfs.getAudit", new Dictionary<string, object>()
        {
          { "cfs_id", cfs_id }
        });

        var auditList = resultAudit.Elements.Where(x => x.Name == "Entity").Select(x =>
        {
          return new AuditItem()
          {
            cfs_block_begin = (DateTime?)x.Element("cfs_block_begin"),
            cfs_block_end = (DateTime?)x.Element("cfs_block_end"),
            cfs_suspend_begin = (DateTime?)x.Element("cfs_suspend_begin"),
            cfs_suspend_end = (DateTime?)x.Element("cfs_suspend_end"),
            cfs_fin_block_begin = (DateTime?)x.Element("cfs_fin_block_begin"),
            cfs_fin_block_end = (DateTime?)x.Element("cfs_fin_block_end"),
            public_document_group_id = (string)x.Element("public_document_group_id"),
            cfs_audit_date = (DateTime)x.Element("cfs_audit_date")
          };
        }).ToList();

        var auditListFiltered = auditList.Where(x => x.GetType().GetProperties().Where(p => p.PropertyType == typeof(DateTime?)).Select(p => p.GetValue(x)).Any(value => value != null));
             
        var auditBlockArr = auditListFiltered.ToArray();

        var finBlockList = new List<DatumNode.CfsBlockItem>();
        var admBlockList = new List<DatumNode.CfsBlockItem>();
        var t = new CfsBlockItem();

        for (int i = auditBlockArr.Length-1; i > 0; i--)
				{
          if (auditBlockArr[i].cfs_fin_block_begin.HasValue == false && auditBlockArr[i-1].cfs_fin_block_begin.HasValue == false)
            continue;
          if (auditBlockArr[i].cfs_fin_block_begin.HasValue == false && auditBlockArr[i-1].cfs_fin_block_begin.HasValue == true)
          {
            t = new CfsBlockItem() {
              type_block = "fin",
              date_begin = auditBlockArr[i-1].cfs_fin_block_begin.Value,
              date_end = null,
              orderId_begin = auditBlockArr[i-1].public_document_group_id,
              cfs_audit_date = auditBlockArr[i-1].cfs_audit_date
            };
            //finBlockList.Add(t);
            continue;
          }
          if (auditBlockArr[i].cfs_fin_block_begin.Value == auditBlockArr[i-1].cfs_fin_block_begin.Value)
          {
            if (auditBlockArr[i].cfs_fin_block_end.HasValue == false && auditBlockArr[i-1].cfs_fin_block_end.HasValue == true)
            {
              var t = new CfsBlockItem()
              {
                type_block = "fin",
                date_begin = null,
                date_end = auditBlockArr[i-1].cfs_fin_block_end,
                 orderId_begin = auditBlockArr[i-1].public_document_group_id,
                cfs_audit_date = auditBlockArr[i-1].cfs_audit_date
              };
              finBlockList.Add(t);
            }
          }
          else
          {
              var t = new CfsBlockItem()
              {
                type_block = "fin",
                date_begin = auditBlockArr[i-1].cfs_fin_block_begin,
                date_end = null,
                 orderId_begin = auditBlockArr[i-1].public_document_group_id,
                cfs_audit_date = auditBlockArr[i-1].cfs_audit_date
              };
            finBlockList.Add(t);
          }
				};

        for (int i = auditBlockArr.Length - 1; i > 0; i--)
        {
          if (auditBlockArr[i].cfs_block_begin.HasValue == false && auditBlockArr[i - 1].cfs_block_begin.HasValue == false)
            continue;
          if (auditBlockArr[i].cfs_block_begin.HasValue == false && auditBlockArr[i - 1].cfs_block_begin.HasValue == true)
          {
            var t = new CfsBlockItem()
            {
              type_block = "adm",
              date_begin = auditBlockArr[i - 1].cfs_block_begin.Value,
              date_end = null,
               orderId_begin = auditBlockArr[i - 1].public_document_group_id,
              cfs_audit_date = auditBlockArr[i - 1].cfs_audit_date
            };
            admBlockList.Add(t);
            continue;
          }
          if (auditBlockArr[i].cfs_block_begin.Value == auditBlockArr[i - 1].cfs_block_begin.Value)
          {
            if (auditBlockArr[i].cfs_block_end.HasValue == false && auditBlockArr[i - 1].cfs_block_end.HasValue == true)
            {
              var t = new CfsBlockItem()
              {
                type_block = "fin",
                date_begin = null,
                date_end = auditBlockArr[i - 1].cfs_block_end,
                 orderId_begin = auditBlockArr[i - 1].public_document_group_id,
                cfs_audit_date = auditBlockArr[i - 1].cfs_audit_date
              };
              admBlockList.Add(t);
            }
          }
          else
          {
            var t = new CfsBlockItem()
            {
              type_block = "fin",
              date_begin = auditBlockArr[i-1].cfs_block_begin,
              date_end = null,
               orderId_begin = auditBlockArr[i - 1].public_document_group_id,
              cfs_audit_date = auditBlockArr[i - 1].cfs_audit_date
            };
            admBlockList.Add(t);
          }
        };

        var resultSet = new List<DatumNode.CfsBlockItem>();
        resultSet.AddRange(finBlockList);

        result = resultSet.OrderByDescending(x => x.cfs_audit_date).ToList();
      }
      catch (Exception e)
      {
        throw new Exception(e.Message);
      }
    }
  }
}

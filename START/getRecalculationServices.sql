declare
  v_main_sql clob; 
begin
  -- Call the function
  /*v_res_recalc := recalculation_core.get_recalculation_services
  (
    p_date_begin => :p_date_begin,
    p_date_end => :p_date_end,
    p_date_begin_recalc => :p_date_begin_recalc,
    p_date_end_recalc => :p_date_end_recalc,
    p_territoryid => :p_territoryid,
    p_line => :p_line,
    p_account => :p_account,
    p_resource => :p_resource,
    p_sql => :p_sql,
    p_user => :p_user,
    p_role => :p_role
  );  */
   v_main_sql := 'with t_binds
                  as
                  (
                   select
                    :p_date_begin        as p_date_begin,
                    :p_date_end          as p_date_end,
                    :p_date_begin_recalc as p_date_begin_recalc,
                    :p_date_end_recalc   as p_date_end_recalc,
                    :p_territoryid       as p_territoryid,
                    :p_line              as p_line,
                    :p_account           as p_account,
                    :p_resource          as p_resource
                   from dual
                 )
    select rc.recalculation_service_id,
           rc.recalculation_id,
           rc.document_group_id,
           rc.recalculation_begin_local,
           rc.recalculation_end_local,
           rc.recalculation_accessed,
           nvl(s.service_name,rc.service_synonym) as service_synonym,
           rc.line,
           rc.public_resource,
           et.external_resource_type_desc as public_resource_type,
           rc.public_resource_type_id,
           rc.public_account,
           tr.territory_name,
           rc.recalculation_state_id,
           rc.transfer_date,
           rc.transfer_state,
           rc.inclusion_state,
           rc.is_open,
           rc.public_document_group_ids,
           rc.cfs_id,
           (select t.territory_name from TERRITORY t
            where level=2
             connect by   t.territory_id= prior t.territory_parent_id
            start with t.territory_id=tr.territory_id
           ) as filial_name
      from t_recalculation_service rc
        cross join t_binds b
        join territory tr           on rc.territory_id = tr.territory_id
      left join external_resource_type et on et.ext_resource_parent_type_id is null and  et.public_resource_type_id=rc.public_resource_type_id and et.external_system_type_id=rc.external_system_type_id
    left join service s on s.service_id=rc.service_id and sysdate between s.service_date_start and nvl(s.service_date_end,sysdate)
     where 1=1';

     if :p_date_begin_recalc is not null and :p_date_end_recalc is not null then
      v_main_sql:=v_main_sql||' and rc.recalculation_accessed between p_date_begin_recalc and p_date_end_recalc';
     end if;

     if :p_date_begin is not null and :p_date_end is not null then
      v_main_sql:=v_main_sql||'
      and rc.recalculation_begin_local<p_date_end and nvl(rc.recalculation_end_local,p_date_begin)>=p_date_begin';
     end if;

     if :p_line is not null then
       v_main_sql:=v_main_sql||' and rc.line = p_line';
      end if;

     if :p_account is not null then
       v_main_sql:=v_main_sql||' and rc.public_account = p_account';
     end if;


     if :p_resource is not null then
       v_main_sql:=v_main_sql||' and rc.public_resource = p_resource';
     end if;

     if :p_territoryid is not null then
       v_main_sql:=v_main_sql|| ' and  rc.territory_id in
           (select ter.territory_id
              from territory ter
            connect by prior ter.territory_id = ter.territory_parent_id
             start with ter.territory_id = p_territoryid)';
     end if;

     v_main_sql:=v_main_sql||chr(13)|| 'order by rc.recalculation_begin_local ';

     if :p_sql is not null then
        :p_sql:=v_main_sql;
     end if;


    open :result for
      v_main_sql
      using
       :p_date_begin,
       :p_date_end,
       :p_date_begin_recalc ,
       :p_date_end_recalc,
       :p_territoryid,
       :p_line,
       :p_account,
       :p_resource;                                                      
end;

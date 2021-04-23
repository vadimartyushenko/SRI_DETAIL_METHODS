declare 
  v_finblock_status varchar2(200);
  v_admblock_status varchar2(200);
  v_suspend_status varchar2(200);
  v_cfs_id number;
begin
  begin
    select c.cfs_id
    into v_cfs_id
    from cfs c
    where c.cfs_public_id = :p_public_cfs_id;
  exception 
    when no_data_found then
      null;
  end;
  
  begin
    select
      case 
        when (nvl(c.cfs_block_begin,sysdate + 1) <= sysdate and nvl(c.cfs_block_end, to_date('9999','yyyy')) > sysdate)
        then 'Административная - c ' || to_char(c.cfs_block_begin,'dd.mm.yyyy HH24:mi:ss') || '; ' else '' end adm_block,
      case when (nvl(c.cfs_fin_block_begin, sysdate + 1) <= sysdate and nvl(c.cfs_fin_block_end, to_date('9999','yyyy')) > sysdate)
        then 'Финансовая - c ' || to_char(c.cfs_fin_block_begin,'dd.mm.yyyy HH24:mi:ss') || '; ' else '' end fin_block,
      case when (nvl(c.cfs_suspend_begin,sysdate+1)<=sysdate and nvl(c.cfs_suspend_end, to_date('9999','yyyy'))>sysdate)
        then 'Добровольная - c ' || to_char(c.cfs_suspend_begin,'dd.mm.yyyy HH24:mi:ss') || '; 'else '' end suspend_block
    into v_admblock_status, v_finblock_status, v_suspend_status
    from cfs c
    where c.cfs_id = case when v_cfs_id is not null 
      then v_cfs_id else to_number(:p_cfs_id) end;
  exception
    when no_data_found then
      :result := '';
      return;
  end;
      
  :result := coalesce(v_admblock_status || v_finblock_status || v_suspend_status, 'Нет');
end;

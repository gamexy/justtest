using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class test : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        refresh();
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        GetCoil();
    }

    private void GetCoil()
    {
        using (OracleConnection myConn = new OracleConnection())
        {
            myConn.ConnectionString = @"Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST =10.1.1.22)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = l3db))); User Id = l3dbuser;Password = l3dbuser;";

            myConn.Open();

            OracleCommand oracleCommand = new OracleCommand
            {
                Connection = myConn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "GY_REQUIRE"
            };
            oracleCommand.ExecuteNonQuery();

            myConn.Close();
        }
    }

    private void refresh()
    {
        DataTable mytable = new DataTable();
        using (OracleConnection myConn = new OracleConnection())
        {
            myConn.ConnectionString = @"Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST =10.1.1.22)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = l3db))); User Id = l3dbuser;Password = l3dbuser;";

            myConn.Open();

            //OracleCommand oracleCommand = new OracleCommand
            //{
            //    Connection = myConn,
            //    CommandType = CommandType.StoredProcedure,
            //    CommandText = "GY_REQUIRE"
            //};
            //oracleCommand.ExecuteNonQuery();

            OracleCommand oraCmd = new OracleCommand();
            oraCmd.Connection = myConn;

            oraCmd.CommandText = string.Format(@"select 单位名,slab_numbers 块数,sx,状态,品种报信 from （select unitname as 单位名,count(*) as 块数,max(identify_order) as sx
                ,(case  when max(STATUSFLAG)=100 then '未入炉' when max(STATUSFLAG)>100 then '已入炉' end ) as 状态,wm_concat(rules_name) 品种报信 from (select furnace_query.*,rules_name from furnace_query left join rules_coil on furnace_query.coilno=rules_coil.coilno where del_flag<>1) t group by unitname order by sx) t1 
                inner join L3_PDI_MAIN on t1.单位名=L3_PDI_MAIN.unit_name ");

            OracleDataAdapter Da = new OracleDataAdapter(oraCmd);
            Da.Fill(mytable);
            myConn.Close();
        }
        mytable.Columns.Remove("sx");

        for (int i = 0; i < mytable.Rows.Count; i++)//去掉重复内容
        {
            if (mytable.Rows[i]["品种报信"] != null)
            {
                string[] rs = mytable.Rows[i]["品种报信"].ToString().Split(',');
                string t = rs[0];
                foreach (string s in rs)
                {
                    if (t.IndexOf(s) < 0)//如果结果字符串不包括它，就加进去
                    {
                        t += "," + s;
                    }
                }
                mytable.Rows[i]["品种报信"] = t;
            }
        }

        GridView1.DataSource = mytable;
        GridView1.DataBind();
        GridView1.RowCommand += new GridViewCommandEventHandler(GridView1_RowCommand);
    }

    private void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "select")
        {
            int index = Convert.ToInt32(e.CommandArgument);
            string unitname = GridView1.Rows[index].Cells[1].Text;

            DataTable mytable = new DataTable();
            using (OracleConnection myConn = new OracleConnection())
            {
                myConn.ConnectionString = @"Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST =10.1.1.22)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = l3db))); User Id = l3dbuser;Password = l3dbuser;";

                myConn.Open();

                OracleCommand oraCmd = new OracleCommand();
                oraCmd.Connection = myConn;

                oraCmd.CommandText = string.Format(@"select
                    furnace_query.COILNO 钢卷号,MSRD 钢种,PSRD 成品材质,decode(ssflag,'S','是') 剪,rules 工艺要点,COILTHICK 厚,COILWIDTH 宽,RT4AIM RT4,FT7AIM FT7,CTAIM CT,SLABNO 板坯号,SLABTHICK 坯厚,SLABWIDTH 坯宽,SLABLENGTH 坯长,NEXTPROCESS 下工序
                    from furnace_query left join
                    (select coilno,wm_concat(rules_name) rules from rules_coil group by coilno ) r
                    on furnace_query.coilno=r.coilno where unitname='{0}' and del_flag<>1 order by identify_order", unitname);

                OracleDataAdapter Da = new OracleDataAdapter(oraCmd);
                Da.Fill(mytable);
                myConn.Close();
            }

            GridView_detail.DataSource = mytable;
            GridView_detail.DataBind();

            //Response.Write(unitname);

            string gy_name = HttpUtility.HtmlDecode(GridView1.Rows[index].Cells[4].Text).Trim();
            DataTable gytable = new DataTable();
            using (OracleConnection myConn = new OracleConnection())
            {
                myConn.ConnectionString = @"Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST =10.1.1.22)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = l3db))); User Id = l3dbuser;Password = l3dbuser;";

                myConn.Open();

                OracleCommand oraCmd = new OracleCommand();
                oraCmd.Connection = myConn;

                if (gy_name.Length > 0)
                {
                    string[] rs = gy_name.Split(',');
                    string t = string.Format("rule_name like '{0}'", rs[0]);
                    foreach (string s in rs)
                    {
                        if (t.IndexOf(s) < 0)
                        {
                            t += string.Format(" or rule_name like '{0}'", s);
                        }
                    }
                    oraCmd.CommandText = string.Format(@"select rule_name 工艺要点,rule_detail 具体要求 from rules where {0}", t);

                    OracleDataAdapter Da = new OracleDataAdapter(oraCmd);
                    Da.Fill(gytable);
                }
                else
                {
                    gytable.Clear();
                }
                myConn.Close();
            }

            //换行 需和绑定事件配合使用
            for (int i = 0; i < gytable.Rows.Count; i++)
            {
                gytable.Rows[i]["具体要求"] = gytable.Rows[i]["具体要求"].ToString().Replace("\n", "<br />");
                //gytable.Rows[i]["具体要求"] = Server.HtmlEncode(gytable.Rows[i]["具体要求"].ToString());
            }
            //if (gytable.Rows.Count > 0)
            {
                GridView_gy.DataSource = gytable;
                GridView_gy.DataBind();
                for (int i = 0; i < gytable.Rows.Count; i++)
                {
                    GridView_gy.Rows[i].Cells[0].Font.Bold = true;
                }
            }
        }
        //throw new NotImplementedException();
    }

    protected void Timer1_Tick(object sender, EventArgs e)
    {
        refresh();
    }

    protected void GridView_detail_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow || e.Row.RowType == DataControlRowType.Header)
        {
            //保持列不变形
            for (int i = 0; i < e.Row.Cells.Count; i++)
            {
                //方法一：
                //e.Row.Cells[i].Text = "&nbsp;" + e.Row.Cells[i].Text + "&nbsp;";
                e.Row.Cells[i].Text = e.Row.Cells[i].Text;
                e.Row.Cells[i].Wrap = false;

                //my
                e.Row.Cells[i].ToolTip = e.Row.Cells[i].Text;
                if (e.Row.Cells[i].Text.Length > 10)
                {
                    e.Row.Cells[i].Text = e.Row.Cells[i].Text.Substring(0, 10);
                }

                //方法二：
                //e.Row.Cells[i].Text = "<nobr>&nbsp;" + e.Row.Cells[i].Text + "&nbsp;</nobr>";
            }
        }
    }

    protected void GridView_gy_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            TableCellCollection cells = e.Row.Cells;
            foreach (TableCell cell in cells)
            {
                cell.Text = Server.HtmlDecode(cell.Text);
            }
        }
    }
}
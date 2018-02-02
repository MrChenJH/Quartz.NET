using System;
using System.Data;
using System.Threading;
using Common.Logging;
using System.Xml;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml.Serialization;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Net;
using iTextSharp.tool.xml;
using System.Linq;
using System.Configuration;
using System.Data.SqlClient;

namespace Quartz.Server
{
    /// <summary>
    /// A sample job that just prints info on console for demostration purposes.
    /// </summary>
    public class SampleJob : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SampleJob));
        private List<KeyValuePair<string, string>> listkeyvalue = new List<KeyValuePair<string, string>>();

        private string startTableSgin = "<table",
            endTableSgin = "</table>";

        private string appurl = ConfigurationManager.AppSettings["url"].ToString();

        private string tags = ConfigurationManager.AppSettings["tags"].ToString();

        private void Deletebak()
        {
            string sql = "TRUNCATE TABLE resourceobj79_bak";
            SqlHeplerTo.ExcuteSQL(sql);
            sql = "TRUNCATE TABLE node_objdata_bak";
            SqlHeplerTo.ExcuteSQL(sql);
            sql = "TRUNCATE TABLE media_attrach";
            SqlHeplerTo.ExcuteSQL(sql);
            sql = "TRUNCATE TABLE media_attr_use";
            SqlHeplerTo.ExcuteSQL(sql);
        }

        /// <summary>
        /// 
        /// </summary>
        private void InsertNode()
        {
            var site = ConfigurationManager.AppSettings["site"].ToString();
            var mapkey = ConfigurationManager.AppSettings["objName"].ToString();
            string sql = "select id,isnull(parent_id,0) parent_id,name from v_channel";
            var Channeldata = SqlHeplerFrom.GetTable(sql);
            foreach (DataRow row in Channeldata.Rows)
            {
                try
                {

                    sql = @"insert into site_node_bak 
                              (node_id,site_code,node_name,node_type,map_type,map_key,node_status,is_audit,audit_wf,is_review,is_auedit,creator,createtime,inputtime,isdept,urlPre)
                              values
                              (" + Convert.ToString(row["id"]) + ",'" + site + "','" + Convert.ToString(row["name"]) + "',0,0,'" + mapkey + "',0,1,'" + mapkey + "',0,1,'Creator','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "',0,'gg')";
                    SqlHeplerTo.ExcuteSQL(sql);
                    sql = @"select  node_path  from node_net_bak where node_id='" + Convert.ToString(row["parent_id"]) + "'";
                    var nodeTable = SqlHeplerTo.GetTable(sql);
                    if (nodeTable.Rows.Count > 0)
                    {
                        foreach (DataRow netrow in nodeTable.Rows)
                        {
                            sql = @"insert into  node_net_bak 
                                        (node_id,node_pid,node_path,order_no)
                                  values('" + Convert.ToString(row["id"]) + "','" + Convert.ToString(netrow["node_path"]) + "','" + Convert.ToString(netrow["node_path"]) + "-" + Convert.ToString(row["id"]) + "',1)";
                            SqlHeplerTo.ExcuteSQL(sql);
                        }
                    }
                    else
                    {
                        sql = @"insert into  node_net_bak
                                        (node_id,node_pid,node_path,order_no)
                                  values('" + Convert.ToString(row["id"]) + "',0,'0-" + Convert.ToString(row["id"]) + "',1)";
                        SqlHeplerTo.ExcuteSQL(sql);
                    }

                }
                catch (Exception ex)
                {

                    logger.Error(ex.Message);
                }
            }
        }

        private void InsertScript()
        {
            var mapkey = ConfigurationManager.AppSettings["objName"].ToString();
            string sql = "select id,channel_id,top_level,title,short_title,author,isnull(link,origin_url) link,release_date,content,title_img  from v_content";
            var scriptData = SqlHeplerFrom.GetTable(sql);
            string content1 = string.Empty;
            foreach (DataRow row in scriptData.Rows)
            {
                try
                {
                    content1 = Convert.ToString(row["content"]);
                    string content = ConvertTableToA(Convert.ToString(row["content"]), Convert.ToString(row["id"]));
                    var arr = content.ToLower().Split(new string[] { "src=\"" }, StringSplitOptions.RemoveEmptyEntries);
                    if (arr.Length > 1)
                    {
                        for (int i = 1; i < arr.Length; i++)
                        {
                            if (!arr[i].Contains("http"))
                            {
                                arr[i] = ConfigurationManager.AppSettings["imgeurl"].ToString() + arr[i];
                            }

                        }
                        content = string.Join("src=\"", arr);
                    }
                    var arrshref = content.ToLower().Split(new string[] { "href=\"" }, StringSplitOptions.RemoveEmptyEntries);
                    if (arrshref.Length > 1)
                    {
                        for (int i = 1; i < arrshref.Length; i++)
                        {
                            if (!arrshref[i].Contains("http"))
                            {
                                arrshref[i] = ConfigurationManager.AppSettings["imgeurl"].ToString() + arrshref[i];
                            }
                        }
                        content = string.Join("href=\"", arrshref);
                    }

                    string link = Convert.ToString(row["link"]);
                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        if (!link.Contains("http"))
                        {
                            link = ConfigurationManager.AppSettings["imgeurl"].ToString() + link;
                        }
                    }

                    sql = @"insert into  " + ConfigurationManager.AppSettings["table"].ToString() + "_bak" + @"
                              (IDLeaf, resourceprop1, resourceprop3,creator,url,createtime, content,state,name,img_top)
                        values(@IDLeaf,@resourceprop1,  @resourceprop3,@creator,@url,@createtime,@content,99,@name,@imgtop)";
                    SqlParameter[] paras = new SqlParameter[] {
                        new SqlParameter("@IDLeaf",Convert.ToString(row["id"]) ),
                        new SqlParameter("@resourceprop1",Convert.ToString(row["top_level"]) ),
                        new SqlParameter("@resourceprop3",Convert.ToString(row["short_title"])),
                        new SqlParameter("@creator",Convert.ToString(row["author"])),
                        new SqlParameter("@url",link),
                        new SqlParameter("@createtime", Convert.ToString(row["release_date"]).PadRight(20, ' ').Substring(0, 19)),
                        new SqlParameter("@content", content.Replace("'", "''")),
                        new SqlParameter("@name",  Convert.ToString(row["title"])),
                        new SqlParameter("@imgtop",  Convert.ToString(row["title_img"]))
                    };
                    SqlHeplerTo.ExcuteSQL(sql, paras);

                    sql = @"insert into [node_objdata_bak]
                            (node_id,obj_name,ID_Leaf,order_no,is_head,is_top,is_hot) 
                     values (@nodeid,@objname,@idleaf,0,0,0,0)";

                    SqlParameter[] parass = new SqlParameter[] {
                        new SqlParameter("@nodeid",Convert.ToString(row["channel_id"]) ),
                        new SqlParameter("@objname",mapkey ),
                        new SqlParameter("@idleaf", Convert.ToString(row["id"]))
                    };
                    SqlHeplerTo.ExcuteSQL(sql, parass);

                }
                catch (Exception ex)
                {
                    logger.Error(ex.StackTrace);
                    logger.Error(content1);
                    logger.Error(ex.Message);
                }
            }
        }

        private void InsertMedia()
        {
            string sql = "select content_id,path,name from v_content_ext";
            var mediatData = SqlHeplerFrom.GetTable(sql);

            foreach (DataRow row in mediatData.Rows)
            {
                try
                {
                    sql = "select attr_id from  dbo.media_attrach where attr_path='" + Convert.ToString(row["path"]) + "'";
                    var isExistData = SqlHeplerTo.GetTable(sql);
                    string attrid = string.Empty;
                    if (isExistData.Rows.Count == 0)
                    {
                        attrid = Math.Abs(Guid.NewGuid().GetHashCode()).ToString();

                        sql = @"insert into media_attrach(attr_id,attr_type,attr_title,attr_tag,attr_path,start_page,attr_author,attr_note,creator,createtime,inputtime) 
                                               values('" + attrid + "',2,'" + Convert.ToString(row["name"]) + "','','" + Convert.ToString(row["path"]) + "','','administrator','系统','administrator','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
                        SqlHeplerTo.ExcuteSQL(sql);
                    }
                    else
                    {
                        attrid = Convert.ToString(isExistData.Rows[0]["attr_id"]);
                    }
                    sql = @"insert into  dbo.media_attr_use  
                            (auto_no,attr_id,use_tag,mapping_key,mapping_val,creator,createtime) 
                             values('" + Math.Abs(Guid.NewGuid().GetHashCode()).ToString() + "','" + attrid + "',1,'IDLeaf','" + Convert.ToString(row["content_id"]) + "','administrator','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
                    SqlHeplerTo.ExcuteSQL(sql);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }
        }

        public void Execute(IJobExecutionContext context)
        {
            logger.Info("开始");
            Deletebak();
            InsertScript();
            InsertMedia();
            SqlHeplerTo.ExcuteProc("EdataProcess");
            logger.Info("结束");
        }

        class PdfCal
        {
            public string pdfname { get; set; }
            public string text { get; set; }
        }


        private void Poc(ref string str, int startloc, int length, string idleaf)
        {
            var id = Math.Abs(Guid.NewGuid().GetHashCode());
            var table = str.Substring(startloc, length);
            var startstr = str.Substring(0, startloc);
            var endstr = str.Substring(startloc + length, str.Length - table.Length - startstr.Length);
            var ta1 = string.Empty;
            ta1 = table.Clone().ToString().ToLower().Trim();
            tags.Split(',').ToList().ForEach(t => { ta1 = ta1.Replace("?/" + t + ">", "</" + t + ">"); });

            var pdfFile = WebSnapshotsHelper.CreatePdf(ta1, idleaf);

            string urlkey = "http://@#";

            string rev = PostResponse(ConfigurationManager.AppSettings["posturl"].ToString(), new Dictionary<string, string>(), Encoding.UTF8, pdfFile);
            var re = JsonConvert.DeserializeObject<result>(rev);
            string a = "<br/><a href=\"" + appurl + re.url + "\"   title=\"" + re.title + "\">" + re.title + "</a>";
            listkeyvalue.Add(new KeyValuePair<string, string>(urlkey, a));
            str = startstr + urlkey.PadRight(table.Length, ' ') + endstr;
            string sql = @"insert into media_attrach
                                   (attr_id, attr_type, attr_title, attr_tag, attr_path, start_page, attr_author, attr_note, creator, createtime, inputtime)
                             values(" + id + ", 2, '" + re.title + "','','" + re.url + "','','系统', '后台表格替换','后台系统','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            SqlHeplerTo.ExcuteSQL(sql);
            sql = "select  isnull(max(auto_no),0) no from dbo.media_attr_use";
            var maxdata = SqlHeplerTo.GetTable(sql);
            sql = @"insert into media_attr_use
                          (auto_no, attr_id, use_tag, mapping_key, mapping_val, creator, createtime)
                    values(" + Convert.ToInt32(maxdata.Rows[0][0]) + ", '" + id + "', '','IDLeaf','" + idleaf + "','系统','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            SqlHeplerTo.ExcuteSQL(sql);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startloc"></param>
        /// <param name="endloc"></param>
        public void FindTable(ref string str, List<int> startSignLoc, List<int> endSignLoc, string idleaf)
        {
            bool isExist = false;
            for (int j = 0; j < startSignLoc.Count; j++)
            {
                if (endSignLoc[0] < startSignLoc[j])
                {
                    isExist = true;
                    int distance = 0;
                    for (int k = j; k < startSignLoc.Count; k++)
                    {
                        if (endSignLoc[j - 1] > startSignLoc[k])
                        {
                            distance = distance + 1;
                        }
                    }
                    j = distance + j;
                    Poc(ref str, startSignLoc[0], endSignLoc[j - 1] + endTableSgin.Length - startSignLoc[0], idleaf);
                    for (int c = 0; c < j; c++)
                    {
                        startSignLoc.RemoveAt(0);
                        endSignLoc.RemoveAt(0);
                    }
                    if (startSignLoc.Count > 0)
                    {
                        FindTable(ref str, startSignLoc, endSignLoc, idleaf);
                    }
                    break;
                }
                listkeyvalue = new List<KeyValuePair<string, string>>();
                if (!isExist && j == startSignLoc.Count - 1)
                {
                    Poc(ref str, startSignLoc[0], endSignLoc.Last() + endTableSgin.Length - startSignLoc[0], idleaf);
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string ConvertTableToA(string str, string idleaf)
        {
            try
            {
                str = str.Trim();

                List<int> startSignLoc = new List<int>();
                List<int> endSignLoc = new List<int>();

                for (int i = 0; i < str.Length; i++)
                {
                    if ((i + endTableSgin.Length) > str.Length)
                    {
                        break;
                    }

                    if (str.Substring(i, startTableSgin.Length).ToLower() == startTableSgin)
                    {
                        startSignLoc.Add(i);
                    }

                    if (str.Substring(i, endTableSgin.Length).ToLower() == endTableSgin)
                    {
                        endSignLoc.Add(i);
                    }
                }

                if (startSignLoc.Count > 0 && endSignLoc.Count > 0 && startSignLoc.Count == endSignLoc.Count)
                {
                    FindTable(ref str, startSignLoc, endSignLoc, idleaf);
                }

                listkeyvalue.ForEach(t => { str = str.Replace(t.Key, t.Value); });
            }
            catch (Exception ex)
            {
                logger.Error(idleaf);
                logger.Error(ex.StackTrace);
                logger.Error(ex.Message);
            }
            return str;
        }

        class result
        {
            public string state { get; set; }

            public string url { get; set; }

            public string title { get; set; }

            public string original { get; set; }

            public string error { get; set; }
        }

        public string PostValue(string Url, string value)
        {
            try
            {

                byte[] postBytes = Encoding.UTF8.GetBytes(value);
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(Url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.ContentLength = postBytes.Length;

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                }
                using (WebResponse wr = req.GetResponse())
                {
                    System.IO.Stream respStream = wr.GetResponseStream();
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string PostResponse(string url, Dictionary<string, string> input, Encoding endoding, byte[] data1, string name = "upfile")
        {
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            //request.Credentials = CredentialCache.DefaultCredentials;
            request.Expect = "";

            MemoryStream stream = new MemoryStream();


            byte[] line = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
            byte[] enterER = Encoding.ASCII.GetBytes("\r\n");
            ////提交文件

            string fformat = "Content-Disposition:form-data; name=\"{0}\";filename=\"{1}\"\r\nContent-Type:{2}\r\n\r\n";
            stream.Write(line, 0, line.Length);        //项目分隔符
            string s = string.Format(fformat, name, "表格" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf", "application/pdf");
            byte[] data = Encoding.UTF8.GetBytes(s);
            stream.Write(data, 0, data.Length);

            stream.Write(data1, 0, data1.Length);
            stream.Write(enterER, 0, enterER.Length);  //添加\r\n    

            //提交文本字段
            if (input != null)
            {
                string format = "--" + boundary + "\r\nContent-Disposition:form-data;name=\"{0}\"\r\n\r\n{1}\r\n";    //自带项目分隔符
                foreach (string key in input.Keys)
                {
                    string s1 = string.Format(format, key, input[key]);
                    byte[] data11 = Encoding.UTF8.GetBytes(s1);
                    stream.Write(data11, 0, data.Length);
                }

            }
            byte[] foot_data = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");      //项目最后的分隔符字符串需要带上--  
            stream.Write(foot_data, 0, foot_data.Length);

            request.ContentLength = stream.Length;
            Stream requestStream = request.GetRequestStream(); //写入请求数据
            stream.Position = 0L;
            stream.CopyTo(requestStream); //
            stream.Close();
            requestStream.Close();
            try
            {
                HttpWebResponse response;
                response = (HttpWebResponse)request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                using (var mstream = new MemoryStream())
                {
                    responseStream.CopyTo(mstream);
                    string message = endoding.GetString(mstream.ToArray());
                    return message;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 将Html文字 输出到PDF档里
        /// </summary>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        public byte[] ConvertHtmlTextToPDF(string htmlText)
        {
            if (string.IsNullOrEmpty(htmlText))
            {
                return null;
            }
            //避免当htmlText无任何html tag标签的纯文字时，转PDF时会挂掉，所以一律加上<p>标签
            htmlText = "<p>" + htmlText + "</p>";

            MemoryStream outputStream = new MemoryStream();//要把PDF写到哪个串流
            byte[] data = Encoding.UTF8.GetBytes(htmlText);//字串转成byte[]
            MemoryStream msInput = new MemoryStream(data);
            Document doc = new Document();//要写PDF的文件，建构子没填的话预设直式A4

            PdfWriter writer = PdfWriter.GetInstance(doc, outputStream);
            //指定文件预设开档时的缩放为100%

            PdfDestination pdfDest = new PdfDestination(PdfDestination.XYZ, 0, doc.PageSize.Height, 1f);
            //开启Document文件 
            doc.Open();

            //使用XMLWorkerHelper把Html parse到PDF档里
            XMLWorkerHelper.GetInstance().ParseXHtml(writer, doc, msInput, null, Encoding.UTF8, new UnicodeFontFactory());
            //XMLWorkerHelper.GetInstance().ParseXHtml(writer, doc, msInput, null, Encoding.UTF8);

            //将pdfDest设定的资料写到PDF档
            PdfAction action = PdfAction.GotoLocalPage(1, pdfDest, writer);
            writer.SetOpenAction(action);
            doc.Close();
            msInput.Close();
            outputStream.Close();
            //回传PDF档案 
            return outputStream.ToArray();

        }

        //设置字体类
        public class UnicodeFontFactory : FontFactoryImp
        {
            private static readonly string arialFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
             "arialuni.ttf");//arial unicode MS是完整的unicode字型。
            private static readonly string 标楷体Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
              "KAIU.TTF");//标楷体


            public override Font GetFont(string fontname, string encoding, bool embedded, float size, int style, BaseColor color, bool cached)
            {
                BaseFont bfChiness = BaseFont.CreateFont(@"C:\Windows\Fonts\SIMSUN.TTC,1", BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                //可用Arial或标楷体，自己选一个
                //BaseFont baseFont = BaseFont.CreateFont(标楷体Path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                return new Font(bfChiness, size, style, color);
            }
        }
       }
}
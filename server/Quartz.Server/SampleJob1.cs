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

namespace Quartz.Server
{
    /// <summary>
    /// A sample job that just prints info on console for demostration purposes.
    /// </summary>
    public class SampleJob1 : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SampleJob));
        private List<KeyValuePair<string, string>> listkeyvalue = new List<KeyValuePair<string, string>>();

        private string startTableSgin = "<table",
            endTableSgin = "</table>";


        /// <summary>
        /// 
        /// </summary>
        private void InsertNode()
        {
            var site = ConfigurationManager.AppSettings["site"].ToString();
            var mapkey = ConfigurationManager.AppSettings["objName"].ToString();
            string sql = "select id,parent_id,name from v_channel";
            var Channeldata = SqlHeplerFrom.GetTable(sql);
            foreach (DataRow row in Channeldata.Rows)
            {
                try
                {
                    sql = @"insert into v_channel 
                              (id,parent_id,name)
                              values
                              ('" + Convert.ToString(row["id"]) + "','" + Convert.ToString(row["parent_id"]) + "','" + Convert.ToString(row["name"]) + "')";
                    SqlHeplerTo.ExcuteSQL(sql);

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
            string sql = "select id,channel_id,top_level,title,short_title,author,link,release_date,content  from v_content";
            var scriptData = SqlHeplerFrom.GetTable(sql);
            foreach (DataRow row in scriptData.Rows)
            {
                try
                {
                    sql = @"insert into  v_content 
                              (id,channel_id,top_level,title,short_title,author,link,release_date,content)
                        values('" + Convert.ToString(row["id"]) + "', '" + Convert.ToString(row["channel_id"]) + "', '" + Convert.ToString(row["title"]) + "', '" + Convert.ToString(row["short_title"]) + "', '" + Convert.ToString(row["author"]) + "',  '" + Convert.ToString(row["link"]) + "', '" + Convert.ToString(row["release_date"]) + "',  '" + Convert.ToString(row["content"]) + "')";
                    SqlHeplerTo.ExcuteSQL(sql);
                }
                catch (Exception ex)
                {
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
                    sql = @"insert into media_attrach(content_id,path,name) 
                                               values('" + Convert.ToString(row["content_id"]) + "','" + Convert.ToString(row["path"]) + "','" + Convert.ToString(row["name"]) + "')";
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
            logger.Info("��ʼ");
            InsertNode();
            InsertScript();
            InsertMedia();
            logger.Info("����");
        }


        private void Poc(ref string str, int startloc, int length, string idleaf)
        {

            var id = Math.Abs(Guid.NewGuid().GetHashCode());
            var table = str.Substring(startloc, length);
            var startstr = str.Substring(0, startloc);
            var endstr = str.Substring(startloc + length, str.Length - table.Length - startstr.Length);
            byte[] pdfFile = this.ConvertHtmlTextToPDF(table);
            string urlkey = "http://@#";
            string rev = PostResponse(ConfigurationManager.AppSettings["posturl"].ToString(), new Dictionary<string, string>(), Encoding.UTF8, pdfFile);
            var re = JsonConvert.DeserializeObject<result>(rev);

            string a = "<a href=\"" + re.url + "\"   title=\"" + re.title + "\">" + re.title + "</a>";
            listkeyvalue.Add(new KeyValuePair<string, string>(urlkey, a));
            str = startstr + urlkey.PadRight(table.Length, ' ') + endstr;
            string sql = @"insert into media_attrach
                                   (attr_id, attr_type, attr_title, attr_tag, attr_path, start_page, attr_author, attr_note, creator, createtime, inputtime)
                             values(" + id + ", 2, '" + re.title + "','','" + re.url + "','','ϵͳ', '��̨����滻','��̨ϵͳ','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            SqlHeplerTo.ExcuteSQL(sql);
            sql = "select  isnull(max(auto_no),0) no from dbo.media_attr_use";
            var maxdata = SqlHeplerTo.GetTable(sql);
            sql = @"insert into media_attr_use
                          (auto_no, attr_id, use_tag, mapping_key, mapping_val, creator, createtime)
                    values(" + Convert.ToInt32(maxdata.Rows[0][0]) + ", '" + id + "', '','IDLeaf','" + idleaf + "','ϵͳ','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
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
                    Poc(ref str, startSignLoc[0], endSignLoc[j - 1] + endTableSgin.Length - startSignLoc[0], idleaf);
                    for (int c = 0; c < j; c++)
                    {
                        startSignLoc.RemoveAt(c);
                        endSignLoc.RemoveAt(c);
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

            if (startSignLoc.Count > 0 && endSignLoc.Count > 0 && endSignLoc.Count == endSignLoc.Count)
            {
                FindTable(ref str, startSignLoc, endSignLoc, idleaf);
            }

            listkeyvalue.ForEach(t => { str = str.Replace(t.Key, t.Value); });

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
            ////�ύ�ļ�

            string fformat = "Content-Disposition:form-data; name=\"{0}\";filename=\"{1}\"\r\nContent-Type:{2}\r\n\r\n";
            stream.Write(line, 0, line.Length);        //��Ŀ�ָ���
            string s = string.Format(fformat, name, "���" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf", "application/pdf");
            byte[] data = Encoding.UTF8.GetBytes(s);
            stream.Write(data, 0, data.Length);

            stream.Write(data1, 0, data1.Length);
            stream.Write(enterER, 0, enterER.Length);  //���\r\n    

            //�ύ�ı��ֶ�
            if (input != null)
            {
                string format = "--" + boundary + "\r\nContent-Disposition:form-data;name=\"{0}\"\r\n\r\n{1}\r\n";    //�Դ���Ŀ�ָ���
                foreach (string key in input.Keys)
                {
                    string s1 = string.Format(format, key, input[key]);
                    byte[] data11 = Encoding.UTF8.GetBytes(s1);
                    stream.Write(data11, 0, data.Length);
                }

            }
            byte[] foot_data = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");      //��Ŀ���ķָ����ַ�����Ҫ����--  
            stream.Write(foot_data, 0, foot_data.Length);

            request.ContentLength = stream.Length;
            Stream requestStream = request.GetRequestStream(); //д����������
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
        /// ��Html���� �����PDF����
        /// </summary>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        public byte[] ConvertHtmlTextToPDF(string htmlText)
        {
            if (string.IsNullOrEmpty(htmlText))
            {
                return null;
            }
            //���⵱htmlText���κ�html tag��ǩ�Ĵ�����ʱ��תPDFʱ��ҵ�������һ�ɼ���<p>��ǩ
            htmlText = "<p>" + htmlText + "</p>";

            MemoryStream outputStream = new MemoryStream();//Ҫ��PDFд���ĸ�����
            byte[] data = Encoding.UTF8.GetBytes(htmlText);//�ִ�ת��byte[]
            MemoryStream msInput = new MemoryStream(data);
            Document doc = new Document();//ҪдPDF���ļ���������û��Ļ�Ԥ��ֱʽA4

            PdfWriter writer = PdfWriter.GetInstance(doc, outputStream);
            //ָ���ļ�Ԥ�迪��ʱ������Ϊ100%

            PdfDestination pdfDest = new PdfDestination(PdfDestination.XYZ, 0, doc.PageSize.Height, 1f);
            //����Document�ļ� 
            doc.Open();

            //ʹ��XMLWorkerHelper��Html parse��PDF����
            XMLWorkerHelper.GetInstance().ParseXHtml(writer, doc, msInput, null, Encoding.UTF8, new UnicodeFontFactory());
            //XMLWorkerHelper.GetInstance().ParseXHtml(writer, doc, msInput, null, Encoding.UTF8);

            //��pdfDest�趨������д��PDF��
            PdfAction action = PdfAction.GotoLocalPage(1, pdfDest, writer);
            writer.SetOpenAction(action);
            doc.Close();
            msInput.Close();
            outputStream.Close();
            //�ش�PDF���� 
            return outputStream.ToArray();

        }

        //����������
        public class UnicodeFontFactory : FontFactoryImp
        {
            private static readonly string arialFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
             "arialuni.ttf");//arial unicode MS��������unicode���͡�
            private static readonly string �꿬��Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
              "KAIU.TTF");//�꿬��


            public override Font GetFont(string fontname, string encoding, bool embedded, float size, int style, BaseColor color, bool cached)
            {
                BaseFont bfChiness = BaseFont.CreateFont(@"C:\Windows\Fonts\SIMSUN.TTC,1", BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                //����Arial��꿬�壬�Լ�ѡһ��
                //BaseFont baseFont = BaseFont.CreateFont(�꿬��Path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                return new Font(bfChiness, size, style, color);
            }
        }

        /// <summary>
        /// �ж��Ƿ�������
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        //public bool isMessyCode(string txt)
        //{
        //    var bytes = Encoding.UTF8.GetBytes(txt);            //239 191 189            
        //    for (var i = 0; i < bytes.Length; i++)
        //    {
        //        if (i < bytes.Length - 3)
        //            if (bytes[i] == 239 && bytes[i + 1] == 191 && bytes[i + 2] == 189)
        //            {
        //                return true;
        //            }
        //    }
        //    return false;
        //}

    }
}
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Media;
using System.Drawing;
using System.Configuration;
using System.Diagnostics;

namespace Quartz.Server
{
    public class WebSnapshotsHelper
    {
        /// <summary>
        /// 创建pdf
        /// </summary>
        /// <param name="source"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static byte[] CreatePdf(string source,string idleaf)
        {
            var targetSorcre = new StringBuilder();
            targetSorcre.AppendLine("<html><head></head>");

            targetSorcre.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" />");
            targetSorcre.AppendLine("<style type=\"text/css\">");
            targetSorcre.AppendLine("tr{ page-break-inside:avoid;}");
            targetSorcre.AppendLine("</style >");
            targetSorcre.AppendLine("<body>");
            targetSorcre.AppendLine(source);
            targetSorcre.AppendLine("</body>");
            targetSorcre.AppendLine("</html>");

            string dirName = string.Format("{0}:\\{1}","D", DateTime.Now.ToString("yyyyMMdd")+"r");
            if (!Directory.Exists(dirName)) {
                Directory.CreateDirectory(dirName);
            }

            if (!Directory.Exists(dirName+"\\html"))
            {
                Directory.CreateDirectory(dirName + "\\html");
            }


            if (!Directory.Exists(dirName + "\\pdf"))
            {
                Directory.CreateDirectory(dirName + "\\pdf");
            }
            string htmlName = string.Format("{0}\\html\\{1}.html",  dirName, idleaf+"_"+ DateTime.Now.Ticks);
            string pdfName = string.Format("{0}\\pdf\\{1}.pdf",  dirName, idleaf + "_" + DateTime.Now.Ticks);

            //以写文件的方式创建文件
            using (FileStream stream = new FileStream(htmlName, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(targetSorcre.ToString());
                stream.Write(bytes, 0, bytes.Length);//写入二进制
                stream.Dispose();
            }


            //执行wkhtmltopdf.exe 
            Process p = System.Diagnostics.Process.Start(@"D:\wkhtmltopdf\bin\wkhtmltopdf.exe", string.Format("--orientation Landscape  {0} {1}", htmlName, pdfName));

            //若不加这一行，程序就会马上执行下一句而抓不到文件发生意外：System.IO.FileNotFoundException: 找不到文件 ''。 
            p.WaitForExit();
            //把文件读进文件流 
            using (FileStream fs = new FileStream(pdfName, FileMode.Open))
            {
                byte[] file = new byte[fs.Length];
                fs.Read(file, 0, file.Length);
                fs.Close();
                return file;
            }
        }

        public static string Filter(string str)
        {
            str = str.ToLower().Trim();

            if (str.IndexOf("<colgroup>") > 0)
            {
                var s = str.Substring(str.IndexOf("<colgroup>"), str.IndexOf("<tbody>") - str.IndexOf("<colgroup>"));
                str = str.Replace(s, string.Empty);
            }
            return str;
        }

        //public static string StartBrowser(string source, string title)
        //{
        //    var dirName = DateTime.Now.ToString("yyyyMMdd");
        //    if (!Directory.Exists(dirName))
        //    {
        //        Directory.CreateDirectory(dirName);
        //    }
        //    string fliename = title + "_" + DateTime.Now.Ticks.ToString();


        //    //Bitmap m_Bitmap = new Bitmap(400, 600);
        //    //PointF point = new PointF(0, 0);
        //    //TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(Graphics.FromImage(m_Bitmap), "<html><body><div align=\"center\">This is my html, does it work here?</div></body></html>", point, 500);
        //    //m_Bitmap.Save(@"C:\test\Test.bmp");
        //    Bitmap m_Bitmap = new Bitmap(1900, 2000);
        //    PointF point = new PointF(0, 0);
        //    SizeF maxSize = new System.Drawing.SizeF(2000, 2000);
        //    TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(Graphics.FromImage(m_Bitmap),
        //                                          source,
        //                                    point, maxSize);
        //    m_Bitmap.Save(dirName + "\\" + fliename + ".jpg");
        //    //source = Filter(source);
        //    //System.Drawing.Image image = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage("<html><body> " + source + "</body></html>", 2000, 1500);

        //    //image.Save(dirName + "\\" + fliename + ".jpg");
        //    ConvertJpgToPdf(dirName + "\\" + fliename + ".jpg", dirName + "/" + fliename + ".pdf");
        //    return dirName + "/" + fliename + ".pdf";
        //}

        public static void ConvertJpgToPdf(string jpgfile, string pdf)
        {
            var document = new Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);
            using (var stream = new FileStream(pdf, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfWriter.GetInstance(document, stream);
                document.Open();
                using (var imageStream = new FileStream(jpgfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var image = iTextSharp.text.Image.GetInstance(imageStream);
                    if (image.Height > iTextSharp.text.PageSize.A4.Height - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    else if (image.Width > iTextSharp.text.PageSize.A4.Width - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    image.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;
                    document.Add(image);
                }
                document.Close();
            }
        }
    }
}

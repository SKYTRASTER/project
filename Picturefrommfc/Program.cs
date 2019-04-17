using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.IO;
using System.Drawing;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;

namespace Picturefrommfc
{
    class dah
    {
        public bool Flag { get; set; } 
        public int FilesCount { get; set; }

        public void Print()
        {
            Console.Write("sdgdfgdf");
        }
    }
    class Program
    {

        static string connpostgre = "Server=10.0.112.190;Port=5432;User Id=postgres;Password=2222;Database=mfc;";
       // static string connpostgre = "Server=192.168.112.190;Port=5432;User Id=postgres;Password=2222;Database=mfc;";
        static NpgsqlConnection npgSqlConnection = new NpgsqlConnection();
        static string[,] list;
        static string[,] xmllist;
        static string[,] listtoxml;
        //static XDocument xdocsave = new XDocument(new XDeclaration("1.0", "UTF-8", null),new XElement("root",null));

        static XDocument xdocsave;
        static bool flag=false;

        static void Main(string[] args)
        {
            if (File.Exists("savexmlver.xml") == true)
            {
                xdocsave = XDocument.Load("savexmlver.xml");                
            }
            else
            {
                //xdocsave= new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement("root", null));
                //xdocsave.Save("savexmlver.xml");
            }
            
            readerpostgre();
            xmltoarray();
            touchscreen();
            
            // touchscreenscan();


     Console.WriteLine("Конец");
           Console.ReadKey();

        }
        //коннектор к постгре
        static void coonnectpostgre ()
        {
            try
            {

                npgSqlConnection.ConnectionString = connpostgre;
                npgSqlConnection.Open();
                Console.WriteLine("Соединено к базе успешно");
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка соединения \n");
                Console.WriteLine(e);
            }
        }

        //ридер
        static void readerpostgre()
        {
            try
            {
                coonnectpostgre();
                NpgsqlCommand npgsqlCommand = new NpgsqlCommand(@"select case when dd.prefix IS NULL THEN null ELSE dd.prefix END AS ""Префикс дела"",
                                                                  case when dd.num IS NULL THEN null ELSE dd.num END AS ""Номер дела"",
                                                                  case when cz.idf IS NULL THEN null ELSE cz.idf END AS ""Учетная карточка"",
                                                                  CASE WHEN cz.fam || ' ' || cz.nam || ' ' || cz.otch  IS NULL
                                                                  THEN  cz.fam || ' ' || cz.nam || ' ' || cz.otch
                                                                  ELSE cz.fam || ' ' || cz.nam || ' ' || cz.otch  END AS ""ФИО"",
                                                                  case when isp.naz IS NULL THEN null ELSE isp.naz END AS ""Название ТО"",
                                                                  case when dd.id IS NULL THEN null ELSE dd.id END AS ""id - дела"" ,
                                                                  case when dxml.doc IS NULL THEN null ELSE dxml.doc END AS ""text"" 
                                                                      from delo.delo dd,
                                                                        clients.zakf cz,
                                                                       isp.sp_struc isp,
                                                                       delo.delo_doc ddd,
                                                                       docxml.docinf dxml,
                                                                        isp.tn_ids ispds
                                                                   Where dd.id_usl_sp = '1479 '
                                                                   AND dd.cl=0
                                                                   AND cz.idf = dd.idf
                                                                   AND ispds.tn = dd.tn_open
                                                                   AND  isp.ids = dd.ids_planvid
                                                                   --  AND dd.cl=0
                                                                   --and dd.num = 287
                                                                   --AND isp.ids = '1'
                                                                   AND ddd.id_delo = dd.id
                                                                   AND dxml.id=ddd.id_docxml");
                npgsqlCommand.Connection = npgSqlConnection;
                NpgsqlDataReader npgsqlreader = npgsqlCommand.ExecuteReader();
                List<string> listtemp = new List<string>();
                int rows = 0;
                if (npgsqlreader.HasRows)
                {
                    
                    Console.WriteLine("Таблица не пустая");
                    while (npgsqlreader.Read())
                    {
                        for (int i= 0;i<npgsqlreader.FieldCount; i++)
                        {
                            listtemp.AddRange(new string[] { npgsqlreader[i].ToString() });
                        }
                        rows++;
                    }
                }
                //формируем двумерный массив данных из listtemp    

                list = new string[rows, npgsqlreader.FieldCount];
                int row = 0;
                int column = 0;
                foreach (string listik in listtemp)
                {
                    if (column < npgsqlreader.FieldCount)
                    {
                        list[row, column] = listik;
                        column++;
                    }
                    else
                    {
                        row++;
                        column = 0;
                        list[row, column] = listik;
                        column++;
                    }
                }
                npgSqlConnection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void touchscreen()
        {

           try
            {
                for (int i = 0; i<list.GetLength(0);i++)
                {
                    int filescount = 0;
                    dah any = searchinarray(list[i, 0], list[i, 1]);
                    list[i, 6] = xmlparsing(list[i, 6]);
                    string prefix = deletepref(list[i,0]);
                    coonnectpostgre();
                    // NpgsqlCommand npgsqlCommand1 = new NpgsqlCommand("select * from docxml.docfiles where id_docinf in (select id from docxml.docinf where idf = '"+list[i,2]+"')");
                    // select* from docxml.docfiles where id_docinf  in (select id_docxml from delo.delo_doc where idf = 19 and id_delo = 7678  ) limit 5--поиск файла
                    NpgsqlCommand npgsqlCommand1 = new NpgsqlCommand("select* from docxml.docfiles where id_docinf  in (select id_docxml from delo.delo_doc where idf = '" + list[i, 2] + "' and id_delo = '" + list[i, 5] + "'   )");
                    npgsqlCommand1.Connection = npgSqlConnection;
                    NpgsqlDataReader npgsqlreader1 = npgsqlCommand1.ExecuteReader();
                    //string pathdir = (@"\\mfc5\z\Бессмертный полк ЦСР\" + list[i, 4]);
                    string pathdir = (@"C:\Бессмертный полк ЦСР\" + list[i, 4]);
                    string pathFile = (pathdir + @"\№" + prefix + "_" + list[i, 1] + "_" + list[i, 3] + ".txt");
                    DirectoryInfo dirInfo = new DirectoryInfo(pathdir);
                    if (!dirInfo.Exists) { dirInfo.Create(); }
                    if (npgsqlreader1.HasRows&&any.Flag==true)                                                                                         
                    {
                      
                        while (npgsqlreader1.Read())
                        {    
                           File.WriteAllBytes(pathdir + @"\№" + prefix + "_" + list[i, 1]+"_" +list[i,3]+"_"+npgsqlreader1["naz"].ToString(), (byte[])npgsqlreader1["file"]);                           
                           Console.WriteLine("Записана картинка "+ pathdir +prefix+" " + list[i, 1] + "_" + npgsqlreader1["naz"].ToString()+"\n");
                           filescount++;
                        }
                        if (File.Exists(pathFile) == true)
                        {
                            File.Delete(pathFile);
                            Console.WriteLine("Файл удален");
                        }
                        if (list[i,6]!=null&&filescount>0)
                        {
                            File.AppendAllText(pathFile, list[i, 6]);
                            xmlsavestepup(list[i, 0], list[i, 1], i, filescount);

                        }
                        
                        npgSqlConnection.Close();                        

                    }
                    else
                    {
                        Console.Write("Нет файлов, или файл уже загружен\n");                                                                       //Вывод сообщения
                        npgSqlConnection.Close();
                    }
                        
                  }
                   xdocsave.Save("savexmlver.xml");

                // xmlsave();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void touchscreenscan()
        {

            try
            {
                for (int i = 0; i < list.GetLength(0); i++)
                {
                    string prefix = deletepref(list[i, 0]);
                    coonnectpostgre();
                    // NpgsqlCommand npgsqlCommand1 = new NpgsqlCommand("select * from docxml.docfiles where id_docinf in (select id from docxml.docinf where idf = '"+list[i,2]+"')");
                    // select* from docxml.docfiles where id_docinf  in (select id_docxml from delo.delo_doc where idf = 19 and id_delo = 7678  ) limit 5--поиск файла
                    NpgsqlCommand npgsqlCommand1 = new NpgsqlCommand("select * from docxml.docpic where id_docinf  in (select id from docxml.docinf where idf = '" + list[i, 2] + "' and isvalid = 1) ");
                    npgsqlCommand1.Connection = npgSqlConnection;
                    NpgsqlDataReader npgsqlreader1 = npgsqlCommand1.ExecuteReader();
                    string pathdir = (@"c:\Бессмертный полк\" + list[i, 4]);
                    if (npgsqlreader1.HasRows)
                    {

                        while (npgsqlreader1.Read())
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(pathdir);
                            if (!dirInfo.Exists)
                            {
                                dirInfo.Create();

                            }
                            File.WriteAllBytes(pathdir + @"\№дела_" + prefix + "_" + list[i, 1] + "_" + npgsqlreader1["primech"].ToString()+".jpg", (byte[])npgsqlreader1["pic"]);
                            Console.WriteLine("Записано в файл " + pathdir + @"\" + prefix + " " + list[i, 1] + "_" + npgsqlreader1["primech"].ToString() + "\n");

                        }

                        npgSqlConnection.Close();
                    }
                    else
                    {
                        Console.Write("Нет файлов\n");                                                                     
                        npgSqlConnection.Close();
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        static private string deletepref(string prefix)
        {

            prefix = prefix.Replace("/","-");          
            return prefix;
        }


        static private string xmlparsing(string text)
        {
            bool istext = false;
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(text);
            XmlNode root = xDoc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("/");            
            foreach (XmlNode node in nodes)
            {
                if (node.ChildNodes != null)
                {
                    foreach (XmlNode childnodes in node.ChildNodes)
                    {
                        if (childnodes.Name =="документ")
                        {
                            foreach (XmlNode childnodes1 in childnodes.ChildNodes)
                            {
                                if (childnodes1.Name == "примечание")
                                {text = childnodes1.InnerText;
                                    istext = true;
                                }     
                                               

                            }

                        }
                    }

                }
               
            }
            if (istext ==false)
            {
                text = null;
            }

            return text;
        }

        //Формируем xml 
        static void xmlsave ()
        {

          
            XDocument xdocsave = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("root", Enumerable.Range(0, list.GetLength(0)).Select(i=>new XElement("CaseNumber", new XElement("prefix", list[i, 0]),new XElement("num", list[i, 1])))));

            //сохраняем документ
            xdocsave.Save("savexml.xml");
            Console.WriteLine("Сохранили xml");
        }

        //создаем массив из  XML
        static void xmltoarray()
        {
            try {
                if (File.Exists("savexmlver.xml") == true)
                    {
                    int i = 0;
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load("savexmlver.xml");
                    XmlNode root = xdoc.DocumentElement;
                    XmlNodeList nodes = root.SelectNodes("/root");

                    foreach (XmlNode node in nodes)
                    {
                        if (node.HasChildNodes == true)
                        {
                            xmllist = new string[node.ChildNodes.Count, 3];
                            
                            foreach (XmlNode casenode in node.ChildNodes)
                            {
                                
                                foreach (XmlNode node2 in casenode.ChildNodes)
                                {
                                    if (node2.Name == "prefix")
                                    {
                                        xmllist[i, 0] = node2.InnerText;
                                    }
                                    if (node2.Name == "num")
                                    {
                                        xmllist[i, 1] = node2.InnerText;
                                    }
                                    if (node2.Name == "filescount")
                                    {
                                        xmllist[i, 2] = node2.InnerText;
                                    }

                                }
                                i++;
                            }
                           
                        }
                        else
                        {
                            Console.WriteLine("Xml-файл пуст, flag делаем true");
                            flag = true;
                        }
                    }
                    
                
                }
                else
                {
                    Console.WriteLine("Xml-файл нет, flag делаем true, создадим xml");
                    flag = true;
                    xdocsave = new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement("root", null));
                    xdocsave.Save("savexmlver.xml");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n");
            }
           


        }

        static private dah searchinarray(string prefix, string num)
        {
            int filescount = 0;

            if (xmllist != null)
            {
                int count = 0;
                for (int i = 0; i < xmllist.GetLength(0); i++) //прогоняем массив с БД
                {
                    if (prefix == xmllist[i, 0] && num == xmllist[i, 1])// ищем номер в массиве
                    {
                        count++;
                        filescount = Convert.ToInt32(xmllist[i, 2]);
                    }

                }
                if (count > 0)
                {
                    flag = false;
                }
                else
                {
                    flag = true;

                }
            }
            else
            {
                Console.WriteLine("Xml-массив пуст, flag делаем true");
                flag = true;
            }

            return new dah { Flag = flag, FilesCount = filescount };
        }

        static void xmlsavestepup(string prefix, string num , int i, int filescount)
        {

            //XDocument xdocsave = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            //new XElement("root", new XElement("CaseNumber", new XElement("prefix", list[i, 0]), new XElement("num", list[i, 1]))));

            XElement el  = new XElement("CaseNumber", new XElement("prefix", list[i, 0]), new XElement("num", list[i, 1]), new XElement("filescount", filescount));
            // xdocsave.Element() =  new XElement("CaseNumber", new XElement("prefix", list[i, 0]), new XElement("num", list[i, 1]));
            //сохраняем документ
            xdocsave.Element("root").Add(el);
            Console.WriteLine("Сохранили virtxml");
        }




    }


}

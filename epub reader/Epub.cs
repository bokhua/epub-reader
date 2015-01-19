using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace epub_reader
{
    public class Epub
    {

        public string toc, tocPath,file, extract, contentPath, title;
        public List<string> links, descrip;
        public List<List<String>> nav;
       
        public string[] tocClean;
        public Epub(string file)
        {
            this.file = file;
        }

        public void load()
        {
            extract = Path.GetTempPath() + "epubreader/"+Guid.NewGuid();
            extract = Regex.Replace(extract, @"\\", "/");
            Directory.CreateDirectory(extract);
            ZipFile.ExtractToDirectory(file, extract);
            
            XDocument doc;
            XElement root;
            XNamespace ns;

            //get container information
            doc = XDocument.Load(extract+@"/META-INF/container.xml");
            root = doc.Root;
            ns = root.GetDefaultNamespace();
            contentPath = extract+"/"+root.Descendants(ns + "rootfile").Attributes("full-path").First().Value;

            Console.WriteLine("######opf path: "+contentPath);
            
            //get toc
            doc = XDocument.Load(contentPath);
            root = doc.Root;
            ns = root.GetDefaultNamespace();

            IEnumerable<string> el =
                from e in root.Descendants(ns + "item")
                where e.Attribute("id").Value == "ncx"
                select e.Attribute("href").Value;

            tocPath = contentPath.Remove(contentPath.LastIndexOf("/") + 1) + el.First();
            Console.WriteLine("######toc.ncx path: " + tocPath);
            tocClean = clean(File.ReadAllText(tocPath)).Split(';');
            //if (tocClean != null)
            buildToc();
        }

        public void close()
        {
            if (Directory.Exists(extract))
                Directory.Delete(extract, true);
        }

        private string clean(string s) 
        {
            s = Regex.Replace(s, @"<[^>]+>|$nbsp;", "").Trim();
            s = Regex.Replace(s, @"\s{2,}", ";");
            return s;
        }

        private void buildToc() 
        {
            XDocument doc = XDocument.Load(tocPath);
            XElement root = doc.Root;
            XNamespace ns = root.Attribute("xmlns").Value;
            IEnumerable<XElement> el;
            //get epub title
            title = root.Element(ns+"docTitle").Value;
            nav = new List<List<string>>();

            //get links
            links = new List<string>();
            el = root.Descendants(ns + "content");
            foreach (XElement e in el)
            {
                links.Add(tocPath.Remove(tocPath.LastIndexOf("/") + 1) + e.Attribute("src").Value);
                Console.WriteLine(">>>>>link added:  " + tocPath.Remove(tocPath.LastIndexOf("/") + 1) + e.Attribute("src").Value);
            }
            //get link descriptions
            descrip = new List<string>();
            el = root.Element(ns + "navMap").Descendants(ns + "text");
            foreach (XElement e in el)
                descrip.Add(e.Value);

            //get navpoints
            el = root.Element(ns+"navMap").Elements(ns+"navPoint");
            Console.WriteLine("navsize: "+el.Count());
            foreach (XElement e in el) 
            {
                IEnumerable<XElement> sub = e.Descendants(ns+"navLabel");
                
                List<string> temp = new List<string>();
                foreach (XElement point in sub)
                {
                    temp.Add(point.Value);
                    Console.WriteLine("navLabel:  "+point.Value);
                }
                nav.Add(temp);        
            }
            //get links

            string path = extract + @"\toc.html";
            if (!File.Exists(path))
            {
               File.Create(path).Dispose();
               StreamWriter writer = new StreamWriter(path);

               string br = Environment.NewLine;
               writer.Write("<!DOCTYPE html>{0}<html>{0}<head>{0}<style></style>{0}</head>{0}<body>", br);
               
               writer.WriteLine("<p style='color:green;'>{0}</p><dl>", title);
               int num = 0;
               foreach (List<string> l in nav)
               {
                   int size = l.Count;
                   for (int i = 0; i < size; i++)
                       if (l[i].Length > 35)
                           l[i] = l[i].Substring(0, 35) + " ...";
                   
                   writer.WriteLine("<dt><a href={0} title='{1}'>{2}</a></dt>", links[num],descrip[num], l[0]);
                   num++;
                  
                   if (size > 1)
                       for (int i = 1; i < l.Count(); i++)
                       {
                           writer.WriteLine("<dd><a href={0} title='{1}'>{2}</a></dd>", links[num], descrip[num],l[i]);
                           num++;
                       }
               }

               writer.WriteLine("<dl/>{0}</body>{0}</html>",br);
               writer.Close();
            }
            /*
            toc += String.Format(@"<h style='color:green'>{0}</h>",tocClean[0]);
            for (int i = 1; i < tocClean.Length; i++)
            {
                toc += String.Format(@"<p>{0}</p>",tocClean[i]);
            }
             * */
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace epub_reader
{
    public partial class MainForm: Form
    {
        private Epub epub;
        private Uri tocUrl;
        public MainForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
          //  webBrowser.DocumentCompleted +=
            //            new WebBrowserDocumentCompletedEventHandler((send, eve) => enableContextMenu(send, eve));
        }

        public void enableContextMenu(object sender, EventArgs e) 
        {
           // rightClickMenu.Visible = true;
        }

        private void OpenClick(object sender, EventArgs e)
        {
            OpenFileDialog openEpub = new OpenFileDialog();

            openEpub.Filter = "ePub file (.epub)|*.epub";
            openEpub.FilterIndex = 1;
            openEpub.Multiselect = false;
            if (openEpub.ShowDialog() == DialogResult.OK)
            {
                if (epub != null)
                {
                    epub.close();
                    epub = null;
                }
      
                epub = new Epub(openEpub.FileName);
                epub.load();
                if (epub.links != null)
                {
                    this.Text = epub.title + " - ePub Reader";
                    tocUrl = new Uri(String.Format("file:///{0}/toc.html", epub.extract));
                    webBrowser.Url = new Uri(String.Format("file:///{0}", epub.links[0]));
                    tocBrowser.Navigate(tocUrl);
            
                }
            }  
        }

        private void CloseClick(object sender, EventArgs e)
        {
            epub.close();
            epub = null;
            tocBrowser.Visible = false;
            webBrowser.Navigate("about:blank");
        }

        private void AppClosing(object sender, FormClosingEventArgs e)
        {
            if (epub != null)
                epub.close();
        }



        private void SearchClick(object sender, EventArgs e)
        {
            webBrowser.Focus();
            SendKeys.Send("^f");
        }
        private int[] positionOnHTML(HtmlElement e) 
        {
            int x = e.OffsetRectangle.Left, y = e.OffsetRectangle.Top;
            HtmlElement temp = e.OffsetParent;
            while (temp != null) 
            {
                x += temp.OffsetRectangle.Left;
                y += temp.OffsetRectangle.Top;
                temp = temp.OffsetParent;
            }
            Console.WriteLine("position: x={0}, y={1}", x,y);
            return new int[]{x,y};
        }
        private void scrollToID(object sender, WebBrowserDocumentCompletedEventArgs e, string id) 
        {
            if (id != null)
            {
                HtmlElement elem = webBrowser.Document.GetElementById(id);
                if (elem != null)
                {
                    int[] position = positionOnHTML(elem);

                    webBrowser.Document.Window.ScrollTo(position[0], position[1]);
                }
            }
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webBrowser.ContextMenuStrip = webContextMenuStrip;
        }

        private void render(object sender, EventArgs e)
        {

        }

        private void fullToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }

        private void contentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tocBrowser.Visible == false && epub!=null)
            {
                tocBrowser.Visible = true;

                tocBrowser.Url = new Uri(String.Format("{0}/toc.html", epub.extract));
      
             }
            else
                tocBrowser.Visible = false;
        }



        private void tocBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }



        private void tocLink_Click(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url != tocUrl && e.Url!=new Uri("about:blank"))
            {
                e.Cancel = true;
                string s = e.Url.ToString();
                Console.WriteLine("link clicked: "+s);
                if (s.Contains("%23"))
                {
                    string[] link = s.Split(new string[]{"%23"}, StringSplitOptions.None);
                    webBrowser.Navigate(link[0]);
                    webBrowser.DocumentCompleted +=
                        new WebBrowserDocumentCompletedEventHandler((send, eve) => scrollToID(send, eve, link[1]));
                }
                else
                    webBrowser.Url = e.Url;
            }
        }

        private void webLink_Click(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.ToString().Contains("http://"))
            {
                e.Cancel = true;
                var confirmResult = MessageBox.Show("Is this a trusted link??", "Confirm",MessageBoxButtons.YesNo);
                if(confirmResult == DialogResult.Yes)
                    System.Diagnostics.Process.Start(e.Url.ToString());
            }
        }

        private void web_KeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            List<Keys> keys = new List<Keys>(){Keys.Up, Keys.Down, Keys.Left, Keys.Right, Keys.PageDown, Keys.PageUp};

            if (keys.Contains(e.KeyCode))
                e.IsInputKey = true;
            
            if (webBrowser.Document!=null && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
            {
                Console.WriteLine("current link: "+webBrowser.Url.ToString());
                changeChapter(e.KeyCode);
            }
        }

        private void changeChapter(Keys key)
        {
            if (key == Keys.Left) 
            {
                int index = epub.links.IndexOf(Regex.Replace(webBrowser.Url.ToString(), "file:///", ""));
                Console.WriteLine("links: "+index);
                if (index>0)                 
                    webBrowser.Navigate(epub.links[index-1]);           
            }
            else if (key == Keys.Right)
            {
                int index = epub.links.IndexOf(Regex.Replace(webBrowser.Url.ToString(), "file:///", ""));
                if (index >= 0 && index<epub.links.Count()-1)
                    webBrowser.Navigate(epub.links[index+1]);
            }
        }

        private void lookToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            webBrowser.ContextMenuStrip = webContextMenuStrip; 
        }

    }

   
}

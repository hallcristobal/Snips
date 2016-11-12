using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Xml;
using System.IO;

namespace Snippets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, string> snips;

        public MainWindow()
        {
            snips = new Dictionary<string, string>();
            InitializeComponent();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastFile) && File.Exists(Properties.Settings.Default.LastFile))
                Open_FileOk(new OpenFileDialog() { FileName = Properties.Settings.Default.LastFile }, null);
        }

        private void PopulateButtons()
        {
            foreach (var item in snips)
                StackMain.Children.Add(CreateButton(item.Key, item.Value));
        }

        private Button CreateButton(string name, string full)
        {
            var b = new Button();
            b.Name = name.Replace(" ", "");
            b.Content = name;
            b.Style = (Style)this.Resources["buttonStyle"];
            b.MouseUp += B_MouseUp;
            b.Click += B_Click;
            b.ToolTip = full;
            return b;
        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(snips[(sender as Button).Content.ToString()]);
        }

        private void B_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.RightButton == MouseButtonState.Released)
            {
                ContextMenu cm = this.Resources["cmSnippet"] as ContextMenu;
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
            }
                
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var n = new newSnippet();
            var res = n.ShowDialog();
            if (res != null && res == true)
            {
                var ind = 1;
                var name = n.tbShortName.Text;
                var d_name = name;
                var full = n.tbFullText.Text;

                while(snips.ContainsKey(name))
                {
                    name = d_name;
                    name += ind;
                    ind++;
                }

                snips.Add(name, full);
                StackMain.Children.Add(CreateButton(name, full));
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            MenuItem i = sender as MenuItem;
            Button b = (i.Parent as ContextMenu).PlacementTarget as Button;
            snips.Remove(b.Content.ToString());
            StackMain.Children.Remove(b);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            MenuItem i = sender as MenuItem;
            Button b = (i.Parent as ContextMenu).PlacementTarget as Button;

            var n = new newSnippet();

            var d_full = snips[b.Content.ToString()];
            var d_short = b.Content.ToString();

            n.tbFullText.Text = d_full;
            n.tbShortName.Text = d_short;


            var res = n.ShowDialog();
            if (res != null && res == true)
            {
                var name = n.tbShortName.Text;
                var full = n.tbFullText.Text;
                if(n.tbShortName.Text != d_short)
                {
                    snips.Remove(d_short);
                    snips.Add(n.tbShortName.Text, n.tbFullText.Text);
                    b.Content = n.tbShortName.Text;
                    b.ToolTip = n.tbFullText.Text;
                }
                else if (n.tbShortName.Text == d_short)
                {
                    snips[d_short] = n.tbFullText.Text;
                    b.ToolTip = n.tbFullText.Text;
                }

            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var save = new SaveFileDialog();
            save.Filter = "XML|*.xml|All Files|*.*";
            save.FilterIndex = 0;
            save.FileOk += Save_FileOk;
            save.ShowDialog();
        }

        private void Save_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var save = sender as SaveFileDialog;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.IndentChars = "\t";
            settings.Indent = true;
            settings.WriteEndDocumentOnClose = true;
            using (var fs = new FileStream(save.FileName, FileMode.Create))
            using (var xml = XmlWriter.Create(fs, settings))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("Snippets");
                xml.WriteAttributeString("File", save.SafeFileName);
                foreach (var item in snips)
                {
                    xml.WriteStartElement("Snippet");
                    xml.WriteElementString("Name", item.Key);
                    xml.WriteElementString("Value", item.Value);
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            Properties.Settings.Default.LastFile = save.FileName;
            Properties.Settings.Default.Save();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "XML|*.xml|All Files|*.*";
            open.FilterIndex = 0;
            open.FileOk += Open_FileOk;
            open.Multiselect = false;
            open.ShowDialog();
        }

        private void Open_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var open = sender as OpenFileDialog;
            snips.Clear();

            if(StackMain.Children.Count > 1)
                StackMain.Children.RemoveRange(1, StackMain.Children.Count - 1);

            using (var fs = new FileStream(open.FileName, FileMode.Open))
            using (var xml = XmlReader.Create(fs))
            {
                string n = string.Empty;
                string f = string.Empty;
                while (xml.Read())
                {
                    if (xml.IsStartElement())
                    {
                        switch(xml.Name)
                        {
                            case "Name":
                                n = xml.ReadElementContentAsString();
                                break;
                            case "Value":
                                f = xml.ReadElementContentAsString();
                                break;
                        }
                        if(!string.IsNullOrEmpty(n) && !string.IsNullOrEmpty(f))
                        {
                            snips.Add(n, f);
                            n = string.Empty;
                            f = string.Empty;
                        }
                    }
                }
            }
            PopulateButtons();
            Properties.Settings.Default.LastFile = open.FileName;
            Properties.Settings.Default.Save();
        }
    }
}

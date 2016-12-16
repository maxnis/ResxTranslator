using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResxTranslate
{
    public partial class Form1 : Form
    {
        private Dictionary<String, String> _dictionary = new Dictionary<string, string>();
        private Dictionary<String, String> _srcDictionary = new Dictionary<string, string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void loadMainDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "Tab separated files|*.tsv|Csv files|*.csv|All files|*.*";
                var dlgres = openFileDialog1.ShowDialog();
                if (dlgres == System.Windows.Forms.DialogResult.OK)
                {
                    toolStripStatusLabel1.Text = openFileDialog1.FileName;
                    var loadtask = loadDictionary(openFileDialog1.FileName);
                    loadtask.ContinueWith(x => {
                        toolStripStatusLabel1.Text += string.Format(": {0}", x.Result.ToString());
                    });
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel4.Text = ex.Message;
            }
        }

        private async Task<string> loadDictionary(string fileName)
        {
            // load dictionary file
            using (var sr = new StreamReader(fileName))
            {
                while (!sr.EndOfStream)
                {
                    string line = await sr.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] args = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (args != null && args.Length > 1)
                        {
                            if (this._dictionary.Count == 0) // headers
                            {
                                textBox1.AppendText(String.Format("{0} -> {1}{2}", args[0], args[1], Environment.NewLine));
                            }
                            if (!this._dictionary.ContainsKey(args[0]))
                            { // ignore duplicates
                                this._dictionary.Add(args[0], args[1]);
                            }
                        }
                    }
                }
            }

            return this._dictionary.Count.ToString();
        }

        private void loadResourceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "Resource files|*.resx|Csv files|*.csv|All files|*.*";
                var dlgres = openFileDialog1.ShowDialog();
                if (dlgres == System.Windows.Forms.DialogResult.OK)
                {
                    toolStripStatusLabel2.Text = openFileDialog1.FileName;
                    // load en resource file
                    using (ResXResourceReader resxReader = new ResXResourceReader(openFileDialog1.FileName))
                    {
                        foreach (DictionaryEntry entry in resxReader)
                        {
                            this._srcDictionary.Add(entry.Key.ToString(), entry.Value.ToString());
                            textBox1.AppendText(String.Format("{0} -> {1}{2}", entry.Key, entry.Value, Environment.NewLine)); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel4.Text = ex.Message;
            }
        }

        private void translateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Path.Combine(Path.GetDirectoryName(openFileDialog1.FileName), 
                    String.Format("{0}.es{1}", Path.GetFileNameWithoutExtension(openFileDialog1.FileName), Path.GetExtension(openFileDialog1.FileName)));
                using (ResXResourceWriter resxWriter = new ResXResourceWriter(fileName))
                {
                    var q = from line in this._srcDictionary.AsQueryable()
                            select line;

                    foreach (var entry in q.ToList())
                    {
                        if (this._dictionary.ContainsKey(entry.Value.ToString()))
                        {
                            var translation = this._dictionary.FirstOrDefault(x => x.Key.ToString().Equals(entry.Value.ToString()));
                            resxWriter.AddResource(entry.Key.ToString(), translation.Value.ToString());
                        }
                        else // translation not found
                        {
                            var node = new ResXDataNode(entry.Key.ToString(), entry.Value.ToString());
                            node.Comment = "to be translated";
                            resxWriter.AddResource(node);
                        }
                    }
                }
                toolStripStatusLabel4.Text = "Done";
            }
            catch (Exception ex)
            {
                toolStripStatusLabel4.Text = ex.Message;
            }
        }
    }
}
